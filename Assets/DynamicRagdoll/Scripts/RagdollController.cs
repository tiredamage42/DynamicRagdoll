using UnityEngine;
using System.Collections.Generic;

/*
	Script for handling when to ragdoll, and switching between ragdolled and animations

	attach this script to the main character

	hides ragdoll (but keeps it moving with physics to follow the shown animated model)
	when ragdolled we switch off the animated main model and enable the ragdoll renderers

	that way we can have the leftover physics from the animation follow, without having to 
	worry too much about how closely the rigidbodies follow the actual animation
	(since we only use them when going ragdoll)

	when transitioning back to an animated state:
		
		- we play a get up animation

		- store the positions and rotations of teh ragdolled bones
		
		- turn off physics for the ragdoll
		
		- lerp the positions and rotations from their ragdolled positions
		  to the animated positions (in late update)
		
		- when the blend lerp reaches 1, disable the ragdoll (start it's physics follow again)
		  and enable the original master character model 
		  (which should still be playing the get up animation)

	
	TODO: try implementing a system where ragdoll trigger is delayed, so the physics
			following doesnst have to run every frame
	
*/
namespace DynamicRagdoll {
	//if transition from ragdoll blend to fully aniamted looks jittery, 
	//try lerping to full follow values
	
	public class RagdollController : MonoBehaviour
	{
		public RagdollControllerProfile profile;	
		public enum RagdollState { Ragdolled=0, Blending=1, Animated=2,}
		[HideInInspector] public RagdollState state = RagdollState.Animated;
		
		Animator animator;
		Transform masterHips;				
		
		// Rotation of ragdollRootBone relative to transform.forward
		Quaternion rootboneToForward;			
		bool calculatedRootboneToForward, orientated = true;					
		byte i;
		Renderer[] masterRenderers;
		float ragdollPhaseStartTime=-100;
		public Ragdoll ragdoll;
		AnimatorCullingMode originalAnimatorCullingMode;

		float currentMaxForce = 100f; // Limits the force
		float currentMaxJointTorque = 10000f; // Limits the force
			
		JointDrive jointDrive = new JointDrive();
		BoneFollower[] rbFollowers;
		Follower[] nonRbFollowers, allFollowers;
		
		//or set up system to callback when animation is done
		const float getupTime = 3;
		public bool isGettingUp { get { return Time.time - ragdollPhaseStartTime < getupTime; } }

		void SetKinematic(bool value) {
			for (int i = 0; i < rbFollowers.Length; i++)
				rbFollowers[i].boneRB.isKinematic = value;
		}
		void EnableRenderers (bool masterEnabled, bool slaveEnabled) {
			for (int i = 0; i < masterRenderers.Length; i++)
				masterRenderers[i].enabled = masterEnabled;

			ragdoll.EnableRenderers(slaveEnabled);
		}
		void SetFollowValues (float force, float jointTorque) {
			currentMaxForce = force;
			currentMaxJointTorque = jointTorque;
		}
		void EnableJointLimits (bool enabled) {
			ConfigurableJointMotion j = enabled ? ConfigurableJointMotion.Limited : ConfigurableJointMotion.Free;
			for (int i = 0; i < rbFollowers.Length; i++) {
				rbFollowers[i].EnableJointLimits(j);
			}
		}	

		public void GoRagdoll (){
			if (!profile) {
				Debug.LogWarning("No Controller Profile on " + name);
				return;
			}
			if (!ragdoll) {
				Debug.LogWarning("No Ragdoll for " + name + " to control...");
				return;
			}


			if (state == RagdollState.Ragdolled)
				return;

			state = RagdollState.Ragdolled;

			// The initial strength immediately after the impact
			SetFollowValues(profile.residualForce, profile.residualJointTorque);
			
			EnableJointLimits(true);
			
			//enable gravity
			SetKinematic(false); 
			
			//update position of non rigidbodied transforms on ragdoll model
			for (int i = 0; i < nonRbFollowers.Length; i++) {
				nonRbFollowers[i].slave.localRotation = nonRbFollowers[i].master.localRotation;
			}
			
			//turn on ragdoll renderers, disable master renderers
			EnableRenderers(false, true);
		}

