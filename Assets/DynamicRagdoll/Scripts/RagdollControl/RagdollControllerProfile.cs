using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace DynamicRagdoll {
    [System.Serializable] public class RagdollControllerProfileBoneDataElement : BoneDataElement<RagdollControllerProfile.BoneProfile> { }
    [System.Serializable] public class RagdollControllerProfileBoneData : BoneData<RagdollControllerProfileBoneDataElement, RagdollControllerProfile.BoneProfile> {
        public RagdollControllerProfileBoneData () : base() { }
        public RagdollControllerProfileBoneData ( Dictionary<HumanBodyBones, RagdollControllerProfile.BoneProfile> template ) :  base (template) { }
        public RagdollControllerProfileBoneData ( RagdollControllerProfile.BoneProfile template ) :  base (template) { }
    }

    /*
        object for holding controller values (makes it easier to set during runtime)
    */
    [CreateAssetMenu()]
    public class RagdollControllerProfile : ScriptableObject
    {
        // per bone options
        [System.Serializable] public class BoneProfile {

            /*
                Normal velocity set method
            */
            public AnimationCurve fallForceDecay = new AnimationCurve(new Keyframe[] { new Keyframe(0, 1), new Keyframe(1, 0) });
            public AnimationCurve fallTorqueDecay = new AnimationCurve(new Keyframe[] { new Keyframe(0, 1), new Keyframe(1, 0) });
            
            /*
			    Define which bones count as neighbors for other bones (for the bone decay system)
		    */
            public HumanBodyBones[] neighbors;


            public int boneIndex;

            public BoneProfile(int boneIndex, HumanBodyBones[] neighbors) {
                this.boneIndex = boneIndex;
                
                fallForceDecay = new AnimationCurve(new Keyframe[] { new Keyframe(0, 1), new Keyframe(1, 0) });
                fallTorqueDecay = new AnimationCurve(new Keyframe[] { new Keyframe(0, 1), new Keyframe(1, 0) });
            
                this.neighbors = neighbors;
            }
        }

        [Header("Fall Decays and Bone Neighbors")] 
        [BoneData] public RagdollControllerProfileBoneData boneData = new RagdollControllerProfileBoneData(
            new Dictionary<HumanBodyBones, BoneProfile> () {
                { HumanBodyBones.Hips, new BoneProfile(0, new HumanBodyBones[] { HumanBodyBones.Chest, HumanBodyBones.LeftUpperLeg, HumanBodyBones.RightUpperLeg,}) },
                { HumanBodyBones.Chest, new BoneProfile(1, new HumanBodyBones[] { HumanBodyBones.Head, HumanBodyBones.LeftUpperArm, HumanBodyBones.RightUpperArm}) }, 
                { HumanBodyBones.Head, new BoneProfile(2, new HumanBodyBones[] { HumanBodyBones.Chest, HumanBodyBones.LeftUpperArm, HumanBodyBones.RightUpperArm }) }, 
                { HumanBodyBones.RightLowerLeg, new BoneProfile(3, new HumanBodyBones[] { HumanBodyBones.RightUpperLeg, HumanBodyBones.Hips ,}) }, 
                { HumanBodyBones.LeftLowerLeg, new BoneProfile(4, new HumanBodyBones[] { HumanBodyBones.LeftUpperLeg, HumanBodyBones.Hips }) }, 
                { HumanBodyBones.RightUpperLeg, new BoneProfile(5, new HumanBodyBones[] { HumanBodyBones.Hips, HumanBodyBones.RightLowerLeg ,}) }, 
                { HumanBodyBones.LeftUpperLeg, new BoneProfile(6, new HumanBodyBones[] { HumanBodyBones.Hips, HumanBodyBones.LeftLowerLeg ,}) }, 
                { HumanBodyBones.RightLowerArm, new BoneProfile(7, new HumanBodyBones[] { HumanBodyBones.RightUpperArm, HumanBodyBones.Chest }) }, 
                { HumanBodyBones.LeftLowerArm, new BoneProfile(8, new HumanBodyBones[] { HumanBodyBones.LeftUpperArm, HumanBodyBones.Chest }) }, 
                { HumanBodyBones.RightUpperArm, new BoneProfile(9, new HumanBodyBones[] { HumanBodyBones.Chest, HumanBodyBones.RightLowerArm,HumanBodyBones.LeftUpperArm, HumanBodyBones.Head,}) }, 
                { HumanBodyBones.LeftUpperArm, new BoneProfile(10, new HumanBodyBones[] { HumanBodyBones.Chest, HumanBodyBones.LeftLowerArm ,HumanBodyBones.RightUpperArm, HumanBodyBones.Head,}) }, 
            }
        );
           
        /*
            Normal velocity set values
        */
        [Header("Falling")]
        [Range(0f, 10000f)] public float maxTorque = 250f;

        [Tooltip("Default for how fast the character loses control after ragdolling")]
        [Range(.5f, 4.5f)] public float fallDecaySpeed = 1.5f;				
        [Tooltip("Calculated animation velocities will have gravity added to them\nif their magnitude is under this value.\n\nRaise if ragdoll hangs in the air too much")]
		[Range(0f, 1f)] public float maxGravityAddVelocity = 1f;

        [Range(0,1)] public float loseFollowDot = .5f;

        
        [Header("Get Up")]
        [Tooltip("Minimum time to spend ragdolled before trying to get up")]
		[Range(0f, 5f)] public float ragdollMinTime = 3;				
		
        [Tooltip("When ragdollRootBoone goes below this speed\nThe falling state is through and the get up starts")]
		[Range(0f, .4f)] public float settledSpeed = .05f;				
		
        [Tooltip("Wait for animation transition into getup animation")]
		public float orientateDelay = .25f;
        public LayerMask checkGroundMask;

		[Tooltip("How long do we blend when transitioning from ragdolled to animated")]
		public float blendTime = 1f;
		


        

        [Header("Ragdoll OnCollision Detection")]
        [Tooltip("When we run into something.\nHow fast do we have to be going to go ragdoll")]
        public float outgoingMagnitudeThreshold = 5;
        
        [Tooltip("When we get hit with an object.\nHow fast does it have to be going to go ragdoll")]
        public float incomingMagnitudeThreshold = 5;
        
        [Tooltip("Objects touching us from above, that are over this mass, will trigger ragdoll")]
        public float crushMass = 20;
        [Tooltip("How far down from the top of our head should we consider a crush contact")]
        public float crushMassTopOffset = .25f;
                
        /*
            this wide range means it doesnt necessarily decay any bones completely
            but adds some noise to the decay, that way the more we collide the more we slow down
            ...not in one fell swoop 
        */
        [Header("Bone Decay On Collision")]
        [Tooltip("Collisions magnitude range for linearly interpolating the bone decay set by collisions.\nIf magnitude == x, then bone decay = 0\nIf magnitude == y, then bone decay = 1")] 
        public Vector2 decayMagnitudeRange = new Vector2(10, 50);

        [Tooltip("Multiplier for decay set by collisions on neighbors of collided bones")]
        [Range(0,1)] public float neighborDecayMultiplier = .75f;
		
    }


    
    
#if UNITY_EDITOR
    [CustomPropertyDrawer(typeof(RagdollControllerProfile.BoneProfile))] public class RagdollControllerProfileBoneProfileDrawer : PropertyDrawer
    {
        const int buttonWidth = 20;
        public static float DrawIndent (int level, float startX) {
            return startX + buttonWidth * level;
        }       


        static void DrawPropertiesBlock(SerializedProperty property, Rect position, float _x, ref float y, string[] names){
            
            EditorGUI.indentLevel++;
            for (int i = 0; i < names.Length; i++) {
                EditorGUI.PropertyField(new Rect(_x, y, position.width, EditorGUIUtility.singleLineHeight), property.FindPropertyRelative(names[i]));   
                y += EditorGUIUtility.singleLineHeight;
            }
            EditorGUI.indentLevel--;   
        }
                

        static void DrawBoneProfileNeighbors (SerializedProperty neighborsProp, Rect position, HumanBodyBones baseBone) {
            

            if (GUI.Button(position, new GUIContent("Neighbors", "Define which bones count as neighbors for other bones (for the bone decay system)"), EditorStyles.miniButton)) {
                int neighborsLength = neighborsProp.arraySize;
                System.Func<HumanBodyBones, bool> containsBone = (b) => {
                    int bi = (int)b;
                    for (int i = 0; i < neighborsLength; i++) {
                        if (neighborsProp.GetArrayElementAtIndex(i).enumValueIndex == bi) {
                            return true;
                        }
                    }
                    return false;
                };
                System.Func<HumanBodyBones, int> indexOf = (b) => {
                    int bi = (int)b;
                    for (int i = 0; i < neighborsLength; i++) {
                        if (neighborsProp.GetArrayElementAtIndex(i).enumValueIndex == bi) {
                            return i;
                        }
                    }
                    return -1;
                };

                System.Action<HumanBodyBones> removeBone = (b) => { neighborsProp.DeleteArrayElementAtIndex(indexOf(b)); };
            
                System.Action<HumanBodyBones> addBone = (b) => {
                    neighborsProp.InsertArrayElementAtIndex(neighborsLength);
                    neighborsProp.GetArrayElementAtIndex(neighborsLength).enumValueIndex = (int)b;
                };

                GenericMenu menu = new GenericMenu();
                for (int i = 0; i < Ragdoll.bonesCount; i++) {
                    HumanBodyBones hb = Ragdoll.humanBones[i];
                    if (hb == baseBone) continue;

                    menu.AddItem(new GUIContent(hb.ToString()), containsBone(hb), 
                        (b) => {
                        HumanBodyBones hb2 = (HumanBodyBones)b;
                        if (containsBone(hb2)) {
                            removeBone(hb2);
                        }
                        else {
                            addBone(hb2);
                        }

                    }, hb);
                }
                
                // display the menu
                menu.ShowAsContext();
            }
        }

        static string[] hipsPropsNames = new string[] { "fallForceDecay" };
        static string[] propsNames = new string[] { "fallForceDecay", "fallTorqueDecay" };
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            float _x = DrawIndent(EditorGUI.indentLevel, position.x);
            float y = position.y;
        
            int boneIndex = property.FindPropertyRelative("boneIndex").intValue;

            DrawPropertiesBlock(property, position, _x, ref y, boneIndex == 0 ? hipsPropsNames : propsNames);
            
            DrawBoneProfileNeighbors(property.FindPropertyRelative("neighbors"), new Rect(_x, y, position.width, EditorGUIUtility.singleLineHeight), Ragdoll.humanBones[boneIndex]);
            
            EditorGUI.EndProperty();
        }
        
        public override float GetPropertyHeight(SerializedProperty prop, GUIContent label)
        {
            return EditorGUIUtility.singleLineHeight * (prop.FindPropertyRelative("boneIndex").intValue == 0 ? 2 : 3);
        }
    }
#endif
}
