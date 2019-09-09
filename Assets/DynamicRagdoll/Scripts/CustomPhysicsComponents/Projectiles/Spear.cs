using System.Collections.Generic;
using UnityEngine;
namespace DynamicRagdoll {

    /*
        object that will impale rigidbodies (like a skewer)
        against any static background it detects

        shoots a ray in a direction:

        if we've hit a static object at the end:

            S (SPEAR)->     A       B                   C (Static)

            and add grab points (joints) to the rigidbodies between us and the end point (A and B)

            then we move the spear towards the end point (C)

            as the spear (S) reaches each point along it's travel, 
            we start moving that point along with the spear

            each point goes towards a target position, to make it look like it's "pressed up" against the 
            static end point by the spear

            S (SPEAR)->                         -A--B--C (Static)

            once the spear reaches it's end point we turn on it's physical components 
            (rigidbody is kinematic so it sticks into the wall though)
        
        if we havent hit a static object but hit some rigidbodes:

            S (SPEAR)->     A       B                   

            and add grab points (joints) and move the spear along grabbing each object like before

            once teh spear reaches a point beyond the last grabbed rigidbody, we turn on the physics
            components of the spear, and set it's velocity to that of the orignal spear speed
            with a small multiplier to simulate the spear getting snagged on the rigidbodies
            it's impaled
            
            -A--B- S (SPEAR)->

        if we only hit a static collider:

            move towards that static point and stick into the wall

        if we didnt hit anything:
            
            just throw the spear using physics velocity
    */

    public class Spear : PhysicsProjectile<Spear>
    {
        const float determineSizeTrySize = .25f;
        const float maxTrySize = 5;

        static float MaxV3 (Vector3 v) {
            return Mathf.Max(Mathf.Max(v.x, v.y), v.z);
        }
        static float DetermineBoxSize (BoxCollider box) {
            return MaxV3(Vector3.Scale(box.size, box.transform.lossyScale));
        }
        static float DetermindSphereSize (SphereCollider sphere) {
            return sphere.radius * 2 * sphere.transform.lossyScale.x;
        }
        static float DetermineCapsuleSize(CapsuleCollider capsule) {
            return Mathf.Max(capsule.height, capsule.radius) * capsule.transform.lossyScale.x;   
        }
        
        // determing the full size of a series of hit colliders
        static float DetermineHitPointsSize (PhysicsHit hitGroup, Vector3 direction, LayerMask mask) {
            int hitGroupHitsCount = hitGroup.hitElements.Count;
            if (!hitGroup.isRagdoll || hitGroupHitsCount == 1) {
                PhysicsHitElement h = hitGroup.hitElements[0];

                if (h.box != null) return DetermineBoxSize(h.box);
                if (h.sphere != null) return DetermindSphereSize(h.sphere);
                if (h.capsule != null) return DetermineCapsuleSize(h.capsule);
            }

            // determine size for multiple grabbed ragdolls / mesh colliders

            // start ray from the last hit (furthest from origin)
            Vector3 startRayPoint = hitGroup.hitElements[hitGroupHitsCount - 1].hitPoint;
            Rigidbody furthestHitRB = hitGroup.hitElements[hitGroupHitsCount - 1].rigidbody;

            // keep trying until we've searched too far        
            float trySize = determineSizeTrySize;
            while (trySize < maxTrySize) {

                RaycastHit hit;

                // go forward and then raycast back towards our last hit point, to find the "other" side of the collider
                if (Physics.Raycast(new Ray(startRayPoint + direction * trySize, -direction), out hit, determineSizeTrySize, mask, QueryTriggerInteraction.Ignore)) {
                    
                    // if it's our collider we hit, then we found the other side
                    if (hit.rigidbody == furthestHitRB) {
                        Vector3 lastCollidersOtherSide = hit.point;
                        
                        // distance start calculated from the first hit point (nearest to ray origin)
                        return Vector3.Distance(hitGroup.hitElements[0].hitPoint, lastCollidersOtherSide);
                    }
                }
                // didnt find end
                trySize += determineSizeTrySize; 
            }

            // Debug.LogError("Couldnt find end " + hitGroup.hitElements[0].rigidbody.name);
            return 1;
        }

        /*
            our target position at the end of impalement
            // calculate backwards
        */
        static Vector3 CalculateSkewerTargetPosition (Vector3 endPoint, Vector3 rayDir, float impaleSeperation, float skeweredObjectSize, ref float lastEndPoint, out float skewerOffset) {
            skewerOffset = lastEndPoint + skeweredObjectSize * .5f;
            lastEndPoint = lastEndPoint + skeweredObjectSize + impaleSeperation;
            return endPoint - rayDir * skewerOffset;
        }

