using UnityEngine;
using System.Collections;
namespace DynamicRagdoll.Demo
{
	/*
		a demo script to move the character around using the animator
		add this script to the character
	*/
	public class PlayerMovement : MonoBehaviour
	{
		public Texture crosshairTexture;
		public float turnSpeed = 500f;
		public float slowTime = .3f;
		public float bulletForce = 3000f;

		public float heightSpeed = 20;

		public LayerMask groundLayerMask;
		
		bool slomo;
		float origFixedDelta;
		Camera cam;
		Animator anim;		
		CannonBall cannonBall;
		RagdollController ragdollController;
		CameraFollow camFollow;

			
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
		bool ragdolled;
		float floorY;

		void OnAnimatorMove ()
		{
			//transform.position += anim.deltaPosition;
			
			Vector3 pos = transform.position;
			Vector3 d = anim.deltaPosition;
			transform.position = new Vector3(pos.x + d.x, floorY, pos.z + d.z);
		}

		void DemoRagdoll () {
			ragdollController.GoRagdoll();
			ragdolled = true;
			
			//switch camera to follow ragdoll	
			camFollow.target = ragdollController.ragdoll.RootBone().transform;
		}
			
		void FixedUpdate () {
			RaycastHit hit;
			if (Physics.Raycast(transform.position + Vector3.up, Vector3.down, out hit, 10f, groundLayerMask )) {
				floorY = Mathf.Lerp(floorY, hit.point.y, Time.fixedDeltaTime * heightSpeed);
			}
		}

		void Update () 
		{
			//switch cameara to follow our characetr
			if (ragdolled) {
				if (ragdollController.state == RagdollController.RagdollState.BlendToAnimated) {
					camFollow.target = anim.GetBoneTransform(HumanBodyBones.Hips);
					ragdolled = false;
				} 
			}
			
			//check for manual ragdoll
			if (Input.GetKeyDown(KeyCode.R)) {
				DemoRagdoll();
			}
		
			if (!ragdolled && !ragdollController.isGettingUp) {
				transform.Rotate(0f, Input.GetAxis("Horizontal") * turnSpeed * Time.deltaTime, 0f);
			}
			anim.SetFloat("Speed", Input.GetAxis("Vertical") * (Input.GetKey(KeyCode.LeftShift) ? 2 : 1));
			
			UpdateSloMo();

			UpdateShooting();

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

						System.Action executePhysics = () => AddDelayedForce(rb, ray.direction.normalized * (bulletForce/ Time.timeScale), hit.point);

						if (hit.transform.IsChildOf(ragdollController.ragdoll.transform)) {
							
							ragdollController.SetBoneDecay(hit.transform, 1, .75f);
							//stores the method for delayed execution
							ragdollController.StorePhysicsHit(executePhysics);
							DemoRagdoll();
							
						}
						else {
							executePhysics();
						}
					}
				}
			}
		}
		void AddDelayedForce (Rigidbody rb, Vector3 force, Vector3 point)
		{
			rb.AddForceAtPosition(force, point, ForceMode.VelocityChange);
		}

		void OnGUI ()
		{
			DrawCrosshair();
			DrawTutorialBox();
		}

		void DrawTutorialBox () {
			GUI.Box(new Rect(5, 5, 160, 120), "Fire = Left mouse\nB = Launch Ball\nN = Slow motion\nR = Go Ragdoll\nMove With Arrow Keys or WASD");
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
