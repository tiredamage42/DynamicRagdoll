using UnityEngine;

namespace DynamicRagdoll {
    /*
        object for holding controller values (makes it easier to set during runtime)
    */
    [CreateAssetMenu()]
    public class RagdollControllerProfile : ScriptableObject
    {
        // per bone options
        [System.Serializable] public class BoneProfile {
            public HumanBodyBones bone;

            /*
                Normal velocity set method
            */
            public AnimationCurve fallForceDecay = new AnimationCurve(new Keyframe[] { new Keyframe(0, 1), new Keyframe(1, 0) });
            public AnimationCurve fallTorqueDecay = new AnimationCurve(new Keyframe[] { new Keyframe(0, 1), new Keyframe(1, 0) });
            
            /*
			    Define which bones count as neighbors for other bones (for the bone decay system)
		    */
            public HumanBodyBones[] neighbors;

            /*
                PD Control
            */
            [Range(0,2)] public float inputForce = 1;
            [Range(0,2)] public float maxForce = 1; //check initial values above
            [Range(0,2)] public float maxTorque = 1;


            public BoneProfile(HumanBodyBones bone, float maxForce, HumanBodyBones[] neighbors) {
                this.bone = bone;
                this.maxForce = maxForce;
                inputForce = 1;
                maxTorque = 1;

                fallForceDecay = new AnimationCurve(new Keyframe[] { new Keyframe(0, 1), new Keyframe(1, 0) });
                fallTorqueDecay = new AnimationCurve(new Keyframe[] { new Keyframe(0, 1), new Keyframe(1, 0) });
            
                this.neighbors = neighbors;
            }
        }

        public BoneProfile[] bones = new BoneProfile[] {
            new BoneProfile(HumanBodyBones.Hips, 1, 
                new HumanBodyBones[] { 
                    HumanBodyBones.Chest, 
                    HumanBodyBones.LeftUpperLeg, 
                    HumanBodyBones.RightUpperLeg,
                }),
            new BoneProfile(HumanBodyBones.Chest, 1, 
                new HumanBodyBones[] { 
                    HumanBodyBones.Head, 
                    HumanBodyBones.LeftUpperArm, 
                    HumanBodyBones.RightUpperArm
                }), 
            new BoneProfile(HumanBodyBones.Head, 1, 
                new HumanBodyBones[] { 
                    HumanBodyBones.Chest, 
                    HumanBodyBones.LeftUpperArm, 
                    HumanBodyBones.RightUpperArm 
                }), 
            
            new BoneProfile(HumanBodyBones.RightLowerLeg, .2f, 
                new HumanBodyBones[] { 
                    HumanBodyBones.RightUpperLeg, HumanBodyBones.Hips ,
                }), 
            new BoneProfile(HumanBodyBones.LeftLowerLeg, .2f, 
                new HumanBodyBones[] { 
                    HumanBodyBones.LeftUpperLeg, HumanBodyBones.Hips 
                }), 
            
            new BoneProfile(HumanBodyBones.RightUpperLeg, .2f, 
                new HumanBodyBones[] { 
                    HumanBodyBones.Hips, HumanBodyBones.RightLowerLeg ,
                }), 
            new BoneProfile(HumanBodyBones.LeftUpperLeg, .2f, 
                new HumanBodyBones[] { 
                    HumanBodyBones.Hips, HumanBodyBones.LeftLowerLeg ,
                }), 
            
            new BoneProfile(HumanBodyBones.RightLowerArm, .2f, 
                new HumanBodyBones[] { 
                    HumanBodyBones.RightUpperArm, HumanBodyBones.Chest 
                }), 
            new BoneProfile(HumanBodyBones.LeftLowerArm, .2f,
                new HumanBodyBones[] { 
                    HumanBodyBones.LeftUpperArm, HumanBodyBones.Chest 
                }), 

            new BoneProfile(HumanBodyBones.RightUpperArm, .2f, 
                new HumanBodyBones[] { 
                    HumanBodyBones.Chest, HumanBodyBones.RightLowerArm,
                    HumanBodyBones.LeftUpperArm, 
                    HumanBodyBones.Head,
                }), 
            new BoneProfile(HumanBodyBones.LeftUpperArm, .2f, 
                new HumanBodyBones[] { 
                    HumanBodyBones.Chest, HumanBodyBones.LeftLowerArm ,
                    HumanBodyBones.RightUpperArm, 
                    HumanBodyBones.Head,
                }), 
        };



        [Tooltip("Use PD physics controller to move rigidbodies on the ragdoll to their masters.\n\nHere for comparison")]
        public bool usePDControl;


        /*
            Normal velocity set values
        */
        [Range(0f, 10000f)] public float maxTorque = 250f;

        [Tooltip("Calculated animation velocities will have gravity added to them\nif their magnitude is under this value.\n\nRaise if ragdoll hangs in the air too much")]
		[Range(0f, 1f)] public float maxGravityAddVelocity = 1f;

            
        /*
            PD controller follow values
        */
        [Range(0f, 160f)] public float PForce = 30f;		
		[Range(0f, .064f)] public float DForce = .01f;
		[Range(0f, 100f)] public float maxForcePD = 100f; //maybe 25
		[Range(0f, 10000f)] public float maxTorquePD = 10000f;
        [Range(1,10)] public int calculateVelocityFrames = 1;
        public bool skipFrames = true, followRigidbodyParents;
		[Range(0f, 25f)] public float residualForce = .1f;
		[Range(0f, 120f)] public float residualTorque = 120f;



		
        [Tooltip("Default for how fast the character loses control after ragdolling")]
        [Range(.5f, 4.5f)] public float fallDecaySpeed = 1.5f;				
		
        [Tooltip("Minimum time to spend ragdolled before trying to get up")]
		[Range(0f, 5f)] public float ragdollMinTime = 3;				
		
        [Tooltip("When ragdollRootBoone goes below this speed\nThe falling state is through and the get up starts")]
		[Range(0f, .4f)] public float settledSpeed = .05f;				
		
        [Tooltip("Wait for animation transition into getup animation")]
		public float orientateDelay = .25f;
		
        public LayerMask checkGroundMask;

		[Tooltip("How long do we blend when transitioning from ragdolled to animated")]
		public float blendTime = 1f;
		
    }
}
