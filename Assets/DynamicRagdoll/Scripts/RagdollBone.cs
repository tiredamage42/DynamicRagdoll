using UnityEngine;
using System;
namespace DynamicRagdoll {
    /*
        component scene representation of teh physical ragdoll bones

        used for collision callbacks
    */
    [RequireComponent(typeof(Rigidbody))]
    public class RagdollBone : MonoBehaviour
    {
        public HumanBodyBones bone;
        Action<RagdollBone, Collision> onCollisionEnter, onCollisionStay;
        public Ragdoll ragdoll;
        public Collider col;

        void Awake () {
            col = GetComponent<Collider>();
        }

        /*
            has to be public i guess...  :/
        */
        public void _InitializeInternal (Ragdoll ragdoll, HumanBodyBones bone, Action<RagdollBone, Collision> onCollisionEnter, Action<RagdollBone, Collision> onCollisionStay) {
            this.ragdoll = ragdoll;
            this.bone = bone;
            this.onCollisionEnter = onCollisionEnter;
            this.onCollisionStay = onCollisionStay;
        }

        
        void OnCollisionEnter(Collision collision) {

            onCollisionEnter(this, collision);
        }

        void OnCollisionStay(Collision collision) {

            onCollisionStay(this, collision);
        }
    }
}
