using System.Collections.Generic;
using System.Reflection;
using System.IO;
using UnityEngine;
using UnityEditor;
using ModTool.Shared;

namespace ModTool.Editor
{
    internal class ExporterCreator
    {
        /// <summary>
        /// Create a mod exporter package for this game.
        /// </summary>
        [MenuItem("Tools/ModTool/Create Exporter", priority = 1)]
        public static void CreateExporter()
        {
            CreateExporter(Directory.GetCurrentDirectory(), true);
        }

        /// <summary>
        /// Create a mod exporter package after building the game.
        /// </summary>
        [UnityEditor.Callbacks.PostProcessBuild]
        public static void CreateExporterPostBuild(BuildTarget target, string pathToBuiltProject)
        {
            pathToBuiltProject = Path.GetDirectoryName(pathToBuiltProject);

            CreateExporter(pathToBuiltProject);
        }
        
        private static void CreateExporter(string path, bool revealPackage = false)
        {
            LogUtility.LogInfo("Creating Exporter");

            UpdateSettings();

            ModToolSettings modToolSettings = ModToolSettings.instance;
            CodeSettings codeSettings = CodeSettings.instance;

            string modToolDirectory = GetModToolDirectory();
//             string exporterPath = Path.Combine(modToolDirectory, Path.Combine("Editor", "ModTool.Editor.Exporting.dll"));
            string fileName = Path.Combine(path, Application.productName + " Mod Tools.unitypackage");
            string projectSettingsDirectory = "ProjectSettings";

            List<string> assetPaths = new List<string>
            {
                AssetDatabase.GetAssetPath(modToolSettings),
                AssetDatabase.GetAssetPath(codeSettings),
                
//                 Path.Combine(modToolDirectory, Path.Combine("Editor", "ModTool.Editor.Exporting.dll")),
                Path.Combine(modToolDirectory, Path.Combine("Editor", Path.Combine("ModTool.Editor.Exporting", "Asset.cs"))),
                Path.Combine(modToolDirectory, Path.Combine("Editor", Path.Combine("ModTool.Editor.Exporting", "EditorModPlatformExtensions.cs"))),
                Path.Combine(modToolDirectory, Path.Combine("Editor", Path.Combine("ModTool.Editor.Exporting", "ExportData.cs"))),
                Path.Combine(modToolDirectory, Path.Combine("Editor", Path.Combine("ModTool.Editor.Exporting", "ExporterEditorWindow.cs"))),
                Path.Combine(modToolDirectory, Path.Combine("Editor", Path.Combine("ModTool.Editor.Exporting", "ExportSettings.cs"))),
                Path.Combine(modToolDirectory, Path.Combine("Editor", Path.Combine("ModTool.Editor.Exporting", "ExportSettingsEditor.cs"))),
                Path.Combine(modToolDirectory, Path.Combine("Editor", Path.Combine("ModTool.Editor.Exporting", "ExportStep.cs"))),
                Path.Combine(modToolDirectory, Path.Combine("Editor", Path.Combine("ModTool.Editor.Exporting", "FilteredEnumMaskField.cs"))),
                Path.Combine(modToolDirectory, Path.Combine("Editor", Path.Combine("ModTool.Editor.Exporting", "ModExporter.cs"))),
                Path.Combine(modToolDirectory, Path.Combine("Editor", Path.Combine("ModTool.Editor.Exporting", "PackageInstaller.cs"))),
//                 Path.Combine(modToolDirectory, "ModTool.Shared.dll"),
//                 Path.Combine(modToolDirectory, "ModTool.Shared.xml"),
                Path.Combine(modToolDirectory, Path.Combine("ModTool.Shared", "AssemblyResolver.cs")),
                Path.Combine(modToolDirectory, Path.Combine("ModTool.Shared", "AssemblyUtility.cs")),
                Path.Combine(modToolDirectory, Path.Combine("ModTool.Shared", "AssemblyVerifier.cs")),
                Path.Combine(modToolDirectory, Path.Combine("ModTool.Shared", "CodeSettings.cs")),
                Path.Combine(modToolDirectory, Path.Combine("ModTool.Shared", "Extensions.cs")),
                Path.Combine(modToolDirectory, Path.Combine("ModTool.Shared", "LogUtility.cs")),
                Path.Combine(modToolDirectory, Path.Combine("ModTool.Shared", "ModCompression.cs")),
                Path.Combine(modToolDirectory, Path.Combine("ModTool.Shared", "ModContent.cs")),
                Path.Combine(modToolDirectory, Path.Combine("ModTool.Shared", "ModInfo.cs")),
                Path.Combine(modToolDirectory, Path.Combine("ModTool.Shared", "ModPlatform.cs")),
                Path.Combine(modToolDirectory, Path.Combine("ModTool.Shared", "ModToolSettings.cs")),
                Path.Combine(modToolDirectory, Path.Combine("ModTool.Shared", "Restriction.cs")),
                Path.Combine(modToolDirectory, Path.Combine("ModTool.Shared", "Singleton.cs")),
                Path.Combine(modToolDirectory, Path.Combine("ModTool.Shared", "ModTool.Shared.asmdef")),

//                 Path.Combine(modToolDirectory, "ModTool.Interface.dll"),
//                 Path.Combine(modToolDirectory, "ModTool.Interface.xml"),
                Path.Combine(modToolDirectory, Path.Combine("ModTool.Interface", "IModHandler.cs")),

                Path.Combine(modToolDirectory, Path.Combine("Mono.Cecil", "ModTool.Cecil.dll")),
                Path.Combine(modToolDirectory, Path.Combine("Mono.Cecil", "LICENSE.txt")),
                Path.Combine(projectSettingsDirectory, "EditorBuildSettings.asset"),
                Path.Combine(projectSettingsDirectory, "InputManager.asset"),
                Path.Combine(projectSettingsDirectory, "TagManager.asset"),
                Path.Combine(projectSettingsDirectory, "Physics2DSettings.asset"),
                Path.Combine(projectSettingsDirectory, "DynamicsManager.asset"),
            };

            assetPaths.AddRange(ModToolSettings.sharedAssets);

//             SetPluginEnabled(exporterPath, true);

            AssetDatabase.ExportPackage(assetPaths.ToArray(), fileName);
                       
//             SetPluginEnabled(exporterPath, false);

            if(revealPackage)
                EditorUtility.RevealInFinder(fileName);
        }

        private static void SetPluginEnabled(string pluginPath, bool enabled)
        {
            PluginImporter pluginImporter = AssetImporter.GetAtPath(pluginPath) as PluginImporter;

            if (pluginImporter.GetCompatibleWithEditor() == enabled)
                return;

            pluginImporter.SetCompatibleWithEditor(enabled);
            pluginImporter.SaveAndReimport();
        }
               
        private static void UpdateSettings()
        {
            if (string.IsNullOrEmpty(ModToolSettings.productName) || ModToolSettings.productName != Application.productName)
                typeof(ModToolSettings).GetField("_productName", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(ModToolSettings.instance, Application.productName);

            if (string.IsNullOrEmpty(ModToolSettings.unityVersion) || ModToolSettings.unityVersion != Application.unityVersion)            
                typeof(ModToolSettings).GetField("_unityVersion", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(ModToolSettings.instance, Application.unityVersion);

            EditorUtility.SetDirty(ModToolSettings.instance);
        }

        private static string GetModToolDirectory()
        {
            return ModInfo.modToolDirectory ();
//             string modToolDirectory = Path.GetDirectoryName(typeof(ModInfo).Assembly.Location);
//             
//             return modToolDirectory.Substring(Application.dataPath.Length - 6);
        }
    }
}
