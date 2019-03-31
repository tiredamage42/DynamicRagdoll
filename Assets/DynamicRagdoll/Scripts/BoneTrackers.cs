using UnityEngine;

namespace DynamicRagdoll {
    public class BoneTracker {
		protected Transform slave, master;
		Quaternion savedRotation;
		Vector3 savedPosition;
		public bool isPhysicsParent;

		bool useWorldSpace;

		public BoneTracker(Transform slave, Transform master, bool useWorldSpace, bool isPhysicsParent) {
			this.slave = slave;
			this.master = master;
			this.useWorldSpace = useWorldSpace;
			this.isPhysicsParent = isPhysicsParent;
		}
		public void SaveSlaveValues () {
			if (useWorldSpace) {
				savedPosition = slave.position;
			}
			savedRotation = useWorldSpace ? slave.rotation : slave.localRotation;
		}

		public void Teleport () {
			if (useWorldSpace) {
				slave.position = master.position;
				slave.rotation = master.rotation;
			}
			else {
				slave.localRotation = master.localRotation;
			}
		}
		public void LerpFromSavedTowardsMaster (float blend) {
			if (useWorldSpace) {
				slave.position = Vector3.Lerp(savedPosition, master.position, blend);
				slave.rotation = Quaternion.Slerp(savedRotation, master.rotation, blend);
			}
			else {
				slave.localRotation = Quaternion.Slerp(savedRotation, master.localRotation, blend);
			}
		}
	}

	public class PhysicalBoneTracker : BoneTracker {
		Vector3 originalRBPosition, forceLastError;
		Quaternion startLocalRotation, localToJointSpace;
		public Ragdoll.Bone bone;
		float lastJointTorque = -1;

		public PhysicalBoneTracker(Ragdoll.Bone bone, Transform master, ref JointDrive jointDrive) : base(bone.transform, master, bone.joint == null, true)
		{
			this.bone = bone;
			
			originalRBPosition = Quaternion.Inverse(bone.rigidbody.rotation) * (bone.rigidbody.worldCenterOfMass - bone.rigidbody.position); 		
			
			if (bone.joint) {
				//save rotation values for setting joint rotation
				localToJointSpace = Quaternion.LookRotation(Vector3.Cross (bone.joint.axis, bone.joint.secondaryAxis), bone.joint.secondaryAxis);
				startLocalRotation = slave.localRotation * localToJointSpace;
				localToJointSpace = Quaternion.Inverse(localToJointSpace);
				

				jointDrive = bone.joint.slerpDrive;
				bone.joint.slerpDrive = jointDrive;
			}
		}

		public void ResetError () {
			forceLastError = Vector3.zero;
		}
	
		public void MoveBoneToMaster (RagdollControllerProfile profile, float maxForce, float maxJointTorque, float reciprocalDeltaTime, RagdollControllerProfile.BoneProfile boneProfile, JointDrive jointDrive){
			
			Vector3 forceError = Vector3.zero;

			if (boneProfile.inputForce != 0 && maxForce != 0){
				
				// Force error
				forceError = (master.position + master.rotation * originalRBPosition) - bone.rigidbody.worldCenterOfMass;
				// Calculate and apply world force
				Vector3 force = PDControl(profile.PForce * boneProfile.inputForce, profile.DForce, forceError, ref forceLastError, maxForce, boneProfile.maxForce, reciprocalDeltaTime);
				
				bone.rigidbody.AddForce(force, ForceMode.VelocityChange);
			}
			forceLastError = forceError;
					
			if (bone.joint) { 

				float jointTorque = maxJointTorque * boneProfile.maxTorque;
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
		static Vector3 PDControl (float P, float D, Vector3 error, ref Vector3 lastError, float maxForce, float weight, float reciprocalDeltaTime) 
		{
			// theSignal = P * (theError + D * theDerivative) This is the implemented algorithm.
			Vector3 signal = P * (error + D * ( error - lastError ) * reciprocalDeltaTime);
			
			float max = maxForce * weight;
			float sqrMag = signal.sqrMagnitude;
			if (sqrMag > max * max) {
				return signal * (max / Mathf.Sqrt(sqrMag));
				//return Vector3.ClampMagnitude(signal, max);
			}
			return signal;
		}
	}
}
