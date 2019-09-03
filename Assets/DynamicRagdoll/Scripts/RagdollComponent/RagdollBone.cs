using UnityEngine;
using System;
namespace DynamicRagdoll {

    
    /*
        component scene representation of teh physical ragdoll bones

        used for collision callbacks
    */
    [RequireComponent(typeof(Rigidbody))]
    public class RagdollBone : MonoBehaviour {
        [HideInInspector] public HumanBodyBones bone;
        [HideInInspector] public Collider boneCollider;
        [HideInInspector] public Ragdoll ragdoll;

        public event Action<RagdollBone, Collision> onCollisionEnter, onCollisionStay, onCollisionExit;

        void Awake () {
            boneCollider = GetComponent<Collider>();
        }

        /*
            has to be public i guess...  :/
        */
        public void _InitializeInternal (HumanBodyBones bone, Ragdoll ragdoll, Action<RagdollBone, Collision> onCollisionEnter, Action<RagdollBone, Collision> onCollisionStay, Action<RagdollBone, Collision> onCollisionExit) {
            this.bone = bone;
            this.ragdoll = ragdoll;
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
