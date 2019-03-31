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

		public static HumanBodyBones[] usedBones = new HumanBodyBones[] {
			HumanBodyBones.Hips, HumanBodyBones.Chest, HumanBodyBones.Head, 
			
			HumanBodyBones.RightLowerLeg, HumanBodyBones.LeftLowerLeg, 
			HumanBodyBones.RightUpperLeg, HumanBodyBones.LeftUpperLeg, 

			HumanBodyBones.RightLowerArm, HumanBodyBones.LeftLowerArm, 
			HumanBodyBones.RightUpperArm, HumanBodyBones.LeftUpperArm, 
		};
		public readonly static int bonesCount = usedBones.Length;


		public RagdollProfile ragdollProfile;
		[HideInInspector] public bool preBuilt;

		public class Bone {
			public Rigidbody rigidbody;
			public ConfigurableJoint joint;
			public Collider collider;
			public Transform transform;


			public Vector3 position { get { return transform.position; } }
			public Quaternion rotation { get { return transform.rotation; } }
			public GameObject gameObject { get { return transform.gameObject; } }
			
			
			public T AddComponent<T> () where T : Component {
				return gameObject.AddComponent<T>();
			}

			public Bone (Transform bone) {
				transform = bone;
				rigidbody = bone.GetComponent<Rigidbody>();
				joint = bone.GetComponent<ConfigurableJoint>();
				collider = bone.GetComponent<Collider>(); 
			}
		}
		

		Renderer[] allRenderers;
		Dictionary<HumanBodyBones, Bone> myBones;
		
		bool initializedValues;
		
		//initial head position from chest (used for resizing chest collider based on head offset)				
		float initialHeadOffsetFromChest;

		void InitializeIfNeeded () {
			if (!initializedValues) {
				Awake();
			}
		}
		public void EnableRenderers(bool enabled) {
			InitializeIfNeeded();
			if (CheckForErroredRagdoll())
				return;
			
			for (int i = 0; i < allRenderers.Length; i++) {
				allRenderers[i].enabled = enabled;
			}
		}
		public void SetKinematic(bool value) {
			InitializeIfNeeded();
			if (CheckForErroredRagdoll())
				return;
			
			for (int i = 0; i < usedBones.Length; i++) {	
				myBones[usedBones[i]].rigidbody.isKinematic = value;
			}
		}
		public void UseGravity(bool value) {
			InitializeIfNeeded();
			if (CheckForErroredRagdoll())
				return;
			
			for (int i = 0; i < usedBones.Length; i++) {	
				myBones[usedBones[i]].rigidbody.useGravity = value;
			}
		}
		public void SetLayer (int layer) {
			InitializeIfNeeded();
			if (CheckForErroredRagdoll())
				return;
			
			
			for (int i = 0; i < usedBones.Length; i++) {	
				myBones[usedBones[i]].gameObject.layer = layer;
			}
		}

		bool CheckForErroredRagdoll() {
			if (myBones == null) {
				Debug.LogError("Ragdoll is in error state, maybe it's not humanoid?", transform);
				return true;
			}
			return false;
		}

		public Bone GetBone (HumanBodyBones bone) {
			InitializeIfNeeded();
			if (CheckForErroredRagdoll())
				return null;
			
			Bone r;
			if (myBones.TryGetValue(bone, out r)) {
				return r;
			}	
			Debug.LogWarning("Cant find: " + bone + " on ragdoll:", transform);
			return null;
		}

		public Bone RootBone () {
			return GetBone(HumanBodyBones.Hips);
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
					myBones = RagdollBuilder.BuildRagdollFull(GetComponent<Animator>(), ragdollProfile, out initialHeadOffsetFromChest);
				}
				else { 
					//just build bones dictionary
					myBones = RagdollBuilder.BuildRagdollFromPrebuilt(GetComponent<Animator>(), ragdollProfile, out initialHeadOffsetFromChest);
				}

				//if there werent any errros
				if (myBones != null) {

					//set the bones to the ragdoll layer
					SetLayer(LayerMask.NameToLayer("Ragdoll"));
					
					//get all renderers
					allRenderers = GetComponentsInChildren<Renderer>();
				}

				CheckForErroredRagdoll();
				
			}
		}

		//update values during runtime (if not in build)
		//for easier adjustments
		#if UNITY_EDITOR
		[Header("Editor Only")] public bool setValuesUpdate = true;
		void Update () {
			if (setValuesUpdate) {
				if (ragdollProfile) {
					if (myBones!= null) {
						UpdateBonesToProfileValues(myBones, ragdollProfile, initialHeadOffsetFromChest);
					}
				}
			}
		}
		#endif
		
		public static float CalculateHeadOffsetFromChest (Dictionary<HumanBodyBones, Bone> bones) {
			if (bones == null) {
				Debug.LogError("Ragdoll is in error state, maybe it's not humanoid? (CalculateHeadOffsetFromChest)");
				return -1;
			}
			return bones[HumanBodyBones.Chest].transform.InverseTransformPoint(bones[HumanBodyBones.Head].transform.position).y;
		}

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

