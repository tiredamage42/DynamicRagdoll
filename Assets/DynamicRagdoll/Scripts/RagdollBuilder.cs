using System.Collections.Generic;
using UnityEngine;

namespace DynamicRagdoll {
    /*
        Methods used to build the ragdoll during runtime

        a base ragdoll is built 
        then the variables, (like joint limits and rigidbody masses), 
        are adjusted via a ragdoll profile (or the pre supplied default one if none)
    */
    public static class RagdollBuilder {
        
		
        static void DestroyComponents<T> (GameObject g) where T : Component {
            foreach (var c in g.GetComponentsInChildren<T>()) {
                if (Application.isPlaying)
                    GameObject.Destroy(c);
                else
                    GameObject.DestroyImmediate(c);
            }
        }

        /*
            remove ragdoll added components below the animator
        */
        public static void EraseRagdoll (Animator animator) {
            //check for null animator
            if (animator == null) {
                Debug.LogError("No animator found... (EraseRagdoll)");
                return;
            }
            
            GameObject gameObject = animator.GetBoneTransform(HumanBodyBones.Hips).gameObject;
            DestroyComponents<ConfigurableJoint>(gameObject);
            DestroyComponents<Rigidbody>(gameObject);
            DestroyComponents<Collider>(gameObject);
            DestroyComponents<RagdollBone>(gameObject);
            
		}	

        /*
            call to get the ragdoll bone references

            animator component must be humanoid

            set 'addComponents' to true to add the ragdoll components

            if non physics is set to true, we just build the references but dont check for (or add)
            Rigidbodies, joints, and colliders...
        */
        //public static bool BuildRagdoll (Animator animator, RagdollProfile profile, bool addComponents, bool nonPhysics, out float initialHeadOffsetFromChest, out Ragdoll.Element[] allBones, out Dictionary<HumanBodyBones, Ragdoll.Element> physicsBones) {
		public static bool BuildRagdollElements (Animator animator, out Ragdoll.Element[] allElements, out Dictionary<HumanBodyBones, Ragdoll.Element> boneElements) {
		
        	// initialHeadOffsetFromChest = -1;
            allElements = null;
            boneElements = null;
            
            //check for null animator
            if (animator == null) {
                Debug.LogError("No animator found...(BuildRagdollFromPrebuilt");
                return false;// null;
            }

            List<Ragdoll.Element> allBonesList = new List<Ragdoll.Element>();
            
            // instance ids of the bone transforms so we dont re-add them when checking all children bones below
            HashSet<int> usedPhysicsTransforms = new HashSet<int>();
            
            //build bones list that use physics
			boneElements = new Dictionary<HumanBodyBones, Ragdoll.Element>();

            for (int i = 0; i < Ragdoll.bonesCount; i++) {
                HumanBodyBones humanBodyBone = Ragdoll.humanBones[i];

				Transform boneT = animator.GetBoneTransform(humanBodyBone);

				if (boneT == null) {
					Debug.LogError("Cant find physics bone: " + humanBodyBone + " on ragdoll: " + animator.name);
                    boneElements = null;
                    usedPhysicsTransforms = null;
                    return false;
				}

                usedPhysicsTransforms.Add(boneT.GetInstanceID());

                Ragdoll.Element ragdollBone = new Ragdoll.Element(boneT, false, true, i == 0);

				boneElements.Add(humanBodyBone, ragdollBone);	
                allBonesList.Add(ragdollBone);
			}

            //build other non physics bones

            //get all transform children of the hip bone
            Transform[] allChildren = allBonesList[0].transform.GetComponentsInChildren<Transform>();

			for (int i = 0; i < allChildren.Length; i++) {
                Transform child = allChildren[i];
                //if its not a physics bone
                if (!usedPhysicsTransforms.Contains(child.GetInstanceID())) {
                    bool isPhysicsParent = child.GetComponentInChildren<Rigidbody>() != null;
					allBonesList.Add(new Ragdoll.Element(child, isPhysicsParent, false, false));
                }
			}
			allElements = allBonesList.ToArray();

            // if (!nonPhysics) {

            //     if (addComponents) {

            //         EraseRagdoll(animator);

            //         //add capsules
            //         BuildCapsules(physicsBones);
            //         AddBreastColliders(physicsBones);
            //         AddHeadCollider(physicsBones);

            //         //add rigidbodies
            //         BuildRigidodies(physicsBones);

            //         //add joints
            //         BuildJoints(physicsBones);
                    
            //         //add bone components
            //         BuildBones(physicsBones);
            //     }
            
            //     //initial head position from chest (used for resizing chest collider based on head offset)				
            //     initialHeadOffsetFromChest = physicsBones[HumanBodyBones.Chest].transform.InverseTransformPoint(physicsBones[HumanBodyBones.Head].transform.position).y;

            //     // update the ragdoll to reflect it's profile values
            //     Ragdoll.UpdateBonesToProfileValues(physicsBones, profile, initialHeadOffsetFromChest);
            // }
            return true;
		}

