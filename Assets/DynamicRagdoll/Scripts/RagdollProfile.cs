using UnityEngine;
using System.Collections.Generic;


#if UNITY_EDITOR
using UnityEditor;
#endif

namespace DynamicRagdoll {
    //ragdolled angular drag was 20

    /*
        Object to hold values for ragdolls
    */

    [System.Serializable] public class RagdollProfileBoneDataElement : BoneDataElement<RagdollProfile.BoneProfile> { }
    [System.Serializable] public class RagdollProfileBoneData : BoneData<RagdollProfileBoneDataElement, RagdollProfile.BoneProfile> {
        public RagdollProfileBoneData () : base() { }
        public RagdollProfileBoneData ( Dictionary<HumanBodyBones, RagdollProfile.BoneProfile> template ) :  base (template) { }
        public RagdollProfileBoneData ( RagdollProfile.BoneProfile template ) :  base (template) { }
    }


    [CreateAssetMenu()]
    public class RagdollProfile : ScriptableObject {

        //per bone options
        [System.Serializable] public class BoneProfile {
            /*
                JOINT OPTIONS
            */
            public Vector2 angularXLimit;
            public float angularYLimit;
            public float angularZLimit = 0;
            [Tooltip("Temporarily disable joint motion (all limits == 0)")] public bool forceOff;
            public Vector3 axis1, axis2;

            /*
                RIGIDBODY OPTIONS
            */
            public float mass;
            public float angularDrag = 0.05f;
            public float drag = 0.0f;
            public float maxAngularVelocity = 1000f; 
            public float maxDepenetrationVelocity = 10; // or 1e+32;
            
            public RigidbodyInterpolation interpolation = RigidbodyInterpolation.None;
            public CollisionDetectionMode collisionDetection = CollisionDetectionMode.ContinuousDynamic;
            
            /*
                COLLIDER OPTIONS
            */
            public PhysicMaterial colliderMaterial;
            public float colliderRadius;
            public float boxZOffset;
            public float boxZSize = .35f;


            public int boneIndex;
            public bool showJointsEditor, showRBEditor, showColliderEditor;
  
            public BoneProfile(int boneIndex, Vector2 angularXLimit, float angularYLimit, float angularZLimit, Vector3 axis1, Vector3 axis2, float mass, float colliderRadius, float boxZOffset, float boxZSize) {
                this.boneIndex = boneIndex;

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
                collisionDetection = CollisionDetectionMode.ContinuousDynamic;
            

                this.colliderRadius = colliderRadius;
                this.boxZOffset = boxZOffset;
                this.boxZSize = boxZSize;
            }
        }

        
        public Vector3 headOffset = defaultHeadOffset;
        [BoneData] public RagdollProfileBoneData boneData = defaultBoneData;

        static RagdollProfile _defaultProfile;
        public static RagdollProfile defaultProfile {
            get {
                if (_defaultProfile == null || !Application.isPlaying) {
                    _defaultProfile = ScriptableObject.CreateInstance<RagdollProfile>();
                    _defaultProfile.headOffset = defaultHeadOffset;
                    _defaultProfile.boneData = defaultBoneData;
                }
                return _defaultProfile;
            }
        }
        
        static Vector3 defaultHeadOffset { get { return new Vector3(0, -.05f, 0); } }


