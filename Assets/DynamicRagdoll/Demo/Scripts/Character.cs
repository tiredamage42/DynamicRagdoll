using UnityEngine;
namespace DynamicRagdoll.Demo
{
	/*

		a demo character control script that shows how to interact with 
		the ragdoll controller component
		
		moves the character around using the animator
	*/

	[RequireComponent(typeof(RagdollController))]
	public class Character : MonoBehaviour
	{
		/*
			fall speeds to set for the ragdoll controller
		*/
		[Header("Fall Decay Speeds")]
		public float idleFallSpeed = 3;
		public float walkFallSpeed = 1;
		public float runFallSpeed = 1;

		[Header("Movement")]
		public UpdateMode moveUpdate = UpdateMode.Update;

		[Tooltip("Maximum gravity velocity")]
		public float maxGravity = -1;
		public LayerMask groundLayerMask;

		[Tooltip("How much time to hang in the air and extend being 'grounded'")]
		public float coyoteTime = .2f;

		[Header("Falling Ragdoll")]
		[Tooltip("How far we have to drop when not gorunded in order to go ragdoll from a fall")]
		public float fallDistance = 3f;

		[Tooltip("How much time to wait until initiating ragdoll after not being grounded and falling from high enough")]
		public float fallRagdollTime = .2f;

		[HideInInspector] public RagdollController ragdollController;
		public bool overrideControl { get { return ragdollController.state != RagdollControllerState.Animated || ragdollController.isGettingUp; } }


		public float currentSpeed;
		bool grounded, inCoyoteHang;
		float floorY, currentGravity, lastGroundHitTime, lastGroundTime;
		Animator anim;		
		Vector3 animDelta;
		CharacterController characterController;


		/*
			set movemnt speed and update the animator
		*/
		public void SetMovementSpeed(float speed) {
			currentSpeed = speed;
			anim.SetFloat("Speed", currentSpeed);
		}

		void Awake ()
		{
			anim = GetComponent<Animator>();
			ragdollController = GetComponent<RagdollController>();
			characterController = GetComponent<CharacterController>();
		}

		void OnAnimatorMove ()
		{
			animDelta = anim.deltaPosition;
		}

		void Update () 
		{
			//cehck if we started getting up
			if (ragdollController.state == RagdollControllerState.BlendToAnimated) {
				//set zero speed
				if (currentSpeed != 0) {
					SetMovementSpeed(0);
				}
			}

			//set the ragdolls fall speed based on our speed
			ragdollController.SetFallSpeed(currentSpeed == 0 ? idleFallSpeed : (currentSpeed == 1 ? walkFallSpeed : runFallSpeed));

			ApplyMovement(UpdateMode.Update);
		}

		void FixedUpdate () {

			bool isFalling;
			CheckPhysics(out isFalling);

			if (isFalling) {
				ragdollController.GoRagdoll();
			}
			
			ApplyMovement(UpdateMode.FixedUpdate);
		}
		void LateUpdate(){
			ApplyMovement(UpdateMode.LateUpdate);
		}		

		void ApplyMovement (UpdateMode checkMode) {
			if (moveUpdate != checkMode)
				return;

			/*
				skip moving the main transform if we're completely ragdolled, or waiting to reorient
				the main transform view the ragdoll controller
			*/
			if (ragdollController.state == RagdollControllerState.Ragdolled || ragdollController.state == RagdollControllerState.TeleportMasterToRagdoll)
				return;

			/*
				when animated or blending to animation
				use character controller movement 
				
				it has less step offset jitter than the normal transform movement
				especially when getting up 
			*/
			if (ragdollController.state == RagdollControllerState.Animated || ragdollController.state == RagdollControllerState.BlendToAnimated) {
				
				if (!characterController.enabled)
					characterController.enabled = true;

				Vector3 animMove = animDelta;
				
				if (grounded) {

					if (!inCoyoteHang) {
						//stick to ground
						animMove.y = maxGravity;
					}

				}
				else {
					//add gravity				
					animMove.y += currentGravity;
				}

				characterController.Move(animMove);				
			}
			else {
				/*
					when falling or calculating velocity,
					
					use normal transform stuff (dont want the character controller collisions messing stuff up)
					for falling /calculating fall ( we need all exterion collisions to reach ragdol bones)
					and teh characer chontroller acts as a 'protective shell' when it's enabled
				*/

				if (characterController.enabled)
					characterController.enabled = false;

				Vector3 animMove = transform.position + animDelta;

				if (grounded) {
					//stick to ground
					animMove.y = floorY;
				}
				else {
					//add gravity				
					animMove.y += currentGravity;
				}
				transform.position = animMove;
			}
		}

		void CheckPhysics (out bool isFalling) {

			Ray groundRay = new Ray(transform.position + Vector3.up * characterController.stepOffset, Vector3.down);
			
			//chekc if we're groudned
			CheckIfGrounded(groundRay, characterController.stepOffset, grounded, .5f);
			
			//check for a big fall
			isFalling = CheckForFallRagdoll(groundRay, characterController.stepOffset);
			
			//calculate gravity to use for movement
			CalculateCurrentGravity(Time.fixedDeltaTime);
		}

		void CheckIfGrounded (Ray groundRay, float rayDistaneBuffer, bool wasGrounded, float checkDistance) {
			grounded = false;
			inCoyoteHang = false;
			RaycastHit hit;
			if (Physics.Raycast(groundRay, out hit, checkDistance + rayDistaneBuffer, groundLayerMask, QueryTriggerInteraction.Ignore)) {
				
				// if we're falling, dont let us go "up"
				// ragdoll was falling up stairs...
				bool skipFloorSet = ragdollController.state == RagdollControllerState.Falling && hit.point.y > floorY;
				
				if (!skipFloorSet) {
					floorY = hit.point.y;
				}

				grounded = true;
				lastGroundHitTime = Time.time;
			}
			else {
				//stay grounded if we just left the ground (like wile e coyote)
				if (wasGrounded) {
					grounded = Time.time - lastGroundHitTime <= coyoteTime;
					inCoyoteHang = grounded;
				}
			}
			if (grounded) {
				lastGroundTime = Time.time;
			}
		}

		bool CheckForFallRagdoll (Ray groundRay, float rayDistaneBuffer) {
			if (grounded)
				return false;
			
			//check if we've spend enough time not grounded
			if (Time.time - lastGroundTime >= fallRagdollTime) {

				//if we have and the drop is high enough, go ragdoll
				if (!Physics.Raycast(groundRay, fallDistance + rayDistaneBuffer, groundLayerMask, QueryTriggerInteraction.Ignore)) {
					return true;
				}
			}
			return false;
		}

		void CalculateCurrentGravity (float deltaTime) {
			if (grounded) {
				currentGravity = 0;
				return;
			}
			
			// add gravity
			currentGravity += Physics.gravity.y * deltaTime * deltaTime;
			
			// cap gravity
			if (currentGravity < maxGravity)
				currentGravity = maxGravity;
		}		
	}
}