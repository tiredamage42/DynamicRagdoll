using UnityEngine;
using UnityEditor;
namespace DynamicRagdoll {
    [CustomEditor(typeof(RagdollProfile))]
    public class RagdollProfileEditor : Editor {
        static bool[] showBones = new bool[Ragdoll.physicsBonesCount];
        static bool showRBOptions, showColliderOptions, showJointOptions;

        static void DrawPropertiesBlock(SerializedProperty baseProp, string label, GUIStyle s, string[] names, ref bool show) {
            show = EditorGUILayout.Foldout(show, "<b>" + label + ":</b>", s);
            if (show) {
                EditorGUI.indentLevel++;
                for (int i = 0; i < names.Length; i++) {
                    EditorGUILayout.PropertyField(baseProp.FindPropertyRelative(names[i]));   
                }
                EditorGUI.indentLevel--;   
            }     
        }
        public static void DrawProfile (SerializedObject profile) {
            GUIStyle s = new GUIStyle(EditorStyles.label);
            s.richText=true;
            GUIStyle fs = new GUIStyle(EditorStyles.foldout);
            fs.richText=true;


            EditorGUILayout.Space();
            
            
            EditorGUILayout.LabelField("<b>Profile Values:</b>", s);

            GUI.backgroundColor = new Color32(0,0,0,25);
            EditorGUILayout.BeginVertical(GUI.skin.box, GUILayout.MinHeight(0));
            GUI.backgroundColor = Color.white;            
            
                        
            EditorGUI.indentLevel++;

            GUI.backgroundColor = new Color32(0,0,0,25);
            EditorGUILayout.BeginVertical(GUI.skin.box);
            GUI.backgroundColor = Color.white;            

            
            SerializedProperty boneProfiles = profile.FindProperty("bones");
            for (int i = 0; i < boneProfiles.arraySize; i++) {
                if (i == 3 || i == 7) {
                    EditorGUILayout.EndVertical();
                    EditorGUILayout.Space();
            
                    GUI.backgroundColor = new Color32(0,0,0,25);
                    EditorGUILayout.BeginVertical(GUI.skin.box);
                    GUI.backgroundColor = Color.white;            
                }
                
                
                SerializedProperty boneProfile = boneProfiles.GetArrayElementAtIndex(i);
                SerializedProperty bone = boneProfile.FindPropertyRelative("bone");
                
                showBones[i] = EditorGUILayout.Foldout(showBones[i], "<b>" + bone.enumDisplayNames[bone.enumValueIndex] + ":</b>", fs);
                
                if (showBones[i]) {
                    EditorGUI.indentLevel++;
                    
                    //joints
                    if (i != 0){
                        DrawPropertiesBlock(boneProfile, "Joints", fs, new string[] { "angularXLimit", "angularYLimit", "angularZLimit", "forceOff", "axis1", "axis2" }, ref showJointOptions);   
                    }
                    
                    //rigidbody
                    DrawPropertiesBlock(boneProfile, "Rigidbody", fs, new string[] { "mass", "angularDrag", "drag", "maxAngularVelocity", "interpolation", "collisionDetection", "maxDepenetrationVelocity" }, ref showRBOptions);
                    
                    //collider
                    bool isBox = i < 2;
                    DrawPropertiesBlock(boneProfile, "Collider", fs, isBox ? new string[] { "boxZOffset", "boxZSize", "colliderMaterial" } : new string[] { "colliderRadius", "colliderMaterial" }, ref showColliderOptions);
                    
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
                

            profile.ApplyModifiedProperties();
            EditorUtility.SetDirty(profile.targetObject);
        }
        public override void OnInspectorGUI() {
            //base.OnInspectorGUI();
            DrawProfile(serializedObject);
        }
    }
}