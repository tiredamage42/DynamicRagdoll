using UnityEngine;
using System.Collections;
namespace DynamicRagdoll.Demo
{
	/*
		a demo script to move the character around using the animator
		add this script to the character


		also shows how to interact with the attached ragdoll controller component
	*/

	[RequireComponent(typeof(RagdollController))]
	public class PlayerMovement : MonoBehaviour
	{
		/*
			fall speeds to set for the ragdoll controller
		*/
		public float idleFallSpeed = 3;
		public float walkFallSpeed = 1;
		public float runFallSpeed = 1;


		/*
			Demo variables
		*/
		public Texture crosshairTexture;
		public float turnSpeed = 500f;
		public float slowTime = .3f;
		public float bulletForce = 3000f;
		[Range(0,1)] public float gravity = 1;
		public float heightSpeed = 20;
		public LayerMask groundLayerMask;
		float currentSpeed, origFixedDelta, floorY;		
		bool slomo;
		Camera cam;
		Animator anim;		
		CannonBall cannonBall;
		CameraFollow camFollow;


		RagdollController ragdollController;
		bool ragdolled;

			
		void Awake ()
		{
			anim = GetComponent<Animator>();
			ragdollController = GetComponent<RagdollController>();
			
			cam = Camera.main;
			cannonBall = GameObject.FindObjectOfType<CannonBall>();

			camFollow = GameObject.FindObjectOfType<CameraFollow>();

			Cursor.visible = false;
			origFixedDelta = Time.fixedDeltaTime;

			camFollow.target = anim.GetBoneTransform(HumanBodyBones.Hips);
		}
		
		void OnAnimatorMove ()
		{
			Vector3 pos = transform.position;
			Vector3 d = anim.deltaPosition;
			transform.position = new Vector3(pos.x + d.x, floorY, pos.z + d.z);
		}

		void DoRagdoll () {
			//set the fall speed based on our speed
			ragdollController.SetFallSpeed(currentSpeed == 0 ? idleFallSpeed : (currentSpeed == 1 ? walkFallSpeed : runFallSpeed));
			
			//go ragdoll
			ragdollController.GoRagdoll();

			//switch camera to follow ragdoll	
			camFollow.target = ragdollController.ragdoll.RootBone().transform;
			
			ragdolled = true;
		}
			
		void FixedUpdate () {
			UpdateShooting();

			RagdollController.RagdollState state = ragdollController.state;

			// was going through floor when getting up
			if (state == RagdollController.RagdollState.TeleportMasterToRagdoll || state == RagdollController.RagdollState.BlendToAnimated) {
			//if (ragdolled) {
				return;
			}

			//stick to floor

			float deltaTime = Time.fixedDeltaTime;
			RaycastHit hit;
			if (Physics.Raycast(transform.position + Vector3.up * .25f, Vector3.down, out hit, .5f, groundLayerMask )) {
				floorY = Mathf.Lerp(floorY, hit.point.y, deltaTime * heightSpeed);
			}
			else {
				floorY += Physics.gravity.y * deltaTime * gravity;
			}
		}

		void SetMovementSpeed(float speed) {
			currentSpeed = speed;
			anim.SetFloat("Speed", currentSpeed);
		}

		void Update () 
		{

			//cehck if we started getting up
			if (ragdolled) {
				if (ragdollController.state == RagdollController.RagdollState.BlendToAnimated) {
					//switch cameara to follow our characetr
					camFollow.target = anim.GetBoneTransform(HumanBodyBones.Hips);
					
					//set zero speed
					SetMovementSpeed(0);

					//as far as this script is concerned we're not ragdolled anymore
					ragdolled = false;
				} 
			}
			
			//check for manual ragdoll
			if (Input.GetKeyDown(KeyCode.R)) {
				DoRagdoll();
			}

			if (ragdollController.state == RagdollController.RagdollState.Animated && !ragdollController.isGettingUp) {
			//if (!ragdolled && !ragdollController.isGettingUp) {
				transform.Rotate(0f, Input.GetAxis("Horizontal") * turnSpeed * Time.deltaTime, 0f);
				SetMovementSpeed(Input.GetAxis("Vertical") * (Input.GetKey(KeyCode.LeftShift) ? 2 : 1));
			}
			
			UpdateSloMo();


			if (Input.GetKeyDown(KeyCode.B)) 
			{
				cannonBall.LaunchToPosition(transform.position);
			}
		}

		void UpdateSloMo () {
			if (Input.GetKeyDown(KeyCode.N)) {
				Time.timeScale = slomo ? 1 : slowTime;
				Time.fixedDeltaTime = origFixedDelta * Time.timeScale;
				slomo = !slomo;
			}
		}


		void UpdateShooting ()
		{
			if (Input.GetMouseButtonDown(0))
			{
				Ray ray = cam.ScreenPointToRay(Input.mousePosition);
				RaycastHit hit;
				if (Physics.Raycast(ray, out hit, 100f))
				{
					Rigidbody rb = hit.transform.GetComponent<Rigidbody>();
					if (rb) {

						System.Action executePhysics = () => rb.AddForceAtPosition(ray.direction.normalized * (bulletForce/ Time.timeScale), hit.point, ForceMode.VelocityChange);
						 
						if (hit.transform.IsChildOf(ragdollController.ragdoll.transform)) {
							
							ragdollController.SetBoneDecay(hit.transform, 1, .75f);
							//stores the method for delayed execution
							ragdollController.StorePhysics(executePhysics);
							
							
							DoRagdoll();
							
						}
						else {
							executePhysics();
						}
					}
				}
			}
		}

		void OnGUI ()
		{
			DrawCrosshair();
			DrawTutorialBox();
		}

		void DrawTutorialBox () {
			GUI.Box(new Rect(5, 5, 160, 120), "Fire = Left mouse\nB = Launch Ball\nN = Slow motion\nR = Go Ragdoll\nMove With Arrow Keys\nor WASD");
		}

		void DrawCrosshair () {
			float crossHairSize = 40;
			float halfSize = crossHairSize / 2;
			Vector2 mousePos = Input.mousePosition;
			Rect crosshairRect = new Rect(mousePos.x - halfSize, Screen.height - mousePos.y - halfSize, crossHairSize, crossHairSize);
			GUI.DrawTexture(crosshairRect, crosshairTexture, ScaleMode.ScaleToFit, true);
		}
	}
}
