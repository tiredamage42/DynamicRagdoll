using UnityEngine;


namespace DynamicRagdoll {

    public partial class Ragdoll : MonoBehaviour
    {
        /*
			teleport ragdoll transforms (based on teleport type)

			to their master positions

			TODO: implemetn checking for follow target for follow target specific methods
		*/
		public enum TeleportType { All, Bones, BoneParents, BonesAndBoneParents, NonBones };
		public void TeleportToTarget (TeleportType teleportType) {
			if (CheckForErroredRagdoll("TeleportToTarget"))
				return;

			// if we're teleporting non physics bones, start looping after their indicies
			int startIndex = teleportType == TeleportType.NonBones || teleportType == TeleportType.BoneParents ? bonesCount : 0;
			
			// set the ending index for the loop
			int endIndex = teleportType == TeleportType.Bones ? bonesCount : allElements.Length;
			
			for (int i = startIndex; i < endIndex; i++) {

				bool teleportTransform = false;
				switch (teleportType) {
					case TeleportType.All:
						teleportTransform = true;
						break;
					case TeleportType.Bones:
						teleportTransform = allElements[i].isBone;
						break;
					case TeleportType.BoneParents:
						teleportTransform = allElements[i].isBoneParent;
						break;
					case TeleportType.BonesAndBoneParents:
						teleportTransform = allElements[i].isBone || allElements[i].isBoneParent;
						break;
					case TeleportType.NonBones:
						teleportTransform = !allElements[i].isBone && !allElements[i].isBoneParent;
						break;
				}
				if (teleportTransform) {
					allElements[i].TeleportToTarget();
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

			// generate Ragdoll transforms for the follow target
			
			RagdollTransform[] followTransforms;
			
			// if there was an error return...
			if (!RagdollBuilder.BuildRagdollElements(followAnimator, out followTransforms, out _)) {
			    return;
            }
			
			int l = followTransforms.Length;
			if (l != allElements.Length) {
				Debug.LogError("children list different sizes for ragdoll: "+name+", and follow target: " + followAnimator.name);
				return;
			}

			//set follow targets on our bones as these new master bones
			for (int i = 0; i < l; i++) {
				allElements[i].SetFollowTarget(followTransforms[i]);
			}
		}

    }
}
