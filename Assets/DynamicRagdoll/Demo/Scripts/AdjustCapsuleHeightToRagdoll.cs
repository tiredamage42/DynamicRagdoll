using UnityEngine;

namespace DynamicRagdoll.Demo {
    /*
        
        the characterController's hiehgt is set to the world position
        of the top of the ragdoll head collider in case our animation crouches down or whatever, 
    */
    [RequireComponent(typeof(RagdollController))]
    [RequireComponent(typeof(CharacterController))]
    public class AdjustCapsuleHeightToRagdoll : MonoBehaviour
    {
        public float minHeight = .25f;
                
        RagdollController ragdollController;
        CharacterController characterController;

        
        //calculate the character height based on the distance between the top of our head
        //and out feet
        float calculateCharHeight {
            get {
                //get head bone (should be teleporting to master anyways)
                Ragdoll.Bone headBone = ragdollController.ragdoll.GetPhysicsBone(HumanBodyBones.Head);
                
                //get its shpere collider
                SphereCollider sphere = (SphereCollider)headBone.collider;
                
                Vector3 headCenterWorldPos = headBone.transform.position + (headBone.transform.rotation * sphere.center);
                
                //the height is the distanc form teh top of the head collider to our character feet
                return Mathf.Max(minHeight, (headCenterWorldPos.y + sphere.radius) - transform.position.y);
            }
        }

        void Awake () {
            characterController = GetComponent<CharacterController>();
            ragdollController = GetComponent<RagdollController>();
		}
                
        void FixedUpdate () {
            if (characterController.enabled)
                UpdateCapsuleHeight();
        }
        
        void UpdateCapsuleHeight () {
            float charHeight = calculateCharHeight;
            characterController.height = charHeight;
            characterController.center = new Vector3(0, charHeight * .5f, 0);       
        }
    }
}