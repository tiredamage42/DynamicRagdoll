using UnityEngine;

namespace Game.Combat {
    public class DamageMessage {

        public GameObject damageGiver;
        public float baseDamage;

        public DamageMessage(GameObject damageGiver, float baseDamage) {
            this.damageGiver = damageGiver;
            this.baseDamage = baseDamage;
        }
    }
    
    public class Damageable : MonoBehaviour
    {

        public Transform damageableRoot;
        public event System.Action<Damageable, DamageMessage> onDamageReceive;
        public void SendDamage (DamageMessage damageMessage) {
            if (onDamageReceive != null) {
                onDamageReceive(this, damageMessage);
            }
            else {
                Debug.LogError(name + " Damageable isn't doing anything...");
            }
        }
    }
}