		//Transition from ragdolled to animated through the Blend state
		void StartGetUp () {
			state = RagdollState.Blending;

			//isGettingUp = true;
			orientated = false;			
			
			//store the state change time
			ragdollPhaseStartTime = Time.time; 
			
			//master renderers are disabled, but animation needs to play regardless
			//so dont cull
			animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

			//disable follow
			SetFollowValues(0, 0);

			//disable joint limits
			EnableJointLimits(false);
			
			//disable gravity etc.
			SetKinematic(true); 

			//save the ragdoll positions in order to lerp from them to the animated
			//positions / local rotations
			for(int i = 0; i < allFollowers.Length; i++) {
				allFollowers[i].SaveRagdollValues(allFollowers[i].master == masterHips);
			}
			
			Vector3 rootBoneForward = ragdoll.RootBone().rotation * rootboneToForward * Vector3.forward;
			// Check if ragdoll is lying on its back or front, then transition to getup animation		
			bool onBack = Vector3.Dot(rootBoneForward, Vector3.down) < 0f; 
			
			//play get up animation
			animator.SetTrigger(onBack ? "BackTrigger" : "FrontTrigger");
			
		}

		// Here the master gets reorientated to the ragdoll 
		//which could have ended its fall in any direction and position
		void OrientateMaster () {		
			Transform ragHips = ragdoll.RootBone();

			//calculate the rotation for the master root object
			Quaternion masterRotation = ragHips.rotation * Quaternion.Inverse(masterHips.rotation) * transform.rotation;
			Vector3 fwd = masterRotation * Vector3.forward;
			fwd.y = 0;
			masterRotation = Quaternion.LookRotation(fwd);

			//calculate the position for the master root object				
			Vector3 masterPosition = transform.position + (ragHips.position - masterHips.position);
			//Now cast a ray from the computed position downwards and find the highest hit that does not belong to the character 
			RaycastHit hit;
			if (Physics.Raycast(new Ray(masterPosition + Vector3.up, Vector3.down), out hit, 20, profile.checkGroundMask)) {
				masterPosition.y = hit.point.y;
			}
							
			//set the position and rotation
			transform.rotation = masterRotation;
			transform.position = masterPosition;	

			//reset the phase time
			ragdollPhaseStartTime = Time.time;
		}

		bool HandleTransitionToAnim () {
			// Wait until transition to getUp is done 
			// so that the master animation is lying down before orientating the master 
			// to the ragdoll rotation and position
			
			float timeSinceRagdoll = Time.time - ragdollPhaseStartTime;
			if (!orientated)	
			{
				if (timeSinceRagdoll < profile.orientateDelay) {
					return false;
				}
				OrientateMaster();

				//Debug.Break();
				orientated = true;

				return false;
			}
			
			//compute the ragdoll blend amount in the range 0...1
			float blendT = timeSinceRagdoll / profile.blendTime;			
			blendT = Mathf.Clamp01(blendT);
			
			//In LateUpdate(), Mecanim has already updated the body pose according to the animations. 
			//lerp the position of the hips and slerp all the rotations towards the ones stored when ending the ragdolling

			//world position and rotation is interpolated for the hips, local rotation is interpolated for all other body parts
			
			for(int i = 0; i < allFollowers.Length; i++) {
				allFollowers[i].LerpFromSavedPositionToAnimatedPosition(blendT, allFollowers[i].master == masterHips);
			}
			return blendT == 1;
		}

		void OnEndBlend () {
			state = RagdollState.Animated;

			//enable gravity etc.
			SetKinematic(false); 
			
			//maybe lerp smoothly
			//enable follow forces
			SetFollowValues(profile.maxForce, profile.maxJointTorque);
			
			//turn off ragdoll renderers, enable master renderers
			EnableRenderers(true, false);

			//reset culling mode
			animator.cullingMode = originalAnimatorCullingMode;
		}

		void LateUpdate()
		{
			if (!profile) {
				return;
			}
			if (!ragdoll) {
				return;
			}

			
			if (state == RagdollState.Blending)
			{
				if (HandleTransitionToAnim()) {
					OnEndBlend();
				}
			}
		}

