using System.Collections.Generic;
using UnityEngine;

namespace DynamicRagdoll {
    public partial class Ragdoll : MonoBehaviour
    {
        /*
			Adjust Ragdoll component values per bone to reflect the supplied
			Ragdoll profile (default profile if none is supplied)
		*/
		public static void UpdateBonesToProfileValues (Dictionary<HumanBodyBones, RagdollTransform> bones, RagdollProfile profile, float initialHeadOffsetFromChest) {
			if (bones == null)
				return;

			if (profile == null)
				return;
			
			Vector3 headOffset = profile.headOffset;

			//clamp head offset (values too high or too low become unstable for some reason)
			headOffset.y = Mathf.Clamp(headOffset.y, -initialHeadOffsetFromChest + .1f, 2);
			
			
			for (int i = 0; i < bonesCount; i++) {
            
				RagdollProfile.BoneProfile boneProfile = profile.boneData[humanBones[i]];
				
                HumanBodyBones hBodyBone = humanBones[i];
				
                RagdollTransform bone = bones[hBodyBone];

				
                //set rigidbody values for bone
				UpdateRigidbodyToProfile(bone.rigidbody, boneProfile);
				
				//adjust collider values for bone
				UpdateColliderToProfile (hBodyBone, bone.collider, boneProfile, headOffset, initialHeadOffsetFromChest);

				//set joint values
				if (bone.joint) {
					UpdateJointToProfile(hBodyBone, bone.joint, boneProfile, headOffset);
				}
			}
		}
		static void UpdateColliderToProfile (HumanBodyBones bone, Collider collider, RagdollProfile.BoneProfile boneProfile, Vector3 headOffset, float initialHeadOffsetFromChest) {
			
			//change physic material
			collider.sharedMaterial = boneProfile.colliderMaterial;

			//head
			if (bone == HumanBodyBones.Head) {

				//adjust the head collider based on headRadius and head Offset
				SphereCollider sphere = collider as SphereCollider;
				sphere.radius = boneProfile.colliderRadius;
				sphere.center = new Vector3(headOffset.x, boneProfile.colliderRadius + headOffset.y, headOffset.z);
			}
			
			//breast box colliders
			else if (bone == HumanBodyBones.Chest || bone == HumanBodyBones.Hips) {
				
				BoxCollider box = collider as BoxCollider;
				
				Vector3 center = box.center;
				Vector3 size = box.size;
				
				if (bone == HumanBodyBones.Chest) {
					//adjust the chest collider, so it's top reaches the head collider joint
					
					Bounds chestBounds = new Bounds(center, size);
					Vector3 max = chestBounds.max;
					max.y = initialHeadOffsetFromChest + headOffset.y;
					chestBounds.max = max;
				
					center = chestBounds.center;
					size = chestBounds.size;
				}
				
				//adjust chest and hips Z thickness and offset
				//maybe some models are 'fatter' than others
				center.z = boneProfile.boxZOffset;
				size.z = boneProfile.boxZSize;
				
				box.center = center;
				box.size = size;
			}
			//adjust the radius of the arms and leg capsules
			else {
				CapsuleCollider capsule = collider as CapsuleCollider;
				capsule.radius = boneProfile.colliderRadius;
			}
		}

		static void UpdateJointToProfile (HumanBodyBones bone, ConfigurableJoint joint, RagdollProfile.BoneProfile boneProfile, Vector3 headOffset) {

			// joint.projectionMode = JointProjectionMode.PositionAndRotation;
			
			joint.connectedMassScale = boneProfile.connectedMassScale;
			joint.massScale = boneProfile.massScale;

			joint.xMotion = joint.yMotion = joint.zMotion = ConfigurableJointMotion.Limited;

			//adjust anchor for head offset
			if (bone == HumanBodyBones.Head) {
				if (joint.anchor != headOffset) {
					joint.anchor = headOffset;
				}
			}
			
			//adjust axes (changing every fram was slow, so checking if same value first)
			if (joint.axis != boneProfile.axis1) {
				joint.axis = boneProfile.axis1;
			}
			if (joint.secondaryAxis != boneProfile.axis2) {
				joint.secondaryAxis = boneProfile.axis2;
			}
			
			//adjust limits (0 if forceOff is enabled)
			var l = joint.lowAngularXLimit;
			l.limit = boneProfile.forceOff ? 0 : boneProfile.angularXLimit.x;
			l.contactDistance = 45;
			joint.lowAngularXLimit = l;

			l = joint.highAngularXLimit;
			l.limit = boneProfile.forceOff ? 0 : boneProfile.angularXLimit.y;
			l.contactDistance = 45;
			joint.highAngularXLimit = l;
			
			l = joint.angularYLimit;
			l.limit = boneProfile.forceOff ? 0 : boneProfile.angularYLimit;
			l.contactDistance = 45;
			joint.angularYLimit = l;
			
			l = joint.angularZLimit;
			l.limit = boneProfile.forceOff ? 0 : boneProfile.angularZLimit;
			l.contactDistance = 45;
			joint.angularZLimit = l;
		}
		static void UpdateRigidbodyToProfile (Rigidbody rigidbody, RagdollProfile.BoneProfile boneProfile) {
			
			//set rigidbody values for bone
			rigidbody.maxAngularVelocity = boneProfile.maxAngularVelocity;
			rigidbody.angularDrag = boneProfile.angularDrag;
			rigidbody.angularDrag = boneProfile.drag;
			rigidbody.mass = boneProfile.mass;

			rigidbody.interpolation = boneProfile.interpolation;
			rigidbody.collisionDetectionMode = boneProfile.collisionDetection;

			//setting thebone default so it can be changed at runtime
			rigidbody.maxDepenetrationVelocity = boneProfile.maxDepenetrationVelocity;
		}
    }
}
