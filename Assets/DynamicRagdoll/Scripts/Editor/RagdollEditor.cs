using UnityEngine;
using UnityEditor;
namespace DynamicRagdoll {
    [CustomEditor(typeof(Ragdoll))]
    public class RagdollEditor : Editor {

        Ragdoll ragdoll;
        RagdollProfile profile;
        SerializedObject profileSO;
        void CheckForProfileChange () {
            if (profile != ragdoll.ragdollProfile) {
                profile = ragdoll.ragdollProfile;
                profileSO = profile != null ? new SerializedObject(profile) : null;
            }
        }
                    
        void OnEnable () {
            ragdoll = target as Ragdoll;
            CheckForProfileChange();
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
                    RagdollBuilder.BuildRagdoll (ragdoll.GetComponent<Animator>(), ragdoll.ragdollProfile, true, false, out _, out _, out _);

                preBuiltProp.boolValue = !isBuilt;
            }

            if (isBuilt) {
                if (ragdoll.ragdollProfile) {
                    if (GUILayout.Button("Update Ragdoll To Profile")) {
                        RagdollBuilder.BuildRagdoll(ragdoll.GetComponent<Animator>(), ragdoll.ragdollProfile, false, false, out _, out _, out _);
                    }
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        public override void OnInspectorGUI() {
            CheckForProfileChange();

            if (profileSO != null) {
                //RagdollProfileEditor.DrawProfile(profileSO);
            }
            else {
                EditorGUILayout.HelpBox("\nAdd a Ragdoll Profile to adjust ragdoll properties.\n\nOtherwise defualts will be used\n", MessageType.Info);
            }

            base.OnInspectorGUI();   
            
            DrawBuildOptions();
            if (profileSO != null) {
                RagdollProfileEditor.DrawProfile(profileSO);
            }
            else {
                //EditorGUILayout.HelpBox("\nAdd a Ragdoll Profile to adjust ragdoll properties.\n\nOtherwise defualts will be used\n", MessageType.Info);
            }

            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(serializedObject.targetObject);
        }
    }
}