		void Awake () // Initialize
		{
			if (!profile) {
				Debug.LogWarning("No Controller Profile on " + name);
				return;
			}
			if (!ragdoll) {
				Debug.LogWarning("No Ragdoll for " + name + " to control...");
				return;
			}

			


			state = RagdollState.Animated;
			
			//get all the master renderers to switch off when going ragdoll
			masterRenderers = GetComponentsInChildren<Renderer>();
		
			animator = GetComponent<Animator>();
			
			//store original culling mode
			originalAnimatorCullingMode = animator.cullingMode;
			
			masterHips = animator.GetBoneTransform(HumanBodyBones.Hips);
		

			//get all the followers that use physics
			int l = profile.bones.Length;

			rbFollowers = new BoneFollower[l];
			
			for (int i = 0; i < l; i++) {
				HumanBodyBones bone = ragdoll.ragdollProfile.bones[i].bone;
				rbFollowers[i] = new BoneFollower(bone, ragdoll.GetRigidbody(bone), animator.GetBoneTransform(bone), ref jointDrive);
			}

			//get all the followers without rididbodies
			List<Follower> nonRbFollowersL = new List<Follower>();

			Transform[] allRags = ragdoll.RootBone().GetComponentsInChildren<Transform>();
			Transform[] allMasters = masterHips.GetComponentsInChildren<Transform>();
			
			if (allMasters.Length != allRags.Length) {
				Debug.LogError("children list different sizes for ragdoll and master");
			}
			
			for (int i = 0; i < allRags.Length; i++) {
				if (!allRags[i].GetComponent<Rigidbody>())
					nonRbFollowersL.Add(new Follower(allRags[i], allMasters[i]));
			}
			nonRbFollowers = nonRbFollowersL.ToArray();

			//get all followers in a single array
			int nonRBFollowersLength = nonRbFollowers.Length;
			allFollowers = new Follower[nonRBFollowersLength + rbFollowers.Length];
			for (int i = 0; i < nonRBFollowersLength; i++) {
				allFollowers[i] = nonRbFollowers[i];	
			}
			for (int i = 0; i < rbFollowers.Length; i++) {
				allFollowers[nonRBFollowersLength + i] = rbFollowers[i];	
			}
			
			EnableJointLimits(false);
			SetFollowValues(profile.maxForce, profile.maxJointTorque);
		}

		void CheckForForwardCalculation () {
			// Should have been done in Awake but mecanim does a strange initial rotation for some models
			if (!calculatedRootboneToForward) {
				if (i == 2) {
					// Relative orientation of ragdollRootBone to ragdoll transform
					rootboneToForward = Quaternion.Inverse(masterHips.rotation) * transform.rotation; 
					calculatedRootboneToForward = true;
				}
				i++;
			}
		}
	
		void FixedUpdate ()		
		{
			if (!profile) {
				return;
			}
			if (!ragdoll) {
				return;
			}

			
			UpdateLoop(Time.fixedDeltaTime);
		}
		void UpdateLoop(float deltaTime)
		{
			CheckForForwardCalculation();
		
			switch (state) {
		
			case RagdollState.Ragdolled:
				// Lerp force to zero from residual values
				if (currentMaxForce != 0 || currentMaxJointTorque != 0) {
					float speed = profile.fallLerp * deltaTime;
					SetFollowValues(Mathf.Lerp(currentMaxForce, 0, speed), Mathf.Lerp(currentMaxJointTorque, 0, speed));
				}

				//if not dead
				if (ragdoll.RootRigidbody().velocity.sqrMagnitude < profile.settledSpeed * profile.settledSpeed) {
					StartGetUp();
				}
				break;
			}
			
			RagdollFollowMasters(deltaTime);
		}

		// update rigidbody followers so ragdoll follows animated master
		void RagdollFollowMasters (float deltaTime) {
			float reciDeltaTime = 1f / deltaTime;
			for (int i = 0; i < rbFollowers.Length; i++) {
				rbFollowers[i].DoFollow(profile.PForce, profile.DForce, currentMaxForce, currentMaxJointTorque, reciDeltaTime, profile.bones[i], jointDrive);
			}
		}

