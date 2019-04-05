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
        public Rigidbody rb;
        public Collider col;

        void Awake () {
            rb = GetComponent<Rigidbody>();
            col = GetComponent<Collider>();
        }

        /*
            has to be public i guess...  :/
        */
        public void _InitializeInternal (Ragdoll ragdoll, HumanBodyBones bone, Action<RagdollBone, Collision> broadCastCollision) {
            this.ragdoll = ragdoll;
            this.bone = bone;
            this.broadCastCollision = broadCastCollision;
        }

        
        void OnCollisionEnter(Collision collision) {

            broadCastCollision(this, collision);
        }
    }
}
