using UnityEngine;
using System.Collections;
using System.Collections.Generic;
namespace DynamicRagdoll {


    /*
        enables simple dismemberment on a ragdoll

        when a bone is dismemebered, we set it's scale to a small amount
        
        and disable physics on the bone
    */

    public class DismembermentModule //: MonoBehaviour
    {

        const string ignorePhysicsLayer = "Ignore Raycast";
        const float dismemberScale = .01f;
        
        // keep track of our dismembered bones
        Dictionary<HumanBodyBones, RagdollTransform> dismemberedBones = new Dictionary<HumanBodyBones, RagdollTransform>();

        // needs to be in a specific order, so that when repairing, parent bones are repaired first
        // that way auto configure joints place our anchor point at the correct position...
        static readonly List<HumanBodyBones> dismemberableBones = new List<HumanBodyBones> () {
            HumanBodyBones.Head,
            HumanBodyBones.RightUpperLeg,
            HumanBodyBones.LeftUpperLeg, 
            HumanBodyBones.RightUpperArm, 
            HumanBodyBones.LeftUpperArm,

            HumanBodyBones.RightLowerLeg,
            HumanBodyBones.LeftLowerLeg, 
            HumanBodyBones.RightLowerArm,
            HumanBodyBones.LeftLowerArm,
        };
        
        void InitializeDismemberedBones () {
            if (dismemberedBones.Count != dismemberableBones.Count) {
                dismemberedBones.Clear();
                for (int i = 0; i < dismemberableBones.Count; i++) {
                    dismemberedBones.Add(dismemberableBones[i], null);
                }
            }
        }


        /*
            repairs a bone, if dismemebred
        */
        public void RepairBone (HumanBodyBones bone) {
            InitializeDismemberedBones();
            RepairBone( dismemberedBones[bone] );
            dismemberedBones[bone] = null;
        }
            
        /*
            repairs all dismembered bones
        */
        public void RepairBones ( ) {
            for (int i = 0; i < dismemberableBones.Count; i++) 
                RepairBone(dismemberableBones[i]);
        }

        void RepairBone (RagdollTransform bone) {
            if (bone == null)
                return;
            
            bool isFollow = bone.joint == null;
            
            // if (!isFollow) {
            //     bone.transform.localRotation = bone.originalRotation;
            //     bone.transform.localPosition = bone.originalPosition;
            //     bone.SetKinematic(false);
            // }
            
            bone.transform.localScale = Vector3.one;

            if (!isFollow) {
                // bone.joint.connectedBody = bone.connectedBody;
                // bone.collider.isTrigger = false;
                bone.collider.gameObject.layer = Ragdoll.layer;
                
                if (bone.followTarget != null) {
                    RepairBone(bone.followTarget);
                }            
            }
            
        }

        public bool BoneDismembered (RagdollTransform bone) {
            return BoneDismembered(bone.bone.bone);
        }
        public bool BoneDismembered (HumanBodyBones bone) {
            if (!dismemberableBones.Contains(bone))
                return false;
            
            InitializeDismemberedBones();
            return dismemberedBones[bone] != null;
        }

        /*
            delay the dismemberment for a few frames in order for physics exerted on the bone
            to play out before we disable it
        */
        IEnumerator DismemberBoneDelayed (Ragdoll ragdoll, RagdollTransform bone, int delay) {
            for (int i = 0; i < delay; i++) yield return new WaitForFixedUpdate();
            DismemberBone(ragdoll, bone, 0, false);
        }

        public void DismemberBone (Ragdoll ragdoll, RagdollTransform bone, int delay, bool isFollowBone=false) {
            InitializeDismemberedBones();

            if (!isFollowBone) {
                if (!dismemberableBones.Contains(bone.bone.bone)) {
                    return;
                }

                if (delay > 0) {
                    ragdoll.StartCoroutine(DismemberBoneDelayed(ragdoll, bone, delay));
                    return;
                }

                dismemberedBones[bone.bone.bone] = bone;
                
                bone.collider.gameObject.layer = LayerMask.NameToLayer(ignorePhysicsLayer);
                
                // bone.joint.connectedBody = null;
                // bone.collider.isTrigger = true;
                // bone.SetKinematic(true);
                


                // dismember the follow target, so it reflects our dismemberment
                // (dismembers the animated model...)
                if (bone.followTarget != null) {
                    DismemberBone(ragdoll, bone.followTarget, 0, true);
                }

                // dismember and disable any child bones (helps physics from getting all jittery)
                HumanBodyBones childBone = Ragdoll.GetChildBone(bone.bone.bone);
                if (childBone != HumanBodyBones.Hips) {
                    RagdollTransform childBoneTransform = ragdoll.GetBone(childBone);
                    if (!BoneDismembered(childBoneTransform)) {
                        DismemberBone(ragdoll, childBoneTransform, 0, false);
                    }
                }
            }
            
            bone.transform.localScale = Vector3.one * dismemberScale;             
        }
    }


    public partial class Ragdoll : MonoBehaviour {
        DismembermentModule dismemberment = new DismembermentModule();

        public void RepairBone (HumanBodyBones bone) {
            dismemberment.RepairBone(bone);
        }
        public void RepairBones ( ) {
            dismemberment.RepairBones();
        }
        public bool BoneDismembered (RagdollTransform bone) {
            return dismemberment.BoneDismembered(bone);
        }
        public void DismemberBone (RagdollTransform bone, int delay) {
            dismemberment.DismemberBone(this, bone, delay, false);       
        }
    }
}