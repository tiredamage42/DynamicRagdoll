using System.Collections.Generic;
using UnityEngine;
using DynamicRagdoll;

namespace  Game.Combat
{
    [CreateAssetMenu()]
    public class SpearAmmo : AmmoType {
        public Spear spearPrefab;
        public float velocity = 50;
        public float maxDistance = 25;
        public float lifeTime = 10;
        public float skewerSeperation = .25f;
        public float minSpaceFromWall = .25f; 
        public float skeweredVelocityMultiplier = .1f; 
        
        public override GameObject FireAmmo (IDamager damager, Ray damageRay, LayerMask layerMask, float damageMultiplier) {
            

            Spear spear = Spear.pool.GetPrefabInstance(spearPrefab);

            System.Action<PhysicsHit, bool> onHitReach = (physicsHit, isImpaled) => {

            };
            
            List<PhysicsHit> hits = spear.ShootSpear(velocity, damageRay, maxDistance, layerMask, lifeTime, skewerSeperation, minSpaceFromWall, skeweredVelocityMultiplier, onHitReach);

            for (int i = 0; i < hits.Count; i++) {
                Damageable damageable = hits[i].hitElements[0].rigidbody.GetComponent<Damageable>();
                if (damageable) {
                    damageable.SendDamage(new DamageMessage(damager, baseDamage * damageMultiplier, 0));
                }
            }

            
            return spear.gameObject;
        }
    }
}