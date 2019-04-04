using System;
using System.Collections.Generic;
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
        CapsuleCollider _capsule;
        public CapsuleCollider capsule {
            get {
                if (_capsule == null) {
                    _capsule = GetComponent<CapsuleCollider>();
                }
                return _capsule;
            }
        }

        HashSet<Action<Collider>> onTriggerCallbacks = new HashSet<Action<Collider>>();
        
        public void SubscribeToTrigger (System.Action<Collider> onCollision) {
            onTriggerCallbacks.Add(onCollision);
        }

        void BroadcastTrigger (Collider collider) {
            foreach (var cb in onTriggerCallbacks) {
                cb(collider);
            }
        }

        void Awake () {
            capsule.isTrigger = true;
            GetComponent<Rigidbody>().isKinematic = true;
        }
        
        void OnTriggerEnter (Collider other) {

            // physics callbacks are still called even when monobehaviours are enabled
            // but we dont need that
            if (!enabled) {
                return;
            }

            BroadcastTrigger(other);
        }        
    }
}