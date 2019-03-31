using System.Collections.Generic;
using UnityEngine;

namespace DynamicRagdoll {
	/*

		Handles the actual ragdoll model object, 
		add this script to the ragdoll's root 
		(with an animator that has a humanoid avatar setup)

		you can either pre build the base ragdoll in the editor or let it be done on awake

		builds a ragdoll on this animated character model

		then adjusts it's values based on the ragdoll profile you give it

		while in editor mode values are adjusted every update loop for easier tweaking
		of values
		
	*/
	
	[RequireComponent(typeof(Animator))]
	public class Ragdoll : MonoBehaviour {

		public static HumanBodyBones[] ragdollUsedBones = new HumanBodyBones[] {
			HumanBodyBones.Hips, HumanBodyBones.Chest, HumanBodyBones.Head, 
			
			HumanBodyBones.RightLowerLeg, HumanBodyBones.LeftLowerLeg, 
			HumanBodyBones.RightUpperLeg, HumanBodyBones.LeftUpperLeg, 

			HumanBodyBones.RightLowerArm, HumanBodyBones.LeftLowerArm, 
			HumanBodyBones.RightUpperArm, HumanBodyBones.LeftUpperArm, 
		};

		public RagdollProfile ragdollProfile;
		[HideInInspector] public bool preBuilt;

		public class Bone {
			public Rigidbody rigidbody;
			public ConfigurableJoint joint;
			public Collider collider;
			public Transform transform;
			public Bone parent;

			public Bone() {}
			public Bone (Transform bone) {
				transform = bone;
				rigidbody = bone.GetComponent<Rigidbody>();
				joint = bone.GetComponent<ConfigurableJoint>();
				collider = bone.GetComponent<Collider>(); 
			}
		}
		

		Renderer[] allRenderers;
		Dictionary<HumanBodyBones, Bone> myBones = new Dictionary<HumanBodyBones, Bone>();
		
		bool initializedValues;
		
		//for sizing bounds of chest to fit head offset
		float initialHeadOffsetFromChest;

		public void EnableRenderers(bool enabled) {
			if (!initializedValues) {
				Awake();
			}
			for (int i = 0; i < allRenderers.Length; i++) {
				allRenderers[i].enabled = enabled;
			}
		}

		public Bone GetBone (HumanBodyBones bone) {
			if (!initializedValues) {
				Awake();
			}
			Bone r;
			if (myBones.TryGetValue(bone, out r)) {
				return r;
			}	
			Debug.LogWarning("cant find: " + bone + " on ragdoll " + transform.name);
			return null;
		}

		public Bone RootBone () {
			return GetBone(HumanBodyBones.Hips);
		}
		

		void Awake () {

			if (!initializedValues) {
				
				initializedValues = true;

				//get all renderers
				allRenderers = GetComponentsInChildren<Renderer>();
				
				//build the ragdoll if not built in editor
				if (!preBuilt) {
					BuildRagdoll(this, out myBones, out initialHeadOffsetFromChest);
				}
				else {
					//build bones dictionary
					myBones = BuildBones(GetComponent<Animator>());

					//update to profile values if using a custom profile
					if (ragdollProfile != null) {
						//initial head position from chest (used for resizing chest collider based on head offset)
						initialHeadOffsetFromChest = myBones[HumanBodyBones.Chest].transform.InverseTransformPoint(myBones[HumanBodyBones.Head].transform.position).y;
						//update ragdoll with profile values
						UpdateBonesToProfileValues();
					}	
				}

				//get the ragdoll layer
				int ragdollLayer = LayerMask.NameToLayer("Ragdoll");
				foreach (var k in myBones.Keys) {
					myBones[k].transform.gameObject.layer = ragdollLayer;
				}
			}
		}

		#if UNITY_EDITOR
		[Header("Editor Only")]
		public bool setValuesUpdate = true;
		
		//update values during runtime (if not in build)
		//for easier adjustments
		void Update () {
			if (setValuesUpdate) {
				UpdateBonesToProfileValues();
			}
		}
		#endif
		void UpdateBonesToProfileValues () {
			if (!ragdollProfile) {
				return;
			}
			UpdateBonesToProfileValues(myBones, ragdollProfile.bones, ragdollProfile.headOffset, initialHeadOffsetFromChest);
		}

