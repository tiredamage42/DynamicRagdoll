using UnityEngine;
using System.Collections.Generic;

//jumping forward when running? try lowering max force...

/*
	Script for handling when to ragdoll, and switching between ragdolled and animations

	attach this script to the main character


	hides ragdoll and makes it kinematic while teleporting it around to fit animation

	when ragdoll is initiated, 
		- 	we turn on the rigidbodies (kinematic = false) but keep gravity off

		-	the ragdoll begins to follow the animations via physics
			for a specified number of frames in order to calculate the velocities of the limbs
			during the animation

	After velocity calculation frames:

		-	turn on gravity for the ragdoll

		-	adjust all teh secondary transforms on the ragdoll to match the character
			(fingers and other non rigidbody parts)

		-	switch the renderrs to show the ragdoll that has the velocity of the animations


	that way we can have the leftover physics from the animation follow, without having to 
	worry too much about how closely the rigidbodies follow the actual animation
	(since we only use them when going ragdoll)

	when transitioning back to an animated state:
		
		- we play a get up animation

		- store the positions and rotations of teh ragdolled bones
		
		- turn off physics for the ragdoll
		
		- lerp the positions and rotations from their ragdolled positions
		  to the animated positions (in late update)
		
		- when the blend lerp reaches 1, disable the ragdoll renderers
		  and enable the original master character model 
		  (which should still be playing the get up animation)
*/
namespace DynamicRagdoll {

	[RequireComponent(typeof(Animator))]
	public class RagdollController : MonoBehaviour
	{
		public enum RagdollState { 
			Ragdolled=0, TeleportMasterToRagdoll=1, BlendToAnimated=2, Animated=3, CalculateAnimationVelocity=4,
		}
		
		public Ragdoll ragdoll;
		public RagdollControllerProfile profile;	
		public bool noGetUp;
		[HideInInspector] public RagdollState state = RagdollState.Animated;
		
		
		Animator animator;
		Transform masterHips;				
		
		// Rotation of ragdollRootBone relative to transform.forward
		Quaternion rootboneToForward;
		byte i;
		int calculatedAnimationVelocityFrames;

					
		Renderer[] masterRenderers;
		AnimatorCullingMode originalAnimatorCullingMode;

		float currentMaxForce, currentMaxJointTorque, ragdollPhaseStartTime;
			
		JointDrive jointDrive = new JointDrive();
		PhysicalBoneTracker[] rbFollowers;
		BoneTracker[] nonRbFollowers, allFollowers;
		bool skipFrame, calculatedRootboneToForward;
		bool controllerInvalid { get { return profile == null || ragdoll == null; } }
		

		//or set up system to callback when animation is done
		const float getupTime = 3;
		public bool isGettingUp { get { return Time.time - ragdollPhaseStartTime < getupTime; } }



		void EnableRenderers (bool masterEnabled, bool slaveEnabled) {
			for (int i = 0; i < masterRenderers.Length; i++)
				masterRenderers[i].enabled = masterEnabled;

			ragdoll.EnableRenderers(slaveEnabled);
		}

		void SetFollowValues (float force, float jointTorque) {
			currentMaxForce = force;
			currentMaxJointTorque = jointTorque;
		}

		//teleports secondary non rigidbody transforms on the ragdoll to their
		//corresponding local rotations on the master 
		//(for fingers and other stuff that's only important when the ragdoll model is actually showing)
		void TeleportSecondaryRagdollTransforms () {
			for (int i = 0; i < nonRbFollowers.Length; i++) {
				if (!nonRbFollowers[i].isPhysicsParent) {
					nonRbFollowers[i].Teleport();
				}
			}
		}

		void FinishGoRagdoll () {
			
			// The initial strength immediately after the impact
			SetFollowValues(profile.residualForce, profile.residualJointTorque);
			
			//enable gravity
			ragdoll.UseGravity(true);

			TeleportSecondaryRagdollTransforms();

			//do delayed physics hits
			OnPhysicsHits();
			
			//turn on ragdoll renderers, disable master renderers
			EnableRenderers(false, true);	

			ChangeRagdollState(RagdollState.Ragdolled);
		}

		void ChangeRagdollState (RagdollState newState) {
			state = newState;
			//store the state change time
			ragdollPhaseStartTime = Time.time;
		}
		

		//we need to store any physics calculations to be called after we calculate the animation velocities
		//that way they affect the ragdoll bones when actually ragdolled
		HashSet<System.Action> physicsHits = new HashSet<System.Action>();
		public void StorePhysicsHit (System.Action hitCallback) {
			physicsHits.Add(hitCallback);
		}
		void OnPhysicsHits () {
			foreach (var ph in physicsHits) {
				ph();
			}
			physicsHits.Clear();
		}


