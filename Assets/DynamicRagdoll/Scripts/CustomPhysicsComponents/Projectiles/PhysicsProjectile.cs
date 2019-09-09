using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DynamicRagdoll {
    /*
        base class for physics projectiles
    */

    public abstract class PhysicsProjectile<T> : MonoBehaviour where T : MonoBehaviour
    {
        public static PrefabPool<T> pool = new PrefabPool<T>();
        protected Collider myCollider;
        protected Rigidbody rb;
        protected float travelZ, velocity, endPointDistance, hitObjectVelocityMultiplier;
        protected Ray launchRay;
        protected bool reachedDestination;

        List<ColliderIgnorePair> myColliderIgnorePairs = new List<ColliderIgnorePair>();


        /*
            make our collider stop ignoring collisions
        */
        void EndColliderIgnore () {
            RagdollPhysics.EndPhysicsHitsIgnoreCollider(myColliderIgnorePairs);
        }

        /*
            make our collider ignore collisions with the supplied colliders in the physics hit list
        */
        protected void StartCollisionIgnoreWithMyCollider (List<PhysicsHit> physicsHits) {
            RagdollPhysics.MakePhysicsHitsIgnoreCollider(physicsHits, myCollider, myColliderIgnorePairs);
        }

        void FixedUpdate () {
            UpdateProjectile(Time.fixedDeltaTime);
        }
        protected abstract void UpdateProjectile (float deltaTime);


        protected void UpdateTravelZ (float deltaTime, float endPointOffset) {
            float speed = deltaTime * velocity;
            travelZ += speed;
            if (travelZ >= endPointDistance + endPointOffset) {
                reachedDestination = true;
            }
        }

        protected void InitializeLaunch (Ray launchRay, float velocity, float lifeTime, float hitObjectVelocityMultiplier) {
            StartCoroutine(DeactivateAfterDelay(lifeTime));
            EnablePhysics(false);
            this.velocity = velocity;
            this.hitObjectVelocityMultiplier = hitObjectVelocityMultiplier;
            this.launchRay = launchRay;
            
            travelZ = 0;
            reachedDestination = false;
            
            transform.position = launchRay.origin;
            transform.rotation = Quaternion.LookRotation(launchRay.direction);
        }


        protected void EnableCollider (bool enabled) {
            if (myCollider != null) myCollider.enabled = enabled;
        }
        protected void SetKinematic (bool isKinematic) {
            if (rb != null) rb.isKinematic = isKinematic;
        }

        protected void EnablePhysics (bool enabled) {
            SetKinematic(!enabled);
            EnableCollider(enabled);
        }

        protected IEnumerator DeactivateAfterDelay (float delay) {
            yield return new WaitForSeconds(delay);
            DisableProjectile();
            EndColliderIgnore();
            EnablePhysics(false);
            transform.SetParent(null);
            gameObject.SetActive(false);
            rb.velocity = Vector3.zero;
        }

        protected abstract void DisableProjectile ();
        

        protected virtual void Awake () {
            rb = GetComponent<Rigidbody>();
            myCollider = GetComponent<Collider>();
            EnablePhysics(false);
            
        }   
    }
}