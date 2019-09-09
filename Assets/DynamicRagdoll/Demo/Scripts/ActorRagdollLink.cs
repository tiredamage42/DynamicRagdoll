

using UnityEngine;
using System.Collections.Generic;

// include our namespace
using DynamicRagdoll;

namespace Game.Combat {

    [System.Serializable] public class CombatBoneData {
        public float damageMultiplier = 1;
        [Range(0,1)] public float chanceDismember = .5f;
    }

    [System.Serializable] public class CombatBoneDataElement : BoneDataElement<CombatBoneData> { }
    [System.Serializable] public class CombatBoneDataList : BoneData<CombatBoneDataElement, CombatBoneData> {
        public CombatBoneDataList () : base() { }
        public CombatBoneDataList ( Dictionary<HumanBodyBones, CombatBoneData> template ) :  base (template) { }
        public CombatBoneDataList ( CombatBoneData template ) :  base (template) { }
    }

    /* 
        script that shows how to handle ragdolling in a combat setting

        includes using the dismemberment system
    */
    public class ActorRagdollLink : MonoBehaviour
    {
        [BoneData] public CombatBoneDataList boneData;
        Actor actor;
        RagdollController ragdollController;
        public int minSeverityForDismember = 2;

        

        void Awake () {
            actor = GetComponent<Actor>();
            ragdollController = GetComponent<RagdollController>();
        }
        void Start () {
            actor.SetDamageModifier(ModifyDamage);    
            actor.SetSubDamageables(ragdollController.ragdoll.AddComponentsToBones<Damageable>());
            ragdollController.ragdoll.SetDismembermentAvailableCheck(DismembermentAvailable);
        }

        bool DismembermentAvailable () {
            return actor.health <= 0;
        }

        void OnEnable () {
            actor.onTakeDamage += OnTakeDamage;
            actor.onDeath += OnDeath;
            actor.onRevive += OnRevive;
        }
        void OnDisable () {
            actor.onTakeDamage -= OnTakeDamage;
            actor.onDeath -= OnDeath;
            actor.onRevive -= OnRevive;
        }

        void OnRevive () {
            Debug.Log(name + " REVIVING");
            ragdollController.ragdoll.RepairBones();
            ragdollController.ragdoll.ForceDetach();
            
            ragdollController.disableGetUp = false;
            ragdollController.GetUpImmediate();
        }

        float ModifyDamage (Damageable damageable, float baseDamage) {
            if (damageable != null) {
                RagdollTransform damagedBone;
                if ( ragdollController.ragdoll.Transform2HumanBone (damageable.transform, out damagedBone) ) {
                    return baseDamage * boneData[damagedBone.bone.bone].damageMultiplier;
                }
            }
            return baseDamage;
        }

        void OnTakeDamage (Damageable damageable, DamageMessage damageMessage) {
            
            // if we're dead and passed in a damageable that's part of our ragdoll
            // maybe dismember it
            
            if (damageable != null) {

                RagdollTransform damagedBone;
                if ( ragdollController.ragdoll.Transform2HumanBone (damageable.transform, out damagedBone) ) {
                    if (damageMessage.severity >= minSeverityForDismember) {
                        float dismemberChance = boneData[damagedBone.bone.bone].chanceDismember;
                        if (Random.value <= dismemberChance) {
                            ragdollController.ragdoll.DismemberBone("damage", damagedBone);
                        }
                    }

                    // set bone decay for the hit bone, so the physics will affect it
                    // (slightly lower for neighbor bones)
                    float mainDecay = 1;
                    float neighborMultiplier = .75f;
                    ragdollController.AddBoneDecay(damagedBone.bone.bone, mainDecay, neighborMultiplier);
                }  
            }
        }

        void OnDeath (Damageable damageable, DamageMessage damageMessage) {

            // if (damageable != null) {

            //     RagdollTransform damagedBone = null;
                
            //     if ( ragdollController.ragdoll.Transform2HumanBone (damageable.transform, out damagedBone) ) {

            //         // set bone decay for the hit bone, so the physics will affect it
            //         // (slightly lower for neighbor bones)
            //         float mainDecay = 1;
            //         float neighborMultiplier = .75f;
            //         ragdollController.AddBoneDecay(damagedBone.bone.bone, mainDecay, neighborMultiplier);
            //     }  
            // }
                            
            //make it go ragdoll
            ragdollController.GoRagdoll("death");

            ragdollController.disableGetUp = true;
        }
    }
}