		//call to start the ragdoll process
		public void GoRagdoll (){

			if (!profile) {
				Debug.LogWarning("No Controller Profile on " + name);
				return;
			}
			if (!ragdoll) {
				Debug.LogWarning("No Ragdoll for " + name + " to control...");
				return;
			}
			
			if (state == RagdollState.Ragdolled || state == RagdollState.CalculateAnimationVelocity)
				return;

			//set the physics follow values
			SetFollowValues(profile.maxForce, profile.maxJointTorque);
			
			//dont skip first frame of velocity calculations 
			//(or maybe not, need to check this)
			skipFrame = false;

			//enable physics on the ragdoll
			ragdoll.SetKinematic(false);
			
			//dont use gravity though, it's not needed as the ragdoll isnt being rendered yet
			ragdoll.UseGravity(false);
			
			// reset the ragdoll rigidbody followers errors to 0 so we dont have
			// any leftover forces from a previous ragdolling
			for (int i = 0; i < Ragdoll.bonesCount; i++) {
				rbFollowers[i].ResetError();	
			}

			//wait a few fixed frames in order for the bone trackers to calculate the velocity of the current animations
			//then actually go ragdoll
			calculatedAnimationVelocityFrames = 0;

			ChangeRagdollState(RagdollState.CalculateAnimationVelocity);
		}

		//Transition from ragdolled to animated through the Blend state
		void StartGetUp () {

			//disable physics affecting ragdoll
			ragdoll.SetKinematic(true); 
			
			// master renderers are disabled, but animation needs to play regardless
			// so dont cull
			animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

			//save the ragdoll positions
			for(int i = 0; i < allFollowers.Length; i++) {
				allFollowers[i].SaveSlaveValues();
			}
			
			Vector3 rootBoneForward = ragdoll.RootBone().rotation * rootboneToForward * Vector3.forward;
			// Check if ragdoll is lying on its back or front, then transition to getup animation		
			bool onBack = Vector3.Dot(rootBoneForward, Vector3.down) < 0f; 
			
			//play get up animation
			animator.SetTrigger(onBack ? "BackTrigger" : "FrontTrigger");

			ChangeRagdollState(RagdollState.TeleportMasterToRagdoll);

		}

		// Here the master gets reorientated to the ragdoll 
		// which could have ended its fall in any direction and position
		void TeleportMasterToRagdoll () {		
			Transform ragHips = ragdoll.RootBone().transform;

			//calculate the rotation for the master root object
			Quaternion rotation = ragHips.rotation * Quaternion.Inverse(masterHips.rotation) * transform.rotation;
			
			Vector3 fwd = rotation * Vector3.forward;
			fwd.y = 0;
			rotation = Quaternion.LookRotation(fwd);

			//calculate the position for the master root object				
			Vector3 position = transform.position + (ragHips.position - masterHips.position);
			
			//Now cast a ray from the computed position downwards and find the highest hit that does not belong to the character 
			RaycastHit hit;
			if (Physics.Raycast(new Ray(position + Vector3.up, Vector3.down), out hit, 5, profile.checkGroundMask)) {
				position.y = hit.point.y;
			}
			else {
				position.y = 0;
			}
							
			//set the position and rotation
			transform.rotation = rotation;
			transform.position = position;

			ChangeRagdollState(RagdollState.BlendToAnimated);
		}

		bool HandleBlendToAnimation () {
			float timeSinceBlendStart = Time.time - ragdollPhaseStartTime;
			
			//compute the ragdoll blend amount in the range 0...1
			float blendT = timeSinceBlendStart / profile.blendTime;
			if (blendT > 1)
				blendT = 1;
			
			//In LateUpdate(), Mecanim has already updated the body pose according to the animations. 
			//lerp the position of the hips and slerp all the rotations towards the ones stored when ending the ragdolling
		
			for (int i = 0; i < allFollowers.Length; i++) {
				allFollowers[i].LerpFromSavedTowardsMaster(blendT);
			}

			return blendT == 1;
		}

		
		void OnEndBlendToAnimation () {

			ragdoll.UseGravity(false);

			//reset culling mode
			animator.cullingMode = originalAnimatorCullingMode;
			
			//turn off ragdoll renderers, enable master renderers
			EnableRenderers(true, false);

			ChangeRagdollState(RagdollState.Animated);
		}

		/*
			Wait until transition to getUp animation is done,
			so the animation is lying down before teleporting 
			the master to the ragdoll rotation and position
		*/
		bool HandleWaitForMasterTeleport () {
			return Time.time - ragdollPhaseStartTime >= profile.orientateDelay;
		}
		void LateUpdate()
		{
			if (controllerInvalid) {
				return;	
			}

			switch (state) {
			case RagdollState.TeleportMasterToRagdoll:
				if (HandleWaitForMasterTeleport()) {
					TeleportMasterToRagdoll();
				}
				break;
			case RagdollState.BlendToAnimated:
				if (HandleBlendToAnimation()) {
					OnEndBlendToAnimation();
				}
				break;
			}
		}


		PhysicalBoneTracker[] InitializePhysicsTrackers () {
			PhysicalBoneTracker[] trackers = new PhysicalBoneTracker[Ragdoll.bonesCount];
			for (int i = 0; i < Ragdoll.bonesCount; i++) {
				trackers[i] = new PhysicalBoneTracker(ragdoll.GetBone(Ragdoll.usedBones[i]), animator.GetBoneTransform(Ragdoll.usedBones[i]), ref jointDrive);
			}
			return trackers;
		}

