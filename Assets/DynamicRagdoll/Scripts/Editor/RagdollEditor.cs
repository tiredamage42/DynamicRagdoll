using UnityEngine;
using UnityEditor;
namespace DynamicRagdoll {
    [CustomEditor(typeof(Ragdoll))]
    public class RagdollEditor : Editor {
        void DrawBuildOptions () {
            EditorGUILayout.Space();
            EditorGUILayout.Space();
            
            EditorGUILayout.BeginHorizontal();
            
            SerializedProperty preBuiltProp = serializedObject.FindProperty("preBuilt");
            if (!preBuiltProp.boolValue) {
                if (GUILayout.Button("Pre Build Ragdoll")) {
                    Ragdoll.BuildRagdollBase ((target as Component).GetComponent<Animator>());
                    preBuiltProp.boolValue = true;
                    serializedObject.ApplyModifiedProperties();
                    EditorUtility.SetDirty(serializedObject.targetObject);
                }
            }
            else {
                if (GUILayout.Button("Clear Ragdoll")) {
                    Ragdoll.EraseRagdoll((target as Component).GetComponent<Animator>());
                    preBuiltProp.boolValue = false;
                    serializedObject.ApplyModifiedProperties();
                    EditorUtility.SetDirty(serializedObject.targetObject);
                }
            }

            EditorGUILayout.EndHorizontal();
        }

        public override void OnInspectorGUI() {
            DrawBuildOptions();
            base.OnInspectorGUI();   
            RagdollProfile profile = (target as Ragdoll).ragdollProfile;
            if (profile) {
                RagdollProfileEditor.DrawProfile(new SerializedObject( profile ));
            }
            else {
                EditorGUILayout.HelpBox("Add a Ragdoll Profile to start using this ragdoll", MessageType.Warning);
            }
        }
    }
}