        static RagdollProfileBoneData defaultBoneData {
            get {
                return new RagdollProfileBoneData(
                    new Dictionary<HumanBodyBones, BoneProfile> () {
                        { HumanBodyBones.Hips,          new BoneProfile(0,  new Vector2(0, 0), 0, 0, Vector3.right, Vector3.forward, 2.5f, 0, 0, .3f) },
                        { HumanBodyBones.Chest,         new BoneProfile(1,  new Vector2(-45, 15), 15, 15, Vector3.right, Vector3.forward, 2.5f, 0, 0, .3f) },
                        { HumanBodyBones.Head,          new BoneProfile(2,  new Vector2(-75, 75), 25, 25, Vector3.right, Vector3.forward, 1.0f, .15f, 0, .3f) },
                        { HumanBodyBones.RightLowerLeg, new BoneProfile(3,  new Vector2(-90, 0), 10, 10, Vector3.right, Vector3.forward, 1.5f, .075f, 0, .2f) },
                        { HumanBodyBones.LeftLowerLeg,  new BoneProfile(4,  new Vector2(-90, 0), 10, 10, Vector3.right, Vector3.forward, 1.5f, .075f, 0, .2f) },
                        { HumanBodyBones.RightUpperLeg, new BoneProfile(5,  new Vector2(-75, 100), 25, 0, Vector3.right, Vector3.forward, 1.5f, .1f, 0, .2f) },
                        { HumanBodyBones.LeftUpperLeg,  new BoneProfile(6,  new Vector2(-75, 100), 25, 0, Vector3.right, Vector3.forward, 1.5f, .1f, 0, .2f) },
                        { HumanBodyBones.RightLowerArm, new BoneProfile(7,  new Vector2(0, 180 ), 15, 15, Vector3.up, Vector3.forward, 1.0f, .075f, 0, .2f ) },
                        { HumanBodyBones.LeftLowerArm,  new BoneProfile(8,  new Vector2(-180, 0), 15, 15, Vector3.up, Vector3.forward, 1.0f, .075f, 0, .2f) },
                        { HumanBodyBones.RightUpperArm, new BoneProfile(9,  new Vector2(-45, 90), 85, 25, Vector3.forward, Vector3.up, 1.0f, .075f, 0, .2f) },
                        { HumanBodyBones.LeftUpperArm,  new BoneProfile(10, new Vector2(-90, 45), 85, 25, Vector3.forward, Vector3.up, 1.0f, .075f, 0, .2f) },
                    }
                );
            }
        }
    }


    
#if UNITY_EDITOR
    [CustomPropertyDrawer(typeof(RagdollProfile.BoneProfile))] public class RagdollProfileBoneProfileDrawer : PropertyDrawer
    {

        static GUIStyle _foldoutStyle;
        static GUIStyle foldoutStyle {
            get {
                if (_foldoutStyle == null) {
                    _foldoutStyle = new GUIStyle(EditorStyles.foldout);
                    _foldoutStyle.richText = true;
                }
                return _foldoutStyle;
            }
        }


        const int buttonWidth = 20;
        public static float DrawIndent (int level, float startX) {
            return startX + buttonWidth * level;
        }       

        static void DrawPropertiesBlock(SerializedProperty property, Rect position, float _x, ref float y, string shownProp, string label, string[] names){//, ref bool show) {
            
            SerializedProperty shown = property.FindPropertyRelative(shownProp);
            
            shown.boolValue = EditorGUI.Foldout(new Rect(_x, y, position.width, EditorGUIUtility.singleLineHeight), shown.boolValue, "<b>" + label + ":</b>", foldoutStyle);
            y += EditorGUIUtility.singleLineHeight;
            
            if (shown.boolValue) {
                EditorGUI.indentLevel++;
                for (int i = 0; i < names.Length; i++) {
                    EditorGUI.PropertyField(new Rect(_x, y, position.width, EditorGUIUtility.singleLineHeight), property.FindPropertyRelative(names[i]));   
                    y += EditorGUIUtility.singleLineHeight;
                }
                EditorGUI.indentLevel--;   
            }     
        }
        static void DecideHeightPropBlock(SerializedProperty property, string shownProp, ref float h, int propsLength){
            h += EditorGUIUtility.singleLineHeight;
            if (property.FindPropertyRelative(shownProp).boolValue) {
                h += EditorGUIUtility.singleLineHeight * propsLength;        
            }     
        }

        static string[] jointsPropsNames = new string[] { "angularXLimit", "angularYLimit", "angularZLimit", "forceOff", "axis1", "axis2" };
        static string[] rigidbodyPropsNames = new string[] { "mass", "angularDrag", "drag", "maxAngularVelocity", "interpolation", "collisionDetection", "maxDepenetrationVelocity" };
        static string[] boxColliderPropsNames = new string[] { "boxZOffset", "boxZSize", "colliderMaterial" };
        static string[] capsuleColliderPropsNames = new string[] { "colliderRadius", "colliderMaterial" };
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            float _x = DrawIndent(EditorGUI.indentLevel, position.x);
            float y = position.y;
        
            int boneIndex = property.FindPropertyRelative("boneIndex").intValue;
            //joints
            if (boneIndex != 0) DrawPropertiesBlock(property, position, _x, ref y, "showJointsEditor", "Joints", jointsPropsNames);
            //rigidbody
            DrawPropertiesBlock(property, position, _x, ref y, "showRBEditor", "Rigidbody", rigidbodyPropsNames);
            //collider
            bool isBox = boneIndex < 2;
            DrawPropertiesBlock(property, position, _x, ref y, "showColliderEditor", "Collider", isBox ? boxColliderPropsNames : capsuleColliderPropsNames);
              
            EditorGUI.EndProperty();
        }
        
        public override float GetPropertyHeight(SerializedProperty prop, GUIContent label)
        {
            float h = 0;
            int boneIndex = prop.FindPropertyRelative("boneIndex").intValue;
            //joints
            if (boneIndex != 0) DecideHeightPropBlock( prop, "showJointsEditor", ref h, jointsPropsNames.Length);   
            //rigidbody
            DecideHeightPropBlock( prop, "showRBEditor", ref h, rigidbodyPropsNames.Length);        
            //collider
            bool isBox = boneIndex < 2;
            DecideHeightPropBlock( prop, "showColliderEditor", ref h, boneIndex < 2 ? boxColliderPropsNames.Length : capsuleColliderPropsNames.Length);
            return h;
        }
    }
#endif
}