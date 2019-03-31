using UnityEngine;
using UnityEditor;
namespace DynamicRagdoll {
    [CustomEditor(typeof(RagdollController))]
    public class RagdollControllerEditor : Editor {
        RagdollController controller;
        void OnEnable () {
            controller = target as RagdollController;
        }
        public override void OnInspectorGUI() {
            base.OnInspectorGUI();
            if (!controller.ragdoll) {
                EditorGUILayout.HelpBox("Controller doesnt have a Ragdoll to control...", MessageType.Error);
            }
            else {
                if (controller.profile) {
                    RagdollControllerProfileEditor.DrawProfile(new SerializedObject( controller.profile ) );
                }
                else {
                    EditorGUILayout.HelpBox("Controller doesnt have a Ragdoll Controller Profile assigned...", MessageType.Error);
                }
            }
        }
    }
}