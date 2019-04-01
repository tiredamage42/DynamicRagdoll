using UnityEngine;

namespace DynamicRagdoll {
    //ragdolled angular drag was 20

    [CreateAssetMenu()]
    public class RagdollProfile : ScriptableObject {
        [System.Serializable] public class BoneProfile {
            public HumanBodyBones bone;
            
            public Vector2 angularXLimit;
            public float angularYLimit;
            public float angularZLimit = 0;
            public bool forceOff;
            public Vector3 axis1, axis2;

            public float mass;
            public float angularDrag = 0.05f;
            public float drag = 0.0f;
            public float maxAngularVelocity = 1000f; 
            public float maxDepenetrationVelocity = 10; // or 1e+32;
            
            public RigidbodyInterpolation interpolation = RigidbodyInterpolation.None;
            public CollisionDetectionMode collisionDetection = CollisionDetectionMode.Discrete;
            
            public PhysicMaterial colliderMaterial;
            public float colliderRadius;
            public float boxZOffset;
            public float boxZSize = .25f;

            public BoneProfile(HumanBodyBones bone, Vector2 angularXLimit, float angularYLimit, float angularZLimit, Vector3 axis1, Vector3 axis2, float mass, float colliderRadius, float boxZOffset, float boxZSize) {
                this.bone = bone;

                this.angularXLimit = angularXLimit;
                this.angularYLimit = angularYLimit;
                this.angularZLimit = angularZLimit;
                
                this.axis1 = axis1;
                this.axis2 = axis2;

                angularDrag = 0.05f;
                this.mass = mass;
                maxAngularVelocity = 1000f;
                maxDepenetrationVelocity = 10;
                
                interpolation = RigidbodyInterpolation.None;
                collisionDetection = CollisionDetectionMode.Discrete;
            

                this.colliderRadius = colliderRadius;
                this.boxZOffset = boxZOffset;
                this.boxZSize = boxZSize;
            }
        }
        public Vector3 headOffset = defaultHeadOffset;
        public BoneProfile[] bones = defaultBoneProfiles;

        static RagdollProfile _defaultProfile;
        public static RagdollProfile defaultProfile {
            get {
                if (_defaultProfile == null) {
                    _defaultProfile = ScriptableObject.CreateInstance<RagdollProfile>();
                    _defaultProfile.headOffset = defaultHeadOffset;
                    _defaultProfile.bones = defaultBoneProfiles;
                }
                return _defaultProfile;
            }
        }
        
        static Vector3 defaultHeadOffset {
            get {
                return new Vector3(0, -.05f, 0);
            }
        }

        static BoneProfile[] defaultBoneProfiles {
            get {
                return new BoneProfile[] {
                    new BoneProfile(HumanBodyBones.Hips,  new Vector2(0, 0), 0, 0, Vector3.right, Vector3.forward, 2.5f, 0, 0, .2f),
                    new BoneProfile(HumanBodyBones.Chest, new Vector2(-45, 15), 15, 15, Vector3.right, Vector3.forward, 2.5f, 0, 0, .2f),
                    new BoneProfile(HumanBodyBones.Head,  new Vector2(-75, 75), 25, 25, Vector3.right, Vector3.forward, 1.0f, .15f, 0, .2f),
                    
                    new BoneProfile(HumanBodyBones.RightLowerLeg, new Vector2(-90, 0), 10, 10, Vector3.right, Vector3.forward, 1.5f, .075f, 0, .2f),
                    new BoneProfile(HumanBodyBones.LeftLowerLeg,  new Vector2(-90, 0), 10, 10, Vector3.right, Vector3.forward, 1.5f, .075f, 0, .2f),

                    new BoneProfile(HumanBodyBones.RightUpperLeg, new Vector2(-75, 100), 25, 0, Vector3.right, Vector3.forward, 1.5f, .1f, 0, .2f),
                    new BoneProfile(HumanBodyBones.LeftUpperLeg,  new Vector2(-75, 100), 25, 0, Vector3.right, Vector3.forward, 1.5f, .1f, 0, .2f),

                    new BoneProfile(HumanBodyBones.RightLowerArm, new Vector2(0, 180 ), 15, 15, Vector3.up, Vector3.forward, 1.0f, .075f, 0, .2f ),
                    new BoneProfile(HumanBodyBones.LeftLowerArm,  new Vector2(-180, 0), 15, 15, Vector3.up, Vector3.forward, 1.0f, .075f, 0, .2f),

                    new BoneProfile(HumanBodyBones.RightUpperArm, new Vector2(-45, 90), 85, 25, Vector3.forward, Vector3.up, 1.0f, .075f, 0, .2f),
                    new BoneProfile(HumanBodyBones.LeftUpperArm,  new Vector2(-90, 45), 85, 25, Vector3.forward, Vector3.up, 1.0f, .075f, 0, .2f),
                };
            }
        }
    }
}