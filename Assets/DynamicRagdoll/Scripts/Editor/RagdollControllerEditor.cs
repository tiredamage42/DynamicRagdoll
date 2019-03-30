using UnityEngine;
using UnityEditor;
namespace DynamicRagdoll {
    [CustomEditor(typeof(RagdollController))]
    public class RagdollControllerEditor : Editor {
        public override void OnInspectorGUI() {
            base.OnInspectorGUI();
            RagdollControllerProfile profile = (target as RagdollController).profile;
            if (profile) {
                RagdollControllerProfileEditor.DrawProfile(new SerializedObject( profile ) );
            }
            else {
                EditorGUILayout.HelpBox("Add a Ragdoll Controller Profile", MessageType.Warning);
            }
        }
    }
}