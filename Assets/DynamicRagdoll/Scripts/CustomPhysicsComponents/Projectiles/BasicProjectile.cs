using UnityEngine;

namespace DynamicRagdoll {
	/*
		basic projectile, just launches toward a position via physics,
		callback to when it collides with an object
	*/

	public class BasicProjectile : PhysicsProjectile<BasicProjectile> {
		bool useGravityOnCollision;
		System.Action<Collision> onCollision;

		// protected override void Awake () {
		// 	base.Awake();
		// 	//it's a fast moving object
		// 	rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
		// }

		protected override void UpdateProjectile(float deltaTime) { }
		protected override void DisableProjectile () { }

		public void LaunchToPosition ( Ray ray, float mass, float velocity, float lifeTime, bool useGravityOnStart, bool useGravityOnCollision, System.Action<Collision> onCollision) {
			this.useGravityOnCollision = useGravityOnCollision;
			this.onCollision = onCollision;
			InitializeLaunch(ray, velocity, lifeTime, 1);
			EnablePhysics(true);

			rb.mass = mass;			
			rb.useGravity = useGravityOnStart;
			// Hurl ball towards hit transform
			rb.velocity = ray.direction * velocity;
		}
			
		void OnCollisionEnter (Collision collision)
		{
			// Turn gravity on for the ball after the ball has hit something.
			if (useGravityOnCollision) {
				rb.useGravity = true; 
			}
			if (onCollision != null) {
				onCollision(collision);
			}
		}
	}
}
