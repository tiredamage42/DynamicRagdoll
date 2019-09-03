using UnityEngine;

namespace Game.Combat {
    public interface IDamager
    {
        void DamageDealtCallback (Actor actor, float damageDone, float newHealth);
        void DamageDeathCallback (Actor actor);
    }
        
    public class DamageMessage {

        public IDamager damager;
        public float baseDamage;

        public DamageMessage(IDamager damager, float baseDamage) {
            this.damager = damager;
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
