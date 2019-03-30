using UnityEngine;
using UnityEditor;
namespace DynamicRagdoll {
    [CustomEditor(typeof(RagdollProfile))]
    public class RagdollProfileEditor : Editor {
        static bool[] showBones = new bool[Ragdoll.ragdollUsedBones.Length];
        static void DrawPropertiesBlock(SerializedProperty baseProp, string label, GUIStyle s, string[] names) {
            EditorGUILayout.LabelField("<b>" + label + "</b>", s);    
            EditorGUI.indentLevel++;
            for (int i = 0; i < names.Length; i++) {
                EditorGUILayout.PropertyField(baseProp.FindPropertyRelative(names[i]));   
            }
            EditorGUI.indentLevel--;        
        }
        public static void DrawProfile (SerializedObject profile) {
            GUIStyle s = new GUIStyle(EditorStyles.label);
            s.richText=true;
            GUIStyle fs = new GUIStyle(EditorStyles.foldout);
            fs.richText=true;
            EditorGUILayout.LabelField("<b>Profile Values:</b>", s);
            EditorGUILayout.BeginVertical(GUI.skin.window, GUILayout.MinHeight(10));
            EditorGUI.indentLevel++;
            SerializedProperty boneProfiles = profile.FindProperty("bones");
            for (int i = 0; i < boneProfiles.arraySize; i++) {
                SerializedProperty boneProfile = boneProfiles.GetArrayElementAtIndex(i);
                SerializedProperty bone = boneProfile.FindPropertyRelative("bone");
                if (i == 3 || i == 7) {
                    EditorGUILayout.Space();
                }
                showBones[i] = EditorGUILayout.Foldout(showBones[i], "<b>" + bone.enumDisplayNames[bone.enumValueIndex] + ":</b>", fs);
                if (showBones[i]) {
                    EditorGUI.indentLevel++;
                    HumanBodyBones b = (HumanBodyBones)bone.enumValueIndex;
                    if (b == HumanBodyBones.Head) {
                        EditorGUILayout.PropertyField(boneProfile.FindPropertyRelative("headOffset"));
                    }
                    EditorGUILayout.Space();
                    if (b != HumanBodyBones.Hips) {
                        DrawPropertiesBlock(boneProfile, "Joints:", s, new string[] { "angularXLimit", "angularYLimit", "angularZLimit", "forceOff", "axis1", "axis2" });   
                    }
                    DrawPropertiesBlock(boneProfile, "Rigidbody:", s, new string[] { "mass", "angularDrag", "drag", "maxAngularVelocity" });
                    DrawPropertiesBlock(boneProfile, "Collider:", s, b == HumanBodyBones.Hips || b == HumanBodyBones.Chest ? new string[] { "boxZOffset", "boxZSize" } : new string[] { "colliderRadius" });
                    EditorGUI.indentLevel--;                
                }
            }
            EditorGUI.indentLevel--;
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