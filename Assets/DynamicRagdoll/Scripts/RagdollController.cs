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
		PhysicalBoneTracker[] rbFollowers;
		BoneTracker[] nonRbFollowers, allFollowers;
		
		
		//or set up system to callback when animation is done
		const float getupTime = 3;
		public bool isGettingUp { get { return Time.time - ragdollPhaseStartTime < getupTime; } }

		bool controllerInvalid { get { return profile == null || ragdoll == null; } }

		void SetKinematic(bool value) {
			for (int i = 0; i < rbFollowers.Length; i++) {
				rbFollowers[i].bone.rigidbody.isKinematic = value;
				rbFollowers[i].bone.rigidbody.detectCollisions = !value;
			}
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

			//store the state change time
			ragdollPhaseStartTime = Time.time; 

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
				Transform master = allFollowers[i].master;
				Transform slave = allFollowers[i].slave;
				bool doWorldSpace = master == masterHips;

				if (doWorldSpace) {
					allFollowers[i].savedPosition = slave.position;
				}

				allFollowers[i].savedRotation = doWorldSpace ? slave.rotation : slave.localRotation;
			}
			
			Vector3 rootBoneForward = ragdoll.RootBone().transform.rotation * rootboneToForward * Vector3.forward;
			// Check if ragdoll is lying on its back or front, then transition to getup animation		
			bool onBack = Vector3.Dot(rootBoneForward, Vector3.down) < 0f; 
			
			//play get up animation
			animator.SetTrigger(onBack ? "BackTrigger" : "FrontTrigger");
			
		}

		// Here the master gets reorientated to the ragdoll 
		//which could have ended its fall in any direction and position
		void OrientateMaster () {		
			Transform ragHips = ragdoll.RootBone().transform;

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
				orientated = true;

				return false; //needs to skip this frame or there's a jitter
			}
			
			//compute the ragdoll blend amount in the range 0...1
			float blendT = Mathf.Clamp01(timeSinceRagdoll / profile.blendTime);
			
			//In LateUpdate(), Mecanim has already updated the body pose according to the animations. 
			//lerp the position of the hips and slerp all the rotations towards the ones stored when ending the ragdolling
			
			for(int i = 0; i < allFollowers.Length; i++) {
				Transform master = allFollowers[i].master;
				Transform slave = allFollowers[i].slave;
				
				//world position and rotation is interpolated for the hips, 
				bool doWorldSpace = master == masterHips;
				if (doWorldSpace) {
					slave.position = Vector3.Lerp(allFollowers[i].savedPosition, master.position, blendT);
					slave.rotation = Quaternion.Slerp(allFollowers[i].savedRotation, master.rotation, blendT);
				}
				//local rotation is interpolated for all other body parts
				else {
					slave.localRotation = Quaternion.Slerp(allFollowers[i].savedRotation, master.localRotation, blendT);
				}
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
			if (controllerInvalid) {
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
			int l = Ragdoll.ragdollUsedBones.Length;// profile.bones.Length;

			rbFollowers = new PhysicalBoneTracker[l];
			for (int i = 0; i < l; i++) {
				rbFollowers[i] = new PhysicalBoneTracker(ragdoll.GetBone(Ragdoll.ragdollUsedBones[i]), animator.GetBoneTransform(Ragdoll.ragdollUsedBones[i]), ref jointDrive);
			}

			//get all the followers without rididbodies
			List<BoneTracker> nonRbFollowersL = new List<BoneTracker>();

			Transform[] allRags = ragdoll.RootBone().transform.GetComponentsInChildren<Transform>();
			Transform[] allMasters = masterHips.GetComponentsInChildren<Transform>();
			
			if (allMasters.Length != allRags.Length) {
				Debug.LogError("children list different sizes for ragdoll and master");
			}
			
			for (int i = 0; i < allRags.Length; i++) {
				if (!allRags[i].GetComponent<Rigidbody>())
					nonRbFollowersL.Add(new BoneTracker(allRags[i], allMasters[i]));
			}
			nonRbFollowers = nonRbFollowersL.ToArray();

			//get all followers in a single array
			int nonRBFollowersLength = nonRbFollowers.Length;
			allFollowers = new BoneTracker[nonRBFollowersLength + rbFollowers.Length];
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
			if (controllerInvalid) {
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
				
				//if we've spent enough time ragdolled
				if (Time.time - ragdollPhaseStartTime > profile.ragdollMinTime) {
				
					if (ragdoll.RootBone().rigidbody.velocity.sqrMagnitude < profile.settledSpeed * profile.settledSpeed) {
						StartGetUp();
					}
				}
				
				break;
			}
			
			MoveRagdollToMaster(deltaTime);
		}

		// update rigidbody followers so ragdoll follows animated master
		void MoveRagdollToMaster (float deltaTime) {
			float reciDeltaTime = 1f / deltaTime;
			for (int i = 0; i < rbFollowers.Length; i++) {
				rbFollowers[i].MoveBoneToMaster(profile, currentMaxForce, currentMaxJointTorque, reciDeltaTime, profile.bones[i], jointDrive);
			}
		}
	}
}