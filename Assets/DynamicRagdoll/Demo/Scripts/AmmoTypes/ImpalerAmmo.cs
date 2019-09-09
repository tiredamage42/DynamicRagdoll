using System.Collections.Generic;
using UnityEngine;
using DynamicRagdoll;

namespace Game.Combat {

    [CreateAssetMenu()]
    public class ImpalerAmmo : AmmoType {
        public Impaler impalerPrefab;
        public float velocity = 50;
        public float maxDistance = 25;
        public float lifeTime = 10;
        public float hitObjectVelocityMultiplier = .5f;
        public float zMovementLimit = 1;
        public float impalerRadius = .1f; 
        public bool kinematicOnReach = true;
        public bool useColliderOnReach = true;
        

        public override GameObject FireAmmo (IDamager damager, Ray damageRay, LayerMask layerMask, float damageMultiplier) {
            
            Impaler impaler = Impaler.pool.GetPrefabInstance(impalerPrefab);

            System.Action<PhysicsHit> onHitReach = (physicsHit) => {

            };
            
            List<PhysicsHit> hits = impaler.ShootImpaler (velocity, hitObjectVelocityMultiplier, zMovementLimit, damageRay, maxDistance, layerMask, impalerRadius, kinematicOnReach, useColliderOnReach, lifeTime, onHitReach);
            for (int i = 0; i < hits.Count; i++) {
                Damageable damageable = hits[i].hitElements[0].rigidbody.GetComponent<Damageable>();
                if (damageable) {
                    damageable.SendDamage(new DamageMessage(damager, baseDamage * damageMultiplier, 0));
                }
            }
            return impaler.gameObject;
        }
    }
}