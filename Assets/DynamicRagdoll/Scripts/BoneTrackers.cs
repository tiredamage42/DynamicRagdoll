using UnityEngine;

namespace DynamicRagdoll {
    public class BoneTracker {
		public Transform slave, master;
		public Quaternion savedRotation;
		public Vector3 savedPosition;

		// public virtual void SetLocalRotation (Quaternion localRotation) {
		// 	slave.localRotation = localRotation;
		// }

		public BoneTracker(Transform slave, Transform master) {
			this.slave = slave;
			this.master = master;
		}
	}

	public class PhysicalBoneTracker : BoneTracker {
		//public float runtimeMultiplier = 1;
		Vector3 originalRBPosition, forceLastError;
		Quaternion startLocalRotation, localToJointSpace;
		public Ragdoll.Bone bone;
		float lastJointTorque = -1;

		// public override void SetLocalRotation(Quaternion localRotation) {
		// 	if (bone.joint) {
		// 		bone.joint.SetTargetRotationLocal(localRotation, startLocalRotation);
		// 	}
		// }

		public PhysicalBoneTracker(Ragdoll.Bone bone, Transform master, ref JointDrive jointDrive) 
			: base(bone.transform, master)
		{
			this.bone = bone;
			
			if (bone.joint) {
					


				
				
				localToJointSpace = Quaternion.LookRotation(Vector3.Cross (bone.joint.axis, bone.joint.secondaryAxis), bone.joint.secondaryAxis);
				startLocalRotation = slave.localRotation * localToJointSpace;
				localToJointSpace = Quaternion.Inverse(localToJointSpace);
				
				
				jointDrive = bone.joint.slerpDrive;
				bone.joint.slerpDrive = jointDrive;
			}
			originalRBPosition = Quaternion.Inverse(bone.rigidbody.rotation) * (bone.rigidbody.worldCenterOfMass - bone.rigidbody.position); 		
		}
		/*
		
		 Quaternion LocalRotation = Quaternion.Inverse(Target.transform.rotation) * WorldRotation;
 //this will transform WorldRotation to Target's local space
		 
		 Quaternion WorldRotation = Target.Transform.rotation * rotation;
 //Transforms rotation from Targets local space to world space
		 
		 */


		public void MoveBoneToMaster (RagdollControllerProfile profile, float maxForce, float maxJointTorque, float reciDeltaTime, RagdollControllerProfile.BoneProfile boneProfile, JointDrive jointDrive){
			
			Vector3 forceError = Vector3.zero;
			if (boneProfile.inputForce != 0 && maxForce != 0){// && runtimeMultiplier != 0) {

				// Force error
				forceError = (master.position + master.rotation * originalRBPosition) - bone.rigidbody.worldCenterOfMass;
				// Calculate and apply world force
				//Vector3 force = PDControl (profile.PForce * boneProfile.inputForce, profile.DForce, forceError, ref forceLastError, maxForce, boneProfile.maxForce * runtimeMultiplier, reciDeltaTime);
				Vector3 force = PDControl (profile.PForce * boneProfile.inputForce, profile.DForce, forceError, ref forceLastError, maxForce, boneProfile.maxForce, reciDeltaTime);
				
				bone.rigidbody.AddForce(force, ForceMode.VelocityChange);
			}
			forceLastError = forceError;

					
			if (bone.joint) { 

				float jointTorque = maxJointTorque * boneProfile.maxTorque;// * runtimeMultiplier;
				if (jointTorque != lastJointTorque) {

					jointDrive.positionSpring = jointTorque;
					bone.joint.slerpDrive = jointDrive;
			
					lastJointTorque = jointTorque;
				}
							
				if (jointTorque != 0) {
					

					bone.joint.targetRotation = localToJointSpace * Quaternion.Inverse(master.localRotation) * startLocalRotation;
				}
			}
		}
		static Vector3 PDControl (float P, float D, Vector3 error, ref Vector3 lastError, float maxForce, float weight, float reciDeltaTime) // A PD controller
		{
			// theSignal = P * (theError + D * theDerivative) This is the implemented algorithm.
			Vector3 signal = P * (error + D * ( error - lastError ) * reciDeltaTime);
			
			float max = maxForce * weight;

			float sqrMag = signal.sqrMagnitude;
			if (sqrMag > max * max) {
				return signal * (max / Mathf.Sqrt(sqrMag));
				//return Vector3.ClampMagnitude(signal, max);
			}
			return signal;


			

		}

		// public void EnableJointLimits (ConfigurableJointMotion jointLimits) {
		// 	if (bone.joint) {
		// 		bone.joint.angularXMotion = bone.joint.angularYMotion = bone.joint.angularZMotion = jointLimits;
		// 	}
		// }
	}
}
