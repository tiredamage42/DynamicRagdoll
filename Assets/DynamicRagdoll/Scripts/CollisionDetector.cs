using System.Collections.Generic;
using UnityEngine;

namespace DynamicRagdoll.Collisions {

    /*
        checks for collisions within the capsule when 

        velocity is above a certain magnitude, then call a callback

        if checking self 
            we check if we've run into anything
        else 
            we're checking if anything else has run into us

        these are seperated in case we nee need seperate capsule sizes for each use
    */

    [ExecuteInEditMode]
    [RequireComponent(typeof(CapsuleCollider))]
    [RequireComponent(typeof(Rigidbody))]
    public class CollisionDetector : MonoBehaviour
    {
        public enum CalculateVelocityMode {
            Update, FixedUpdate, LateUpdate
        };
        public CalculateVelocityMode updateMode;

        [Tooltip("The minimum velocity of the collision to register as collided")]
        public float velocityThreshold = 1.0f;

        [Tooltip("Should we check our velocity or the other collider in the collision?")]
        public bool checkingSelf;

        [Tooltip("Calculate planar velocity (if checking self)")]
        public bool velocity2D = true;

        public float radius = 1;
        public float height = 2;

        [HideInInspector] public CapsuleCollider capsule;
        Rigidbody rb;
        
        Vector3 myVelocity, lastPosition;
        HashSet<System.Action<Collider>> onCollisionCallbacks = new HashSet<System.Action<Collider>>();
        

        public void SubscribeToCollision (System.Action<Collider> onCollision) {
            onCollisionCallbacks.Add(onCollision);
        }
        void BroadcastCollision (Collider collider) {
            foreach (var cb in onCollisionCallbacks) {
                cb(collider);
            }
        }

        void CalculateSelfVelocity (CalculateVelocityMode modeCheck, float deltaTime) {
            if (!checkingSelf) {
                return;
            }
            if (modeCheck != updateMode) {
				return;
			}

            //calculate the transform's velocity

            Vector3 currentPosition = transform.position;
            Vector3 direction = (currentPosition - lastPosition);
            
            //make planar if just calculating 2d
            if (velocity2D) {
                direction.y = 0;
            }

            myVelocity = direction * (1f / deltaTime);
            
            lastPosition = currentPosition;
        }
        void Awake () {
            capsule = GetComponent<CapsuleCollider>();
            capsule.isTrigger = true;

            rb = GetComponent<Rigidbody>();
            rb.isKinematic = true;

            lastPosition = transform.position;

            UpdateCapsuleSizing();
        }


        void Update () 
		{
            //update in editor and play mode
            UpdateCapsuleSizing();

            if (Application.isPlaying) {
			    CalculateSelfVelocity(CalculateVelocityMode.Update, Time.deltaTime);
            }
		}

		void FixedUpdate () 
		{
			CalculateSelfVelocity(CalculateVelocityMode.FixedUpdate, Time.fixedDeltaTime);
		}
        
		void LateUpdate () 
		{
			CalculateSelfVelocity(CalculateVelocityMode.LateUpdate, Time.deltaTime);
		}

        void UpdateCapsuleSizing () {
            if (capsule == null) {
                capsule = GetComponent<CapsuleCollider>();                
            }
            capsule.radius = radius;
            capsule.height = height;
            capsule.center = new Vector3(0, height * .5f, 0);
        }

        
        void OnTriggerEnter (Collider other) {

            // physics callbacks are still called even when monobehaviours are enabled
            // but we dont need that
            if (!enabled) {
                return;
            }

            // ignore floor collisions
            if (other.CompareTag("Floor")) {
                return;
            }

            float sqrMagnitude = -1;

            if (checkingSelf) {
                //use our velocity
                sqrMagnitude = myVelocity.sqrMagnitude;
                //Debug.Log("collided, myVelocity: " + myVelocity.magnitude);
            }
            else {
                //use other colliders velocity (if its not static)
                Rigidbody rb = other.GetComponent<Rigidbody>();
                if (rb) {   
                    sqrMagnitude = rb.velocity.sqrMagnitude;
                    //Debug.Log("collided, other velocity: " + rb.velocity.magnitude);
                }
            }

            //if we've met the conditions then call the callbacks
            if (sqrMagnitude >= 0) {
                if (sqrMagnitude >= velocityThreshold * velocityThreshold) {
                    BroadcastCollision(other);
                }
            }
        }        
    }
}