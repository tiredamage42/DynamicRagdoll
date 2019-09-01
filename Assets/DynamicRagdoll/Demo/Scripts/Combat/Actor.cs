using UnityEngine;

// include our namespace
using DynamicRagdoll;

namespace Game.Combat {

    /* 
        script that shows how to handle ragdolling in a combat setting
    */
    public class Actor : MonoBehaviour
    {
        public float health = 100;

        RagdollController ragdollController;
        void Awake () {
            ragdollController = GetComponent<RagdollController>();
        }

        Damageable[] allDamageables;

        void Start () {
            allDamageables = ragdollController.ragdoll.AddComponentsToBones<Damageable>();

            for (int i = 0; i < allDamageables.Length; i++) {
                allDamageables[i].onDamageReceive += OnDamageReceive;
                allDamageables[i].damageableRoot = transform;
            }
        }

        void OnDamageReceive (Damageable damageable, DamageMessage damageMessage) {


            // set bone decay for the hit bone, so the physics will affect it
            // (slightly lower for neighbor bones)
            HumanBodyBones damagedBone;
            if ( ragdollController.ragdoll.Transform2HumanBone (damageable.transform, out damagedBone) ) {

                float mainDecay = 1;
                float neighborMultiplier = .75f;
                ragdollController.SetBoneDecay(damagedBone, mainDecay, neighborMultiplier);
                            
                //make it go ragdoll
                ragdollController.GoRagdoll();
            }            
        }
    }
}
