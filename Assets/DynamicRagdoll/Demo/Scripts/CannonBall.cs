using UnityEngine;
using System.Collections;

namespace DynamicRagdoll.Demo {
	public class CannonBall : MonoBehaviour {
		public float velocity = 20f;
		public float mass = 40f;
		public float scale = .4f;
		Rigidbody rb;

		void Awake ()
		{
			rb = GetComponent<Rigidbody>();
			rb.isKinematic = false;
		}
		

		IEnumerator Delay (Vector3 start, Vector3 position) {
			yield return new WaitForFixedUpdate();
			rb.position = start;
			transform.localScale = Vector3.one * scale;
			rb.mass = mass;
			rb.useGravity = false;
			// Hurl ball towards hit transform
			rb.velocity = (position - rb.position).normalized * velocity; 
		}
		public void LaunchToPosition(Vector3 start, Vector3 position) {
			StartCoroutine(Delay(start, position));
		}


			


		void OnCollisionEnter (Collision collision)
		{
			// Turn gravity on for the ball after the ball has hit something.
			rb.useGravity = true; 
		}
	}
}
