using UnityEngine;
using System.Collections;
using System;
namespace Game.Combat {

    public class Actor : MonoBehaviour
    {
        public Transform spawnPoint;
        public float debugReviveTime = 5;
        IEnumerator DebugRevival () {
            yield return new WaitForSeconds(debugReviveTime);
            Revive(spawnPoint);
        }



        // void Awake () {
        //     characterController = GetComponent<CharacterController>();
        // }


        // just needed to disable and enable (if enabled) during spawn point warping
        // can be null...
        // CharacterController characterController; 

            

        public float health = 100;

        public event Action<Damageable, DamageMessage> onDeath, onTakeDamage;
        public event Action onRevive;

        Func<Damageable, float, float> damageModifier;

        public void SetSubDamageables (Damageable[] subDamageables) {
            for (int i = 0; i < subDamageables.Length; i++) {
                subDamageables[i].onDamageReceive += SendDamage;
                subDamageables[i].damageableRoot = transform;
            }
        }
        public void SetDamageModifier (Func<Damageable, float, float> damageModifier) {
            this.damageModifier = damageModifier;
        }

        public void OnDeath (Damageable subDamageable, DamageMessage damageMessage) {
            health = 0;

            if (damageMessage.damager != null) {
                damageMessage.damager.DamageDeathCallback(this);
            }
                
            if (onDeath != null) {
                onDeath(subDamageable, damageMessage);
            }

            // Debug.LogError(name + " is DEAD");
            StartCoroutine(DebugRevival());
        }

        void OnEnable () {
            if (health <= 0) {
                StartCoroutine(DebugRevival());
            }
        }

        public void Revive (Transform spawnPoint) {
            health = 100;

            if (onRevive != null) {
                onRevive();
            }

            if (spawnPoint != null) {


                DynamicRagdoll.RagdollPhysics.MovePossibleCharacterController(transform, spawnPoint.position);
                // bool ccEnabled = false;
                // if (characterController != null) {
                //     ccEnabled = characterController.enabled;
                //     characterController.enabled = false;
                // }

                // transform.position = spawnPoint.position;

                // if (characterController != null) characterController.enabled = ccEnabled;
            }

        }

        public void SendDamage (Damageable subDamageable, DamageMessage damageMessage) {
            
            bool deadFromDamage = false;
            if (health > 0) {
                float damage = damageMessage.baseDamage;
                if (damageModifier != null) {
                    damage = damageModifier(subDamageable, damage);
                }
                health -= damage;
                // Debug.LogError(name + " DAMAGE +" + damage);

                deadFromDamage = health <= 0;
                if (damageMessage.damager != null) {
                    damageMessage.damager.DamageDealtCallback(this, damage, health);
                }
            }

            if (onTakeDamage != null) {
                onTakeDamage(subDamageable, damageMessage);
            }
            if (deadFromDamage) {
                OnDeath (subDamageable, damageMessage);
            }
        }
    }
}
