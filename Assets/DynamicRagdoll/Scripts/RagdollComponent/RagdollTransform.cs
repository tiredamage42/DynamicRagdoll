using UnityEngine;

namespace DynamicRagdoll {

    /*
        Runtime representation of a ragdoll transform child 
        can either be physical bone, like the hips or head
        or secondary, like a finger
    */
    public class RagdollTransform {
        public RagdollBone bone;
        public Rigidbody rigidbody;
        public ConfigurableJoint joint;
        public Collider collider;
        public Transform transform;
        public bool isBoneParent, isRoot, isBone;
        public RagdollTransform followTarget;
        
        Vector3 snapshotPosition;
        Quaternion snapshotRotation;


        public RagdollTransform (Transform transform, bool isBoneParent, bool isBone, bool isRoot) {
            this.transform = transform;
            this.isBoneParent = isBoneParent;
            this.isRoot = isRoot;
            this.isBone = isBone;

            bone = transform.GetComponent<RagdollBone>();
            rigidbody = transform.GetComponent<Rigidbody>();
            collider = transform.GetComponent<Collider>();  
            if (!isRoot) {
                joint = transform.GetComponent<ConfigurableJoint>();
            }
            
        }	
        
        public void SetFollowTarget(RagdollTransform followTarget) {
            this.followTarget = followTarget;
        }

        public void SaveSnapshot () {
            if (isRoot) {
                snapshotPosition = transform.position;
            }
            snapshotRotation = GetRotation();
        }
        public void LoadSnapShot () {
            TeleportTo(snapshotPosition, snapshotRotation);
        }

        public void EnableJointLimits(ConfigurableJointMotion m) {
            if (joint) {
                joint.angularXMotion = joint.angularYMotion = joint.angularZMotion = m;
            }
        }

        public void TeleportTo (Vector3 position, Quaternion rotation) {
            if (isRoot) {
                transform.position = position;
            }
            SetRotation(rotation);
            
            
            /*
                immediately update physics position...
                transform set wasnt updating fast enough for physics detection of the ragdoll
                through raycasts/collisions
            */
            if (rigidbody != null) {
                CollisionDetectionMode originalDetectionMode = rigidbody.collisionDetectionMode;

                //need to set the collision detection mode as discrete to set kinematic and 
                //teleport rigidbody (or unity throws an error)
                rigidbody.collisionDetectionMode = CollisionDetectionMode.Discrete;

                rigidbody.isKinematic = true;

                rigidbody.MovePosition (transform.position);
                rigidbody.MoveRotation (transform.rotation);
                
                rigidbody.isKinematic = false;
                
                rigidbody.collisionDetectionMode = originalDetectionMode;
            }
        }

        public void TeleportToTarget () {
            if (followTarget == null) {
                return;
            }
            TeleportTo(followTarget.transform.position, followTarget.GetRotation());
        }


        public void LoadSnapshot (float snapshotBlend, bool useFollowTarget) {
            RagdollTransform element = useFollowTarget && followTarget != null ? followTarget : this;
            
            TeleportTo(
                Vector3.Lerp(element.transform.position, snapshotPosition, snapshotBlend), 
                Quaternion.Slerp(element.GetRotation(), snapshotRotation, snapshotBlend)
            );
        }

        Quaternion GetRotation () {
            return isRoot ? transform.rotation : transform.localRotation;
        }

        void SetRotation(Quaternion rotation) {
            if (isRoot) 
                transform.rotation = rotation;
            else 
                transform.localRotation = rotation;
        }

        public T AddComponent<T> () where T : Component {
            return transform.gameObject.AddComponent<T>();
        }		
    }
}
