using UnityEngine;
using UnityEditor;
namespace DynamicRagdoll {
    [CustomEditor(typeof(RagdollProfile))]
    public class RagdollProfileEditor : Editor {
        static bool[] showBones = new bool[Ragdoll.physicsBonesCount];
        static bool showRBOptions, showColliderOptions, showJointOptions;

        static void DrawPropertiesBlock(SerializedProperty baseProp, string label, string[] names, ref bool show) {
            show = EditorGUILayout.Foldout(show, "<b>" + label + ":</b>", RagdollEditor.foldoutStyle);
            if (show) {
                EditorGUI.indentLevel++;
                for (int i = 0; i < names.Length; i++) {
                    EditorGUILayout.PropertyField(baseProp.FindPropertyRelative(names[i]));   
                }
                EditorGUI.indentLevel--;   
            }     
        }
        public static void DrawProfile (SerializedObject profile) {
            EditorGUI.BeginChangeCheck();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("<b>Profile Values:</b>", RagdollEditor.labelStyle);

            RagdollEditor.StartBox();
                        
            EditorGUI.indentLevel++;

            RagdollEditor.StartBox();
            
            SerializedProperty boneProfiles = profile.FindProperty("bones");
            for (int i = 0; i < boneProfiles.arraySize; i++) {
                if (i == 3 || i == 7) {
                    EditorGUILayout.EndVertical();
                    EditorGUILayout.Space();
                   
                    RagdollEditor.StartBox();
                }
                
                
                SerializedProperty boneProfile = boneProfiles.GetArrayElementAtIndex(i);
                SerializedProperty bone = boneProfile.FindPropertyRelative("bone");
                
                showBones[i] = EditorGUILayout.Foldout(showBones[i], "<b>" + bone.enumDisplayNames[bone.enumValueIndex] + ":</b>", RagdollEditor.foldoutStyle);
                
                if (showBones[i]) {
                    EditorGUI.indentLevel++;
                    
                    //joints
                    if (i != 0){
                        DrawPropertiesBlock(boneProfile, "Joints", new string[] { "angularXLimit", "angularYLimit", "angularZLimit", "forceOff", "axis1", "axis2" }, ref showJointOptions);   
                    }
                    
                    //rigidbody
                    DrawPropertiesBlock(boneProfile, "Rigidbody", new string[] { "mass", "angularDrag", "drag", "maxAngularVelocity", "interpolation", "collisionDetection", "maxDepenetrationVelocity" }, ref showRBOptions);
                    
                    //collider
                    bool isBox = i < 2;
                    DrawPropertiesBlock(boneProfile, "Collider", isBox ? new string[] { "boxZOffset", "boxZSize", "colliderMaterial" } : new string[] { "colliderRadius", "colliderMaterial" }, ref showColliderOptions);
                    
                    EditorGUI.indentLevel--;                
                }
            }
            EditorGUILayout.EndVertical();


            EditorGUILayout.Space();
            
            EditorGUILayout.PropertyField(profile.FindProperty("headOffset"));
            EditorGUILayout.Space();
            

            EditorGUI.indentLevel--;
            EditorGUILayout.EndVertical();    
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