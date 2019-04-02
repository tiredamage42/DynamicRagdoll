using UnityEngine;
using System.Collections.Generic;

//jumping forward when running? try lowering max force...

/*
	Script for handling when to ragdoll, and switching between ragdolled and animations

	attach this script to the main character


	hides ragdoll and makes it kinematic while teleporting it around to fit animation


//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	when ragdoll is initiated, 

		When using the new velocity calculation method:

			-	we store all the current positions of teh animated character

			- 	we wait a frame to have a calculated velocity to set in the next physics update
		
		when using the PD controller method:

			- 	we turn on the rigidbodies (kinematic = false) but keep gravity off

			-	the ragdoll begins to follow the animations via physics
				for a specified number of frames in order to calculate the velocities of the limbs
				during the animation

//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	After velocity calculation frames:
		When using the new velocity calculation method:

			-	adjust all the transforms on the ragdoll to match their masters

			-	turn on all physics for the ragdoll


		when using the PD controller method:

			-	turn on gravity for the ragdoll

			-	adjust all teh secondary transforms on the ragdoll to match the character
				(fingers and other non rigidbody parts)

		-	switch the renderrs to show the ragdoll that has the velocity of the animations

		- 	call any delayed physics:
				Since physics is delayed until after we go ragdoll,
				any physics forces or calculations performed on the ragdoll (such as bullet hits)
				have to be saved as delegates. 
				
				Example:

					Rigidbody ragdollRigidbody = ...; //

					System.Action addBulletForce = () => {
						Vector3 force = shootDirection * (bulletForce/ Time.timeScale);
						ragdollRigidbody.AddForceAtPosition(force, point, ForceMode.VelocityChange); 
					};

					ragdollController.StorePhysics(executePhysics);
					ragdollController.GoRagdoll();

//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
					 
						
	now we can have the leftover physics from the animation follow, without having to 
	worry too much about how closely the rigidbodies follow the actual animation
	(since we only use them when going ragdoll)

	after going complete ragdoll we continue following the animated (but now invisible) master
	character, either by setting the animated velocities, or adding forces using the PD controller method.
	
	we degenerate the following at a defined speed (which can be set at runtime)

		ragdollController.SetFallSpeed (running ? 2 : 5);
		ragdollController.GoRagdoll ();


	this will give us a smooth transition into giving unity complete physical control 
	over the ragdoll

//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	Bone Decay
		when using the velocity set method (default method), we can use slower fall decays
		which in my opinion look better, the downside is that by setting teh velocity directly,
		we lose any external forces we had set towards the beginning of the fall

		in order to avoid this, set the bone decay for a bone to a value in the range (0,1).
		
		0 means it follows the animation fully (normal decay)
		1 means it ignores the animation completely and goes by the bones normal velocity

		bone decay for the bone's neighbors (which can be edited below) can be set as well.

		Example:
			if we just shot the character's head:

			float mainDecay = 1f;
			float neighborDecay = .75f;

			Transform ragdollHeadTransform = ...; 
			
			ragdollController.SetBoneDecay(ragdollHeadTransform, mainDecay, neighborDecay);
			
			// alternatively...
			// ragdollController.SetBoneDecay(HumanBodyBones.Head, mainDecay, neighborDecay);
							
			ragdollController.GoRagdoll ();


//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	when transitioning back to an animated state:
		
		- we play a get up animation

		- store the positions and rotations of the ragdolled bones
		
		- turn off physics for the ragdoll
		
		- linearly interpolate the positions and rotations from their ragdolled positions
		  to the animated positions (in late update)
		
		- when the blend lerp reaches 1, disable the ragdoll renderers
		  and enable the original master character model 
		  (which should still be playing the get up animation)

*/


namespace DynamicRagdoll {

	//Animator must have a humanoid avatar, figure out how to check this
	[RequireComponent(typeof(Animator))] 
	public class RagdollController : MonoBehaviour
	{
		public enum RagdollState { 
			Animated, 					//fully animated
			CalculateAnimationVelocity,	//calculating while still showing fully animated
			Ragdolled, 					//decaying fall, or complete ragdoll (TODO: make seperate states)
			TeleportMasterToRagdoll, 	//waiting for get up animation transition, to reorient invisible master
			BlendToAnimated, 			//blend into animated position
		}
		
		public Ragdoll ragdoll;
		public RagdollControllerProfile profile;	

		[Tooltip("Don't get up anymore... 'dead'")]
		public bool disableGetUp;
		
