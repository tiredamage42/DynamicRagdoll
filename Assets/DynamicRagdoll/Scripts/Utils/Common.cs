using UnityEngine;
namespace DynamicRagdoll {
    public enum UpdateMode { Update, FixedUpdate, LateUpdate };

    public enum RagdollControllerState { 
        Animated, 					//fully animated
        Falling,					//decaying fall
        Ragdolled, 					//complete ragdoll
        TeleportMasterToRagdoll, 	//waiting for get up animation transition, to reorient invisible master
        BlendToAnimated, 			//blend into animated position
    };


    /*
        use to temporarily ignore collisions between two colliders
    */
    class ColliderIgnorePair {
        public Collider collider1, collider2;
        public float ignoreTime;
        public float timeSinceIgnore { get { return Time.time - ignoreTime; } }

        public ColliderIgnorePair(Collider collider1, Collider collider2) {
            this.collider1 = collider1;
            this.collider2 = collider2;
            this.ignoreTime = Time.time;
            Physics.IgnoreCollision(collider1, collider2, true);
        }

        public void EndIgnore () {
            Physics.IgnoreCollision(collider1, collider2, false);
        }
    }


    public static class RagdollPhysics {
        public static void HangRigidbody (Rigidbody rb, Rigidbody parent) {
            HangRigidbody(rb, parent, parent.transform.InverseTransformPoint(rb.transform.position));
        }


        public static void HangRigidbody (Rigidbody rb, Rigidbody parent, Vector3 connectedAnchorOffset) {
            FixedJoint fj = rb.GetComponent<FixedJoint>();
            if (fj != null) {
                Debug.LogError(rb.name + " is already hanging");
                return;
            }
            fj = rb.gameObject.AddComponent<FixedJoint>();
            fj.autoConfigureConnectedAnchor = false;
            fj.connectedAnchor = connectedAnchorOffset;
            fj.anchor = Vector3.zero;
            fj.connectedBody = parent;
            fj.enablePreprocessing = false;
        }

        public static void DetachRigidbody (Rigidbody rb) {
            FixedJoint fj = rb.GetComponent<FixedJoint>();
            if (fj != null) MonoBehaviour.Destroy(fj);
        }

    }
}
