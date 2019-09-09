using UnityEngine;

namespace DynamicRagdoll {
    /*
        use to temporarily ignore collisions between two colliders
    */
    public class ColliderIgnorePair {
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
}