        public static void BuildBones (Animator animator, RagdollProfile profile, bool addComponents, Dictionary<HumanBodyBones, Ragdoll.Element> boneElements, out float initialHeadOffsetFromChest) {
            if (addComponents) {

                EraseRagdoll(animator);

                //add capsules
                BuildCapsules(boneElements);
                AddBreastColliders(boneElements);
                AddHeadCollider(boneElements);

                //add rigidbodies
                BuildRigidodies(boneElements);

                //add joints
                BuildJoints(boneElements);
                
                //add bone components
                BuildBones(boneElements);
            }
            
            //initial head position from chest (used for resizing chest collider based on head offset)				
            initialHeadOffsetFromChest = boneElements[HumanBodyBones.Chest].transform.InverseTransformPoint(boneElements[HumanBodyBones.Head].transform.position).y;

            // update the ragdoll to reflect it's profile values
            Ragdoll.UpdateBonesToProfileValues(boneElements, profile, initialHeadOffsetFromChest);
    
        }
        
        

        static HumanBodyBones GetParentBone (HumanBodyBones bone) {
            switch (bone) {
                case HumanBodyBones.Chest:          return HumanBodyBones.Hips;
                case HumanBodyBones.Head:           return HumanBodyBones.Chest;
                case HumanBodyBones.RightLowerLeg:  return HumanBodyBones.RightUpperLeg;
                case HumanBodyBones.LeftLowerLeg:   return HumanBodyBones.LeftUpperLeg;
                case HumanBodyBones.RightUpperLeg:  return HumanBodyBones.Hips;
                case HumanBodyBones.LeftUpperLeg:   return HumanBodyBones.Hips;
                case HumanBodyBones.RightLowerArm:  return HumanBodyBones.RightUpperArm;
                case HumanBodyBones.LeftLowerArm:   return HumanBodyBones.LeftUpperArm;
                case HumanBodyBones.RightUpperArm:  return HumanBodyBones.Chest;
                case HumanBodyBones.LeftUpperArm:   return HumanBodyBones.Chest;
            }
            return HumanBodyBones.Hips;
        }
        static HumanBodyBones GetChildBone (HumanBodyBones bone) {
            switch (bone) {
                case HumanBodyBones.RightUpperLeg:  return HumanBodyBones.RightLowerLeg;
                case HumanBodyBones.LeftUpperLeg:   return HumanBodyBones.LeftLowerLeg;
                case HumanBodyBones.RightUpperArm:  return HumanBodyBones.RightLowerArm;
                case HumanBodyBones.LeftUpperArm:   return HumanBodyBones.LeftLowerArm;
            }
            return HumanBodyBones.Hips;
        }
        static HashSet<HumanBodyBones> capsuleBones = new HashSet<HumanBodyBones>() {
            HumanBodyBones.RightUpperLeg, HumanBodyBones.RightLowerLeg,
            HumanBodyBones.LeftUpperLeg, HumanBodyBones.LeftLowerLeg,
            HumanBodyBones.RightUpperArm, HumanBodyBones.RightLowerArm,
            HumanBodyBones.LeftUpperArm, HumanBodyBones.LeftLowerArm,
        };
        static HashSet<HumanBodyBones> upperCapsuleBones = new HashSet<HumanBodyBones> () {
            HumanBodyBones.LeftUpperArm, HumanBodyBones.RightUpperArm,
            HumanBodyBones.LeftUpperLeg, HumanBodyBones.RightUpperLeg,
        };
			
        static void BuildCapsules(Dictionary<HumanBodyBones, Ragdoll.Element> bones)
        {   
            foreach (var k in capsuleBones)
            {
				Ragdoll.Element bone = bones[k];
                
				int direction = k.ToString().Contains("Arm") ? 0 : 1;
                
                float distance;
                if (upperCapsuleBones.Contains(k)) {
					distance = bone.transform.InverseTransformPoint(bones[GetChildBone(k)].transform.position)[direction];
                }
                else
                {
                    Vector3 endPoint = (bone.transform.position - bones[GetParentBone(k)].transform.position) + bone.transform.position;
					distance = bone.transform.InverseTransformPoint(endPoint)[direction];

                    if (bone.transform.GetComponentsInChildren(typeof(Transform)).Length > 1)
                    {
                        Bounds bounds = new Bounds();
                        foreach (Transform child in bone.transform.GetComponentsInChildren(typeof(Transform)))
                            bounds.Encapsulate(bone.transform.InverseTransformPoint(child.position));
                        
                        if (distance > 0)
                            distance = bounds.max[direction];
                        else
                            distance = bounds.min[direction];
                    }
                }

                CapsuleCollider collider = bone.AddComponent<CapsuleCollider>();
                collider.direction = direction;

                Vector3 center = Vector3.zero;
                center[direction] = distance * 0.5F;
                collider.center = center;

                collider.height = Mathf.Abs(distance);
				bone.collider = collider;
            }
        }
        