		public static void UpdateBonesToProfileValues(Ragdoll ragdoll) {
			UpdateBonesToProfileValues(ragdoll.GetComponent<Animator>(), ragdoll.ragdollProfile);
		}
		public static void UpdateBonesToProfileValues(Animator characterAnimator, RagdollProfile profile) {
			UpdateBonesToProfileValues(BuildBones(characterAnimator), profile);
		}
		public static void UpdateBonesToProfileValues(Dictionary<HumanBodyBones, Bone> bones, RagdollProfile profile) {
			float initialHeadOffsetFromChest = bones[HumanBodyBones.Chest].transform.InverseTransformPoint(bones[HumanBodyBones.Head].transform.position).y;
			UpdateBonesToProfileValues(bones, profile != null ? profile.bones : RagdollProfile.defaultBoneProfiles, profile != null ? profile.headOffset : RagdollProfile.defaultHeadOffset, initialHeadOffsetFromChest);
		}
		static void UpdateBonesToProfileValues (Dictionary<HumanBodyBones, Bone> bones, RagdollProfile.BoneProfile[] boneProfiles, Vector3 headOffset, float initialHeadOffsetFromChest) {
			//clamp head offset (values too high or too low become unstable for some reason)
			headOffset.y = Mathf.Clamp(headOffset.y, -initialHeadOffsetFromChest + .1f, 2);
			
			for (int i = 0; i < boneProfiles.Length; i++) {
				RagdollProfile.BoneProfile boneProfile = boneProfiles[i];
				Bone bone = bones[boneProfile.bone];

				//set rigidbody values for bone
				UpdateRigidbodyToProfile(bone.rigidbody, boneProfile);
				
				//adjust collider values for bone
				UpdateColliderToProfile (bone.collider, boneProfile, headOffset, initialHeadOffsetFromChest);

				//set joint values
				if (bone.joint) {
					UpdateJointToProfile(bone.joint, boneProfile, headOffset);
				}
			}
		}
		static void UpdateColliderToProfile (Collider collider, RagdollProfile.BoneProfile boneProfile, Vector3 headOffset, float initialHeadOffsetFromChest) {
			//change physic material
			collider.sharedMaterial = boneProfile.colliderMaterial;

			if (boneProfile.bone == HumanBodyBones.Head) {

				//adjust the head collider based on headRadius and head Offset
				SphereCollider sphere = collider as SphereCollider;
				sphere.radius = boneProfile.colliderRadius;
				sphere.center = new Vector3(headOffset.x, boneProfile.colliderRadius + headOffset.y, headOffset.z);
			}
			else if (boneProfile.bone == HumanBodyBones.Chest || boneProfile.bone == HumanBodyBones.Hips) {
				
				BoxCollider box = collider as BoxCollider;
				
				Vector3 center = box.center;
				Vector3 size = box.size;
				
				if (boneProfile.bone == HumanBodyBones.Chest) {
					//adjust the chest collider, so it's top reaches the head collider joint
					
					Bounds chestBounds = new Bounds(center, size);
					Vector3 max = chestBounds.max;
					max.y = initialHeadOffsetFromChest + headOffset.y;
					chestBounds.max = max;
				
					center = chestBounds.center;
					size = chestBounds.size;
				}
				
				//adjust chest and hips Z thickness and offset
				//maybe some models are 'fatter' than others
				center.z = boneProfile.boxZOffset;
				size.z = boneProfile.boxZSize;
				
				box.center = center;
				box.size = size;
			}
			else {
				//adjust the radius of the arms and leg capsules
				CapsuleCollider capsule = collider as CapsuleCollider;
				capsule.radius = boneProfile.colliderRadius;
			}
		}

