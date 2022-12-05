using UnityEngine;
using UnityEditor;
using ModTool.Shared;
using System.Collections.Generic;

namespace ModTool.Editor.Exporting
{
    [CustomEditor(typeof(ExportSettings))]
    public class ExportSettingsEditor : UnityEditor.Editor
    {
        private SerializedProperty _name;
        private SerializedProperty _author;
        private SerializedProperty _description;
        private SerializedProperty _version;
        private SerializedProperty _platforms;
        private SerializedProperty _content;
        private SerializedProperty _compression;
        private SerializedProperty _outputDirectory;

        private SerializedProperty _platform_content_matrixSAVE;
        private SerializedProperty _platform_contentSAVE;

        private FilteredEnumMaskField platforms;
        private FilteredEnumMaskField[] compressions;

        void OnEnable()
        {
            _name = serializedObject.FindProperty("_name");
            _author = serializedObject.FindProperty("_author");
            _description = serializedObject.FindProperty("_description");
            _version = serializedObject.FindProperty("_version");
            _platforms = serializedObject.FindProperty("_platforms");
            _content = serializedObject.FindProperty("_content");
            _compression = serializedObject.FindProperty("_compression");

            _outputDirectory = serializedObject.FindProperty("_outputDirectory");

            _platform_content_matrixSAVE = serializedObject.FindProperty("_platform_content_matrixSAVE");
            _platform_contentSAVE = serializedObject.FindProperty("_platform_contentSAVE");

            for(int i=0; i < platform_content.Length; i++)
            {
                platform_content[i] = (ModContent)_platform_contentSAVE.GetArrayElementAtIndex(i).intValue;
            }

            for (int i = 0; i < platform_content_matrix.Length; i++)
            {
                platform_content_matrix[i] = _platform_content_matrixSAVE.GetArrayElementAtIndex(i).boolValue;
            }

            platforms = new FilteredEnumMaskField(typeof(ModPlatform), (int)ModToolSettings.supportedPlatforms);
            compressions = new FilteredEnumMaskField[5];
            for (int i = 0; i < 5; i++)
            {
                compressions[i] = new FilteredEnumMaskField(typeof(ModCompression), (int)ModToolSettings.supportedCompression[i]);
            }
            
        }

        // Windows = 1, Linux = 2, OSX = 4, Android = 8, iPhone = 16 //Windows - Scene, Asset, Code
        private bool[] platform_content_matrix = new bool[] {
                                                      false, false, false, //Windows - Scene, Asset, Code
                                                      false, false, false, //Linux - Scene, Asset, Code
                                                      false, false, false, //OSX - Scene, Asset, Code
                                                      false, false, false, //Android - Scene, Asset, Code
                                                      false, false, false  //iPhone - Scene, Asset, Code
        };

        private ModContent[] platform_content = new ModContent[] { 0, 0, 0, 0, 0 };
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            GUILayout.Space(5);

            EditorGUILayout.PropertyField(_name, new GUIContent("Mod Name*:"));
            EditorGUILayout.PropertyField(_author, new GUIContent("Author:"));
            EditorGUILayout.PropertyField(_version, new GUIContent("Version:"));

            EditorGUILayout.PropertyField(_description, new GUIContent("Description:"), GUILayout.Height(60));

            GUILayout.Space(5);

            _platforms.intValue = platforms.DoMaskField("Platforms*:", _platforms.intValue);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            //matrix to choose what platform supports what
            EditorGUIUtility.labelWidth = 50f;
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Platform");
            EditorGUILayout.LabelField("Scene");
            EditorGUILayout.LabelField("Asset");
            EditorGUILayout.LabelField("Code");
            EditorGUILayout.LabelField("Compression");
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            //(self & ModPlatform.Windows)

