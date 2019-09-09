

using System.Collections.Generic;
using UnityEngine;
using DynamicRagdoll;

namespace  Game.Combat
{
    [CreateAssetMenu()]
    public class ShrapnelAmmo : AmmoType {
        public Shrapnel prefab;
        public float velocity = 50;
        public float maxDistance = 25;
        public float lifeTime = 10;
        public float radiusCheck = .05f;
        public float hitObjectVelocityMultiplier = .1f; 
        public bool simulateArc;
        
        public override GameObject FireAmmo (IDamager damager, Ray damageRay, LayerMask layerMask, float damageMultiplier) {
            
            Shrapnel spear = Shrapnel.pool.GetPrefabInstance(prefab);

            System.Action<PhysicsHit> onHitReach = (physicsHit) => {
                if (physicsHit != null) {
                    Damageable damageable = physicsHit.mainCollider.GetComponent<Damageable>();
                    if (damageable) {
                        damageable.SendDamage(new DamageMessage(damager, baseDamage * damageMultiplier, 0));
                    }
                }
            
            };
            
            PhysicsHit hit = spear.Launch(velocity, damageRay, maxDistance, radiusCheck, layerMask, lifeTime, hitObjectVelocityMultiplier, simulateArc, onHitReach);

            // if (hit != null) {
            //     Damageable damageable = hit.mainCollider.GetComponent<Damageable>();
            //     if (damageable) {
            //         damageable.SendDamage(new DamageMessage(damager, baseDamage * damageMultiplier));
            //     }
            // }
            
            return spear.gameObject;
        }
    }
}
