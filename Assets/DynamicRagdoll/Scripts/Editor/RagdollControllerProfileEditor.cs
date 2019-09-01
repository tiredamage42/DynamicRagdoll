using UnityEngine;
using UnityEditor;

namespace DynamicRagdoll {

    [CustomEditor(typeof(RagdollControllerProfile))]
    public class RagdollControllerProfileEditor : Editor {
        
        static void DrawPropertiesBlock(SerializedObject baseProp, string label, string[] names) {
            EditorGUILayout.LabelField("<b>"+label+":</b>", RagdollEditor.labelStyle);
            EditorGUI.indentLevel++;
            for (int i = 0; i < names.Length; i++) {
                EditorGUILayout.PropertyField(baseProp.FindProperty(names[i]));   
            }
            EditorGUI.indentLevel--;        
        }
        public static void DrawProfile (SerializedObject profile) {

            EditorGUI.BeginChangeCheck();

            EditorGUILayout.Space();
            
            EditorGUILayout.LabelField("<b>Controller Profile Values:</b>", RagdollEditor.labelStyle);

            EditorGUILayout.PropertyField(profile.FindProperty("boneData"));  

            RagdollEditor.StartBox();
            
            EditorGUI.indentLevel++;
                        
            EditorGUILayout.Space();

            DrawPropertiesBlock(profile, "Falling", new string[] { "maxTorque", "fallDecaySpeed", "maxGravityAddVelocity", "loseFollowDot" } );

		    DrawPropertiesBlock(profile, "Get Up", new string[] { "ragdollMinTime", "settledSpeed", "orientateDelay", "checkGroundMask", "blendTime" });
            
            EditorGUILayout.Space();

            EditorGUILayout.EndVertical();   
            
            EditorGUI.indentLevel--;

            EditorGUILayout.Space();   

            if (EditorGUI.EndChangeCheck()) {
                profile.ApplyModifiedProperties();
                EditorUtility.SetDirty(profile.targetObject);
            }
        }

        public override void OnInspectorGUI() {
            //base.OnInspectorGUI();
            DrawProfile(serializedObject);
        }

    }
}