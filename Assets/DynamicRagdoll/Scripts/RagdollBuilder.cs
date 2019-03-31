using System.Collections.Generic;
using UnityEngine;

namespace DynamicRagdoll {
    public static class RagdollBuilder {
        
		/*
			Methods used to build the ragdoll during runtime

			a base ragdoll is built 
			then the variables, (like joint limits and rigidbody masses), 
			are adjusted via a ragdoll profile (or the pre supplied default one if none)
		*/
		

        static void DestroyComponents<T> (GameObject g) where T : Component {
            foreach (var c in g.GetComponentsInChildren<T>()) {
                if (Application.isPlaying)
                    GameObject.Destroy(c);
                else
                    GameObject.DestroyImmediate(c);
            }
        }
        
        public static void EraseRagdoll (Animator animator) {
            //check for null animator
            if (animator == null) {
                Debug.Log("No animator found... (EraseRagdoll)");
                return;
            }
            
            GameObject c = animator.GetBoneTransform(HumanBodyBones.Hips).gameObject;
            DestroyComponents<ConfigurableJoint>(c);
            DestroyComponents<Rigidbody>(c);
            DestroyComponents<Collider>(c);
		}	

        /*
            call when you jsut need to get the ragdoll bone references but not add the components
        */

        public static Dictionary<HumanBodyBones, Ragdoll.Bone> BuildRagdollFromPrebuilt (Animator animator, RagdollProfile profile, out float initialHeadOffsetFromChest) {
			initialHeadOffsetFromChest = -1;
            
            //check for null animator
            if (animator == null) {
                Debug.Log("No animator found...(BuildRagdollFull");
                return null;
            }

			Dictionary<HumanBodyBones, Ragdoll.Bone> bones = new Dictionary<HumanBodyBones, Ragdoll.Bone>();
            for (int i = 0; i < Ragdoll.usedBones.Length; i++) {
				Transform boneT = animator.GetBoneTransform(Ragdoll.usedBones[i]);
				if (boneT == null) {
					Debug.LogError("Cant find: " + Ragdoll.usedBones[i] + " on ragdoll:", animator);
                    return null;
				}
				bones.Add(Ragdoll.usedBones[i], new Ragdoll.Bone(boneT));	
			}
			
            //initial head position from chest (used for resizing chest collider based on head offset)				
			initialHeadOffsetFromChest = Ragdoll.CalculateHeadOffsetFromChest(bones);


            //if no profile we already pre built and adjusted to defaults
            
            //else: update to profile values if using a custom profile
            if (profile) {
					
                Ragdoll.UpdateBonesToProfileValues(bones, profile, initialHeadOffsetFromChest);
            }
            
			return bones;
		}

        /*
            call when building a ragdoll from scratch
            animator component must be humanoid
        */
        public static Dictionary<HumanBodyBones, Ragdoll.Bone> BuildRagdollFull (Animator animator, RagdollProfile profile, out float initialHeadOffsetFromChest) {
            initialHeadOffsetFromChest = -1;
            //check for null animator
            if (animator == null) {
                Debug.Log("No animator found...(BuildRagdollFull");
                return null;
            }

            EraseRagdoll(animator);
            
            //null profile so it doesnt try adjust values before we add collider/joint/rb components
            Dictionary<HumanBodyBones, Ragdoll.Bone> bones = BuildRagdollFromPrebuilt(animator, null, out initialHeadOffsetFromChest);
            if (bones == null) {
                return null;
            }

            //add capsules
            BuildCapsules(bones);
            AddBreastColliders(bones);
            AddHeadCollider(bones);

            //add rigidbodies
            BuildBodies(bones);
            
            //add joints
            BuildJoints(bones);

            //initial head position from chest (used for resizing chest collider based on head offset)				
			initialHeadOffsetFromChest = Ragdoll.CalculateHeadOffsetFromChest(bones);
            
            Ragdoll.UpdateBonesToProfileValues(bones, profile, initialHeadOffsetFromChest);

            return bones;
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
			
        static void BuildCapsules(Dictionary<HumanBodyBones, Ragdoll.Bone> bones)
        {   
            foreach (var k in capsuleBones)
            {
				Ragdoll.Bone bone = bones[k];
                
				int direction = k.ToString().Contains("Arm") ? 0 : 1;
                
                float distance;
                if (upperCapsuleBones.Contains(k)) {
					distance = bone.transform.InverseTransformPoint(bones[GetChildBone(k)].position)[direction];
                }
                else
                {
                    Vector3 endPoint = (bone.position - bones[GetParentBone(k)].position) + bone.position;
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
        

        static Bounds GetBoxBounds(Ragdoll.Bone bone, Ragdoll.Bone topCutoff, Ragdoll.Bone[] encapsulate, bool adjustMin)
        {
            Bounds bounds = new Bounds();

            //encapsulate upper arms and upper legs positions
            for (int i = 0; i < 4; i++) bounds.Encapsulate(bone.transform.InverseTransformPoint(encapsulate[i].position));
            
            if (adjustMin) {
                Vector3 min = bounds.min;
                min.y = 0;
                bounds.min = min;
            }

            //adjust max bounds based on next bone
            Vector3 max = bounds.max;
            max.y = bone.transform.InverseTransformPoint(topCutoff.position).y;
			
            bounds.max = max;
            return bounds;
        }
        

        static void AddBreastColliders(Dictionary<HumanBodyBones, Ragdoll.Bone> bones)
        {
            Ragdoll.Bone[] encapsulate = new Ragdoll.Bone[] { 
                bones[HumanBodyBones.LeftUpperArm], bones[HumanBodyBones.RightUpperArm], 
                bones[HumanBodyBones.LeftUpperLeg], bones[HumanBodyBones.RightUpperLeg], 
            };
            Ragdoll.Bone hips = bones[HumanBodyBones.Hips];
            Ragdoll.Bone chest = bones[HumanBodyBones.Chest];
			Ragdoll.Bone head = bones[HumanBodyBones.Head];
            hips.collider = AddBoxCollider(hips, GetBoxBounds(hips, chest, encapsulate, false));
            chest.collider = AddBoxCollider(chest, GetBoxBounds(chest, head, encapsulate, true));
        }
		
        static Collider AddBoxCollider(Ragdoll.Bone bone, Bounds bounds) {
            BoxCollider box = bone.AddComponent<BoxCollider>();
            box.center = bounds.center;
            box.size = bounds.size;
			return box;
        }

        static void AddHeadCollider(Dictionary<HumanBodyBones, Ragdoll.Bone> bones) {
            bones[HumanBodyBones.Head].collider = bones[HumanBodyBones.Head].AddComponent<SphereCollider>();
        }

        static void BuildBodies(Dictionary<HumanBodyBones, Ragdoll.Bone> bones)
        {
            foreach (var k in bones.Keys)
                bones[k].rigidbody = bones[k].AddComponent<Rigidbody>();
        }

        static void BuildJoints(Dictionary<HumanBodyBones, Ragdoll.Bone> bones)
        {
            foreach (var k in bones.Keys)
            {
				if (k == HumanBodyBones.Hips) continue;
                
				Ragdoll.Bone bone = bones[k];
                
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
            }
        }
    }
}
