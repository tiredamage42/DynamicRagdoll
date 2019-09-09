

using System.Collections.Generic;
using UnityEngine;


namespace DynamicRagdoll {

    /*
        object that will go towards and stick into rigidbodies
        
        shoots a ray in a direction:

        if we've hit an object at the end:

            S (Shrapnel)->       C (Static)

            we move the Shrapnel towards the end point (C) and stick into it
            
        if we didnt hit anything:
            
            just throw the spear using physics velocity
    */

    public class Shrapnel : PhysicsProjectile<Shrapnel>
    {
        
        void OnCollisionEnter(Collision other){
            collidedDuringPhysicsFly = true;
        }
        
        void UpdatePhysicsFly () {
            // keep our forward facing our velocity, to simulate an arc
            // if we're not already collided, and using physics
            if (!collidedDuringPhysicsFly && !rb.isKinematic) {
            RagdollPhysics.SimulateArc(rb);
            }
        }
        
        protected override void UpdateProjectile(float deltaTime) {    
        
            UpdatePhysicsFly();
            
            if (reachedDestination)
                return;

            UpdateTravelZ(deltaTime, 0);
            
            // move model
            transform.localPosition = Vector3.Lerp( startLocalPosition, localHitPoint, Mathf.Clamp01(travelZ / endPointDistance));
            if (reachedDestination) {
                ReachDestination();
            }
        }


        System.Action<PhysicsHit> onReachHitObject;


        void ReachDestination () {
            if (onReachHitObject != null) {
                onReachHitObject(physicsHit);
            }

            reachedDestination = true;
            // EnableCollider(true);

            // maybe dismemebr or make go ragdoll ?
            
            //simulate shrapnel hitting object
            if (!physicsHit.hitStatic) {
                Collider hitCollider = physicsHit.hitElements[0].collider;
                
                Vector3 direction = hitCollider.transform.TransformDirection(localHitPoint - startLocalPosition);
                Vector3 hitPoint = hitCollider.transform.TransformPoint(localHitPoint);
                physicsHit.hitElements[0].rigidbody.AddForceAtPosition( direction * velocity * hitObjectVelocityMultiplier, hitPoint, ForceMode.VelocityChange );
            }
        }


        protected override void DisableProjectile () {

        }

        
        bool simulateArc, collidedDuringPhysicsFly;
        Vector3 localHitPoint, startLocalPosition;
        PhysicsHit physicsHit;
        
        public PhysicsHit Launch (float velocity, Ray ray, float maxDistance, float radiusCheck, LayerMask layerMask, float lifeTime, float hitObjectVelocityMultiplier, bool simulateArc, System.Action<PhysicsHit> onReachHitObject) {

            this.simulateArc = simulateArc;
            this.onReachHitObject = onReachHitObject;
            
            InitializeLaunch(ray, velocity, lifeTime, hitObjectVelocityMultiplier);
            if (!simulateArc) {
                transform.rotation = Quaternion.Euler(Random.Range(0,360), Random.Range(0,360), Random.Range(0,360));
            }

            physicsHit = RagdollPhysics.SphereCast ( ray, radiusCheck, layerMask, maxDistance);

            if (physicsHit != null)
            {
                Collider hitCollider = physicsHit.hitElements[0].collider;
                transform.SetParent(hitCollider.transform);
                localHitPoint = hitCollider.transform.InverseTransformPoint(physicsHit.averagePosition);
                startLocalPosition = transform.localPosition;
                endPointDistance = physicsHit.hitDistance;
                
                StartCollisionIgnoreWithMyCollider(new List<PhysicsHit>() { physicsHit });
    
                DynamicRagdoll.Demo.GameTime.SetTimeDilation(.5f, .1f, 10, .1f);
            }
            else {
                EnablePhysics(true);
                rb.velocity = ray.direction * velocity;
                collidedDuringPhysicsFly = false;
                reachedDestination = true;

            }   
            return physicsHit;
        }
    }
}
