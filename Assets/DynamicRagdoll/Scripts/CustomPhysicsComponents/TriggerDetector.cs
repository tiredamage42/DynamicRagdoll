using System;
using UnityEngine;

namespace DynamicRagdoll {

    /*
        script used to callback when a trigger is enabled.

        allows an object to use multiple trigger checks
    */

    [RequireComponent(typeof(CapsuleCollider))]
    [RequireComponent(typeof(Rigidbody))]
    public class TriggerDetector : MonoBehaviour
    {
        public event Action<Collider> onTriggerEnter;
        [HideInInspector] public CapsuleCollider capsule;

        void Awake () {
            capsule = GetComponent<CapsuleCollider>();
            capsule.isTrigger = true;
            GetComponent<Rigidbody>().isKinematic = true;
        }
        
        void OnTriggerEnter (Collider other) {

            // physics callbacks are still called even when monobehaviours are enabled
            // but we dont need that
            if (!enabled) {
                return;
            }

            if (onTriggerEnter != null) {
                onTriggerEnter(other);
            }
        }        
    }
}