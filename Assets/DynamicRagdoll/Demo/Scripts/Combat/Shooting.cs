using System.Collections;
using UnityEngine;

namespace Game.Combat {
    public class Shooting : MonoBehaviour
    {
        public LayerMask shootMask;
        public float bulletForce = 25f;

		// needed for slo motion or forces are too small
		public float modifiedBulletForce { get { return bulletForce / Time.timeScale; } }

        public void Shoot (Ray ray) {
            StartCoroutine(ShootBullet(ray));
        }

		IEnumerator ShootBullet (Ray ray){
			yield return new WaitForFixedUpdate();
			
			RaycastHit hit;
			
			if (Physics.Raycast(ray, out hit, 100f, shootMask, QueryTriggerInteraction.Ignore))
            {
				Damageable damageable = hit.transform.GetComponent<Damageable>();
				if (damageable) {
					damageable.SendDamage(new DamageMessage(gameObject, 50f));
				}
				
				Rigidbody rb = hit.transform.GetComponent<Rigidbody>();
				if (rb) {
					rb.AddForceAtPosition(ray.direction.normalized * modifiedBulletForce, hit.point, ForceMode.VelocityChange);
				}
			}
		}
    }
}
