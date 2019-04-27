using UnityEngine;
using System;
namespace DynamicRagdoll {
    /*
        component scene representation of teh physical ragdoll bones

        used for collision callbacks
    */
    [RequireComponent(typeof(Rigidbody))]
    public class RagdollBone : MonoBehaviour {
        public HumanBodyBones bone;


        public event Action<RagdollBone, Collision> onCollisionEnter, onCollisionStay, onCollisionExit;
        public Ragdoll ragdoll;
        public Collider boneCollider;

        void Awake () {
            boneCollider = GetComponent<Collider>();
        }

        /*
            has to be public i guess...  :/
        */
        public void _InitializeInternal (Ragdoll ragdoll, HumanBodyBones bone, Action<RagdollBone, Collision> onCollisionEnter, Action<RagdollBone, Collision> onCollisionStay) {
            this.ragdoll = ragdoll;
            this.bone = bone;
            this.onCollisionEnter += onCollisionEnter;
            this.onCollisionStay += onCollisionStay;
            this.onCollisionExit += onCollisionExit;   
        }

        void OnCollisionEnter(Collision collision) {
            if (onCollisionEnter != null) {
                onCollisionEnter(this, collision);
            }
        }
        void OnCollisionStay(Collision collision) {
            if (onCollisionStay != null) {
                onCollisionStay(this, collision);
            }
        }
        void OnCollisionExit(Collision collision) {
            if (onCollisionExit != null) {
                onCollisionExit(this, collision);
            }
        }
    }
}
