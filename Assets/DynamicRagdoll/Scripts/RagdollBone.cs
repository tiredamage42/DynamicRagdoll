using UnityEngine;
using System;
namespace DynamicRagdoll {
    /*
        component scene representation of teh physical ragdoll bones

        use like a rigidbody/collider

        TODO:
            add explosion force and other force add methods...
    
    */
    [RequireComponent(typeof(Rigidbody))]
    public class RagdollBone : MonoBehaviour
    {
        public HumanBodyBones bone;
        Action<RagdollBone, Collision> broadCastCollision;
        public Ragdoll ragdoll;
        Rigidbody rb;

        void Awake () {
            rb = GetComponent<Rigidbody>();
        }

        /*
            has to be public i guess...  :/
        */
        public void _InitializeInternal (Ragdoll ragdoll, HumanBodyBones bone, Action<RagdollBone, Collision> broadCastCollision) {
            this.ragdoll = ragdoll;
            this.bone = bone;
            this.broadCastCollision = broadCastCollision;
        }

        /*
            call this when you want to add physics to the ragdoll

            it checks for you if it's controlled or not, and stores the hits for you
        */
        public void AddForceAtPosition (Vector3 force, Vector3 position, ForceMode forceMode) {

            if (ragdoll.hasController) {
                // have the ragdoll controller store physics calculations (just in case it has to delay them)
                ragdoll.controller.StorePhysics(

                    () => rb.AddForceAtPosition(force, position, forceMode)
                    
                );
            }
            else {
                rb.AddForceAtPosition(force, position, forceMode);
            }
        }

        void OnCollisionEnter(Collision collision) {
            broadCastCollision(this, collision);
        }
    }
}
