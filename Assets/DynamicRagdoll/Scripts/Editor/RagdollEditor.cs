using UnityEngine;
using UnityEditor;
namespace DynamicRagdoll {
    [CustomEditor(typeof(Ragdoll))]
    public class RagdollEditor : Editor {

        static GUIStyle InitializeStyle (GUIStyle s) {
            GUIStyle r = new GUIStyle(s);
            r.richText = true;
            return r;
        }
        static GUIStyle _labelStyle;
        public static GUIStyle labelStyle {
            get {
                if (_labelStyle == null) _labelStyle = InitializeStyle(EditorStyles.label);
                return _labelStyle;       
            }
        }

        public static void StartBox () {
            GUI.backgroundColor = new Color32(0,0,0,25);
            EditorGUILayout.BeginVertical(GUI.skin.box);
            GUI.backgroundColor = Color.white;            
        }
        

        Ragdoll ragdoll;
                    
        void OnEnable () {
            ragdoll = target as Ragdoll;
        }
                    
        void DrawBuildOptions () {
            EditorGUILayout.Space();

            EditorGUILayout.BeginHorizontal();
            
            SerializedProperty preBuiltProp = serializedObject.FindProperty("preBuilt");
            
            bool isBuilt = preBuiltProp.boolValue;
            
            if (isBuilt) {

                if (GUILayout.Button(isBuilt ? "Clear Ragdoll" : "Pre Build Ragdoll")) {
                    RagdollBuilder.EraseRagdoll(ragdoll.GetComponent<Animator>());
                    preBuiltProp.boolValue = !isBuilt;
                }

            }
            else {

                GUI.enabled = ragdoll.ragdollProfile != null;
                if (GUILayout.Button(isBuilt ? "Clear Ragdoll" : "Pre Build Ragdoll")) {
                    System.Collections.Generic.Dictionary<HumanBodyBones, RagdollTransform> bones;
                    RagdollBuilder.BuildRagdollElements (ragdoll.GetComponent<Animator>(), out _, out bones);
                    RagdollBuilder.BuildBones(ragdoll.GetComponent<Animator>(), ragdoll.ragdollProfile, true, bones, out _);
                    preBuiltProp.boolValue = !isBuilt;
    
                }
                GUI.enabled = true;
            }
            
            

            if (isBuilt) {
                GUI.enabled = ragdoll.ragdollProfile != null;
                // if (ragdoll.ragdollProfile) {

                    if (GUILayout.Button("Update Ragdoll To Profile")) {
                        System.Collections.Generic.Dictionary<HumanBodyBones, RagdollTransform> bones;
                        RagdollBuilder.BuildRagdollElements (ragdoll.GetComponent<Animator>(), out _, out bones);
                        RagdollBuilder.BuildBones(ragdoll.GetComponent<Animator>(), ragdoll.ragdollProfile, false, bones, out _);
                    }
                GUI.enabled = true;
                // }
            }
            EditorGUILayout.EndHorizontal();
        }

        public override void OnInspectorGUI() {
            if (ragdoll.ragdollProfile == null){
                EditorGUILayout.HelpBox("Ragdoll doesnt have a Ragdoll Profile assigned...", MessageType.Error);
                // EditorGUILayout.HelpBox("\nAdd a Ragdoll Profile to adjust ragdoll properties.\n\nOtherwise defualts will be used\n", MessageType.Info);
            }
            
            
            base.OnInspectorGUI();   
               
            DrawBuildOptions();
            serializedObject.ApplyModifiedProperties();
        }
    }
}