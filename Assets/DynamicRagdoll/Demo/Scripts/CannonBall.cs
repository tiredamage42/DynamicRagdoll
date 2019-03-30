using UnityEngine;

namespace DynamicRagdoll.Demo
{
	public class CannonBall : MonoBehaviour
	{
		public float velocity = 20f;
		public float mass = 40f;
		public float scale = .4f;
		Rigidbody rb;

		void Awake ()
		{
			rb = GetComponent<Rigidbody>();
			rb.isKinematic = false;
		}
		public void LaunchToPosition (Vector3 position) {
			transform.localScale = Vector3.one * scale;
			rb.mass = mass;
			rb.useGravity = false;
			// Hurl ball towards hit transform
			rb.velocity = (position - transform.position).normalized * velocity; 
		}
		void OnCollisionEnter (Collision collision)
		{
			// Turn gravity on for the ball after the ball has hit something.
			rb.useGravity = true; 
		}
	}
}
