using UnityEngine;
using UnityEditor;
namespace DynamicRagdoll {
    [CustomEditor(typeof(Ragdoll))]
    public class RagdollEditor : Editor {
        Ragdoll ragdoll;
                    
        void OnEnable () {
            ragdoll = target as Ragdoll;
        }
                    
        void DrawBuildOptions () {
            EditorGUILayout.Space();

            EditorGUILayout.BeginHorizontal();
            
            SerializedProperty preBuiltProp = serializedObject.FindProperty("preBuilt");
            bool isBuilt = preBuiltProp.boolValue;
            if (GUILayout.Button(isBuilt ? "Clear Ragdoll" : "Pre Build Ragdoll")) {
                if (isBuilt)
                    RagdollBuilder.EraseRagdoll(ragdoll.GetComponent<Animator>());
                else
                    RagdollBuilder.BuildRagdollFull (ragdoll.GetComponent<Animator>(), ragdoll.ragdollProfile, out _);

                preBuiltProp.boolValue = !isBuilt;
            }

            if (isBuilt) {
                if (ragdoll.ragdollProfile) {
                    if (GUILayout.Button("Update Ragdoll To Profile")) {
                        RagdollBuilder.BuildRagdollFromPrebuilt(ragdoll.GetComponent<Animator>(), ragdoll.ragdollProfile, out _);
                    }
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        public override void OnInspectorGUI() {
            base.OnInspectorGUI();   
            
            DrawBuildOptions();
            if (ragdoll.ragdollProfile) {
                RagdollProfileEditor.DrawProfile(new SerializedObject( ragdoll.ragdollProfile ));
            }
            else {
                EditorGUILayout.HelpBox("\nAdd a Ragdoll Profile to adjust ragdoll properties.\n\nOtherwise defualts will be used\n", MessageType.Info);
            }

            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(serializedObject.targetObject);
        }
    }
}