        static Bounds GetBoxBounds(Ragdoll.Element bone, Ragdoll.Element topCutoff, Ragdoll.Element[] encapsulate, bool adjustMin)
        {
            Bounds bounds = new Bounds();

            //encapsulate upper arms and upper legs positions
            for (int i = 0; i < 4; i++) bounds.Encapsulate(bone.transform.InverseTransformPoint(encapsulate[i].transform.position));
            
            if (adjustMin) {
                Vector3 min = bounds.min;
                min.y = 0;
                bounds.min = min;
            }

            //adjust max bounds based on next bone
            Vector3 max = bounds.max;
            max.y = bone.transform.InverseTransformPoint(topCutoff.transform.position).y;
			
            bounds.max = max;
            return bounds;
        }
        

        static void AddBreastColliders(Dictionary<HumanBodyBones, Ragdoll.Element> bones)
        {
            Ragdoll.Element[] encapsulate = new Ragdoll.Element[] { 
                bones[HumanBodyBones.LeftUpperArm], bones[HumanBodyBones.RightUpperArm], 
                bones[HumanBodyBones.LeftUpperLeg], bones[HumanBodyBones.RightUpperLeg], 
            };
            Ragdoll.Element hips = bones[HumanBodyBones.Hips];
            Ragdoll.Element chest = bones[HumanBodyBones.Chest];
			Ragdoll.Element head = bones[HumanBodyBones.Head];
            hips.collider = AddBoxCollider(hips, GetBoxBounds(hips, chest, encapsulate, false));
            chest.collider = AddBoxCollider(chest, GetBoxBounds(chest, head, encapsulate, true));
        }
		
        static Collider AddBoxCollider(Ragdoll.Element bone, Bounds bounds) {
            BoxCollider box = bone.AddComponent<BoxCollider>();
            box.center = bounds.center;
            box.size = bounds.size;
			return box;
        }

        static void AddHeadCollider(Dictionary<HumanBodyBones, Ragdoll.Element> bones) {
            bones[HumanBodyBones.Head].collider = bones[HumanBodyBones.Head].AddComponent<SphereCollider>();
        }

        static void BuildRigidodies(Dictionary<HumanBodyBones, Ragdoll.Element> bones)
        {
            foreach (var k in bones.Keys)
                bones[k].rigidbody = bones[k].AddComponent<Rigidbody>();
        }
        static void BuildBones(Dictionary<HumanBodyBones, Ragdoll.Element> bones)
        {
            foreach (var k in bones.Keys)
                bones[k].bone = bones[k].AddComponent<RagdollBone>();
        }
        

        static void BuildJoints(Dictionary<HumanBodyBones, Ragdoll.Element> bones)
        {
            foreach (var k in bones.Keys)
            {
				if (k == HumanBodyBones.Hips) continue;
                
				Ragdoll.Element bone = bones[k];
                
                bone.joint = bone.AddComponent<ConfigurableJoint>();
				
                // Setup connection and axis
                
                //bone.joint.autoConfigureConnectedAnchor = false;
                
                // turn off to handle degenerated scenarios, like spawning inside geometry.
                bone.joint.enablePreprocessing = false; 
                
                bone.joint.anchor = Vector3.zero;
                bone.joint.connectedBody = bones[GetParentBone(k)].rigidbody;
                
                // Setup limits
                SoftJointLimit limit = new SoftJointLimit();
                limit.contactDistance = 0; // default to zero, which automatically sets contact distance.
                limit.limit = 0;
                
                bone.joint.lowAngularXLimit = bone.joint.highAngularXLimit = bone.joint.angularYLimit = bone.joint.angularZLimit = limit;
                
                bone.joint.xMotion = bone.joint.yMotion = bone.joint.zMotion = ConfigurableJointMotion.Locked;
                bone.joint.angularXMotion = bone.joint.angularYMotion = bone.joint.angularZMotion= ConfigurableJointMotion.Limited;
                
                bone.joint.rotationDriveMode = RotationDriveMode.Slerp;

                bone.joint.projectionMode = JointProjectionMode.PositionAndRotation;
            }
        }
    }
}