		static void UpdateJointToProfile (ConfigurableJoint joint, RagdollProfile.BoneProfile boneProfile, Vector3 headOffset) {
			//adjust anchor for head offset
			if (boneProfile.bone == HumanBodyBones.Head) {
				if (joint.anchor != headOffset)
					joint.anchor = headOffset;
			}
			
			//adjust axes (changing every fram was slow, so checking if same value first)
			if (joint.axis != boneProfile.axis1) {
				joint.axis = boneProfile.axis1;
			}
			if (joint.secondaryAxis != boneProfile.axis2) {
				joint.secondaryAxis = boneProfile.axis2;
			}
			
			
			//adjust limits (0 if forceOff is enabled)
			var l = joint.lowAngularXLimit;
			l.limit = boneProfile.forceOff ? 0 : boneProfile.angularXLimit.x;
			joint.lowAngularXLimit = l;
			
			l = joint.highAngularXLimit;
			l.limit = boneProfile.forceOff ? 0 : boneProfile.angularXLimit.y;
			joint.highAngularXLimit = l;
			
			l = joint.angularYLimit;
			l.limit = boneProfile.forceOff ? 0 : boneProfile.angularYLimit;
			joint.angularYLimit = l;
			
			l = joint.angularZLimit;
			l.limit = boneProfile.forceOff ? 0 : boneProfile.angularZLimit;
			joint.angularZLimit = l;
		}
		static void UpdateRigidbodyToProfile (Rigidbody rigidbody, RagdollProfile.BoneProfile boneProfile) {
			//set rigidbody values for bone
			rigidbody.useGravity = true;
			rigidbody.maxAngularVelocity = boneProfile.maxAngularVelocity;
			rigidbody.angularDrag = boneProfile.angularDrag;
			rigidbody.angularDrag = boneProfile.drag;
			rigidbody.mass = boneProfile.mass;

			rigidbody.maxDepenetrationVelocity = boneProfile.maxDepenetrationVelocity;
			
			rigidbody.interpolation = boneProfile.interpolation;
			rigidbody.collisionDetectionMode = boneProfile.collisionDetection;
		}



		/*
			Methods used to build the ragdoll during runtime

			a base ragdoll is built 
			then the variables, (like joint limits and rigidbody masses), 
			are adjusted via the ragdoll profile
		*/
		#region RAGDOLL BUILDER
		
		static Dictionary<HumanBodyBones, Bone> BuildBones (Animator animator) {
			Dictionary<HumanBodyBones, Bone> ret = new Dictionary<HumanBodyBones, Bone>();
			for (int i = 0; i < ragdollUsedBones.Length; i++) {
				HumanBodyBones bone = ragdollUsedBones[i];
				Transform boneT = animator.GetBoneTransform(bone);
				if (boneT == null) {
					Debug.LogError("cant find: " + bone + " on ragdoll " + animator.name);
					continue;
				}
				ret.Add(bone, new Bone(boneT));	
			}
			return ret;
		}

        static void PrepareBones(out Dictionary<HumanBodyBones, Bone> bones, Animator animator)
        {
            bones = new Dictionary<HumanBodyBones, Bone>();

            Bone rootBone = new Bone();
            rootBone.transform = animator.GetBoneTransform(HumanBodyBones.Hips);
            rootBone.parent = null;
            bones.Add(HumanBodyBones.Hips, rootBone);

            AddJoint(animator, HumanBodyBones.LeftUpperLeg, bones, HumanBodyBones.Hips);//, Vector3.right, Vector3.forward);
            AddJoint(animator, HumanBodyBones.RightUpperLeg, bones, HumanBodyBones.Hips);//, Vector3.right, Vector3.forward);
        
            AddJoint(animator, HumanBodyBones.LeftLowerLeg, bones, HumanBodyBones.LeftUpperLeg);//, Vector3.right, Vector3.forward);
            AddJoint(animator, HumanBodyBones.RightLowerLeg, bones, HumanBodyBones.RightUpperLeg);//, Vector3.right, Vector3.forward);
        
            AddJoint(animator, HumanBodyBones.Chest, bones, HumanBodyBones.Hips);//, Vector3.right, Vector3.forward);
        
            AddJoint(animator, HumanBodyBones.LeftUpperArm, bones, HumanBodyBones.Chest);//, Vector3.forward, Vector3.up);
            AddJoint(animator, HumanBodyBones.RightUpperArm, bones, HumanBodyBones.Chest);//, Vector3.forward, Vector3.up);
        
            AddJoint(animator, HumanBodyBones.LeftLowerArm, bones, HumanBodyBones.LeftUpperArm);//, Vector3.up, Vector3.forward);
            AddJoint(animator, HumanBodyBones.RightLowerArm, bones, HumanBodyBones.RightUpperArm);//, Vector3.up, Vector3.forward);
        
            AddJoint(animator, HumanBodyBones.Head, bones, HumanBodyBones.Chest);//, Vector3.right, Vector3.forward);//, headOffset);
        }
        static void DestroyComponents<T> (GameObject g) where T : Component {
            foreach (var c in g.GetComponentsInChildren<T>()) {
                if (Application.isPlaying) {
                    Destroy(c);
                }
                else {
                    DestroyImmediate(c);
                }
            }
        }
        
