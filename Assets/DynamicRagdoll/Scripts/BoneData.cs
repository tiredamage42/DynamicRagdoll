using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace DynamicRagdoll {
    /*
        to easily have any per bone custom data

        extend BoneDataElement<DATATYPE> where DATATYPE is the custom data you want to store per bone

        extend BoneData<BONE_DATA_ELEMENT_TYPE, DATATYPE> 
            where BONE_DATA_ELEMENT_TYPE is the extended 'BoneDataElement<DATATYPE>' Class, 
            where DATATYPE is the custom data you want to store per bone
        
        and create any constructors you'll use (just call the base constructor where applicable...)


        then when using in editor, add teh bone data attribute:
            
            [BoneData] public ExtendedBoneDataClass editorBoneData;


        this all needs to be done to keep these generic types serializable by unity.

        for example useage see RagdollProfile.cs or RagdollControllerProfile.cs

    */


    public class BoneDataAttribute : PropertyAttribute { }
    
    [System.Serializable] public class BoneDataElement<DATATYPE> {
        [HideInInspector] public bool editorShown;
        [HideInInspector] public HumanBodyBones bone;
        public DATATYPE boneData;

        public static BONE_DATA_ELEMENT_TYPE GetBoneDataElement<BONE_DATA_ELEMENT_TYPE, D> (HumanBodyBones bone) where BONE_DATA_ELEMENT_TYPE : BoneDataElement<D>, new() {
            BONE_DATA_ELEMENT_TYPE newB = new BONE_DATA_ELEMENT_TYPE ();
            newB.bone = bone;
            return newB;
        }
        public static BONE_DATA_ELEMENT_TYPE GetBoneDataElement<BONE_DATA_ELEMENT_TYPE, D> (HumanBodyBones bone, D boneData) where BONE_DATA_ELEMENT_TYPE : BoneDataElement<D>, new() {
            BONE_DATA_ELEMENT_TYPE newB = new BONE_DATA_ELEMENT_TYPE ();
            newB.bone = bone;
            newB.boneData = boneData;
            return newB;
        }
    }

    [System.Serializable] public class BoneData<BONE_DATA_ELEMENT_TYPE, DATATYPE> where BONE_DATA_ELEMENT_TYPE : BoneDataElement<DATATYPE>, new() {
        
        public DATATYPE this[HumanBodyBones bone] { get { return boneDatas[Ragdoll.Bone2Index(bone)].boneData; } }  
        int bonesCount { get { return Ragdoll.bonesCount; } }
        HumanBodyBones[] humanBones { get { return Ragdoll.humanBones; } }
        public BONE_DATA_ELEMENT_TYPE[] boneDatas;
        
        public BoneData ( Dictionary<HumanBodyBones,DATATYPE> template ) {
            boneDatas = new BONE_DATA_ELEMENT_TYPE[bonesCount];
            for (int i = 0; i < bonesCount; i++) boneDatas[i] = BoneDataElement<DATATYPE>.GetBoneDataElement<BONE_DATA_ELEMENT_TYPE, DATATYPE> (humanBones[i], template[humanBones[i]]);
        }

        public BoneData ( DATATYPE template ) {
            boneDatas = new BONE_DATA_ELEMENT_TYPE[bonesCount];
            for (int i = 0; i < bonesCount; i++) boneDatas[i] = BoneDataElement<DATATYPE>.GetBoneDataElement<BONE_DATA_ELEMENT_TYPE, DATATYPE> (humanBones[i], template);
        }
        
        public BoneData ( ) {
            boneDatas = new BONE_DATA_ELEMENT_TYPE[bonesCount];
            for (int i = 0; i < bonesCount; i++) boneDatas[i] = BoneDataElement<DATATYPE>.GetBoneDataElement<BONE_DATA_ELEMENT_TYPE, DATATYPE>  (humanBones[i]);
        }
    }

    

#if UNITY_EDITOR
    [CustomPropertyDrawer(typeof(BoneDataAttribute))] public class BoneDataAttributeDrawer : PropertyDrawer
    {

        GUIStyle _foldoutStyle;
        GUIStyle foldoutStyle {
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

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            float h = GetPropertyHeight(property, null);

            label.text = property.displayName;
            
            EditorGUI.BeginProperty(position, label, property);
            
            property = property.FindPropertyRelative("boneDatas");
            
            float singleLine = EditorGUIUtility.singleLineHeight;
            
            float _x = DrawIndent(EditorGUI.indentLevel, position.x);
            float y = position.y;

            float buttonWidth = 12;
            
            GUI.Label(new Rect(_x + buttonWidth, y, position.width, singleLine), label);
            

            GUI.backgroundColor = new Color(0,0,0,.1f);
            GUI.Box( new Rect(_x, y, position.width, h ),"");
            GUI.backgroundColor = Color.white;
            

            float[] boxHeights = new float[3];
            float[] boxStarts = new float[3];

            EditorGUI.indentLevel ++;

            int boxHeightsIndex = 0;

            int arraySize = property.arraySize;
            for (int i = 0; i < arraySize; i++) {
                if (i == 0 || i == 3 || i == 7) {
                    y += singleLine;
                    if (i != 0) boxHeightsIndex ++;
                    boxStarts[boxHeightsIndex] = y;
                }
                        
                SerializedProperty p = property.GetArrayElementAtIndex(i);

                SerializedProperty shown = p.FindPropertyRelative("editorShown");
                SerializedProperty bone = p.FindPropertyRelative("bone");


                shown.boolValue = EditorGUI.Foldout(new Rect(position.x + buttonWidth + 5, y, position.width, singleLine), shown.boolValue, "<b>" + bone.enumDisplayNames[bone.enumValueIndex] + ":</b>", foldoutStyle);

                if (shown.boolValue) {
                    SerializedProperty boneData = p.FindPropertyRelative("boneData");
                    EditorGUI.PropertyField(new Rect(position.x + buttonWidth + 5, y + singleLine, position.width, singleLine), boneData, includeChildren:true);
                    
                    float hght = EditorGUI.GetPropertyHeight(boneData, true) + singleLine;
                    y += hght;
                    boxHeights[boxHeightsIndex] += hght;
                }
                else {
                    float hght = singleLine;
                    y += hght;
                    boxHeights[boxHeightsIndex] += hght;
                }
            }

            GUI.backgroundColor = new Color(0,0,0,.1f);
            for (int i = 0; i < 3; i++) GUI.Box( new Rect(position.x + buttonWidth, boxStarts[i], position.width, boxHeights[i] ),"");
            GUI.backgroundColor = Color.white;    
            
            EditorGUI.indentLevel--;
            
            EditorGUI.EndProperty();
        }
        
        public override float GetPropertyHeight(SerializedProperty prop, GUIContent label)
        {
            prop = prop.FindPropertyRelative("boneDatas");
            float h = EditorGUIUtility.singleLineHeight * (prop.arraySize == 0 ? 1 : 1.5f);
            for (int i = 0; i < prop.arraySize; i++) {
                if (i == 3 || i == 7) h += EditorGUIUtility.singleLineHeight;

                SerializedProperty shown = prop.GetArrayElementAtIndex(i).FindPropertyRelative("editorShown");
                if (shown.boolValue) {
                    h += EditorGUI.GetPropertyHeight(prop.GetArrayElementAtIndex(i).FindPropertyRelative("boneData"), true) + EditorGUIUtility.singleLineHeight;
                }
                else {
                    h += EditorGUIUtility.singleLineHeight;
                }
            }
            return h;
        }
    }
#endif
}
