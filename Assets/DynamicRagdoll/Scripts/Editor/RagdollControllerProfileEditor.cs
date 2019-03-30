using UnityEngine;
using UnityEditor;
namespace DynamicRagdoll {
    [CustomEditor(typeof(RagdollControllerProfile))]
    public class RagdollControllerProfileEditor : Editor {
        static bool[] showBones = new bool[Ragdoll.ragdollUsedBones.Length];
        static void DrawPropertiesBlock(SerializedProperty baseProp, string[] names) {
            EditorGUI.indentLevel++;
            for (int i = 0; i < names.Length; i++) {
                EditorGUILayout.PropertyField(baseProp.FindPropertyRelative(names[i]));   
            }
            EditorGUI.indentLevel--;        
        }
        static void DrawPropertiesBlock(SerializedObject baseProp, string label, GUIStyle s, string[] names) {
            EditorGUILayout.LabelField(label, s);
            EditorGUI.indentLevel++;
            for (int i = 0; i < names.Length; i++) {
                EditorGUILayout.PropertyField(baseProp.FindProperty(names[i]));   
            }
            EditorGUI.indentLevel--;        
        }
        public static void DrawProfile (SerializedObject profile) {
            GUIStyle s = new GUIStyle(EditorStyles.label);
            s.richText=true;
            GUIStyle fs = new GUIStyle(EditorStyles.foldout);
            fs.richText=true;
            EditorGUILayout.LabelField("<b>Controller Profile Values:</b>", s);
            EditorGUILayout.BeginVertical(GUI.skin.window, GUILayout.MinHeight(0));
            EditorGUILayout.LabelField("<b>Follow Weights:</b>", s);
            EditorGUI.indentLevel++;
            SerializedProperty boneProfiles = profile.FindProperty("bones");
            for (int i = 0; i < boneProfiles.arraySize; i++) {
                SerializedProperty boneProfile = boneProfiles.GetArrayElementAtIndex(i);
                SerializedProperty bone = boneProfile.FindPropertyRelative("bone");
                if (i == 3 || i == 7) {
                    EditorGUILayout.Space();
                }
                showBones[i] = EditorGUILayout.Foldout(showBones[i], bone.enumDisplayNames[bone.enumValueIndex] + ":", fs);
                if (showBones[i]) {
                    DrawPropertiesBlock(boneProfile, new string[] { "inputForce", "maxForce", "maxTorque" });
                }
            }
            EditorGUI.indentLevel--;
            DrawPropertiesBlock(profile, "<b>Controlled:</b>", s, new string[] { "PForce", "DForce", "maxForce", "maxJointTorque" });
		    DrawPropertiesBlock(profile, "<b>Falling:</b>", s, new string[] { "fallLerp", "residualForce", "residualJointTorque" });
		    DrawPropertiesBlock(profile, "<b>Get Up:</b>", s, new string[] { "ragdollMinTime", "settledSpeed", "orientateDelay", "checkGroundMask", "blendTime" });
            
            EditorGUILayout.EndVertical();        
            profile.ApplyModifiedProperties();
            EditorUtility.SetDirty(profile.targetObject);
        }
        public override void OnInspectorGUI() {
            //base.OnInspectorGUI();
            DrawProfile(serializedObject);
        }
    }
}