		public class Follower {
			public Transform slave, master;
			Quaternion savedRotation;
			Vector3 savedPosition;

			public void SaveRagdollValues (bool doWorldSpace) {
				if (doWorldSpace) {
					savedRotation = slave.rotation;
					savedPosition = slave.position;
				}
				else {
					savedRotation = slave.localRotation;
				}
			}
			public void LerpFromSavedPositionToAnimatedPosition (float t, bool doWorldSpace) {
				if (doWorldSpace) {
					slave.position = Vector3.Lerp(savedPosition, master.position, t);
					slave.rotation = Quaternion.Slerp(savedRotation, master.rotation, t);
				}
				else {
					slave.localRotation = Quaternion.Slerp(savedRotation, master.localRotation, t);
				}
			}
			public Follower(Transform slave, Transform master) {
				this.slave = slave;
				this.master = master;
			}
		}

		public class BoneFollower : Follower {
			public float runtimeMultiplier = 1;
			Vector3 originalRBPosition, forceLastError;
			Quaternion startLocalRotation, localToJointSpace;
			ConfigurableJoint joint;
			public Rigidbody boneRB;
			float lastJointTorque;

			public BoneFollower(HumanBodyBones bone, Rigidbody boneRB, Transform master, ref JointDrive jointDrive) 
				: base(boneRB.transform, master)
			{
				this.boneRB = boneRB;
				
				joint = slave.GetComponent<ConfigurableJoint>();
				if (joint) {
					localToJointSpace = Quaternion.LookRotation(Vector3.Cross (joint.axis, joint.secondaryAxis), joint.secondaryAxis);
					startLocalRotation = slave.localRotation * localToJointSpace;
					localToJointSpace = Quaternion.Inverse(localToJointSpace);
					
					jointDrive = joint.slerpDrive;
					joint.slerpDrive = jointDrive;
				}
				originalRBPosition = Quaternion.Inverse(boneRB.rotation) * (boneRB.worldCenterOfMass - boneRB.position); 		
			}

	
			public void DoFollow (float PForce, float DForce, float maxForce, float maxJointTorque, float reciDeltaTime, RagdollControllerProfile.BoneProfile boneProfile, JointDrive jointDrive){
				
				Vector3 forceError = Vector3.zero;
				if (boneProfile.inputForce != 0 && maxForce != 0 && runtimeMultiplier != 0) {
					// Force error
					forceError = (master.position + master.rotation * originalRBPosition) - boneRB.worldCenterOfMass;
					// Calculate and apply world force
					Vector3 force = PDControl (PForce * boneProfile.inputForce, DForce, forceError, ref forceLastError, maxForce, boneProfile.maxForce * runtimeMultiplier, reciDeltaTime);
					boneRB.AddForce(force, ForceMode.VelocityChange);
				}
				forceLastError = forceError;
						
				if (joint) { 

					float jointTorque = maxJointTorque * boneProfile.maxTorque * runtimeMultiplier;
					if (jointTorque != lastJointTorque) {

						jointDrive.positionSpring = jointTorque;
						joint.slerpDrive = jointDrive;
				
						lastJointTorque = jointTorque;
					}
								
					if (jointTorque != 0) {
						joint.targetRotation = localToJointSpace * Quaternion.Inverse(master.localRotation) * startLocalRotation;
					}
				}
			}
			static Vector3 PDControl (float P, float D, Vector3 error, ref Vector3 lastError, float maxForce, float weight, float reciDeltaTime) // A PD controller
			{
				// theSignal = P * (theError + D * theDerivative) This is the implemented algorithm.
				Vector3 signal = P * (error + D * ( error - lastError ) * reciDeltaTime);
				return Vector3.ClampMagnitude(signal, maxForce * weight);
			}

			public void EnableJointLimits (ConfigurableJointMotion jointLimits) {
				if (joint) {
					joint.angularXMotion = joint.angularYMotion = joint.angularZMotion = jointLimits;
				}
			}
		}
	}
}