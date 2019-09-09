using UnityEngine;


namespace DynamicRagdoll {
    public partial class Ragdoll : MonoBehaviour
    {
        /*
			Add all teh physical ragdoll components (for checking collisions)
		*/
		void InitializeRagdollBoneComponents () {
			for (int i = 0; i < bonesCount; i++) {	
				allElements[i].bone._InitializeInternal(humanBones[i], this, BroadcastCollisionEnter, BroadcastCollisionStay, BroadcastCollisionExit);
			}
		}

        /*
			subscribe to get a notification when a ragdoll bone enters a collision
		*/
		public event System.Action<RagdollBone, Collision> onCollisionEnter, onCollisionStay, onCollisionExit;

		/*
			send the message out that bone was collided
			(given to ragdollbone component)
		*/
		void BroadcastCollisionEnter (RagdollBone bone, Collision collision) {
			if (onCollisionEnter != null) 
				onCollisionEnter(bone, collision);
		}
		void BroadcastCollisionStay (RagdollBone bone, Collision collision) {
			if (onCollisionStay != null) 
				onCollisionStay(bone, collision);
		}
		void BroadcastCollisionExit (RagdollBone bone, Collision collision) {
			if (onCollisionExit != null) 
				onCollisionExit(bone, collision);
		}


        public bool Transform2HumanBone (Transform transform, out RagdollTransform bone) {
            bone = null;
			if (CheckForErroredRagdoll("Transform2HumanBone"))
				return false;
	
			for (int i = 0; i < bonesCount; i++) {	
				if (allElements[i].transform == transform) {
                    bone = allElements[i];
					return true;
				}
			}
			return false;
		}
        
		/*
			cehck if a collider is part of our ragdoll
		*/
		public bool ColliderIsPartOfRagdoll (Collider collider) {
			if (CheckForErroredRagdoll("ColliderIsPartOfRagdoll"))
				return false;

			return Transform2HumanBone(collider.transform, out _);
		}


        /*
			make ragdoll ignore collisions with collider
		*/
		public void IgnoreCollisions(Collider collider, bool ignore) {
			if (CheckForErroredRagdoll("IgnoreCollisions")) 
				return;
			for (int i = 0; i < bonesCount; i++) 
				allElements[i].IgnoreCollisions(collider, ignore);
		}

        /*
			ignore collisions with other physics bones on the same ragdoll
		*/
		public void IgnoreSelfCollisions (bool ignore) {
			if (CheckForErroredRagdoll("IgnoreSelfCollisions"))
				return;
	
			for (int i = 0; i < bonesCount; i++) {	
			
				RagdollTransform boneA = allElements[i];

				for (int x = i + 1; x < bonesCount; x++) {	
					RagdollTransform boneB = allElements[x];

					// dont handle connected joints, joint component already does
					if (boneB.joint && boneB.joint.connectedBody == boneA.rigidbody) continue;
					if (boneA.joint && boneA.joint.connectedBody == boneB.rigidbody) continue;
					
					Physics.IgnoreCollision(boneA.collider, boneB.collider, ignore);
				}
			}
		}
    }
}
