using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace DynamicRagdoll.Demo {



    // [System.Serializable] public class CombatBoneData {
    //     public float damageMultiplier = 1;
    //     [Range(0,1)] public float chanceDismember = .5f;
    // }

    // [System.Serializable] public class BoneDamageMultiplierData : BoneDataElement<CombatBoneData> { }
    // [System.Serializable] public class BoneDamageMultipliers : BoneData<BoneDamageMultiplierData, CombatBoneData> {
    //     public BoneDamageMultipliers () : base() { }
    //     public BoneDamageMultipliers ( Dictionary<HumanBodyBones, CombatBoneData> template ) :  base (template) { }
    //     public BoneDamageMultipliers ( CombatBoneData template ) :  base (template) { }
    // }


    /*

		a demo script that shows how to interact with 
		the ragdoll controller component, and link it to any character movement
        script
		
	*/


    [RequireComponent(typeof(Character))]
    public class CharacterMovementRagdollLink : MonoBehaviour
    {
        /*
			fall speeds to set for the ragdoll controller
		*/
		[Header("Fall Decay Speeds")]
		public float[] fallDecaySpeeds = new float[] { 5, 2, 2 };
        Character characterMovement;
        RagdollController ragdollController;


        void Awake () {
            ragdollController = GetComponent<RagdollController>();
			
            characterMovement = GetComponent<Character>();
        }

        void Update () {
            if (characterMovement.freeFalling) {
				ragdollController.GoRagdoll();
			}
			
            
            // if we're falling, dont let us go "up"
            // ragdoll was falling up stairs...
            characterMovement.preventUpwardsGroundedMotion = ragdollController.state == RagdollControllerState.Falling; 
            
            // dont accept any movement changes from player input, or ai input
            characterMovement.disableExternalMovement = ragdollController.state != RagdollControllerState.Animated || ragdollController.isGettingUp; 
                    
            /*
                skip moving the main transform if we're completely ragdolled, or waiting to reorient
                the main transform via the ragdoll controller
            */
            characterMovement.disableAllMovement = ragdollController.state == RagdollControllerState.Ragdolled || ragdollController.state == RagdollControllerState.TeleportMasterToRagdoll; 


            /*
                when animated or blending to animation
                    use character controller movement 
                    
                    it has less step offset jitter than the normal transform movement
                    especially when getting up 

                else when falling:
                        
                    use normal transform stuff (dont want the character controller collisions messing stuff up)
                    for falling /calculating fall ( we need all exterion collisions to reach ragdol bones)
                    and teh characer chontroller acts as a 'protective shell' when it's enabled
            */
            
            characterMovement.usePhysicsForMove = ragdollController.state == RagdollControllerState.Animated || ragdollController.state == RagdollControllerState.BlendToAnimated; 


            //cehck if we started getting up
			if (ragdollController.state == RagdollControllerState.BlendToAnimated) {
				//set zero speed
				if (characterMovement.currentSpeed != 0) {
					characterMovement.SetMovementSpeed(0);
				}
			}

			//set the ragdolls fall speed based on our speed
			ragdollController.SetFallSpeed(fallDecaySpeeds[(int)characterMovement.currentSpeed]);

        }
    }
}