		[HideInInspector] public RagdollState state = RagdollState.Animated;

		/*
			currently returns if we just entered the animated state, 
			theoretically it's always when we just blended to get up
		
			... or set up system to callback when animation is done
		*/
		public bool isGettingUp { get { return state == RagdollState.Animated && timeSinceStateStart < getupTime; } }
		const float getupTime = 2.5f;



		float timeSinceStateStart { get { return Time.time - stateStartTime; } }

		
		Animator animator;
		Transform masterHips;				
		
		// Rotation of ragdollRootBone relative to transform.forward
		byte i;
		Quaternion rootboneToForward;
		bool calculatedRootboneToForward;
		
		int calculatedAnimationVelocityFrames;


		Renderer[] masterRenderers;
		AnimatorCullingMode originalAnimatorCullingMode;

		float stateStartTime = -100;
	
		bool controllerInvalid { get { return profile == null || ragdoll == null; } }

		float fallDecay;
		float fallSpeed = -1;

		Dictionary<HumanBodyBones, float> boneDecays = new Dictionary<HumanBodyBones, float>();

		/*
			Velocity calculation values
		*/
		Vector3[] animatedBoneVelocities, massCenterOffsets, lastAnimatedBonePositions;


		/*
			Configurable joint values for ragdoll bones
		*/
		static JointDrive jointDrive = new JointDrive();
		Quaternion[] startLocalRotations, localToJointSpaces;
		float[] lastTorques;


		/*
			PD Control values
		*/
		float maxForce, maxTorque;
		bool skipFrame;
		Vector3[] originalRBPositions, lastPDErrors;

		/*
			Set the fall decay speed for the next 'Ragdolling'
		*/
		public void SetFallSpeed (float fallSpeed) {
			this.fallSpeed = fallSpeed;
		}





		/*
			enable or disable renderers on the master, and the ragdoll
		*/
		void EnableRenderers (bool masterEnabled, bool ragdollEnabled) {
			for (int i = 0; i < masterRenderers.Length; i++)
				masterRenderers[i].enabled = masterEnabled;

			ragdoll.EnableRenderers(ragdollEnabled);
		}


		/*
			PD Control values
		*/
		void SetPDControllerValues (float force, float jointTorque) {
			maxForce = force;
			maxTorque = jointTorque;
		}

		
		/*
			Actually start using physics on the ragdoll
		*/
		void StartFallState () {
			

			if (!profile.usePDControl) {
				fallDecay = 1;

				//teleport the whole ragdoll to fit the master
				ragdoll.TeleportToTarget(Ragdoll.TeleportType.All);

				//enable physics for ragdoll 
				//(wasnt needed for calculation for velocity method)
				ragdoll.SetKinematic(false);
				
			}
			else {
				
				// The initial strength immediately after the impact
				SetPDControllerValues(profile.residualForce, profile.residualTorque);
		
				/*
					teleports secondary non rigidbody transforms on the ragdoll to their
					corresponding local rotations on the master 
				
					for fingers and other stuff that's only important when 
					the ragdoll model is actually showing
				*/
				ragdoll.TeleportToTarget(Ragdoll.TeleportType.SecondaryNonPhysicsBones);

				//enable gravity
				ragdoll.UseGravity(true);
			}
		
			//turn on ragdoll renderers, disable master renderers
			EnableRenderers(false, true);	

			// master renderers are disabled, but animation needs to play regardless
			// so dont cull
			animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

			//change the state
			ChangeRagdollState(RagdollState.Ragdolled);			
		}


		void ChangeRagdollState (RagdollState newState) {
			state = newState;
			//store the state change time
			stateStartTime = Time.time;
		}
		
		/*
			we need to store any physics calculations to be called after we calculate 
			the animation velocities and turn on the ragdoll
			
			that way they affect the ragdoll bones when actually ragdolled
		*/
		HashSet<System.Action> physicsCallbacks = new HashSet<System.Action>();
		public void StorePhysics (System.Action physicsCallback) {
			physicsCallbacks.Add(physicsCallback);
		}

		//maybe wait for fixed update
		void OnRagdollPhysics () {
			if (physicsCallbacks.Count > 0) {

				foreach (var physicsCallback in physicsCallbacks) {
					physicsCallback();
				}
				physicsCallbacks.Clear();
			}
		}

		/*
			call to start the ragdoll process
		*/
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


