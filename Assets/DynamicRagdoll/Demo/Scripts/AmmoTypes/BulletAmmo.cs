using System.Collections;
using UnityEngine;

namespace Game.Combat {
    [CreateAssetMenu()]
    public class BulletAmmo : AmmoType {
        public float maxDistance = 100;
        public float force = 50;
        public int severity = 2;



        public override GameObject FireAmmo (Game.Combat.IDamager damager, Ray damageRay, LayerMask layerMask, float damageMultiplier) {
            coroutineRunner.StartCoroutine(ShootBullet(damager, damageRay, layerMask, damageMultiplier));
            return null;
        }
        
        IEnumerator ShootBullet (Game.Combat.IDamager damager, Ray damageRay, LayerMask layerMask, float damageMultiplier){
            yield return new WaitForFixedUpdate();
                
            RaycastHit hit;
                
            if (Physics.Raycast(damageRay, out hit, maxDistance, layerMask, QueryTriggerInteraction.Ignore))
            {
                Damageable damageable = hit.transform.GetComponent<Damageable>();
                if (damageable) {
                    damageable.SendDamage(new DamageMessage(damager, baseDamage * damageMultiplier, severity));
                }
                
                Rigidbody rb = hit.transform.GetComponent<Rigidbody>();
                if (rb) {
                    rb.AddForceAtPosition(damageRay.direction.normalized * force, hit.point, ForceMode.VelocityChange);
                }
            }
        }
    }
}