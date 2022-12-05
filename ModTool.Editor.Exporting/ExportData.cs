using System;
using System.Collections.Generic;
using ModTool.Shared;

namespace ModTool.Editor.Exporting
{
    /// <summary>
    /// Class that stores data during the exporting process.
    /// </summary>
    [Serializable]
    public class ExportData
    {
        //This would be better with a dictionary BUT it's an unserialized type by unity so it gets dropped at some point during the build process and you end up losing all data during a step
        public List<ModPlatform> export_platforms = new List<ModPlatform>();

        public List<ModContent> export_content = new List<ModContent>();

        public List<ModCompression> export_compression = new List<ModCompression>();

        public List<Asset> assemblyDefinitions = new List<Asset>();

        public List<Asset> assemblies = new List<Asset>();

        public List<Asset> assets = new List<Asset>();

        public List<Asset> scenes = new List<Asset>();


        public string loadedScene;
    }
}
