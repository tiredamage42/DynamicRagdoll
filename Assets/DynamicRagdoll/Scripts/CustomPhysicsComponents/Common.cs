using UnityEngine;
using System.Collections.Generic;
namespace DynamicRagdoll {

    public enum UpdateMode { Update, FixedUpdate, LateUpdate };
    
    public static partial class RagdollPhysics {
        public static void SimulateArc (Rigidbody rb) {
            // check since we were getting zero look rotation errors
            if (rb.velocity.sqrMagnitude > .1f) {
                rb.transform.rotation = Quaternion.LookRotation(rb.velocity);
            }
        }

        /*
            moving transform position with character controller component
            doesnt work unless it's disabled
        */
        static Dictionary<int, CharacterController> transform2CC = new Dictionary<int, CharacterController>();
        public static void MovePossibleCharacterController (Transform transform, Vector3 newPosition) {
            CharacterController cc = null;
            int id = transform.GetInstanceID();
            if (transform2CC.ContainsKey(id)) {
                cc = transform2CC[id];
            }
            else {
                cc = transform.GetComponent<CharacterController>();
                transform2CC[id] = cc;
            }

            bool ccEnabled = false;
            if (cc != null) {
                ccEnabled = cc.enabled;
                cc.enabled = false;
            }
            transform.position = newPosition;
            if (cc != null) cc.enabled = ccEnabled;
        }
    }
}
