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


		trying to set this up to be used as any other collider / rigidibody
	*/
	
	[RequireComponent(typeof(Animator))]
	public class Ragdoll : MonoBehaviour {

		public static int PhysicsBone2Index (HumanBodyBones bone) {
			switch (bone) {
				case HumanBodyBones.Hips: return 0;
				case HumanBodyBones.Chest:  return 1;
				case HumanBodyBones.Head: return 2;
				case HumanBodyBones.RightLowerLeg: return 3;
				case HumanBodyBones.LeftLowerLeg: return 4;
				case HumanBodyBones.RightUpperLeg:  return 5;
				case HumanBodyBones.LeftUpperLeg: return 6;
				case HumanBodyBones.RightLowerArm:  return 7;
				case HumanBodyBones.LeftLowerArm: return 8;
				case HumanBodyBones.RightUpperArm:  return 9;
				case HumanBodyBones.LeftUpperArm: return 10;
			}
			return -1;
		}

		public static HumanBodyBones[] phsysicsHumanBones = new HumanBodyBones[] {
			HumanBodyBones.Hips, //HIPS NEEDS TO BE FIRST
			
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



		public readonly static int physicsBonesCount = phsysicsHumanBones.Length;

		/*
			Runtime representation of a ragdoll bone 
			can either be physical bone, like the hips or head
			
			or secondary, like a finger
		*/
		public class Bone {
			public Rigidbody rigidbody;
			public ConfigurableJoint joint;
			public Collider collider;
			public Transform transform;
			public bool isPhysicsParent, isRoot, isPhysicsBone;
			public Bone followTarget;
			public Vector3 snapshotPosition;
			public Quaternion snapshotRotation;

			public Vector3 position { get { return transform.position; } }
			public Quaternion rotation { get { return transform.rotation; } }
			public GameObject gameObject { get { return transform.gameObject; } }

			public Bone (Transform transform, bool isPhysicsParent, bool isPhysicsBone, bool isRoot) {
				this.transform = transform;
				this.isPhysicsParent = isPhysicsParent;
				this.isRoot = isRoot;
				this.isPhysicsBone = isPhysicsBone;

				rigidbody = transform.GetComponent<Rigidbody>();
				if (rigidbody != null) {
					collider = transform.GetComponent<Collider>();  
					if (!isRoot) {
						joint = transform.GetComponent<ConfigurableJoint>();
					}
				}
			}	
			
			public void SetFollowTarget(Bone followTarget) {
				this.followTarget = followTarget;
			}

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

				/*
					immediately update physics position...
					transform set wasnt updating fast enough for physics detection of the ragdoll
					through raycasts/collisions
				*/
				if (rigidbody != null && rigidbody.isKinematic) {
					rigidbody.MovePosition(transform.position);
					rigidbody.MoveRotation(transform.rotation);
				}
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

			public T AddComponent<T> () where T : Component {
				return gameObject.AddComponent<T>();
			}		
		}


		public RagdollProfile ragdollProfile;


		// were teh ragdoll components added in the editor already ?
		[HideInInspector] public bool preBuilt;

		Renderer[] allRenderers;
		Dictionary<HumanBodyBones, Bone> physicsBones;
		Bone[] allBones;
		bool initializedValues;
		//initial head position from chest (used for resizing chest collider based on head offset)				
		float initialHeadOffsetFromChest;


		/*
			subscribe to get a notification when a ragdoll bone enters a collision

			callback must take in:
				HumanBodyBones, Collision
		*/
		public void AddCollisionCallback (System.Action<RagdollBone, Collision> callback) {
			if (CheckForErroredRagdoll("AddCollisionCallback"))
				return;
			
			collisionEnterCallbacks.Add(callback);
		}

		HashSet<System.Action<RagdollBone, Collision>> collisionEnterCallbacks = new HashSet<System.Action<RagdollBone, Collision>>();
		
		/*
			send the message out that bone was collided
			(given to ragdollbone component)
		*/
		void BroadcastCollision (RagdollBone bone, Collision collision) {

			foreach (var cb in collisionEnterCallbacks) {
				cb(bone, collision);
			}
		}

		/*
			Add all teh physical ragdoll components (for checking collisions)
		*/
		void InitializeRagdollBoneComponents () {
			
			
			for (int i = 0; i < physicsBonesCount; i++) {	

				RagdollBone boneComponent = allBones[i].AddComponent<RagdollBone>();

				boneComponent._InitializeInternal(this, phsysicsHumanBones[i], BroadcastCollision);
			}
		}

		public RagdollController controller;
		public void SetController (RagdollController controller) {
			this.controller = controller;
		}
		public bool hasController { get { return controller != null; } }


		/*
			make ragdoll ignore collisions with collider
		*/
		public void IgnoreCollisions(Collider collider, bool ignore) {
			if (CheckForErroredRagdoll("SaveSnapshot"))
				return;
			

			for (int i = 0; i < physicsBonesCount; i++) {	
				Physics.IgnoreCollision(allBones[i].collider, collider, ignore);
			}
		}

		/*
			the total mass of all the ragdoll rigidbodies
		*/

		public float mass {
			get {
				if (CheckForErroredRagdoll("LoadSnapshot"))
					return 0;
				
				float m = 0;
				for (int i =0 ; i < physicsBonesCount; i++) {
					m += allBones[i].rigidbody.mass;
				}
				return m;
			
			}
		}






		/*
			save the positions and rotations of all the bones
		*/
		public void SaveSnapshot() {
			if (CheckForErroredRagdoll("SaveSnapshot"))
				return;
			
			for (int i = 0; i < allBones.Length; i++) {	
				allBones[i].SaveSnapshot();
			}
		}

		/*
			sets the positions and rotations of the bones to recreate the saved snapshot

			snapshot blend is teh blend amount:
				1 = fully in snapshot

				0 = original position/rotation, or followTarget position/rotation if useFollowTarget == true
		*/
		public void LoadSnapshot (float snapshotBlend, bool useFollowTarget) {
			if (CheckForErroredRagdoll("LoadSnapshot"))
				return;
			
			for (int i = 0; i < allBones.Length; i++) {	
				allBones[i].LoadSnapshot(snapshotBlend, useFollowTarget);
			}
		}

		/*
			enable or disable ragdoll renderers
		*/
		public void EnableRenderers(bool enabled) {
			if (CheckForErroredRagdoll("EnableRenderers"))
				return;
			
			for (int i = 0; i < allRenderers.Length; i++) {
				allRenderers[i].enabled = enabled;
			}
		}
		
		/*
			set kinematic on all ragdoll rigidbodies
		*/
		public void SetKinematic(bool value) {
			if (CheckForErroredRagdoll("SetKinematic"))
				return;
			
			for (int i = 0; i < physicsBonesCount; i++) {	
				allBones[i].rigidbody.isKinematic = value;
			}
		}
		
		/*
			set use gravity on all ragdoll rigidbodies
		*/
		public void UseGravity(bool value) {
			if (CheckForErroredRagdoll("UseGravity"))
				return;
			
			for (int i = 0; i < physicsBonesCount; i++) {	
				allBones[i].rigidbody.useGravity = value;
			}
		}

		/*
			set layer on all ragdoll physics gameobjects
		*/
		public void SetLayer (int layer) {
			if (CheckForErroredRagdoll("SetLayer"))
				return;
			
			for (int i = 0; i < physicsBonesCount; i++) {	
				allBones[i].gameObject.layer = layer;
			}
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
			return allBones[0];
		}

		
		/*
			teleport ragdoll bones (based on teleport type)

			to their master positions



			TODO: implemetn checking for follow target for follow target specific methods
		*/
		public enum TeleportType { All, PhysicsBones, PhysicsParents, PhysicsBonesAndParents, SecondaryNonPhysicsBones };
		public void TeleportToTarget (TeleportType teleportType) {
			if (CheckForErroredRagdoll("TeleportToTarget"))
				return;

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

		/*
			set teh follow target for this ragdoll

			assumes that the animator avatar is humanoid and has the same transform bone setup 
			
			as the animator and avatar on this ragdoll object
		*/
		public void SetFollowTarget (Animator followAnimator) {
			if (CheckForErroredRagdoll("SetFollowTarget"))
				return;



			// generate Ragdoll bones for the follow target
			// set as non physics, so no profile or adding of components needed, 
			
			Bone[] allFollowBones;
			RagdollBuilder.BuildRagdoll(followAnimator, null, false, true, out _, out allFollowBones, out _);
            
			// if there was an error return...
			if (allFollowBones == null) {
                return;
            }
			
			int l = allFollowBones.Length;
			if (l != allBones.Length) {
				Debug.LogError("children list different sizes for ragdoll: "+name+", and follow target: " + followAnimator.name);
				return;
			}

			//set follow targets on our bones as these new master bones
			for (int i = 0; i < l; i++) {
				allBones[i].SetFollowTarget(allFollowBones[i]);
			}
		}

		/*
			Check for errors...
		*/
		bool CheckForErroredRagdoll(string msg) {
			if (!initializedValues) {
				Awake();
			}
			if (physicsBones == null) {
				Debug.LogError("Ragdoll is in error state, maybe it's not humanoid? (" + msg + ")", transform);
				return true;
			}
			return false;
		}




		/*
			Build the runtime representations of the ragdoll bones

			Adds the ragdoll components (Rigidbodies, joints, colliders...) if they werent
			pre built in the editor.

			then the variables, (like joint limits and rigidbody masses), 
			are adjusted via the ragdoll profile
		*/
		void Awake () {
			if (!initializedValues) {
				initializedValues = true;				
				
				RagdollBuilder.BuildRagdoll(GetComponent<Animator>(), ragdollProfile, !preBuilt, false, out initialHeadOffsetFromChest, out allBones, out physicsBones);
				
				//if there werent any errros
				if (physicsBones != null) {

					//set the bones to the ragdoll layer
					SetLayer(LayerMask.NameToLayer("Ragdoll"));
					
					//get all renderers
					allRenderers = GetComponentsInChildren<Renderer>();

					InitializeRagdollBoneComponents();
				}
				//display errors
				else {
					CheckForErroredRagdoll("Awake");
				}

				
			}
		}

		//update values during runtime (if not in build)
		//for easier adjustments
		#if UNITY_EDITOR
		[Header("Editor Only")] public bool setValuesUpdate = true;
		void Update () {
			if (setValuesUpdate) {

				// if we're using a custom profile
				if (ragdollProfile) {
					
					//if no errors
					if (physicsBones!= null) {
						UpdateBonesToProfileValues(physicsBones, ragdollProfile, initialHeadOffsetFromChest);
					}
				}
			}
		}
		#endif
		
		/*
			Adjust Ragdoll component values per bone to reflect the supplied
			Ragdoll profile (default profile if none is supplied)
		*/
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

			//head
			if (boneProfile.bone == HumanBodyBones.Head) {

				//adjust the head collider based on headRadius and head Offset
				SphereCollider sphere = collider as SphereCollider;
				sphere.radius = boneProfile.colliderRadius;
				sphere.center = new Vector3(headOffset.x, boneProfile.colliderRadius + headOffset.y, headOffset.z);
			}
			
			//breast box colliders
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
			//adjust the radius of the arms and leg capsules
			else {
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