			if (!profile.usePDControl) {
				/*
					store start positions to begin calculating the velocity of the playing animation
				*/
				StoreAnimationStartPositions();
			}
			else {
				//set the pd values
				SetPDControllerValues(profile.maxForcePD, profile.maxTorquePD);
			
				//dont skip first frame of velocity calculations 
				//(or maybe not, need to check this)
				skipFrame = false;

				/*
					reset the PD errors to 0 so we dont have
					any leftover forces from a previous ragdolling
				*/
				ResetPDControllerErrors();

				//enable physics on the ragdoll
				ragdoll.SetKinematic(false);				
				//dont use gravity though, it's not needed as the ragdoll isnt being rendered yet
				ragdoll.UseGravity(false);
			}
				

			//wait a few frames in order to calculate the velocity of the current animations
			calculatedAnimationVelocityFrames = 0;

			ChangeRagdollState(RagdollState.CalculateAnimationVelocity);
		}


		/*
			Transition from ragdolled to animated through the Blend state
		*/
		void StartGetUp () {

			//reset bone fall decay modifiers			
			ResetBoneDecays();

			//set fall speed to use default
			SetFallSpeed(-1);
			
			//disable physics affecting ragdoll
			ragdoll.SetKinematic(true); 
			
			//save the ragdoll positions
			ragdoll.SaveSnapshot();
			
			Vector3 ragdollHipsFwd = ragdoll.RootBone().rotation * rootboneToForward * Vector3.forward;
			
			// Check if ragdoll is lying on its back or front
			bool onBack = Vector3.Dot(ragdollHipsFwd, Vector3.down) < 0f; 
			
			// play get up animation
			animator.SetTrigger(onBack ? "BackTrigger" : "FrontTrigger");

			ChangeRagdollState(RagdollState.TeleportMasterToRagdoll);
		}

		/*
			Here the master (this transform) gets reorientated to the ragdolls position
			which could have ended its fall in any direction and position
		*/
		void TeleportMasterToRagdoll () {		
			Transform ragdollHips = ragdoll.RootBone().transform;

			//calculate the rotation for the master root object (this)
			Quaternion rotation = ragdollHips.rotation * Quaternion.Inverse(masterHips.rotation) * transform.rotation;
			
			Vector3 fwd = rotation * Vector3.forward;
			fwd.y = 0;
			rotation = Quaternion.LookRotation(fwd);

			//calculate the position for the master root object (this)		
			Vector3 position = transform.position + (ragdollHips.position - masterHips.position);
			
			//Now cast a ray from the computed position downwards and find the floor
			RaycastHit hit;
			if (Physics.Raycast(new Ray(position + Vector3.up, Vector3.down), out hit, 5, profile.checkGroundMask)) {
				position.y = hit.point.y;
			}
						
			//set the position and rotation
			transform.rotation = rotation;
			transform.position = position;

			ChangeRagdollState(RagdollState.BlendToAnimated);
		}

		/*
			In LateUpdate(), Mecanim has already updated the body pose according to the animations. 
			blend form the saved 'snapshot' ragdolled positions into the animated positions
		*/
		bool HandleBlendToAnimation () {
			
			// compute the ragdoll blend amount in the range 0...1
			float blend = timeSinceStateStart / profile.blendTime;
		
			if (blend > 1)
				blend = 1;
			
			ragdoll.LoadSnapshot(1-blend, true);

			// return true if we're done blending
			return blend == 1;
		}
		
		void ResetToAnimated () {
			// reset culling mode to original
			animator.cullingMode = originalAnimatorCullingMode;
			
			// turn off ragdoll renderers, enable master renderers
			EnableRenderers(true, false);

			//change state to animated
			ChangeRagdollState(RagdollState.Animated);
		}


		/*
			Wait until transition to getUp animation is done,
			so the animation is lying down before teleporting 
			the master to the ragdoll rotation and position
		*/
		bool HandleWaitForMasterTeleport () {
			return timeSinceStateStart >= profile.orientateDelay;
		}


		/*
			teleport all ragdoll (and ragdoll parents) in order to match animation
			no need for secondary transforms since the ragdoll isnt being shown

			this way we can still 'shoot' or collide with the ragdoll
		*/
		void SimpleTeleportRagdollToMasterWhileAnimated () {
			ragdoll.TeleportToTarget(Ragdoll.TeleportType.PhysicsBonesAndParents);
		}

