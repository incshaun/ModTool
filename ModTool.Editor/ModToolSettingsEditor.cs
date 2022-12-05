using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using ModTool.Shared;

namespace ModTool.Editor
{
    [CustomEditor(typeof(ModToolSettings))]
    public class ModToolSettingsEditor : UnityEditor.Editor
    {
        private SerializedProperty _supportedPlatforms;

        private SerializedProperty _supportedContent;
        private SerializedProperty _supportedCompression;
        private SerializedProperty _settingsLocked;

        private SerializedProperty _logLevel;

        void OnEnable()
        {
            _supportedPlatforms = serializedObject.FindProperty("_supportedPlatforms");

            _supportedContent = serializedObject.FindProperty("_supportedContent");
            _settingsLocked = serializedObject.FindProperty("_settingsLocked");

            _supportedCompression = serializedObject.FindProperty("_supportedCompression");


            for (int i = 0; i < 5; i++)
            {
                int offset = (i * 3);
                platform_content_matrix[offset] = ((((ModContent)_supportedContent.GetArrayElementAtIndex(i).intValue) & ModContent.Scenes) == ModContent.Scenes);
                platform_content_matrix[offset + 1] = ((((ModContent)_supportedContent.GetArrayElementAtIndex(i).intValue) & ModContent.Assets) == ModContent.Assets);
                platform_content_matrix[offset + 2] = ((((ModContent)_supportedContent.GetArrayElementAtIndex(i).intValue) & ModContent.Code) == ModContent.Code);
            }

            _logLevel = serializedObject.FindProperty("_logLevel");
        }

        // Windows = 1, Linux = 2, OSX = 4, Android = 8, iPhone = 16 //Windows - Scene, Asset, Code
        bool[] platform_content_matrix = new bool[] {
                                                      false, false, false, //Windows - Scene, Asset, Code
                                                      false, false, false, //Linux - Scene, Asset, Code
                                                      false, false, false, //OSX - Scene, Asset, Code
                                                      false, false, false, //Android - Scene, Asset, Code
                                                      false, false, false  //iPhone - Scene, Asset, Code
        };

        ModContent[] platform_and_content = new ModContent[] { 0, 0, 0, 0, 0};

        ModPlatform[] selectable_platforms = new ModPlatform[] { ModPlatform.Windows, ModPlatform.Linux, ModPlatform.OSX, ModPlatform.Android, ModPlatform.iPhone };

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            //matrix to choose what platform supports what
            EditorGUIUtility.labelWidth = 50f;
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Platform");
            EditorGUILayout.LabelField("Allow Scene");
            EditorGUILayout.LabelField("Allow Asset");
            EditorGUILayout.LabelField("Allow Code");
            EditorGUILayout.LabelField("Compression");
            EditorGUILayout.LabelField("Config Locked");
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            //(self & ModPlatform.Windows)

            ModPlatform supportedPlatforms = 0;
            List<string> pretty_names = new List<string>();
            for (int i = 0; i < 5; i++)
            {
                int offset = (i * 3);
                EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                EditorGUILayout.LabelField(selectable_platforms[i].ToString());

                platform_content_matrix[offset] = EditorGUILayout.ToggleLeft("", platform_content_matrix[offset]);
                platform_content_matrix[offset + 1] = EditorGUILayout.ToggleLeft("", platform_content_matrix[offset + 1]);
                platform_content_matrix[offset + 2] = EditorGUILayout.ToggleLeft("", platform_content_matrix[offset + 2]);

                platform_and_content[i] = 0;
                platform_and_content[i] |= platform_content_matrix[offset] ? ModContent.Scenes : 0;
                platform_and_content[i] |= platform_content_matrix[offset + 1] ? ModContent.Assets : 0;
                platform_and_content[i] |= platform_content_matrix[offset + 2] ? ModContent.Code : 0;

                //set the compression type for each platform
                _supportedContent.GetArrayElementAtIndex(i).intValue = platform_and_content[i].FixEnum();

                bool platform_available = platform_and_content[i] > 0;

                if (platform_available)
                {
                    supportedPlatforms |= selectable_platforms[i];
                    pretty_names.Add(selectable_platforms[i].ToString());
                }

                GUI.enabled = platform_available;
                //set the compression type for each platform

                if (_settingsLocked.GetArrayElementAtIndex(i).boolValue)
                {
                    ModCompression only_one = ((ModCompression)_supportedCompression.GetArrayElementAtIndex(i).intValue);
                    if (!(only_one == ModCompression.Uncompressed || only_one == ModCompression.LZ4 || only_one == ModCompression.LZMA))
                    {
                        _supportedCompression.GetArrayElementAtIndex(i).intValue = ModCompression.LZ4.FixEnum();
                    }
                    _supportedCompression.GetArrayElementAtIndex(i).intValue = EditorGUILayout.EnumPopup("", platform_available ? (ModCompression)((int)Mathf.Max(1, _supportedCompression.GetArrayElementAtIndex(i).intValue)) : 0).FixEnum(); //choose one
                }
                else
                {
                    int new_value = EditorGUILayout.EnumFlagsField("", (ModCompression)_supportedCompression.GetArrayElementAtIndex(i).intValue).FixEnum();
                    if(new_value <= 0)
                    {
                        new_value = 2;
                    }
                    _supportedCompression.GetArrayElementAtIndex(i).intValue = new_value;
                }

                _settingsLocked.GetArrayElementAtIndex(i).boolValue = EditorGUILayout.ToggleLeft("", platform_available ? _settingsLocked.GetArrayElementAtIndex(i).boolValue : false);

                
                GUI.enabled = true;


                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();
            }

            if (pretty_names.Count <= 0)
            {
                pretty_names.Add("None");
            }

            EditorGUIUtility.labelWidth = 0f;
            //end of matrix

            EditorGUILayout.LabelField("Platforms: " + string.Join(", ", pretty_names));

            LogLevel logLevel = (LogLevel)EditorGUILayout.EnumPopup("Log Level", (LogLevel)_logLevel.intValue);

            _supportedPlatforms.intValue = supportedPlatforms.FixEnum();

            _logLevel.intValue = (int)logLevel;

            EditorGUILayout.EndVertical();
            
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            using (new GUILayout.HorizontalScope())
            {
                GUILayout.Label(ModToolSettings.sharedAssets.Count + " Shared Assets");
                GUILayout.FlexibleSpace();

                if (GUILayout.Button("edit"))
                    AssetSelector.Open();
            }

            GUILayout.Space(2);

            EditorGUILayout.EndVertical();

            GUILayout.BeginVertical(EditorStyles.helpBox);

            using (new GUILayout.HorizontalScope())
            {
                GUILayout.Label(ModToolSettings.sharedPackages.Count + " Shared Packages");
                GUILayout.FlexibleSpace();

                if (GUILayout.Button("edit"))
                    PackageSelector.Open();
            }

            GUILayout.EndVertical();

            serializedObject.ApplyModifiedProperties();
        }        
    }
}
