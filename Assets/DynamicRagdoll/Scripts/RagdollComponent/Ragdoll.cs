using System.Collections.Generic;
using UnityEngine;

namespace DynamicRagdoll {
	/*
		Terminology:
			
			Transform:
				any sub transform of the ragdoll model hips, includes fingers, etc...
			
			Bone:
				any sub transform of the ragdoll model that uses physics
	*/
	
	/*

		Handles the actual ragdoll model object, 
		add this script to the ragdoll's root 
		(with an animator that has a humanoid avatar setup)

		you can either pre build the base ragdoll in the editor or let it be done on awake

		builds a ragdoll on this animated character model

		then adjusts it's values based on the ragdoll profile you give it

		while in editor mode values are adjusted every update loop for easier tweaking
		of values

		Note:
			trying to set this up to be used as any other collider / rigidibody
			with collision / trigger callbacks
	*/
	
	[RequireComponent(typeof(Animator))]
	public partial class Ragdoll : MonoBehaviour {

		public static int layer { get { return LayerMask.NameToLayer("Ragdoll"); } }

		public static int Bone2Index (HumanBodyBones bone) {
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

		public static HumanBodyBones[] humanBones = new HumanBodyBones[] {
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

		public static HumanBodyBones GetParentBone (HumanBodyBones bone) {
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
        public static HumanBodyBones GetChildBone (HumanBodyBones bone) {
            switch (bone) {
                case HumanBodyBones.RightUpperLeg:  return HumanBodyBones.RightLowerLeg;
                case HumanBodyBones.LeftUpperLeg:   return HumanBodyBones.LeftLowerLeg;
                case HumanBodyBones.RightUpperArm:  return HumanBodyBones.RightLowerArm;
                case HumanBodyBones.LeftUpperArm:   return HumanBodyBones.LeftLowerArm;
            }
            return HumanBodyBones.Hips;
        }



		public readonly static int bonesCount = humanBones.Length;

		
		public RagdollProfile ragdollProfile;

		// were teh ragdoll components added in the editor already ?
		[HideInInspector] public bool preBuilt;
		Renderer[] allRenderers;
		Dictionary<HumanBodyBones, RagdollTransform> boneElements;
		RagdollTransform[] allElements;

		bool initializedValues;
		//initial head position from chest (used for resizing chest collider based on head offset)				
		float initialHeadOffsetFromChest;


		public T[] AddComponentsToBones<T> () where T : Component {
			T[] r = new T[bonesCount];
			for (int i = 0; i < bonesCount; i++) {
				r[i] = allElements[i].AddComponent<T>();
			}
			return r;
		}


		
			
		/*
			enable disable joint limits
		*/
		public void EnableJointLimits (bool enabled) {
			if (CheckForErroredRagdoll("EnableJointLimits"))
				return;
			
			ConfigurableJointMotion m = enabled ? ConfigurableJointMotion.Limited : ConfigurableJointMotion.Free;
			for (int i = 0; i < bonesCount; i++) {	
				allElements[i].EnableJointLimits(m);
			}	
		}



		

		

		public RagdollController controller;
		public void SetController (RagdollController controller) {
			this.controller = controller;
		}
		public bool hasController { get { return controller != null; } }


		

		/*
			the total mass of all the ragdoll rigidbodies
		*/
		public float CalculateMass() {
			if (CheckForErroredRagdoll("LoadSnapshot"))
				return 0;
			
			float m = 0;
			for (int i =0 ; i < bonesCount; i++) {
				m += allElements[i].rigidbody.mass;
			}
			return m;
		}


		/*
			save the positions and rotations of all the elements
		*/
		public void SaveSnapshot() {
			if (CheckForErroredRagdoll("SaveSnapshot"))
				return;
			
			for (int i = 0; i < allElements.Length; i++) {	
				allElements[i].SaveSnapshot();
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
			
			for (int i = 0; i < allElements.Length; i++) {	
				allElements[i].LoadSnapshot(snapshotBlend, useFollowTarget);
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
			
			for (int i = 0; i < bonesCount; i++) {	
				allElements[i].rigidbody.isKinematic = value;
			}
		}
		
		/*
			set use gravity on all ragdoll rigidbodies
		*/
		public void UseGravity(bool value) {
			if (CheckForErroredRagdoll("UseGravity"))
				return;
			
			for (int i = 0; i < bonesCount; i++) {	
				allElements[i].rigidbody.useGravity = value;
			}
		}

		/*
			set layer on all ragdoll physics gameobjects
		*/
		public void SetLayer (int layer) {
			if (CheckForErroredRagdoll("SetLayer"))
				return;
			
			for (int i = 0; i < bonesCount; i++) {	
				allElements[i].transform.gameObject.layer = layer;
			}
		}

		public RagdollTransform GetBone (HumanBodyBones bone) {
			if (CheckForErroredRagdoll("GetPhysicsBone"))
				return null;
			
			RagdollTransform r;
			if (boneElements.TryGetValue(bone, out r)) {
				return r;
			}	
			Debug.LogWarning("Cant find: " + bone + " on ragdoll: " + name);
			return null;
		}

		public RagdollTransform RootBone () {
			return allElements[0];
		}

		
		

		/*
			Check for errors...
		*/
		bool CheckForErroredRagdoll(string msg) {
			if (!initializedValues) {
				Awake();
			}
			if (ragdollProfile == null) {
				Debug.LogError("Ragdoll is in error state, no profile assigned!!! (" + msg + ")", transform);
				return true;
			}
			if (boneElements == null) {
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

				Animator myAnimator = GetComponent<Animator>();			
				
				//if there werent any errros
				if (RagdollBuilder.BuildRagdollElements(myAnimator, out allElements, out boneElements)) {
					
					RagdollBuilder.BuildBones(myAnimator, ragdollProfile, !preBuilt, boneElements, out initialHeadOffsetFromChest);


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
				if (CheckForErroredRagdoll("Update")) {
					return;
				}

				// // if we're using a custom profile
				// if (ragdollProfile) {
					
					//if no errors
					// if (boneElements!= null) {
						UpdateBonesToProfileValues(boneElements, ragdollProfile, initialHeadOffsetFromChest);
					// }
				// }
			}
		}
		#endif
		
    }
}

