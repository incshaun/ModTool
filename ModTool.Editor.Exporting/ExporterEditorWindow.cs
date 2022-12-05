using UnityEditor;
using UnityEngine;

namespace ModTool.Editor.Exporting
{
    internal class ExporterEditorWindow : EditorWindow
    {
        private UnityEditor.Editor exportSettingsEditor;

        [MenuItem("Tools/ModTool/Export Mod")]
        public static void ShowWindow()
        {
            ExporterEditorWindow window = GetWindow<ExporterEditorWindow>();
            window.minSize = new Vector2(400f, 300f);
            window.titleContent = new GUIContent("Mod Exporter");
        }

        void OnEnable()
        {
            ExportSettings exportSettings = ExportSettings.instance;

            exportSettingsEditor = UnityEditor.Editor.CreateEditor(exportSettings);
        }

        void OnDisable()
        {
            DestroyImmediate(exportSettingsEditor);
        }

        void OnGUI()
        {
            GUI.enabled = !EditorApplication.isCompiling && !ModExporter.isExporting && !Application.isPlaying;

            exportSettingsEditor.OnInspectorGUI();

            bool buttonPressed = GUILayout.Button("Export Mod", GUILayout.Height(30));

            if (buttonPressed)
                ModExporter.ExportMod();
        }
    }
}