		BoneTracker[] InitializeTransformTrackers () {
			
			Transform[] allRags = ragdoll.RootBone().transform.GetComponentsInChildren<Transform>();
			Transform[] allMasters = masterHips.GetComponentsInChildren<Transform>();
			
			if (allMasters.Length != allRags.Length) {
				Debug.LogError("children list different sizes for ragdoll and master");
				return null;
			}
			
			List<BoneTracker> nonRbFollowersL = new List<BoneTracker>();
			for (int i = 0; i < allRags.Length; i++) {
				if (!allRags[i].GetComponent<Rigidbody>())
					nonRbFollowersL.Add(new BoneTracker(allRags[i], allMasters[i], false, allRags[i].GetComponentInChildren<Rigidbody>() != null));
			}
			return nonRbFollowersL.ToArray();
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

			
			//get all the master renderers to switch off when going ragdoll
			masterRenderers = GetComponentsInChildren<Renderer>();
		
			animator = GetComponent<Animator>();
			
			//store original culling mode
			originalAnimatorCullingMode = animator.cullingMode;
			
			masterHips = animator.GetBoneTransform(HumanBodyBones.Hips);
		
			//get all the followers that use physics
			rbFollowers = InitializePhysicsTrackers();

			//get all the followers without rididbodies
			nonRbFollowers = InitializeTransformTrackers();
			
			//get all followers in a single array
			int nonRBFollowersLength = nonRbFollowers.Length;
			allFollowers = new BoneTracker[nonRBFollowersLength + Ragdoll.bonesCount];
			for (int i = 0; i < nonRBFollowersLength; i++)
				allFollowers[i] = nonRbFollowers[i];	
			for (int i = 0; i < Ragdoll.bonesCount; i++)
				allFollowers[nonRBFollowersLength + i] = rbFollowers[i];	
			
			ResetToAnimated();
		}

		void ResetToAnimated () {
			state = RagdollState.Animated;
			EnableRenderers(true, false);
			ragdoll.SetKinematic(true);
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

				// Lerp follow force to zero from residual values
				if (currentMaxForce != 0 || currentMaxJointTorque != 0) {
					float speed = profile.fallLerp * deltaTime;
					SetFollowValues(Mathf.Lerp(currentMaxForce, 0, speed), Mathf.Lerp(currentMaxJointTorque, 0, speed));
				}


				if (noGetUp == false) {
					//if we've spent enough time ragdolled
					if (Time.time - ragdollPhaseStartTime > profile.ragdollMinTime) {
						//if we're settled start get up
						if (ragdoll.RootBone().rigidbody.velocity.sqrMagnitude < profile.settledSpeed * profile.settledSpeed) {
							StartGetUp();
						}
					}
				}
				break;

			case RagdollState.Animated:

				// teleport all ragdoll (and ragdoll parents) in order to match animation
				// no need for secondary transforms since the ragdoll isnt being shown
				for (int i = 0; i < allFollowers.Length; i++) {
					if (allFollowers[i].isPhysicsParent) {
						allFollowers[i].Teleport();
					}
				}	
			 	break;
			}

			if (state == RagdollState.CalculateAnimationVelocity || state == RagdollState.Ragdolled) {
				HandlePhysicsFollow(deltaTime);


				if (state == RagdollState.CalculateAnimationVelocity) {

					//wait a few fixed frames in order for the bone trackers to calculate the velocity of the current animations
					//then actually go ragdoll
					calculatedAnimationVelocityFrames++;
					if (calculatedAnimationVelocityFrames >= profile.calculateVelocityFrames) {

						calculatedAnimationVelocityFrames = 0;
						FinishGoRagdoll();
					}
				}
			}
			
		}

		void HandlePhysicsFollow (float deltaTime) {
			
			//skip frame to avoid force error for follow being 0 (makes ragdoll drop straight down)
			if (skipFrame) {
				skipFrame = false;
				return;
			}
				
			if (state != RagdollState.Ragdolled) {

				/*
					update position of non rigidbodied transforms on ragdoll model 
					that are parents of rigidbody bones.
					this eliminates large errors between bone and master positions that lead to huge jumps
				*/
				if (profile.followRigidbodyParents) {
					for (int i = 0; i < nonRbFollowers.Length; i++) {
						if (nonRbFollowers[i].isPhysicsParent) {
							nonRbFollowers[i].Teleport();
						}
					}	
				}
			}
			
			// Use physics to move the bones to where the corresponding master bone is
			float reciprocalDeltaTime = 1f / deltaTime;
			
			for (int i = 0; i < Ragdoll.bonesCount; i++) {
				rbFollowers[i].MoveBoneToMaster(profile, currentMaxForce, currentMaxJointTorque, reciprocalDeltaTime, profile.bones[i], jointDrive);
			}

			//only skip frames when following animation
			skipFrame = profile.skipFrames && state != RagdollState.Ragdolled;
		}
	}
}