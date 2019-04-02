using UnityEngine;
using UnityEditor;
namespace DynamicRagdoll {
    [CustomEditor(typeof(RagdollController))]
    public class RagdollControllerEditor : Editor {
        RagdollController controller;
        RagdollControllerProfile profile;
        SerializedObject profileSO;
        
        void OnEnable () {
            controller = target as RagdollController;
            CheckForProfileChange();
        }
        void CheckForProfileChange () {
            if (profile != controller.profile) {
                profile = controller.profile;
                profileSO = profile != null ? new SerializedObject(profile) : null;
            }
        }
        public override void OnInspectorGUI() {
            CheckForProfileChange();
            base.OnInspectorGUI();
            if (!controller.ragdoll) {
                EditorGUILayout.HelpBox("Controller doesnt have a Ragdoll to control...", MessageType.Error);
            }
            else {
                if (profileSO != null) {
                    RagdollControllerProfileEditor.DrawProfile(profileSO);
                }
                else {
                    EditorGUILayout.HelpBox("Controller doesnt have a Ragdoll Controller Profile assigned...", MessageType.Error);
                }
            }
        }
    }
}