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

		public static HumanBodyBones[] phsysicsHumanBones = new HumanBodyBones[] {
			HumanBodyBones.Hips, //HIPS NEEDS TO BE FIRST
			
			HumanBodyBones.Chest, HumanBodyBones.Head, 
			
			HumanBodyBones.RightLowerLeg, HumanBodyBones.LeftLowerLeg, 
			HumanBodyBones.RightUpperLeg, HumanBodyBones.LeftUpperLeg, 

			HumanBodyBones.RightLowerArm, HumanBodyBones.LeftLowerArm, 
			HumanBodyBones.RightUpperArm, HumanBodyBones.LeftUpperArm, 
		};
		public readonly static int physicsBonesCount = phsysicsHumanBones.Length;


		public RagdollProfile ragdollProfile;
		[HideInInspector] public bool preBuilt;


		//static JointDrive jointDrive = new JointDrive();


		public void SaveSnapshot() {
			if (CheckForErroredRagdoll("SaveSnapshot"))
				return;
			
			for (int i = 0; i < allBones.Length; i++) {	
				allBones[i].SaveSnapshot();
			}

		}

		public void LoadSnapshot (float snapshotBlend, bool useFollowTarget) {
			if (CheckForErroredRagdoll("LoadSnapshot"))
				return;
			
			for (int i = 0; i < allBones.Length; i++) {	
				allBones[i].LoadSnapshot(snapshotBlend, useFollowTarget);
			}
		}


		public class Bone {
			public Rigidbody rigidbody;
			public ConfigurableJoint joint;
			public Collider collider;
			public Transform transform;
			public bool isPhysicsParent, isRoot, isPhysicsBone;

			public Bone followTarget;

			public Vector3 snapshotPosition;
			public Quaternion snapshotRotation;

			public void SaveSnapshot () {
				if (isRoot) {
					snapshotPosition = position;
				}
				snapshotRotation = GetRotation();
			}
			public void LoadSnapShot () {
				if (isRoot) {
					transform.position = snapshotPosition;
				}
				SetRotation(snapshotRotation);
			}

			public void TeleportToTarget () {
				if (followTarget == null) {
					return;
				}
				if (isRoot) {
					transform.position = followTarget.position;
				}
				SetRotation(followTarget.GetRotation());
			}

			public void LoadSnapshot (float snapshotBlend, bool useFollowTarget) {
				Bone boneToUse = useFollowTarget && followTarget != null ? followTarget : this;
				if (isRoot) {
					transform.position = Vector3.Lerp(boneToUse.position, snapshotPosition, snapshotBlend);
				}
				SetRotation(Quaternion.Slerp(boneToUse.GetRotation(), snapshotRotation, snapshotBlend));
			}

			Quaternion GetRotation () {
				return isRoot ? transform.rotation : transform.localRotation;
			}
			void SetRotation(Quaternion rotation) {
				if (isRoot) {
					transform.rotation = rotation;
				}
				else {
					transform.localRotation = rotation;
				}
			}








			public Vector3 position { get { return transform.position; } }
			public Quaternion rotation { get { return transform.rotation; } }
			public GameObject gameObject { get { return transform.gameObject; } }
			
			
			public T AddComponent<T> () where T : Component {
				return gameObject.AddComponent<T>();
			}

			public Bone (Transform transform, bool checkForComponents, bool isPhysicsParent, bool isPhysicsBone, bool isRoot) {
				this.transform = transform;
				this.isPhysicsParent = isPhysicsParent;
				this.isRoot = isRoot;
				this.isPhysicsBone = isPhysicsBone;

				if (checkForComponents) {
					rigidbody = transform.GetComponent<Rigidbody>();
					collider = transform.GetComponent<Collider>();  
					if (!isRoot) {
						joint = transform.GetComponent<ConfigurableJoint>();
					}
				}
			}

			public void SetFollowTarget(Bone followTarget) {
				this.followTarget = followTarget;



				




			}




			
		}
		

		Renderer[] allRenderers;
		Dictionary<HumanBodyBones, Bone> physicsBones;
		//HashSet<int> usedPhysicsTransforms;
		Bone[] allBones;


		
		bool initializedValues;
		
		//initial head position from chest (used for resizing chest collider based on head offset)				
		float initialHeadOffsetFromChest;

		void InitializeIfNeeded () {
			if (!initializedValues) {
				Awake();
			}
		}
		public void EnableRenderers(bool enabled) {
			if (CheckForErroredRagdoll("EnableRenderers"))
				return;
			
			for (int i = 0; i < allRenderers.Length; i++) {
				allRenderers[i].enabled = enabled;
			}
		}
		public void SetKinematic(bool value) {
			if (CheckForErroredRagdoll("SetKinematic"))
				return;
			
			for (int i = 0; i < physicsBonesCount; i++) {	
				allBones[i].rigidbody.isKinematic = value;
			}
		}
		public void UseGravity(bool value) {
			if (CheckForErroredRagdoll("UseGravity"))
				return;
			
			for (int i = 0; i < physicsBonesCount; i++) {	
				allBones[i].rigidbody.useGravity = value;
			}
		}
		public void SetLayer (int layer) {
			if (CheckForErroredRagdoll("SetLayer"))
				return;
			
			for (int i = 0; i < physicsBonesCount; i++) {	
				allBones[i].gameObject.layer = layer;
			}
		}

		bool CheckForErroredRagdoll(string msg) {
			
			InitializeIfNeeded();
			
			if (physicsBones == null) {
				Debug.LogError("Ragdoll is in error state, maybe it's not humanoid? (" + msg + ")", transform);
				return true;
			}
			return false;
		}

		public Bone GetPhysicsBone (HumanBodyBones bone) {
			if (CheckForErroredRagdoll("GetPhysicsBone"))
				return null;
			
			Bone r;
			if (physicsBones.TryGetValue(bone, out r)) {
				return r;
			}	
			Debug.LogWarning("Cant find: " + bone + " on ragdoll:", transform);
			return null;
		}

		public Bone RootBone () {
			return allBones[0];// GetPhysicsBone(HumanBodyBones.Hips);
		}

		// public bool TransformIsPhysics (Transform transform) {
		// 	return usedPhysicsTransforms.Contains(transform.GetInstanceID());
		// }

		public enum TeleportType { All, PhysicsBones, PhysicsParents, PhysicsBonesAndParents, SecondaryNonPhysicsBones };
		public void TeleportToTarget (TeleportType teleportType) {

			int startIndex = teleportType == TeleportType.SecondaryNonPhysicsBones || teleportType == TeleportType.PhysicsParents ? physicsBonesCount : 0;
			int endIndex = teleportType == TeleportType.PhysicsBones ? physicsBonesCount : allBones.Length;
			for (int i = startIndex; i < endIndex; i++) {

				bool teleportBone = false;
				switch (teleportType) {
					case TeleportType.All:
						teleportBone = true;
						break;
					case TeleportType.PhysicsBones:
						teleportBone = allBones[i].isPhysicsBone;
						break;
					case TeleportType.PhysicsParents:
						teleportBone = allBones[i].isPhysicsParent;
						break;
					case TeleportType.PhysicsBonesAndParents:
						teleportBone = allBones[i].isPhysicsBone || allBones[i].isPhysicsParent;
						break;
					case TeleportType.SecondaryNonPhysicsBones:
						teleportBone = !allBones[i].isPhysicsBone && !allBones[i].isPhysicsParent;
						break;
				}
				if (teleportBone) {
					allBones[i].TeleportToTarget();
				}
			}
		}

		public void SetFollowTarget (Animator followAnimator) {
			if (CheckForErroredRagdoll("SetFollowTarget"))
				return;


			//generate Ragdoll bones for the follow target
			//Dictionary<HumanBodyBones, Bone> followPhysicsBones;
			Bone[] allFollowBones;
			//HashSet<int> followUsedPhysicsTransforms;

			//null profile so it doesnt try adjust values before we add collider/joint/rb components
            RagdollBuilder.BuildRagdollFromPrebuilt(followAnimator, null, out _, out allFollowBones, out _, false);
            if (allFollowBones == null) {
                return;
            }
			int l = allFollowBones.Length;
			if (l != allBones.Length) {
				Debug.LogError("children list different sizes for ragdoll: "+name+", and follow target: " + followAnimator.name);
				return;
			}
			for (int i = 0; i < l; i++) {
				allBones[i].SetFollowTarget(allFollowBones[i]);
			}
		
		}




		/*
			a base ragdoll is built (if not pre built)
			then the variables, (like joint limits and rigidbody masses), 
			are adjusted via the ragdoll profile
		*/
		
		void Awake () {
			if (!initializedValues) {
				initializedValues = true;				
				
				if (!preBuilt) {
					//build the ragdoll if not built in editor
					RagdollBuilder.BuildRagdollFull(GetComponent<Animator>(), ragdollProfile, out initialHeadOffsetFromChest, out allBones, out physicsBones);
				}
				else { 
					//just build bones dictionary
					RagdollBuilder.BuildRagdollFromPrebuilt(GetComponent<Animator>(), ragdollProfile, out initialHeadOffsetFromChest, out allBones, out physicsBones);
				}

				//if there werent any errros
				if (physicsBones != null) {

					//set the bones to the ragdoll layer
					SetLayer(LayerMask.NameToLayer("Ragdoll"));
					
					//get all renderers
					allRenderers = GetComponentsInChildren<Renderer>();
				}

				CheckForErroredRagdoll("Awake");
				
			}
		}

		//update values during runtime (if not in build)
		//for easier adjustments
		#if UNITY_EDITOR
		[Header("Editor Only")] public bool setValuesUpdate = true;
		void Update () {
			if (setValuesUpdate) {
				if (ragdollProfile) {
					if (physicsBones!= null) {
						UpdateBonesToProfileValues(physicsBones, ragdollProfile, initialHeadOffsetFromChest);
					}
				}
			}
		}
		#endif
		
		
		public static void UpdateBonesToProfileValues (Dictionary<HumanBodyBones, Bone> bones, RagdollProfile profile, float initialHeadOffsetFromChest) {
			if (bones == null) {
				return;
			}
			
			if (profile == null) {
				profile = RagdollProfile.defaultProfile;
			}

			Vector3 headOffset = profile.headOffset;

			//clamp head offset (values too high or too low become unstable for some reason)
			headOffset.y = Mathf.Clamp(headOffset.y, -initialHeadOffsetFromChest + .1f, 2);
			
			for (int i = 0; i < profile.bones.Length; i++) {
				RagdollProfile.BoneProfile boneProfile = profile.bones[i];
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
				if (joint.anchor != headOffset) {
					joint.anchor = headOffset;
				}
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
			
			rigidbody.maxAngularVelocity = boneProfile.maxAngularVelocity;
			rigidbody.angularDrag = boneProfile.angularDrag;
			rigidbody.angularDrag = boneProfile.drag;
			rigidbody.mass = boneProfile.mass;

			rigidbody.maxDepenetrationVelocity = boneProfile.maxDepenetrationVelocity;
			
			rigidbody.interpolation = boneProfile.interpolation;
			rigidbody.collisionDetectionMode = boneProfile.collisionDetection;
		}
    }
}

