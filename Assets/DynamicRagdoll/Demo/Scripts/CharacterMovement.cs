using UnityEngine;


namespace DynamicRagdoll.Demo
{
	/*

		a demo character control script 
		
		moves the character around using the animator
	*/

	public class CharacterMovement : MonoBehaviour
	{
		public bool preventUpwardsGroundedMotion;
		
		// dont accept any movement changes from player input, or ai input
		public bool disableExternalMovement;		
		public bool disableAllMovement;
		public bool usePhysicsForMove;
		

		[Header("Movement")]
		public UpdateMode moveUpdate = UpdateMode.Update;

		[Tooltip("Maximum gravity velocity")]
		public float maxGravity = -1;
		public LayerMask groundLayerMask;

		[Tooltip("How much time to hang in the air and extend being 'grounded'")]
		public float coyoteTime = .2f;

		[Header("Free Falling")]
		// [Header("Falling Ragdoll")]
		[Tooltip("How far a drop when not gorunded has to be, to be considered a free fall")]
		public float freefallDistance = 3f;

		[Tooltip("How much time to wait until initiating Free Fall status after not being grounded and falling from high enough")]
		public float freeFallDelayTime = .2f;

		[HideInInspector] public float currentSpeed = 0;
		bool grounded, inCoyoteHang;
		float floorY, currentGravity, lastGroundHitTime, lastGroundTime;
		Animator anim;		
		Vector3 animDelta;
		CharacterController characterController;

		[HideInInspector] public bool freeFalling;


		/*
			set movemnt speed and update the animator
		*/
		public void SetMovementSpeed(float speed) {
			// if (!disableExternalMovement) {
				currentSpeed = speed;
				anim.SetFloat("Speed", currentSpeed);
			// }
		}

		void Awake () {
			anim = GetComponent<Animator>();
			characterController = GetComponent<CharacterController>();
			currentSpeed = 0;
		}

		void OnAnimatorMove () {
			animDelta = anim.deltaPosition;
		}

		void Update ()  {
			ApplyMovement(UpdateMode.Update);
		}
		void FixedUpdate () {
			CheckPhysics(out freeFalling);
			ApplyMovement(UpdateMode.FixedUpdate);
		}
		void LateUpdate(){
			ApplyMovement(UpdateMode.LateUpdate);
		}		

		void ApplyMovement (UpdateMode checkMode) {
			if (moveUpdate != checkMode)
				return;

			if (disableAllMovement)
				return;


			if (usePhysicsForMove) {
				
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
			isFalling = CheckForFreeFall(groundRay, characterController.stepOffset);
			
			//calculate gravity to use for movement
			CalculateCurrentGravity(Time.fixedDeltaTime);
		}

		void CheckIfGrounded (Ray groundRay, float rayDistaneBuffer, bool wasGrounded, float checkDistance) {
			grounded = false;
			inCoyoteHang = false;
			RaycastHit hit;
			if (Physics.Raycast(groundRay, out hit, checkDistance + rayDistaneBuffer, groundLayerMask, QueryTriggerInteraction.Ignore)) {
				
				bool skipFloorSet = hit.point.y > floorY && preventUpwardsGroundedMotion;
				
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

		bool CheckForFreeFall (Ray groundRay, float rayDistaneBuffer) {
			if (grounded)
				return false;
			
			//check if we've spend enough time not grounded
			if (Time.time - lastGroundTime >= freeFallDelayTime) {

				//if we have and the drop is high enough, go free fall
				if (!Physics.Raycast(groundRay, freefallDistance + rayDistaneBuffer, groundLayerMask, QueryTriggerInteraction.Ignore)) {
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

		/*
            let character controller move rigidbodies
        */
        void OnControllerColliderHit(ControllerColliderHit hit) {

            // We dont want to push objects below us
            if (hit.moveDirection.y < -0.3)
                return;

            //check for rigidbody            
            Rigidbody rb = hit.collider.attachedRigidbody;
            if (rb == null || rb.isKinematic)
                return;
            
            // Calculate push direction from move direction,
            // we only push objects to the sides never up and down
            Vector3 pushDir = new Vector3(hit.moveDirection.x, 0, hit.moveDirection.z);

            // Apply the push
            rb.velocity = pushDir * hit.controller.velocity.magnitude;
        }
	}
}