            ModPlatform[] platform_order = new ModPlatform[] { ModPlatform.Windows, ModPlatform.Linux, ModPlatform.OSX, ModPlatform.Android, ModPlatform.iPhone };
            List<string> pretty_names = new List<string>();
            for (int i = 0; i < 5; i++)
            {

                if (((ModPlatform)_platforms.intValue & platform_order[i]) == platform_order[i])
                {
                    int offset = (i * 3);
                    EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                    EditorGUILayout.LabelField(platform_order[i].ToString());


                    bool scene_enabled = (((ModContent)ModToolSettings.supportedContent[i]) & ModContent.Scenes) == ModContent.Scenes;
                    bool assets_enabled = (((ModContent)ModToolSettings.supportedContent[i]) & ModContent.Assets) == ModContent.Assets;
                    bool code_enabled = (((ModContent)ModToolSettings.supportedContent[i]) & ModContent.Code) == ModContent.Code;
                    if (ModToolSettings.settingsLocked[i])
                    {
                        GUI.enabled = false;
                        platform_content_matrix[offset] = scene_enabled;
                        platform_content_matrix[offset + 1] = assets_enabled;
                        platform_content_matrix[offset + 2] = code_enabled;

                        EditorGUILayout.ToggleLeft("", platform_content_matrix[offset]);
                        EditorGUILayout.ToggleLeft("", platform_content_matrix[offset + 1]);
                        EditorGUILayout.ToggleLeft("", platform_content_matrix[offset + 2]);

                        _compression.GetArrayElementAtIndex(i).intValue = EditorGUILayout.EnumPopup("", (ModCompression)ModToolSettings.supportedCompression[i]).FixEnum();
                        GUI.enabled = true;
                    }
                    else
                    {
                        GUI.enabled = scene_enabled;
                        platform_content_matrix[offset] = EditorGUILayout.ToggleLeft("", scene_enabled ? platform_content_matrix[offset] : false);
                        GUI.enabled = true;

                        GUI.enabled = assets_enabled;
                        platform_content_matrix[offset + 1] = EditorGUILayout.ToggleLeft("", assets_enabled ? platform_content_matrix[offset + 1] : false);
                        GUI.enabled = true;

                        GUI.enabled = code_enabled;
                        platform_content_matrix[offset + 2] = EditorGUILayout.ToggleLeft("", code_enabled ? platform_content_matrix[offset + 2] : false);
                        GUI.enabled = true;

                        int prev_compression_value = _compression.GetArrayElementAtIndex(i).intValue;
                        int new_compression_value = compressions[i].DoMaskField("", prev_compression_value);
                        if (new_compression_value != prev_compression_value || prev_compression_value <= 0 || prev_compression_value > (int)ModCompression.LZMA)
                        {
                            new_compression_value &= ~prev_compression_value;
                            if (new_compression_value <= 0 || new_compression_value > (int)ModCompression.LZMA)
                            {
                                if(((ModCompression)ModToolSettings.supportedCompression[i] & ModCompression.LZ4) == ModCompression.LZ4){
                                    new_compression_value = (int)ModCompression.LZ4;
                                }
                                else if (((ModCompression)ModToolSettings.supportedCompression[i] & ModCompression.LZMA) == ModCompression.LZMA)
                                {
                                    new_compression_value = (int)ModCompression.LZMA;
                                }
                                else if (((ModCompression)ModToolSettings.supportedCompression[i] & ModCompression.LZ4) == ModCompression.LZ4)
                                {
                                    new_compression_value = (int)ModCompression.Uncompressed;
                                }
                                else
                                {
                                    new_compression_value = (int)ModCompression.LZ4;
                                }
                            }
                        }

                        _compression.GetArrayElementAtIndex(i).intValue = new_compression_value;
                    }

                    platform_content[i] = 0;
                    platform_content[i] |= platform_content_matrix[offset] ? ModContent.Scenes : 0;
                    platform_content[i] |= platform_content_matrix[offset + 1] ? ModContent.Assets : 0;
                    platform_content[i] |= platform_content_matrix[offset + 2] ? ModContent.Code : 0;

                    if (platform_content[i] > 0)
                    {
                        pretty_names.Add(platform_order[i].ToString());
                    }

                    GUILayout.FlexibleSpace();
                    EditorGUILayout.EndHorizontal();
                }
                else
                {
                    platform_content[i] = 0;
                }
                _content.GetArrayElementAtIndex(i).intValue = platform_content[i].FixEnum();
            }

            for (int i = 0; i < platform_content.Length; i++)
            {
                _platform_contentSAVE.GetArrayElementAtIndex(i).intValue = (platform_content[i]).FixEnum();
            }

            for (int i = 0; i < platform_content_matrix.Length; i++)
            {
                _platform_content_matrixSAVE.GetArrayElementAtIndex(i).boolValue = platform_content_matrix[i];
            }



            if (pretty_names.Count <= 0)
            {
                pretty_names.Add("None");
            }

            EditorGUIUtility.labelWidth = 0f;
            //end of matrix

            EditorGUILayout.LabelField("Platforms: " + string.Join(", ", pretty_names));

            ModToolSettings.logLevel = (LogLevel)EditorGUILayout.EnumPopup("Log Level:", ModToolSettings.logLevel);

            EditorGUILayout.EndVertical();

            bool enabled = GUI.enabled;

            GUILayout.BeginHorizontal();

            GUI.enabled = false;

            EditorGUILayout.TextField("Output Directory*:", GetShortString(_outputDirectory.stringValue));

            GUI.enabled = enabled;

            if (GUILayout.Button("...", GUILayout.Width(30)))
            {
                string selectedDirectory = EditorUtility.SaveFolderPanel("Choose output directory", _outputDirectory.stringValue, "");
                if (!string.IsNullOrEmpty(selectedDirectory))
                    _outputDirectory.stringValue = selectedDirectory;

                Repaint();
            }

            GUILayout.EndHorizontal();

            GUILayout.FlexibleSpace();

            EditorGUILayout.EndVertical();

            serializedObject.ApplyModifiedProperties();
        }

        private string GetShortString(string str)
        {
            int maxWidth = (int)EditorGUIUtility.currentViewWidth - 252;
            int cutoffIndex = Mathf.Max(0, str.Length - 7 - (maxWidth / 7));
            string shortString = str.Substring(cutoffIndex);
            if (cutoffIndex > 0)
                shortString = "..." + shortString;
            return shortString;
        }
    }
}