		void LateUpdate()
		{
			if (controllerInvalid) {
				return;	
			}
			InitializeForwardCalculation();
		

			switch (state) {
			
			case RagdollState.Animated: 
				/*
					TRY UNCOMMENTING HERE IF WE'RE HAVING TROUBLE WITH PHYSICS DETECTION 
					OF RAGDOLL NOT DETECTING IK CHANGES
				*/
				// SimpleTeleportRagdollToMasterWhileAnimated();
				break;
			
			

			case RagdollState.Ragdolled:
				HandleFallLerp(Time.deltaTime);
				break;

			case RagdollState.TeleportMasterToRagdoll:
				if (HandleWaitForMasterTeleport()) {
					TeleportMasterToRagdoll();
				}
				break;
			case RagdollState.BlendToAnimated:
				if (HandleBlendToAnimation()) {
					ResetToAnimated();
				}
				break;
			}
			
			if (!profile.usePDControl) {

				UpdateLoop(Time.deltaTime);
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
			
			animator = GetComponent<Animator>();
		
			// store original culling mode
			originalAnimatorCullingMode = animator.cullingMode;

			// store master hip bone
			masterHips = animator.GetBoneTransform(HumanBodyBones.Hips);
		
			// get all the master renderers to switch off when going ragdoll
			masterRenderers = GetComponentsInChildren<Renderer>();
					
			// set the ragdolls follow target (assumes same bone setup...)
			ragdoll.SetFollowTarget(animator);


			// initialize animation following
			InitializeJointFollowing();
			InitializeVelocitySetValues();
			InitializePDControl();
			

			// disable physics
			ragdoll.SetKinematic(true);
			ResetToAnimated();

			ResetBoneDecays();
		}

		/*
			initialize root bone forward calculation, for determining
			when ragdoll is on it's front or back (for getting up)
			
			Should have been done in Awake but mecanim does a strange initial rotation 
			for some models
		*/
		void InitializeForwardCalculation () {
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

			switch (state) {

			case RagdollState.Ragdolled:
				// do delayed physics on the ragdoll if any
				OnRagdollPhysics(); 

				if (disableGetUp == false) {
					//check the hips velocity to see if the ragdoll is still
					CheckForGetUp();
				}
				break;

			case RagdollState.Animated:
				/*
					maybe move this to late update if we're not hitting transforms 
					that were edited on ik, as that happens after fixed update...
				*/
				SimpleTeleportRagdollToMasterWhileAnimated();
				break;
			}
			

			if (profile.usePDControl) {

				UpdateLoop(Time.fixedDeltaTime);

			}
			else {
				/*
					if we're ragdolled, set the calculated animation velocities we calculated in
					Late Update to the ragdoll bones, also handle joint targets
				*/
				if (state == RagdollState.Ragdolled) {
					
					SetPhysicsVelocities(Time.fixedDeltaTime);
				}
			}
		}

		

		void HandleFallLerp (float deltaTime) {
			// Lerp follow force to zero from residual values
			float speed = (fallSpeed == -1 ? profile.fallDecaySpeed : fallSpeed) * deltaTime;
			
			if (maxForce != 0 || maxTorque != 0) {
				SetPDControllerValues(Mathf.Lerp(maxForce, 0, speed), Mathf.Lerp(maxTorque, 0, speed));
			}

			if (fallDecay != 0) {
				fallDecay = Mathf.Lerp(fallDecay, 0, speed);
			}
		}


		void CheckForGetUp () {
			//if we've spent enough time ragdolled
			if (Time.time - stateStartTime > profile.ragdollMinTime) {
				//if we're settled start get up
				if (ragdoll.RootBone().rigidbody.velocity.sqrMagnitude < profile.settledSpeed * profile.settledSpeed) {
					StartGetUp();
				}
			}
		}

		/*
			called in late update normally (in order to calculate animation velocities)

			in fixed update if using pd controller
		*/
		void UpdateLoop(float deltaTime)
		{
			
			if (state == RagdollState.CalculateAnimationVelocity || state == RagdollState.Ragdolled) {
				
				if (profile.usePDControl) {
					HandlePDControl(deltaTime);
				}
				else {
					CalculateAnimationVelocities(deltaTime);
				}

				if (state == RagdollState.CalculateAnimationVelocity) {
					/*
						wait a few frames in order to calculate the velocity of the current animations
						then actually go ragdoll
					*/
					
					calculatedAnimationVelocityFrames++;
					/*
						if we're using the velocity change method we just need one frame 
						to register the velocity to start with during falling
						(that calculation was just done above)

						if using the pd controller method, we need a few frames in order to build up
						and stabilize forces used to move the rigidbodies
					*/
					int targetFrames = profile.usePDControl ? profile.calculateVelocityFrames : 1;


					if (calculatedAnimationVelocityFrames >= targetFrames) {

						calculatedAnimationVelocityFrames = 0;
						StartFallState();
					}
				}
			}
		}


			

		void HandlePDControl (float deltaTime) {
			//skip frame to avoid force error for follow being 0 (makes ragdoll drop straight down)
			if (skipFrame) {
				skipFrame = false;
				return;
			}
				
			if (state == RagdollState.CalculateAnimationVelocity) {
				/*
					update position of non rigidbodied transforms on ragdoll model 
					that are parents of rigidbody bones.
					this eliminates large errors between bone and master positions that lead to huge jumps
				*/
				if (profile.followRigidbodyParents) {
					ragdoll.TeleportToTarget(Ragdoll.TeleportType.PhysicsParents);
				}
			}
			
			// Use physics to move the bones to where the corresponding master bone is
			float reciprocalDeltaTime = 1f / deltaTime;
			
			for (int i = 0; i < Ragdoll.physicsBonesCount; i++) {
				Ragdoll.Bone bone = ragdoll.GetPhysicsBone(Ragdoll.phsysicsHumanBones[i]);

				float inputForceMultiplier = profile.bones[i].inputForce;
				float maxForceMultiplier = profile.bones[i].maxForce;
				
				Vector3 forceError = Vector3.zero;

				if (inputForceMultiplier != 0 && maxForce != 0 && maxForceMultiplier != 0){
					
					// Force error
					forceError = (bone.followTarget.position + bone.followTarget.rotation * originalRBPositions[i]) - bone.rigidbody.worldCenterOfMass;
					// Calculate and apply world force
					Vector3 force = PDControl(profile.PForce * inputForceMultiplier, profile.DForce, forceError, lastPDErrors[i], maxForce * maxForceMultiplier, reciprocalDeltaTime);
					bone.rigidbody.AddForce(force, ForceMode.VelocityChange);					
				}
				lastPDErrors[i] = forceError;

				HandleJointFollow(bone, maxTorque * profile.bones[i].maxTorque, i);
			}

			//only skip frames when not showing ragdoll
			skipFrame = profile.skipFrames && state == RagdollState.CalculateAnimationVelocity;
		}

		static Vector3 PDControl (float P, float D, Vector3 error, Vector3 lastError, float maxForce, float reciprocalDeltaTime) 
		{
			// theSignal = P * (theError + D * theDerivative) This is the implemented algorithm.
			Vector3 signal = P * (error + D * ( error - lastError ) * reciprocalDeltaTime);
			
			float sqrMag = signal.sqrMagnitude;
			if (sqrMag > maxForce * maxForce) {
				return signal * (maxForce / Mathf.Sqrt(sqrMag));
				//return Vector3.ClampMagnitude(signal, max);
			}
			return signal;
		}



		void InitializeVelocitySetValues () {

			animatedBoneVelocities = new Vector3[Ragdoll.physicsBonesCount];
			massCenterOffsets = new Vector3[Ragdoll.physicsBonesCount];
			lastAnimatedBonePositions = new Vector3[Ragdoll.physicsBonesCount];
		
			for (int i = 0; i < Ragdoll.physicsBonesCount; i++) {
				Ragdoll.Bone bone = ragdoll.GetPhysicsBone(Ragdoll.phsysicsHumanBones[i]);

				//get the local center of mass offset of the ragdoll bone
				massCenterOffsets[i] = bone.transform.InverseTransformPoint(bone.rigidbody.worldCenterOfMass);
			}
		}

		void InitializePDControl () {
			originalRBPositions = new Vector3[Ragdoll.physicsBonesCount];
			lastPDErrors = new Vector3[Ragdoll.physicsBonesCount];
			for (int i = 0; i < Ragdoll.physicsBonesCount; i++) {
				//get the ragdoll's rigidbody
				Rigidbody rigidbody = ragdoll.GetPhysicsBone(Ragdoll.phsysicsHumanBones[i]).rigidbody;

				// store original offset
				originalRBPositions[i] = Quaternion.Inverse(rigidbody.rotation) * (rigidbody.worldCenterOfMass - rigidbody.position); 		
			}
		}



		void InitializeJointFollowing () {

			startLocalRotations = new Quaternion[Ragdoll.physicsBonesCount];
			localToJointSpaces = new Quaternion[Ragdoll.physicsBonesCount];
			lastTorques = new float[Ragdoll.physicsBonesCount];

			//skip first (hips dont have joints)
			for (int i = 1; i < Ragdoll.physicsBonesCount; i++) {

				//get the ragdoll's joint
				ConfigurableJoint joint = ragdoll.GetPhysicsBone(Ragdoll.phsysicsHumanBones[i]).joint;

				// set last torque to something that'll defineyl make it change
				lastTorques[i] = -1;

				//save rotation values for setting joint rotation
				localToJointSpaces[i] = Quaternion.LookRotation(Vector3.Cross (joint.axis, joint.secondaryAxis), joint.secondaryAxis);
				
				//startLocalRotations[i] = bone.followTarget.transform.localRotation * localToJointSpaces[i];
				startLocalRotations[i] = joint.transform.localRotation * localToJointSpaces[i];

				localToJointSpaces[i] = Quaternion.Inverse(localToJointSpaces[i]);
				
				//set drive
				jointDrive = joint.slerpDrive;
				joint.slerpDrive = jointDrive;
			}
		}
		
		


		
		
		/*
			store the first positions to start calculating the velocity of the master animation
		*/
		void StoreAnimationStartPositions () {
			for (int i = 0; i < Ragdoll.physicsBonesCount; i++) {	
				// the master bone
				Transform followTarget = ragdoll.GetPhysicsBone(Ragdoll.phsysicsHumanBones[i]).followTarget.transform;

				//get position (offset by ragdoll bone's rigidbody centor of mass) of the follow target
				lastAnimatedBonePositions[i] = CalculateBonePosition(followTarget, massCenterOffsets[i]);				
			}
		}

		/*
			the position of the bone,

			offset by the center of mass of the correspoding ragdoll rigidbody
		*/
		static Vector3 CalculateBonePosition(Transform bone, Vector3 centerOfMass) {
			return bone.position + (bone.rotation * centerOfMass);		
		}

		



		/*
			reset the pd controller errors to 0 so we dont have
			any leftover forces from a previous ragdolling
		*/	
		void ResetPDControllerErrors () {
			for (int i = 0; i < Ragdoll.physicsBonesCount; i++) {
				lastPDErrors[i] = Vector3.zero;
			}
		}


		
		/*
			Reset bone decays back to 0 for the next ragdolling
		*/
		void ResetBoneDecays () {
			for (int i = 0; i < Ragdoll.physicsBonesCount; i++) {
				boneDecays[Ragdoll.phsysicsHumanBones[i]] = 0;
			}
		}
		
		
		/*
			Set the bone decay for the bone (in the range 0...1)
			0 = follow animation normally
			1 = dont follow animation at all
		*/
		public void SetBoneDecay (HumanBodyBones bones, float decayValue, float neighborDecay) {
			// making additive, so in case we hit a bone twice in a ragdoll session
			// it doesnt reset with a lower value
			boneDecays[bones] += decayValue;

			if (neighborDecay > 0) {

				HashSet<HumanBodyBones> neighbors = GetNeighbors(bones);
				foreach (var n in neighbors) {
					SetBoneDecay(n, neighborDecay, 0);
				}
			}
		}


		/*
			Set the bone decay for the bone Transform (in the range 0...1)
			0 = follow animation normally
			1 = dont follow animation at all
		*/
		public void SetBoneDecay (Transform bone, float decayValue, float neighborDecay) {
			HumanBodyBones unityBone = GetBoneForTransform(bone);
			
			if (unityBone != HumanBodyBones.Jaw) {

				SetBoneDecay(unityBone, decayValue, neighborDecay);
			}
		}
		/*
			Get the unity bone type by the transform, 
			returns jaw if it's not part of the ragdoll
		*/
		HumanBodyBones GetBoneForTransform (Transform transform) {
			for (int i = 0; i < Ragdoll.physicsBonesCount; i++) {
				if (ragdoll.GetPhysicsBone(Ragdoll.phsysicsHumanBones[i]).transform == transform) {
					return Ragdoll.phsysicsHumanBones[i];
				}
			}
			return HumanBodyBones.Jaw;
		}

		/*
			Define which bones count as neighbors for other bones (for the bone decay system)

			still tweakign this....
		*/
		static HashSet<HumanBodyBones> GetNeighbors (HumanBodyBones bone) {
            switch (bone) {
                case HumanBodyBones.Hips:          
                    return new HashSet<HumanBodyBones>() { 
                        HumanBodyBones.Chest, 
                        HumanBodyBones.LeftUpperLeg, 
                        HumanBodyBones.RightUpperLeg,
                        
                        //do all upper if it looks like its hanging in the air too much
                        
						//HumanBodyBones.Head,
                        // HumanBodyBones.RightLowerLeg,
                        // HumanBodyBones.LeftLowerLeg,
            
                        // HumanBodyBones.LeftUpperArm,
                        // HumanBodyBones.RightUpperArm,

                        // HumanBodyBones.LeftLowerArm,
                        // HumanBodyBones.RightLowerArm,
                    };
                case HumanBodyBones.Chest:          
                    return new HashSet<HumanBodyBones>() { 
                        //HumanBodyBones.Hips, 
                        HumanBodyBones.Head, 
                        HumanBodyBones.LeftUpperArm, 
                        HumanBodyBones.RightUpperArm
                    };
                case HumanBodyBones.Head:           
                    return new HashSet<HumanBodyBones>() { 
                        HumanBodyBones.Chest, 
                        HumanBodyBones.LeftUpperArm, 
                        HumanBodyBones.RightUpperArm 
                    };

				// LOWER LEGS
                case HumanBodyBones.RightLowerLeg:  
                    return new HashSet<HumanBodyBones>() { 
                        HumanBodyBones.RightUpperLeg, HumanBodyBones.Hips ,

                        //do all upper if it looks like its hanging in the air too much
                        // HumanBodyBones.Chest, 
                        // HumanBodyBones.LeftUpperArm, 
                        // HumanBodyBones.RightUpperArm 
                    };
                case HumanBodyBones.LeftLowerLeg:   
                    return new HashSet<HumanBodyBones>() { 
                        HumanBodyBones.LeftUpperLeg, HumanBodyBones.Hips 

                        //do all upper if it looks like its hanging in the air too much
                        // HumanBodyBones.Chest, 
                        // HumanBodyBones.LeftUpperArm, 
                        // HumanBodyBones.RightUpperArm 
                    };

				// UPPER LEGS
                case HumanBodyBones.RightUpperLeg:  
                    return new HashSet<HumanBodyBones>() { 
                        HumanBodyBones.Hips, HumanBodyBones.RightLowerLeg ,

                        //do all upper if it looks like its hanging in the air too much
                        //HumanBodyBones.Chest, 
                        //HumanBodyBones.LeftUpperArm, 
                        //HumanBodyBones.RightUpperArm   
                    };
                case HumanBodyBones.LeftUpperLeg:   
                    return new HashSet<HumanBodyBones>() { 
                        HumanBodyBones.Hips, HumanBodyBones.LeftLowerLeg ,

                        //do all upper if it looks like its hanging in the air too much
                        //HumanBodyBones.Chest, 
                        //HumanBodyBones.LeftUpperArm, 
                        //HumanBodyBones.RightUpperArm 
                    };

				// LOWER ARMS
                case HumanBodyBones.RightLowerArm:  
                    return new HashSet<HumanBodyBones>() { 
                        HumanBodyBones.RightUpperArm, HumanBodyBones.Chest 
                    };
                case HumanBodyBones.LeftLowerArm:   
                    return new HashSet<HumanBodyBones>() { 
                        HumanBodyBones.LeftUpperArm, HumanBodyBones.Chest 
                    };

				// UPPER ARMS
                case HumanBodyBones.RightUpperArm:  
                    return new HashSet<HumanBodyBones>() { 
                        HumanBodyBones.Chest, HumanBodyBones.RightLowerArm,
                        HumanBodyBones.LeftUpperArm, 
                        HumanBodyBones.Head,
                    };
                case HumanBodyBones.LeftUpperArm:   
                    return new HashSet<HumanBodyBones>() { 
                        HumanBodyBones.Chest, HumanBodyBones.LeftLowerArm ,
                        HumanBodyBones.RightUpperArm, 
                        HumanBodyBones.Head,
                    };
            }
            return null;
        }


		/*
			returns number with the largest magnitude
		*/
		static float MaxAbs(float a, float b) {
			return Mathf.Abs(a) > Mathf.Abs(b) ? a : b;
		}
		/*
			returns a vector with the largest components of each (based on absolute value)
		*/
		static Vector3 MaxAbs (Vector3 a, Vector3 b) {
			return new Vector3(MaxAbs(a.x, b.x), MaxAbs(a.y, b.y), MaxAbs(a.z, b.z));
		}
		
		/*
			set the velocities we calculated in late update for each animated bone
			
			on the actual ragdoll
		*/
		void SetPhysicsVelocities (float deltaTime) {

			float maxVelocityForGravityAdd2 = profile.maxGravityAddVelocity * profile.maxGravityAddVelocity;


			float fallDecayCurveSample = 1f - fallDecay; //set up curves backwards... whoops
			
			// for each physics bone...
			for (int i = 0; i < Ragdoll.physicsBonesCount; i++) {
				HumanBodyBones unityBone = Ragdoll.phsysicsHumanBones[i];
				
				Ragdoll.Bone bone = ragdoll.GetPhysicsBone(unityBone);

				Vector3 ragdollBoneVelocty = bone.rigidbody.velocity;

				// get the manually set bone decay value
				float boneDecay = boneDecays[unityBone];
			
				/*
					calculate the force decay based on the overall fall decay and the bone profile's
				
					fall force decay curve
				*/

				float forceDecay = Mathf.Clamp01(profile.bones[i].fallForceDecay.Evaluate (fallDecayCurveSample));
				
				//subtract manual decay
				forceDecay = Mathf.Clamp01(forceDecay - boneDecay);

				// if we're still using some force to follow
				if (forceDecay != 0) {
					/*
						if animation velocity is below threshold magnitude, add some gravity to it
					*/
					float animationVelocityMag2 = animatedBoneVelocities[i].sqrMagnitude;				
					
					if (animationVelocityMag2 < maxVelocityForGravityAdd2) {

						animatedBoneVelocities[i].y = Physics.gravity.y * deltaTime;
					}

					/*
						if bone decay was manually set to make room for external velocities,
						
						use the most extreme component of each vector as the "target animated" velocity
					*/
					if (boneDecay != 0) {	

						animatedBoneVelocities[i] = MaxAbs(ragdollBoneVelocty, animatedBoneVelocities[i]);
					}

					// set the velocity on the ragdoll rigidbody (based on the force decay)
					bone.rigidbody.velocity = Vector3.Lerp(ragdollBoneVelocty, animatedBoneVelocities[i], forceDecay);
				}
					
			
				if (i != 0) {

					/*
						calculate the force decay based on the overall fall decay and the bone profile's
					
						fall force decay curve
					*/

					float torqueDecay = Mathf.Clamp01(profile.bones[i].fallTorqueDecay.Evaluate (fallDecayCurveSample));
					
					//subtract manual decay
					torqueDecay = Mathf.Clamp01(torqueDecay - boneDecay);

					/*
						handle joints target for the ragdoll joints
					*/
					HandleJointFollow(bone, profile.maxTorque * torqueDecay, i);
				}
						
			}
		}


		/* 
			Calculate the velocity (per bone) of the current playing animation

			called in LateUpdate since we're using the animated character for these
			calculations
		*/
		void CalculateAnimationVelocities (float deltaTime) {
			if (fallDecay != 0) {

				float reciprocalDeltaTime = 1f / deltaTime;
					
				// for each physics bone...
				for (int i = 0; i < Ragdoll.physicsBonesCount; i++) {
					HumanBodyBones unityBone = Ragdoll.phsysicsHumanBones[i];

					// the master bone
					Transform followTarget = ragdoll.GetPhysicsBone(unityBone).followTarget.transform;
					
					// the new position of the bone,
					Vector3 bonePosition = CalculateBonePosition(followTarget, massCenterOffsets[i]);
					
					
					Vector3 distance = bonePosition - lastAnimatedBonePositions[i];
					
					/*
						velocity = distance / time
					
						but multiplying is faster
					*/
					Vector3 boneVelocity = distance * reciprocalDeltaTime;

					animatedBoneVelocities[i] = boneVelocity;
					
					lastAnimatedBonePositions[i] = bonePosition;		
					
				}
			}
		}

		/*
			Set the bone's configurable joint target to the master local rotation
		*/
		void HandleJointFollow (Ragdoll.Bone bone, float torque, int boneIndex) {
			if (!bone.joint) 
				return;
					
			//setting joint torque every frame was slow, so check here if its changed
			if (torque != lastTorques[boneIndex]) {
				lastTorques[boneIndex] = torque;

				jointDrive.positionSpring = torque;
				bone.joint.slerpDrive = jointDrive;
			}

			//set joints target rotation		
			if (torque != 0) {
				Quaternion targetLocalRotation = bone.followTarget.transform.localRotation;
				bone.joint.targetRotation = localToJointSpaces[boneIndex] * Quaternion.Inverse(targetLocalRotation) * startLocalRotations[boneIndex];
			}	
		}
	}
}