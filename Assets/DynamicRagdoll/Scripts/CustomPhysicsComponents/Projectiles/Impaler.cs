using System.Collections.Generic;
using UnityEngine;
namespace DynamicRagdoll {


    /*
        object that will impale rigidbodies (like a skewer)
        in place, it "stretches" out from the origin, like a tentacle

        shoots a ray in a direction:

        if we've hit a static object at the end:

            S (SPEAR)->     A       B                   C (Static)

            and add grab points (joints) to the rigidbodies between us and the end point (A and B)

            then we move the spear towards the end point (C)

            as the spear (S) reaches each point along it's travel,
            we add some movement to that point, to simulate the force of impalement

            once the spear reaches it's end point we turn on it's physical components 

            can be kinematic or actual physical object depending on options
            optionally just a line renderer without any physics whatsoever
            
    */

    public class Impaler : PhysicsProjectile<Impaler>
    {

        System.Action<PhysicsHit> onReachHitObject;

        protected override void UpdateProjectile(float deltaTime) {    
        
            if (reachedDestination)
                return;

            UpdateTravelZ(deltaTime, 0);
            
            for (int i = physicsHits.Count - 1; i >= 0; i--) {

                PhysicsHit hit = physicsHits[i];

                // if we've reached the hitgroup average position
                if (travelZ >= hit.hitDistance) {

                    // make sure this only happens for a frame
                    if (!reachedIndicies.Contains(i)) {
        
                        if (onReachHitObject != null) {
                            onReachHitObject(hit);
                        }
                        // foreach rigidbody in the hit group
                        // simulate as if the impaler passed through it
                        // add force to the rigidbodies
                        for (int x = 0; x < hit.hitElements.Count; x++ ) {
                            PhysicsHitElement hitElement = hit.hitElements[x];
                            hitElement.rigidbody.velocity = impaleDirection * velocity * hitObjectVelocityMultiplier;
                            // hitElement.rigidbody.AddForceAtPosition( impaleDirection * velocity * hitObjectVelocityMultiplier, hitElement.hitPoint, ForceMode.VelocityChange);
                        }   

                        reachedIndicies.Add(i);
                    }
                }            
            }

            if (reachedDestination) {
                SetKinematic(kinematicOnReach);
                EnableCollider(!kinematicOnReach || useColliderOnReach);
            }
        }


        void AdjustCapsulCollider (float distance, float radius) {
            CapsuleCollider capsule = myCollider as CapsuleCollider;
            if (capsule != null) {
                capsule.direction = 2;
                capsule.center = new Vector3(0,0,distance*.5f);
                capsule.height = distance;
                capsule.radius = radius;
            }
        }

        protected override void DisableProjectile () {
            // clear all physics joints we used for impalement
            RagdollPhysics.DetachGrabbedBodies(grabbedBodies);
        }

        [HideInInspector] public float impalerRadius;
        bool kinematicOnReach, useColliderOnReach;
        
        List<int> reachedIndicies = new List<int>();
        List<GrabbedBody> grabbedBodies = new List<GrabbedBody>();
        List<PhysicsHit> physicsHits = new List<PhysicsHit>();
        
        
        
        // to be able to move it, calculate these in terms of our transform
        public Vector3 impaleDirection { get { return transform.forward; } }
        public Vector3 impalerOrigin { get { return transform.position; } }
        // public Vector3 imapleEndPoint { get { return impalerOrigin + impaleDirection * endPointDistance; } }
        public Vector3 currentImpalerEndPoint { get { return impalerOrigin + impaleDirection * travelZ; } }
        
        public List<PhysicsHit> ShootImpaler (float velocity, float hitObjectVelocityMultiplier, float zMovementLimit, Ray ray, float maxDistance, LayerMask mask, float impalerRadius, bool kinematicOnReach, bool useColliderOnReach, float lifeTime, System.Action<PhysicsHit> onReachHitObject)
        {

            this.onReachHitObject = onReachHitObject;
            this.kinematicOnReach = kinematicOnReach;
            this.useColliderOnReach = useColliderOnReach;
            this.impalerRadius = impalerRadius;
            
            InitializeLaunch(ray, velocity, lifeTime, hitObjectVelocityMultiplier);
            
            reachedIndicies.Clear();

            //grab any rigidbodies along a ray, until a static collider
            bool hitStatic;
            RagdollPhysics.ShootMultiGrabRay ( ray, impalerRadius, mask, maxDistance, grabbedBodies, physicsHits, out endPointDistance, out hitStatic);

            //for each grabbed rigidbody we've impaled,
            // attach it to our impaler rigidbody via a joint
            for (int i =0; i < grabbedBodies.Count; i++) {
                    
                // need to disable kinematic here to avoid jittery physics
                // grabbedGroups[i].grabPoint.baseRigidbody.isKinematic = false;
                RagdollPhysics.GrabRigidbody(grabbedBodies[i].grabPoint.baseRigidbody, rb, false);
                
                // allow joint movement along teh z axis,
                // so rigidbodies can slide along the impaler (helps stability)
                ConfigurableJoint joint = grabbedBodies[i].grabPoint.baseJoint;
                joint.zMotion = ConfigurableJointMotion.Limited;
                var l = joint.linearLimit;
                l.limit = zMovementLimit;
                joint.linearLimit = l;
            }

            StartCollisionIgnoreWithMyCollider(physicsHits);

            AdjustCapsulCollider(endPointDistance, impalerRadius);   

            return physicsHits;
        }
        
    }
}