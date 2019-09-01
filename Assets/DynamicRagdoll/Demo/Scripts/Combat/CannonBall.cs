using UnityEngine;
using System.Collections;

namespace Game.Combat {
	public class CannonBall : MonoBehaviour {
		Rigidbody rb;

		void Awake () {
			rb = GetComponent<Rigidbody>();
			rb.isKinematic = false;

			//it's a fast moving object
			rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
		}
		
		IEnumerator LaunchToPosition (Vector3 start, Vector3 position, float scale, float mass, float velocity) {
			yield return new WaitForFixedUpdate();
			rb.isKinematic = false;
			rb.position = start;
			GetComponent<SphereCollider>().radius = scale * .5f;
			transform.GetChild(0).localScale = Vector3.one * scale;
			rb.mass = mass;
			rb.useGravity = false;
			// Hurl ball towards hit transform
			rb.velocity = (position - rb.position).normalized * velocity; 
		}
		public void Launch(Vector3 start, Vector3 position, float scale, float mass, float velocity) {
			StartCoroutine(LaunchToPosition(start, position, scale, mass, velocity));
		}

		void OnCollisionEnter (Collision collision)
		{
			// Turn gravity on for the ball after the ball has hit something.
			rb.useGravity = true; 
		}
	}
}