        public static void EraseRagdoll (Animator anim) {
            GameObject c = anim.GetBoneTransform(HumanBodyBones.Hips).gameObject;
            DestroyComponents<ConfigurableJoint>(c);
            DestroyComponents<Rigidbody>(c);
            DestroyComponents<Collider>(c);
		}	
		public static void BuildRagdoll(Ragdoll ragdoll, out Dictionary<HumanBodyBones, Bone> bones, out float initialHeadOffsetFromChest) {
			BuildRagdoll(ragdoll.GetComponent<Animator>(), ragdoll.ragdollProfile, out bones, out initialHeadOffsetFromChest);
		}
		public static void BuildRagdoll(Ragdoll ragdoll){
			BuildRagdoll(ragdoll.GetComponent<Animator>(), ragdoll.ragdollProfile, out _, out _);
		}
			
        public static void BuildRagdoll(Animator anim, RagdollProfile profile, out Dictionary<HumanBodyBones, Bone> bones, out float initialHeadOffsetFromChest) {
            bones = null;
			initialHeadOffsetFromChest = -1;
			if (anim == null) {
                Debug.Log("No animator found...");
                return;
            }
            EraseRagdoll(anim);
            PrepareBones(out bones, anim);
            BuildCapsules(bones);
            AddBreastColliders(bones);
            AddHeadCollider(bones);
            BuildBodies(bones);
            BuildJoints(bones);

			initialHeadOffsetFromChest = bones[HumanBodyBones.Chest].transform.InverseTransformPoint(bones[HumanBodyBones.Head].transform.position).y;
			UpdateBonesToProfileValues(bones, profile != null ? profile.bones : RagdollProfile.defaultBoneProfiles, profile != null ? profile.headOffset : RagdollProfile.defaultHeadOffset, initialHeadOffsetFromChest);
		}

        static void AddJoint(Animator animator, HumanBodyBones boneType, Dictionary<HumanBodyBones, Bone> bones, HumanBodyBones parentBone)//, Vector3 worldTwistAxis, Vector3 worldSwingAxis)
        {
            Bone bone = new Bone();
            bone.transform = animator.GetBoneTransform(boneType);
            bone.parent = bones[parentBone];
            bones.Add( boneType, bone );
        }

