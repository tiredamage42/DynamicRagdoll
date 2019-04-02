using UnityEngine;
using UnityEditor;

namespace DynamicRagdoll {

    [CustomEditor(typeof(RagdollControllerProfile))]
    public class RagdollControllerProfileEditor : Editor {
        static bool[] showBones = new bool[Ragdoll.physicsBonesCount];
        static void DrawPropertiesBlock(SerializedProperty baseProp, string[] names) {
            EditorGUI.indentLevel++;
            for (int i = 0; i < names.Length; i++) {
                EditorGUILayout.PropertyField(baseProp.FindPropertyRelative(names[i]));   
            }
            EditorGUI.indentLevel--;        
        }
        static void DrawPropertiesBlock(SerializedObject baseProp, string label, GUIStyle s, string[] names) {
            EditorGUILayout.LabelField("<b>"+label+":</b>", s);
            EditorGUI.indentLevel++;
            for (int i = 0; i < names.Length; i++) {
                EditorGUILayout.PropertyField(baseProp.FindProperty(names[i]));   
            }
            EditorGUI.indentLevel--;        
        }
        public static void DrawProfile (SerializedObject profile) {

            GUIStyle s = new GUIStyle(EditorStyles.label);
            s.richText=true;
            
            GUIStyle fs = new GUIStyle(EditorStyles.foldout);
            fs.richText=true;

            EditorGUILayout.Space();
            
            EditorGUILayout.LabelField("<b>Controller Profile Values:</b>", s);

            GUI.backgroundColor = new Color32(0,0,0,25);
            EditorGUILayout.BeginVertical(GUI.skin.box, GUILayout.MinHeight(0));
            GUI.backgroundColor = Color.white;            


            SerializedProperty usePDControl_prop = profile.FindProperty("usePDControl");
            bool usePDControl = usePDControl_prop.boolValue;
            
            EditorGUI.indentLevel++;

            SerializedProperty boneProfiles = profile.FindProperty("bones");
            
            
            GUI.backgroundColor = new Color32(0,0,0,25);
            EditorGUILayout.BeginVertical(GUI.skin.box);
            GUI.backgroundColor = Color.white;            

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
                    if (usePDControl) {
                        DrawPropertiesBlock(boneProfile, i == 0 ? new string[] { "inputForce", "maxForce" } : new string[] { "inputForce", "maxForce", "maxTorque" });
                    }
                    else {

                        DrawPropertiesBlock(boneProfile, i == 0 ? new string[] { "fallForceDecay" } : new string[] { "fallForceDecay", "fallTorqueDecay" });
                    
                        EditorGUILayout.BeginHorizontal();
                        GUILayout.FlexibleSpace();
                        DrawBoneProfileNeighbors(boneProfile.FindPropertyRelative("neighbors"), (HumanBodyBones)bone.enumValueIndex);
                        EditorGUILayout.EndHorizontal();
                    }
                }
            }
            EditorGUILayout.EndVertical();

            
            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(usePDControl_prop);
            
            EditorGUILayout.Space();

            DrawPropertiesBlock(profile, "Falling", s, 
            
            usePDControl ?
                new string[] { 
                    "PForce", "DForce", 
                    "maxForcePD", "maxTorquePD",
                    "calculateVelocityFrames", 
                    "residualForce", "residualTorque",
                    "skipFrames", "followRigidbodyParents", 
                    "fallDecaySpeed", 
                } 
            : 
                new string[] {
                    "maxTorque", "fallDecaySpeed", "maxGravityAddVelocity"
                }
            );

		    DrawPropertiesBlock(profile, "Get Up", s, new string[] { "ragdollMinTime", "settledSpeed", "orientateDelay", "checkGroundMask", "blendTime" });
                        EditorGUILayout.Space();

            EditorGUILayout.EndVertical();   
            EditorGUI.indentLevel--;


            EditorGUILayout.Space();     
            
            profile.ApplyModifiedProperties();
            EditorUtility.SetDirty(profile.targetObject);
        }
        public override void OnInspectorGUI() {
            //base.OnInspectorGUI();
            DrawProfile(serializedObject);
        }


        static void DrawBoneProfileNeighbors (SerializedProperty neighborsProp, HumanBodyBones baseBone) {
            int neighborsLength = neighborsProp.arraySize;
            
            System.Func<HumanBodyBones, bool> containsBone = (b) => {
                int bi = (int)b;
                for (int i = 0; i < neighborsLength; i++) {
                    if (neighborsProp.GetArrayElementAtIndex(i).enumValueIndex == bi) {
                        return true;
                    }
                }
                return false;
            };
            System.Func<HumanBodyBones, int> indexOf = (b) => {
                int bi = (int)b;
                for (int i = 0; i < neighborsLength; i++) {
                    if (neighborsProp.GetArrayElementAtIndex(i).enumValueIndex == bi) {
                        return i;
                    }
                }
                return -1;
            };

            System.Action<HumanBodyBones> removeBone = (b) => {
                neighborsProp.DeleteArrayElementAtIndex(indexOf(b));
            };


            System.Action<HumanBodyBones> addBone = (b) => {
                neighborsProp.InsertArrayElementAtIndex(neighborsLength);
                neighborsProp.GetArrayElementAtIndex(neighborsLength).enumValueIndex = (int)b;
            };

            if (GUILayout.Button(new GUIContent("Neighbors", "Define which bones count as neighbors for other bones (for the bone decay system)"), EditorStyles.miniButton)) {
                GenericMenu menu = new GenericMenu();

                for (int i = 0; i < Ragdoll.physicsBonesCount; i++) {
                    HumanBodyBones hb = Ragdoll.phsysicsHumanBones[i];
                    if (hb == baseBone) {
                        continue;
                    }


                    menu.AddItem(new GUIContent(hb.ToString()), containsBone(hb), 
                        (b) => {
                        HumanBodyBones hb2 = (HumanBodyBones)b;
                        if (containsBone(hb2)) {
                            removeBone(hb2);
                        }
                        else {
                            addBone(hb2);
                        }

                    }, hb);
                }
                
                // display the menu
                menu.ShowAsContext();
            }







        }



    }
}