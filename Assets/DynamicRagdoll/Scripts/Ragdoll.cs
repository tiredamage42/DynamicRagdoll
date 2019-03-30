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
			HumanBodyBones.Hips, 
			HumanBodyBones.Chest, 
			HumanBodyBones.Head, 
			HumanBodyBones.RightLowerLeg, 
			HumanBodyBones.LeftLowerLeg, 
			HumanBodyBones.RightUpperLeg, 
			HumanBodyBones.LeftUpperLeg, 
			HumanBodyBones.RightLowerArm, 
			HumanBodyBones.LeftLowerArm, 
			HumanBodyBones.RightUpperArm, 
			HumanBodyBones.LeftUpperArm, 
		};

		public RagdollProfile ragdollProfile;
		[HideInInspector] public bool preBuilt;
		

		Renderer[] _allRenderers;
		Dictionary<HumanBodyBones, Rigidbody> rigidbodies = new Dictionary<HumanBodyBones, Rigidbody>();
		Dictionary<HumanBodyBones, ConfigurableJoint> joints = new Dictionary<HumanBodyBones, ConfigurableJoint>();
		Dictionary<HumanBodyBones, Collider> colliders = new Dictionary<HumanBodyBones, Collider>();
		
		bool initializedValues;

		//for sizing bounds of chest to fit head offset
		float initialHeadOffsetFromChest;


		public void EnableRenderers(bool enabled) {
			if (!initializedValues) {
				Awake();
			}
			for (int i = 0; i < _allRenderers.Length; i++) {
				_allRenderers[i].enabled = enabled;
			}
		}

		public Transform GetBone(HumanBodyBones bone) {
			Rigidbody r = GetRigidbody(bone);
			if (r != null) {
				return r.transform;
			}
			return null;
		}
		public Rigidbody GetRigidbody (HumanBodyBones bone) {
			if (!initializedValues) {
				Awake();
			}
			Rigidbody r;
			if (rigidbodies.TryGetValue(bone, out r)) {
				return r;
			}	
			Debug.LogWarning("cant find: " + bone + " on ragdoll " + transform.name);
			return null;
		}
		public Transform RootBone () {
			return GetBone(HumanBodyBones.Hips);
		}
		public Rigidbody RootRigidbody () {
			return GetRigidbody(HumanBodyBones.Hips);
		}

		void Awake () {

			if (!initializedValues) {

				
				Animator anim = GetComponent<Animator>();
				
				//build the ragdoll if not built in editor
				if (!preBuilt) {
					BuildRagdollBase(anim);
				}

				//get the ragdoll layer
				int layer = LayerMask.NameToLayer("Ragdoll");

				//get all renderers
				_allRenderers = GetComponentsInChildren<Renderer>();
							
				//initial head position from chest (used for resizing chest collider based on head offset)
				initialHeadOffsetFromChest = anim.GetBoneTransform(HumanBodyBones.Chest).InverseTransformPoint(anim.GetBoneTransform(HumanBodyBones.Head).position).y;
				
				for (int i = 0; i < ragdollUsedBones.Length; i++) {
					HumanBodyBones bone = ragdollUsedBones[i];

					Transform ragBone = anim.GetBoneTransform(bone);
					
					if (ragBone == null) {
						Debug.LogError("cant find: " + bone + " on ragdoll " + transform.name);
						continue;
					}

					//set to ragdoll layer
					ragBone.gameObject.layer = layer;
					
					rigidbodies.Add(bone, ragBone.GetComponent<Rigidbody>());
					joints.Add(bone, ragBone.GetComponent<ConfigurableJoint>());
					colliders.Add(bone, ragBone.GetComponent<Collider>());
				}


				if (!ragdollProfile) {
					Debug.LogWarning("No Ragdoll Profile on " + name);
				}
				else {
					//update ragdoll with profile values
					UpdateRagdollProfileValues();
				}
			
				initializedValues = true;
			}
		}

		#if UNITY_EDITOR
		//update values during runtime (if not in build)
		//for easier adjustments
		void Update () {
			UpdateRagdollProfileValues();
		}
		#endif

		void UpdateRagdollProfileValues () {
			if (!ragdollProfile) {
				return;
			}
			
			for (int i = 0; i < ragdollUsedBones.Length; i++) {
				HumanBodyBones bone = ragdollUsedBones[i];
				RagdollProfile.BoneProfile boneProfile = ragdollProfile.bones[i];

				Vector3 headOffset = boneProfile.headOffset;
				//values too high or too low become unstable for some reason
				headOffset.y = Mathf.Clamp(headOffset.y, -initialHeadOffsetFromChest + .1f, 2);


				//set rigidbody values for bone
				Rigidbody rb = rigidbodies[bone];
				rb.useGravity = true;
				rb.maxAngularVelocity = boneProfile.maxAngularVelocity;
				rb.angularDrag = boneProfile.angularDrag;
				rb.angularDrag = boneProfile.drag;
				rb.mass = boneProfile.mass;
				
				

				//adjust collider values for bone
				Collider col = colliders[bone];
				if (bone == HumanBodyBones.Head) {

					//adjust the head collider based on headRadius and head Offset
					float headRadius = boneProfile.colliderRadius;
					SphereCollider sphere = col as SphereCollider;
					if (sphere) {
						sphere.radius = headRadius;
						sphere.center = new Vector3(headOffset.x, headRadius + headOffset.y, headOffset.z);
					}

					//adjust the chest collider, so it's top reaches the head collider joint
					BoxCollider box = colliders[HumanBodyBones.Chest] as BoxCollider;
					
					Bounds bounds = new Bounds(box.center, box.size);
					Vector3 max = bounds.max;
					max.y = Mathf.Max(.1f, initialHeadOffsetFromChest + headOffset.y);
					bounds.max = max;
				
					box.center = bounds.center;
					box.size = bounds.size;
					
				}

				else if (bone == HumanBodyBones.Chest || bone == HumanBodyBones.Hips) {
					
					//adjust chest and hips Z thickness and offset
					//maybe some models are 'fatter' than others
					BoxCollider box = col as BoxCollider;
					if (box) {
						Vector3 center = box.center;
						center.z = boneProfile.boxZOffset;
						box.center = center;
						
						Vector3 size = box.size;
						size.z = boneProfile.boxZSize;
						box.size = size;
					}
				}
				else {
					//adjust the radius of the arms and leg capsules
					CapsuleCollider capsule = col as CapsuleCollider;
					if (capsule) {
						capsule.radius = boneProfile.colliderRadius;
					}
				}

				//set joint values
				ConfigurableJoint joint = joints[bone];
				if (joint) {

					//adjust anchor for head offset
					if (bone == HumanBodyBones.Head) {
						if (joint.anchor != headOffset) {
							joint.anchor = headOffset;
						}
					}
					
					//adjust axes
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
			}
		}



		/*
			Methods used to build the ragdoll during runtime

			a base ragdoll is built 
			then the variables, (like joint limits and rigidbody masses), 
			are adjusted via the ragdoll profile
		*/
		#region RAGDOLL BASE BUILDER
		
        class BoneInfo
        {
            public HumanBodyBones boneType;
            public Transform anchor;
            public BoneInfo parent;
        }

        static void PrepareBones(out Dictionary<HumanBodyBones, BoneInfo> bones, Animator animator)
        {
            bones = new Dictionary<HumanBodyBones, BoneInfo>();

            BoneInfo rootBone = new BoneInfo();
            rootBone.anchor = animator.GetBoneTransform(HumanBodyBones.Hips);
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
            DestroyComponents<CharacterJoint>(c);
            DestroyComponents<ConfigurableJoint>(c);
            DestroyComponents<Rigidbody>(c);
            DestroyComponents<Collider>(c);
		}	

        public static void BuildRagdollBase(Animator anim) {
            if (anim == null) {
                Debug.Log("No animator found...");
                return;
            }
            EraseRagdoll(anim);
            Dictionary<HumanBodyBones, BoneInfo> bones;
            PrepareBones(out bones, anim);
            BuildCapsules(bones);
            AddBreastColliders(anim);
            AddHeadCollider(anim);
            BuildBodies(bones);
            BuildJoints(bones);
        }

        static void AddJoint(Animator animator, HumanBodyBones boneType, Dictionary<HumanBodyBones, BoneInfo> bones, HumanBodyBones parentBone)//, Vector3 worldTwistAxis, Vector3 worldSwingAxis)
        {
            BoneInfo bone = new BoneInfo();
            bone.anchor = animator.GetBoneTransform(boneType);
            bone.boneType = boneType;
            bone.parent = bones[parentBone];
            bones.Add( boneType, bone );
        }

        static void BuildCapsules(Dictionary<HumanBodyBones, BoneInfo> bones)
        {
            HashSet<HumanBodyBones> capsuleBones = new HashSet<HumanBodyBones> () {
                HumanBodyBones.LeftUpperArm, HumanBodyBones.RightUpperArm,
                HumanBodyBones.LeftUpperLeg, HumanBodyBones.RightUpperLeg,
                HumanBodyBones.LeftLowerArm, HumanBodyBones.RightLowerArm,
                HumanBodyBones.LeftLowerLeg, HumanBodyBones.RightLowerLeg,
            };

            foreach (var k in bones.Keys)
            {
                BoneInfo bone = bones[k];
                if (!capsuleBones.Contains(bone.boneType))
                    continue;
                
                int direction;
                float distance;

                if (bone.boneType == HumanBodyBones.LeftUpperArm || bone.boneType == HumanBodyBones.RightUpperArm || bone.boneType == HumanBodyBones.LeftUpperLeg || bone.boneType == HumanBodyBones.RightUpperLeg) {
                    HumanBodyBones childBoneType;
                    if (bone.boneType == HumanBodyBones.LeftUpperArm)
                        childBoneType = HumanBodyBones.LeftLowerArm;
                    else if (bone.boneType == HumanBodyBones.RightUpperArm)
                        childBoneType = HumanBodyBones.RightLowerArm;
                    else if (bone.boneType == HumanBodyBones.LeftUpperLeg)
                        childBoneType = HumanBodyBones.LeftLowerLeg;
                    else //if (bone.boneType == HumanBodyBones.RightUpperLeg)
                        childBoneType = HumanBodyBones.RightLowerLeg;

                    Vector3 endPoint = bones[childBoneType].anchor.position;
                    CalculateDirection(bone.anchor.InverseTransformPoint(endPoint), out direction, out distance);
                }
                else
                {
                    Vector3 endPoint = (bone.anchor.position - bone.parent.anchor.position) + bone.anchor.position;
                    
                    CalculateDirection(bone.anchor.InverseTransformPoint(endPoint), out direction, out distance);

                    if (bone.anchor.GetComponentsInChildren(typeof(Transform)).Length > 1)
                    {
                        Bounds bounds = new Bounds();
                        foreach (Transform child in bone.anchor.GetComponentsInChildren(typeof(Transform)))
                            bounds.Encapsulate(bone.anchor.InverseTransformPoint(child.position));
                        
                        if (distance > 0)
                            distance = bounds.max[direction];
                        else
                            distance = bounds.min[direction];
                    }
                }

                CapsuleCollider collider = (CapsuleCollider)bone.anchor.gameObject.AddComponent<CapsuleCollider>();
                collider.direction = direction;

                Vector3 center = Vector3.zero;
                center[direction] = distance * 0.5F;
                collider.center = center;
                collider.height = Mathf.Abs(distance);
                collider.radius = .1f;
                
            }
        }

        static void BuildBodies(Dictionary<HumanBodyBones, BoneInfo> bones)
        {
            foreach (var k in bones.Keys)
                bones[k].anchor.gameObject.AddComponent<Rigidbody>();
        }

        static void BuildJoints(Dictionary<HumanBodyBones, BoneInfo> bones)
        {
            foreach (var k in bones.Keys)
            {
                BoneInfo bone = bones[k];
                if (bone.parent == null)
                    continue;

                ConfigurableJoint joint = bone.anchor.gameObject.AddComponent<ConfigurableJoint>();
                
                // Setup connection and axis
                //joint.autoConfigureConnectedAnchor = false;
                
                joint.anchor = Vector3.zero;
                
                joint.connectedBody = bone.parent.anchor.GetComponent<Rigidbody>();
                
                joint.enablePreprocessing = false; // turn off to handle degenerated scenarios, like spawning inside geometry.
                
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
        
        static void CalculateDirection(Vector3 point, out int direction, out float distance)
        {
            // Calculate longest axis
            direction = 0;
            if (Mathf.Abs(point[1]) > Mathf.Abs(point[0]))
                direction = 1;
            if (Mathf.Abs(point[2]) > Mathf.Abs(point[direction]))
                direction = 2;
            distance = point[direction];
        }

        static Bounds GetBreastBounds(Transform relativeTo, Vector3[] encapsulatePositions)
        {
            Bounds bounds = new Bounds();
            for (int i = 0; i < 4; i++) bounds.Encapsulate(relativeTo.InverseTransformPoint(encapsulatePositions[i]));
            return bounds;
        }

        static void AddBreastColliders(Animator anim)
        {
            // Middle spine and pelvis
            
            Vector3[] encapsulatePositions = new Vector3[] {
                anim.GetBoneTransform(HumanBodyBones.LeftUpperArm).position,
                anim.GetBoneTransform(HumanBodyBones.RightUpperArm).position,
                anim.GetBoneTransform(HumanBodyBones.LeftUpperLeg).position,
                anim.GetBoneTransform(HumanBodyBones.RightUpperLeg).position,
            };

            Transform hips = anim.GetBoneTransform(HumanBodyBones.Hips);
            Transform chest = anim.GetBoneTransform(HumanBodyBones.Chest);
            
            Bounds bounds = GetBreastBounds(hips, encapsulatePositions);
            
			bounds.max = AdjustBoxMaxBounds(bounds, hips, chest);


            AddBoxCollider(hips.gameObject, bounds);
        
            //chest
            bounds = GetBreastBounds(chest, encapsulatePositions);

            Vector3 min = bounds.min;
            min.y = 0;
            bounds.min = min;

			bounds.max = AdjustBoxMaxBounds(bounds, chest, anim.GetBoneTransform(HumanBodyBones.Head));

            AddBoxCollider(chest.gameObject, bounds);
        }
		static Vector3 AdjustBoxMaxBounds (Bounds bounds, Transform relativeTo, Transform cutoff) {
			Vector3 max = bounds.max;
            max.y = relativeTo.InverseTransformPoint(cutoff.position).y;
			max.z = .1f;
            return max;
		}

        static void AddBoxCollider(GameObject g, Bounds bounds) {
            BoxCollider box = g.AddComponent<BoxCollider>();
            box.center = bounds.center;
            box.size = bounds.size;
        }
        static void AddHeadCollider(Animator anim) {
            SphereCollider sphere = anim.GetBoneTransform(HumanBodyBones.Head).gameObject.AddComponent<SphereCollider>();
            float defualtRadius = .125f;
            sphere.radius = defualtRadius;
            sphere.center = new Vector3(0, defualtRadius, 0);
        }

		#endregion
    }
}

