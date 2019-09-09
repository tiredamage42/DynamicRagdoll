using System.Collections.Generic;
using UnityEngine;
using DynamicRagdoll;
namespace Game.Combat {

    [CreateAssetMenu()]
    public class ProjectileAmmo : AmmoType {
        public BasicProjectile prefab;
        public float velocity = 50;
        public float lifeTime = 10;
        public float mass = 5;

        public bool useGravityOnStart = false;
        public bool useGravityOnCollision = false;
        
        public override GameObject FireAmmo (IDamager damager, Ray damageRay, LayerMask layerMask, float damageMultiplier) {
            
            System.Action<Collision> onCollision = (collision) => {
                // calculate and dole out damage here
            };

            BasicProjectile spear = BasicProjectile.pool.GetPrefabInstance(prefab);
            
            spear.LaunchToPosition(damageRay, mass, velocity, lifeTime, useGravityOnStart, useGravityOnCollision, onCollision);
            return spear.gameObject;
        }
            
    }
}