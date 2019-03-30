using UnityEngine;

namespace DynamicRagdoll {
    [CreateAssetMenu()]
    public class RagdollControllerProfile : ScriptableObject
    {
        [System.Serializable] public class BoneProfile {
            public HumanBodyBones bone;
            [Range(0,2)] public float inputForce = 1;
            [Range(0,2)] public float maxForce = 1; //check initial values above
            [Range(0,2)] public float maxTorque = 1;

            public BoneProfile(HumanBodyBones bone, float maxForce) {
                this.bone = bone;
                this.maxForce = maxForce;
                inputForce = 1;
                maxTorque = 1;
            }
        }

        public BoneProfile[] bones = new BoneProfile[] {
            new BoneProfile(HumanBodyBones.Hips, 1),
            new BoneProfile(HumanBodyBones.Chest, 1), 
            new BoneProfile(HumanBodyBones.Head, 1), 
            
            new BoneProfile(HumanBodyBones.RightLowerLeg, .2f), 
            new BoneProfile(HumanBodyBones.LeftLowerLeg, .2f), 
            
            new BoneProfile(HumanBodyBones.RightUpperLeg, .2f), 
            new BoneProfile(HumanBodyBones.LeftUpperLeg, .2f), 
            
            new BoneProfile(HumanBodyBones.RightLowerArm, .2f), 
            new BoneProfile(HumanBodyBones.LeftLowerArm, .2f), 

            new BoneProfile(HumanBodyBones.RightUpperArm, .2f), 
            new BoneProfile(HumanBodyBones.LeftUpperArm, .2f), 
        };

        [Range(0f, 160f)] public float PForce = 30f;		
		[Range(0f, .064f)] public float DForce = .01f;
			
		[Range(0f, 100f)] public float maxForce = 100f;
		[Range(0f, 10000f)] public float maxJointTorque = 10000f;

        [Tooltip("Determines how fast the character loses control after colliding")]
		[Range(.5f, 4.5f)] public float fallLerp = 1.5f;				
		[Range(0f, .2f)] public float residualForce = .1f;
		[Range(0f, 120f)] public float residualJointTorque = 120f;
		
        [Tooltip("When ragdollRootBoone goes below this speed\nThe falling state is through and the get up starts")]
		[Range(0f, .4f)] public float settledSpeed = .05f;				
		[Tooltip("Wait for animation transition into Getup")]
		public float orientateDelay = .1f;
		public LayerMask checkGroundMask;
		[Tooltip("How long do we blend when transitioning from ragdolled to animated")]
		public float blendTime = 0.5f;
		
    }
}