        void OnCollisionEnter(Collision other){
            collidedDuringPhysicsFly = true;
        }
        
        void UpdateArcSimulation () {
            // keep our forward facing our velocity, to simulate an arc
            // if we're not already collided, using physics, and not skewering
            if (!collidedDuringPhysicsFly) {
                if (!rb.isKinematic && skeweredCount == 0) {
                    RagdollPhysics.SimulateArc(rb);
                }
            }
        }

        System.Action<PhysicsHit, bool> onReachHitObject;

        protected override void UpdateProjectile(float deltaTime) {    
        
            UpdateArcSimulation();
            
            if (reachedDestination)
                return;

            UpdateTravelZ(deltaTime, randomOffset);

            /*
                update backwards for stability, so we set our furthest rigidbody first
                this way A doesnt bump into B accidentally
                -Spear-->>>>  A       B 
            */
            for (int i = physicsHits.Count - 1; i >= 0; i--) {

                bool isSkewered = i >= startIndexForSkewered;

                PhysicsHit hit = physicsHits[i];

                // out offset with regard to all the other impaled entities (local position on spear)
                // 0 if we're not skewering
                float skewerOffset = isSkewered ? skewerOffsets[i] : 0;
                
                // where out imalement "slot" is on the spear thats travelling
                float z = travelZ - skewerOffset;
                
                // our target z (where we'll wind up)
                float end = endPointDistance - skewerOffset;
                
                // stay at 0 until the "spear" reaches our destination
                float t = Mathf.Clamp01((z - hit.hitDistance) / ( end - hit.hitDistance));
                
                // if we've reached the hitgroup average position
                if (t > 0) {

                    // make sure this only happens for a frame
                    if (!reachedIndicies.Contains(i)) {

                        if (onReachHitObject != null) {
                            onReachHitObject(hit, isSkewered);
                        }

                        // if we're not skewering this hit group
                        // simulate as if the spear passed through it
                        if (!isSkewered) {
                            
                            // ungrab the group (disables created joints)
                            grabbedBodies[i].Detach();

                            // foreach rigidbody in the hit group
                            for (int x = 0; x < hit.hitElements.Count; x++ ) {
                                    
                                PhysicsHitElement physicsHitElement = hit.hitElements[x];
                    
                                // add force to the rigidbodies
                                // physicsHitElement.rigidbody.velocity = launchRay.direction * velocity;
                                physicsHitElement.rigidbody.AddForceAtPosition( launchRay.direction * velocity, physicsHitElement.hitPoint, ForceMode.VelocityChange);
                                
                                // if its a ragdoll, attempt to dismember the bone
                                if (physicsHitElement.ragdoll != null) {
                                    physicsHitElement.ragdoll.DismemberBone("Spear", physicsHitElement.ragdollBone.bone);
                                }
                            }   
                        }
                        reachedIndicies.Add(i);
                    }
                }
                
                if (isSkewered) {
                    /* 
                        update each attachment point so that it starts moving when
                        the point on the spear it's going to wind up impaled on reaches the point itself

                        set the target position as the spot it's going to wind up at the end of the 
                        movement
                    */
                    grabbedBodies[i].grabPoint.baseRigidbody.MovePosition(Vector3.Lerp( hit.averagePosition, skewerTargets[i], t));
                        
                    // if we're pinned up against a wall, allow joint movement along teh z axis,
                    // so rigidbodies can slide along the spear (helps stability)
                    if (reachedDestination && stickToEnd) {

                        ConfigurableJoint j = grabbedBodies[i].grabPoint.baseJoint;
                        
                        j.zMotion = ConfigurableJointMotion.Limited;
                        var l = j.linearLimit;
                        l.limit = spearLength - skewerOffset;
                        j.linearLimit = l;
                    }
                }
            }
                
            // move model
            transform.position = Vector3.Lerp( launchRay.origin, modelEndTarget, Mathf.Clamp01(travelZ / endPointDistance));

            if (reachedDestination) {
                ReachDestination(skeweredCount > 0 ? hitObjectVelocityMultiplier : 1);
            }
        }

