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
		public float bulletForce = 8000f;
		
		bool slomo;
		float origFixedDelta;
		Camera cam;
		Animator anim;		
		CannonBall cannonBall;
		RagdollController ragdollController;
			
		void Awake ()
		{
			anim = GetComponent<Animator>();
			ragdollController = GetComponent<RagdollController>();
			
			cam = Camera.main;
			cannonBall = GameObject.FindObjectOfType<CannonBall>();
			Cursor.visible = false;
			origFixedDelta = Time.fixedDeltaTime;
		}

		void OnAnimatorMove ()
		{
			transform.position += anim.deltaPosition;
		}
		
		void Update () 
		{
			if (ragdollController.state != RagdollController.RagdollState.Ragdolled && !ragdollController.isGettingUp) {
				transform.Rotate(0f, Input.GetAxis("Horizontal") * turnSpeed * Time.fixedDeltaTime, 0f);
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
			if (Input.GetKeyDown(KeyCode.N) && !slomo)
			{
				Time.timeScale = slowTime;
				Time.fixedDeltaTime = origFixedDelta * slowTime;
				slomo = true;
			}
			else if (slomo && Input.GetKeyDown(KeyCode.N))
			{
				Time.timeScale = 1f;
				Time.fixedDeltaTime = origFixedDelta;
				slomo = false;
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
					Rigidbody rbHit = hit.transform.GetComponent<Rigidbody>();
					if (rbHit) {
						if (hit.transform.IsChildOf(ragdollController.ragdoll.transform)) {
							ragdollController.GoRagdoll();
						}
						StartCoroutine(AddForceToRigidbody(rbHit, ray.direction.normalized * bulletForce, hit.point));
					}
				}
			}
		}
		IEnumerator AddForceToRigidbody (Rigidbody rb, Vector3 force, Vector3 point)
		{
			yield return new WaitForFixedUpdate();
			rb.AddForceAtPosition(force, point);
		}
		void OnGUI ()
		{
			float crossHairSize = 40;
			float halfSize = crossHairSize / 2;
			Vector2 mousePos = Input.mousePosition;
			Rect crosshairRect = new Rect(mousePos.x - halfSize, Screen.height - mousePos.y - halfSize, crossHairSize, crossHairSize);
			GUI.DrawTexture(crosshairRect, crosshairTexture, ScaleMode.ScaleToFit, true);
		
			Rect guiBox = new Rect(5, 5, 160, 120);
			GUI.Box(guiBox, "Fire = Left mouse\nB = Launch ball\nN = Slow motion");
		}
	}
}