        static void BuildCapsules(Dictionary<HumanBodyBones, Bone> bones)
        {
            HashSet<HumanBodyBones> capsuleBones = new HashSet<HumanBodyBones> () {
                HumanBodyBones.LeftUpperArm, HumanBodyBones.RightUpperArm,
                HumanBodyBones.LeftUpperLeg, HumanBodyBones.RightUpperLeg,
                HumanBodyBones.LeftLowerArm, HumanBodyBones.RightLowerArm,
                HumanBodyBones.LeftLowerLeg, HumanBodyBones.RightLowerLeg,
            };
			HashSet<HumanBodyBones> upperBones = new HashSet<HumanBodyBones> () {
                HumanBodyBones.LeftUpperArm, HumanBodyBones.RightUpperArm,
                HumanBodyBones.LeftUpperLeg, HumanBodyBones.RightUpperLeg,
            };
			
			
            foreach (var k in capsuleBones)
            {
				Bone bone = bones[k];
                
				bool isArm = k.ToString().Contains("Arm");
                int direction = isArm ? 0 : 1;
                float distance;

                if (upperBones.Contains(k)) {

					HumanBodyBones childBoneType;
                    if (k == HumanBodyBones.LeftUpperArm)
                        childBoneType = HumanBodyBones.LeftLowerArm;
                    else if (k == HumanBodyBones.RightUpperArm)
                        childBoneType = HumanBodyBones.RightLowerArm;
                    else if (k == HumanBodyBones.LeftUpperLeg)
                        childBoneType = HumanBodyBones.LeftLowerLeg;
                    else //if (k == HumanBodyBones.RightUpperLeg)
                        childBoneType = HumanBodyBones.RightLowerLeg;

                    Vector3 endPoint = bones[childBoneType].transform.position;

					distance = bone.transform.InverseTransformPoint(endPoint)[direction];
                }
                else
                {
                    Vector3 endPoint = (bone.transform.position - bone.parent.transform.position) + bone.transform.position;
                    
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

                CapsuleCollider collider = bone.transform.gameObject.AddComponent<CapsuleCollider>();
                collider.direction = direction;

                Vector3 center = Vector3.zero;
                center[direction] = distance * 0.5F;
                collider.center = center;

                collider.height = Mathf.Abs(distance);
				collider.radius = defaultCapsulRadius;
				bone.collider = collider;
            }
        }

        static void BuildBodies(Dictionary<HumanBodyBones, Bone> bones)
        {
            foreach (var k in bones.Keys)
                bones[k].rigidbody = bones[k].transform.gameObject.AddComponent<Rigidbody>();
        }

        static void BuildJoints(Dictionary<HumanBodyBones, Bone> bones)
        {
            foreach (var k in bones.Keys)
            {
				if (k == HumanBodyBones.Hips) 
					continue;
                
				Bone bone = bones[k];
                
                ConfigurableJoint joint = bone.transform.gameObject.AddComponent<ConfigurableJoint>();
				bone.joint = joint;
                
                // Setup connection and axis
                //joint.autoConfigureConnectedAnchor = false;
                joint.enablePreprocessing = false; // turn off to handle degenerated scenarios, like spawning inside geometry.
                
                joint.anchor = Vector3.zero;
                joint.connectedBody = bone.parent.rigidbody;
				
                // Setup limits
                SoftJointLimit limit = new SoftJointLimit();
                limit.contactDistance = 0; // default to zero, which automatically sets contact distance.
                limit.limit = 0;
                
                joint.lowAngularXLimit = joint.highAngularXLimit = joint.angularYLimit = joint.angularZLimit = limit;
                
                joint.xMotion = joint.yMotion = joint.zMotion = ConfigurableJointMotion.Locked;
                joint.angularXMotion = joint.angularYMotion = joint.angularZMotion= ConfigurableJointMotion.Limited;
                
                joint.rotationDriveMode = RotationDriveMode.Slerp;
            }
        }
        

        static Bounds GetBreastBounds(Transform relativeTo, Vector3[] encapsulatePositions)
        {
            Bounds bounds = new Bounds();
            for (int i = 0; i < 4; i++) bounds.Encapsulate(relativeTo.InverseTransformPoint(encapsulatePositions[i]));
            return bounds;
        }

        static void AddBreastColliders(Dictionary<HumanBodyBones, Bone> bones)
        {
            // Middle spine and pelvis
            
            Vector3[] encapsulatePositions = new Vector3[] {
                bones[HumanBodyBones.LeftUpperArm].transform.position,
                bones[HumanBodyBones.RightUpperArm].transform.position,
                bones[HumanBodyBones.LeftUpperLeg].transform.position,
                bones[HumanBodyBones.RightUpperLeg].transform.position,
            };

            Transform hips = bones[HumanBodyBones.Hips].transform;
            Transform chest = bones[HumanBodyBones.Chest].transform;
			Transform head = bones[HumanBodyBones.Head].transform;
            
            Bounds bounds = GetBreastBounds(hips, encapsulatePositions);
			bounds.max = AdjustBoxMaxBounds(bounds, hips, chest);
            bones[HumanBodyBones.Hips].collider = AddBoxCollider(hips.gameObject, bounds);
			
            //chest
            bounds = GetBreastBounds(chest, encapsulatePositions);
            Vector3 min = bounds.min;
            min.y = 0;
            bounds.min = min;
			bounds.max = AdjustBoxMaxBounds(bounds, chest, head);
            bones[HumanBodyBones.Chest].collider = AddBoxCollider(chest.gameObject, bounds);
        }
		const float defaultChestDepth = .1f;
		const float defualtHeadRadius = .125f;
		const float defaultCapsulRadius = .1f;

        
		static Vector3 AdjustBoxMaxBounds (Bounds bounds, Transform relativeTo, Transform cutoff) {
			Vector3 max = bounds.max;
            max.y = relativeTo.InverseTransformPoint(cutoff.position).y;
			max.z = defaultChestDepth;
            return max;
		}
		static Collider AddBoxCollider(GameObject g, Bounds bounds) {
            BoxCollider box = g.AddComponent<BoxCollider>();
            box.center = bounds.center;
            box.size = bounds.size;
			return box;
        }
        static void AddHeadCollider(Dictionary<HumanBodyBones, Bone> bones) {
            SphereCollider sphere = bones[HumanBodyBones.Head].transform.gameObject.AddComponent<SphereCollider>();
            sphere.radius = defualtHeadRadius;
            sphere.center = new Vector3(0, defualtHeadRadius, 0);

			bones[HumanBodyBones.Head].collider = sphere;
        }

		#endregion
    }
}