        void ReachDestination (float velocityMultiplier) {
            reachedDestination = true;
            
            EnableCollider(true);
            
            // if we're not sticking into a wall
            if (!stickToEnd) {
                
                // turn on the rigidbody
                rb.isKinematic = false;

                //for each grabbed rigidbody we've impaled,
                for (int i = startIndexForSkewered; i < grabbedBodies.Count; i++) {
                    
                    Rigidbody grabPointRB = grabbedBodies[i].grabPoint.baseRigidbody;

                    // need to disable kinematic here to avoid jittery physics
                    grabPointRB.isKinematic = false;

                    // set all associated rigidbody velocities to zero, or else the leftover momentum
                    // builds up and keeps us shooting forward beyond our control...
                    grabPointRB.velocity = Vector3.zero;
                    grabbedBodies[i].grabPoint.childRigidbody.velocity = Vector3.zero;

                    if (physicsHits[i].isRagdoll) {
                        physicsHits[i].ragdoll.SetVelocity(Vector3.zero);
                    }
                    else {
                        physicsHits[i].mainRigidbody.velocity = Vector3.zero;
                    }
                    
                    // attach it to our spear rigidbody via a joint
                    RagdollPhysics.GrabRigidbody(grabPointRB, rb, false);
                }

                // set our velocity to the spear speed
                rb.velocity = launchRay.direction * velocity * velocityMultiplier;
            }
        }

        protected override void DisableProjectile () {
            // clear all physics joints we used for impalement
            RagdollPhysics.DetachGrabbedBodies(grabbedBodies);
        }

        public float spearLength = 1;
        public float tipLength = 1;
        public float spearRadius = .05f;
        
        List<int> reachedIndicies = new List<int>();
        float randomOffset;
        List<GrabbedBody> grabbedBodies = new List<GrabbedBody>();
        List<PhysicsHit> physicsHits = new List<PhysicsHit>();
        
        Vector3[] skewerTargets;
        float[] skewerOffsets;
        int startIndexForSkewered;
        bool stickToEnd, collidedDuringPhysicsFly;
        Vector3 modelEndTarget;
        int skeweredCount { get { return grabbedBodies.Count - startIndexForSkewered; } }

        public List<PhysicsHit> ShootSpear (float velocity, Ray ray, float maxDistance, LayerMask skewerMask, float lifeTime, float skewerSeperation, float minSpaceFromWall, float hitObjectVelocityMultiplier, System.Action<PhysicsHit, bool> onReachHitObject) {

            this.onReachHitObject = onReachHitObject;

            collidedDuringPhysicsFly = false;

            InitializeLaunch(ray, velocity, lifeTime, hitObjectVelocityMultiplier);
            
            reachedIndicies.Clear();

            //grab any rigidbodies along a ray, until a static collider
            Vector3 endPoint = RagdollPhysics.ShootMultiGrabRay ( ray, spearRadius, skewerMask, maxDistance, grabbedBodies, physicsHits, out endPointDistance, out stickToEnd);
            
            int hitCount = grabbedBodies.Count;

            // if we have no wall to stick to
            if (!stickToEnd) {
                
                //if we have some rigidbodies to impale, recalculate an end point after the last one
                if (hitCount > 0) {

                    PhysicsHit furthestHit = physicsHits[hitCount - 1];
                    endPoint = furthestHit.hitElements[furthestHit.hitElements.Count - 1].hitPoint + ray.direction * spearLength;
                    endPointDistance = Vector3.Distance(ray.origin, endPoint);
                }
                else {
                    // turn on physics and just use our velocity to "fly"
                    ReachDestination(1);
                }
            }

            // this is true already if we didnt hit anything
            if (!reachedDestination) {

                // if we have any impaled rigidbodies
                if (hitCount > 0) {

                    // where each grabbed rigidbody will end up
                    skewerTargets = new Vector3[hitCount];
                    skewerOffsets = new float[hitCount];

                    // the first index that is being skewered
                    startIndexForSkewered = hitCount;
                    
                    // calculate where each grabbed rigidbody will end up:
                    float lastEndPoint = minSpaceFromWall;
                    for (int i = hitCount - 1; i >= 0; i--) {
                        float skewerOffset;
                        skewerTargets[i] = CalculateSkewerTargetPosition (endPoint, ray.direction, skewerSeperation, DetermineHitPointsSize ( physicsHits[i], ray.direction, skewerMask), ref lastEndPoint, out skewerOffset);
                        skewerOffsets[i] = skewerOffset;

                        // if we have enough space on teh spear, skewer this index
                        if (skewerOffset < spearLength) 
                            startIndexForSkewered = i;

                        // if not we'll just add force to it (or dismember it) when the spear reaches it
                    }

                    StartCollisionIgnoreWithMyCollider(physicsHits);
                    
                    DynamicRagdoll.Demo.GameTime.SetTimeDilation(.5f, .1f, 10, .1f);
                }
                
                // randomly offset the amount the spear model sticks out of the end point
                randomOffset = Random.Range(-tipLength * .5f, 0);
                modelEndTarget = endPoint + ray.direction * randomOffset;
            }

            return physicsHits;
        }

    }
}
