using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
using UnityEditor.Compilation;

using ModTool.Shared;
using ModTool.Shared.Verification;

using Mono.Cecil;
using Mono.Cecil.Cil;

namespace ModTool.Editor.Exporting
{
    public abstract class ExportStep
    {
        protected static readonly string assetsDirectory = "Assets";
        protected static readonly string assemblyDirectory = Path.Combine("Library", "ScriptAssemblies");
        protected static readonly string tempModDirectory = Path.Combine("Temp", "ModDirectory");

        private static readonly string dllPath = typeof(ModInfo).Assembly.Location;

        public bool waitForAssemblyReload { get; private set; }

        public abstract string message { get; }

        internal abstract void Execute(ExportData data);

        protected void ForceAssemblyReload()
        {
            waitForAssemblyReload = true;
            AssetDatabase.ImportAsset(dllPath, ImportAssetOptions.ForceUpdate);
        }
    }

    public class StartExport : ExportStep
    {
        public override string message => "Starting Export";

        internal override void Execute(ExportData data)
        {
            data.loadedScene = SceneManager.GetActiveScene().path;

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                throw new Exception("Cancelled by user");

            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            if (Directory.Exists(tempModDirectory))
                Directory.Delete(tempModDirectory, true);

            Directory.CreateDirectory(tempModDirectory);
        }
    }

    public class Verify : ExportStep
    {
        public override string message => "Verifying Project";

        internal override void Execute(ExportData data)
        {
            CheckSerializationMode();
            VerifyProject();
            VerifySettings();
            VerifyAssemblies();
        }

        private void CheckSerializationMode()
        {
            if (EditorSettings.serializationMode != SerializationMode.ForceText)
            {
                LogUtility.LogInfo("Changed serialization mode from " + EditorSettings.serializationMode + " to Force Text");
                EditorSettings.serializationMode = SerializationMode.ForceText;
            }
        }

        private void VerifyProject()
        {
            if (!string.IsNullOrEmpty(ModToolSettings.unityVersion) && Application.unityVersion != ModToolSettings.unityVersion)
                throw new Exception("Mods for " + ModToolSettings.productName + " can only be exported with Unity " + ModToolSettings.unityVersion);

            if (Application.isPlaying)
                throw new Exception("Unable to export mod in play mode");

            if (!VerifyAssemblies())
                throw new Exception("Incompatible scripts or assemblies found");
        }

        private void VerifySettings()
        {
            if (string.IsNullOrEmpty(ExportSettings.name))
                throw new Exception("Mod has no name");

            if (string.IsNullOrEmpty(ExportSettings.outputDirectory))
                throw new Exception("No output directory set");

            if (!Directory.Exists(ExportSettings.outputDirectory))
                throw new Exception("Output directory " + ExportSettings.outputDirectory + " does not exist");

            if (ExportSettings.platforms == 0)
                throw new Exception("No platforms selected");
        }

        [MenuItem("Tools/ModTool/Verify")]
        public static void VerifyScriptsMenuItem()
        {
            if (VerifyAssemblies())
                LogUtility.LogInfo("Scripts Verified!");
            else
                LogUtility.LogWarning("Scripts Not verified!");
        }

        private static bool VerifyAssemblies()
        {
            List<string> assemblies = new List<string>();
            
            AssemblyUtility.GetAssemblies(assemblies, assetsDirectory);
            AssemblyUtility.GetAssemblies(assemblies, assemblyDirectory, ShouldVerify);
                       
            List<string> messages = new List<string>();

            using (var assemblyVerifier = new AssemblyVerifier())
                assemblyVerifier.VerifyAssemblies(assemblies, messages);

            foreach (var message in messages)
                LogUtility.LogWarning(message);

            if (messages.Count > 0)
                return false;

            return true;
        }

        private static bool ShouldVerify(string assembly)
        {
            string assemblyName = Path.GetFileNameWithoutExtension(assembly);

            if (assemblyName == "Assembly-CSharp")
                return true;

            string assemblyDefinition = CompilationPipeline.GetAssemblyDefinitionFilePathFromAssemblyName(assemblyName);

            if (assemblyDefinition == null)
                return false;

            if (assemblyDefinition.StartsWith("Packages") || assemblyDefinition.Contains("Editor"))
                return false;

            return true;            
        }        
    }

    public class SetupDataSettings : ExportStep
    {
        public override string message => "ExportData settings setup";

        internal override void Execute(ExportData data)
        {
            data.export_platforms.Clear();
            data.export_content.Clear();
            data.export_compression.Clear();

            ModPlatform[] all_platforms = new ModPlatform[] { ModPlatform.Windows, ModPlatform.Linux, ModPlatform.OSX, ModPlatform.Android, ModPlatform.iPhone };

            bool has_all_valid_platforms = true;
            
            for(int i=0; i < all_platforms.Length; i++) {
                ModPlatform p = all_platforms[i];
                if ((ExportSettings.platforms & p) == p)
                {
                    data.export_platforms.Add(p);
                    data.export_content.Add((ModContent)ExportSettings.content[i]);

                    if(data.export_content[data.export_content.Count - 1] == 0)
                    {
                        has_all_valid_platforms = false;
                    }

                    data.export_compression.Add((ModCompression)ExportSettings.compression[i]);
                }
            }

            if(data.export_platforms.Count <= 0)
                throw new Exception("You have no platforms selected");

            if (has_all_valid_platforms == false)
                throw new Exception("You have platforms with no content");

        }
    }

    public class CreateAssemblies : ExportStep
    {
        public override string message => "Creating Assemblies";
        public List<ModPlatform> platforms_with_assemblies = new List<ModPlatform>();
        internal override void Execute(ExportData data)
        {
            platforms_with_assemblies.Clear();
            for (int i = 0; i < data.export_platforms.Count; i++)
            {
                if ((data.export_content[i] & ModContent.Code) == ModContent.Code)
                {
                    platforms_with_assemblies.Add(data.export_platforms[i]);
                }
            }

            if (platforms_with_assemblies.Count > 0)
            {
                //Note: already a top-level Assembly Definition present
                if (Directory.GetFiles("Assets", "*.asmdef").Length > 0)
                    return;

                //Note: no mod scripts exist without associated Assembly Definition
                if (!File.Exists(Path.Combine(assemblyDirectory, "Assembly-CSharp.dll")))
                    return;

                CreateScriptAssembly(data);
                CreateEditorAssemblies(data);

                ForceAssemblyReload();
            }
        }

        private void CreateScriptAssembly(ExportData data)
        {
            string name = ExportSettings.name.Replace(" ", "");
            string path = Path.Combine("Assets", name + ".asmdef");

            var references = GetReferences();

            AssemblyData assemblyData = new AssemblyData()
            {
                name = name,
                references = references.ToArray()
            };

            CreateAssemblyDefinition(path, assemblyData);

            data.assemblyDefinitions.Add(new Asset(path));
        }

        private void CreateEditorAssemblies(ExportData data)
        {
            var editorFolders = Directory.GetDirectories("Assets", "Editor", SearchOption.AllDirectories);

            var references = GetReferences();

            foreach (var editorFolder in editorFolders)
            {
                if (!HasScripts(editorFolder) || HasAssemblyDefinition(editorFolder))
                    continue;

                string name = editorFolder.Replace(Path.DirectorySeparatorChar, '-');
                name = name.Replace(" ", "");

                string path = Path.Combine(editorFolder, name + ".asmdef");

                AssemblyData assemblyData = new AssemblyData()
                {
                    name = name,
                    references = references.ToArray(),
                    includePlatforms = new string[] {"Editor"}
                };

                CreateAssemblyDefinition(path, assemblyData);

                data.assemblyDefinitions.Add(new Asset(path));
            }
        }

        private List<string> GetReferences()
        {
            var assemblies = CompilationPipeline.GetAssemblies();

            var references = new List<string>();

            foreach (var assembly in assemblies)
            {
                if (assembly.flags == AssemblyFlags.EditorAssembly)
                    continue;

                if (assembly.name == "Assembly-CSharp")
                    continue;

                references.Add(assembly.name);
            }

            return references;
        }

        private void CreateAssemblyDefinition(string path, AssemblyData data)
        {
            string json = JsonUtility.ToJson(data, true);

            File.WriteAllText(path, json);
            AssetDatabase.ImportAsset(path);
        }

        private bool HasScripts(string path)
        {
            return Directory.GetFiles(path, "*.cs", SearchOption.AllDirectories).Length > 0;
        }

        private bool HasAssemblyDefinition(string path)
        {
            return Directory.GetFiles(path, "*.asmdef").Length > 0;
        }

        [Serializable]
        private class AssemblyData
        {
            public string name;
            public string[] references;
            public string[] includePlatforms;
            public string[] excludePlatforms;
        }
    }

    public class GetContent : ExportStep
    {
        public override string message => "Finding Content";

        internal override void Execute(ExportData data)
        {
            data.assemblies = GetAssemblies();
            data.assets = GetAssets("t:prefab t:scriptableobject");
            data.scenes = GetAssets("t:scene");


            //TODO: add other asset types like TextAsset?
            for(int i=0; i < data.export_platforms.Count; i++)
            {
                ModContent content = data.export_content[i];
                if (data.assets.Count == 0)
                {
                    content &= ~ModContent.Assets;
                }
                if (data.scenes.Count == 0)
                {
                    content &= ~ModContent.Scenes;
                }
                if (data.assemblies.Count == 0) {
                    content &= ~ModContent.Code;
                }
                data.export_content[i] = content;
            }
        }

        private List<Asset> GetAssets(string filter)
        {
            List<Asset> assets = new List<Asset>();

            string[] guids = AssetDatabase.FindAssets(filter);


            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);

                if (path.Contains("/ModTool/") || path.Contains("/Editor/"))
                    continue;

                if (path.StartsWith("Packages"))
                    continue;

                if (ModToolSettings.sharedAssets.Contains(path))
                    continue;

                //NOTE: AssetDatabase.FindAssets() can contain duplicates for some reason
                if (assets.Exists(a => a.assetPath == path))
                    continue;

                assets.Add(new Asset(path));
            }

            return assets;
        }

        private List<Asset> GetAssemblies()
        {
            List<Asset> assemblies = new List<Asset>();

            foreach (string path in AssemblyUtility.GetAssemblies(assetsDirectory))
                assemblies.Add(new Asset(path));

            foreach (string path in AssemblyUtility.GetAssemblies(assemblyDirectory, IsModAssembly))
                assemblies.Add(new Asset(path));
            
            return assemblies;
        }

        private bool IsModAssembly(string assembly)
        {
            string name = Path.GetFileNameWithoutExtension(assembly);
            string assemblyDefinition = CompilationPipeline.GetAssemblyDefinitionFilePathFromAssemblyName(name);

            if (assemblyDefinition == null)
                return false;

            if (assemblyDefinition.StartsWith("Packages"))
                return false;

            return true;
        }
    }

    public class CreateBackup : ExportStep
    {
        public override string message => "Creating Backup";

        internal override void Execute(ExportData data)
        {
            AssetDatabase.SaveAssets();

            if (Directory.Exists(Asset.backupDirectory))
                Directory.Delete(Asset.backupDirectory, true);

            Directory.CreateDirectory(Asset.backupDirectory);

            foreach (Asset asset in data.assets)
                asset.Backup();

            foreach (Asset scene in data.scenes)
                scene.Backup();
        }
    }

    public class UpdateAssemblies : ExportStep
    {
        public override string message => "Updating Assemblies";

        private AssemblyNameDefinition modTool;
        private TypeReference objectManager;
        private TypeReference str;

        private string modName;

        internal override void Execute(ExportData data)
        {
            //Note: replaces all method calls that create new Objects to version that keeps track of Objects.

            modName = ExportSettings.name;

            CreateReferences();

            var assemblyResolver = new AssemblyResolver();

            foreach (var assembly in data.assemblies)
            {
                var assemblyDefinition = AssemblyDefinition.ReadAssembly(assembly.assetPath, new ReaderParameters { InMemory = true, AssemblyResolver = assemblyResolver });

                UpdateModule(assemblyDefinition.MainModule);

                string path = Path.Combine(tempModDirectory, Path.GetFileName(assembly.assetPath));
                assemblyDefinition.Write(path);
            }
        }
        
        private void UpdateModule(ModuleDefinition module)
        {
            ImportReferences(module);
            
            foreach(var type in module.Types)            
                UpdateType(type);            
        }

        private void UpdateType(TypeDefinition type)
        {
            foreach (var nested in type.NestedTypes)
                UpdateType(nested);

            foreach (var method in type.Methods)
                UpdateMethod(method);
        }

        private void UpdateMethod(MethodDefinition method)
        {
            if (!method.HasBody)
                return;

            var updated = new List<Instruction>();

            foreach(var instruction in method.Body.Instructions)
            {
                if (instruction.OpCode.Code != Code.Call && instruction.OpCode.Code != Code.Newobj)
                    continue;

                var methodReference = instruction.Operand as MethodReference;

                if(methodReference.DeclaringType.Name == "GameObject" && methodReference.Name == ".ctor")
                {
                    instruction.OpCode = OpCodes.Call;
                    instruction.Operand = UpdateCtor(methodReference);
                }

                if(methodReference.DeclaringType.Name == "Object" && methodReference.Name == "Instantiate")
                        instruction.Operand = UpdateMethodReference(methodReference);
                
                if(instruction.Operand != methodReference)
                    updated.Add(instruction);
            }

            var iLProcessor = method.Body.GetILProcessor();

            foreach (var instruction in updated)
                iLProcessor.InsertBefore(instruction, Instruction.Create(OpCodes.Ldstr, modName));
        }

        private MethodReference UpdateCtor(MethodReference method)
        {
            MethodReference updatedMethod = new MethodReference("Create", method.DeclaringType, objectManager);

            foreach (var parameter in method.Parameters)
                updatedMethod.Parameters.Add(new ParameterDefinition(parameter.ParameterType));

            updatedMethod.Parameters.Add(new ParameterDefinition(str));

            return method.Module.ImportReference(updatedMethod);
        }

        private MethodReference UpdateMethodReference(MethodReference method)
        {
            MethodReference updatedMethod = new MethodReference(method.Name, method.ReturnType, objectManager);

            foreach (var parameter in method.Parameters)
                updatedMethod.Parameters.Add(new ParameterDefinition(parameter.ParameterType));

            updatedMethod.Parameters.Add(new ParameterDefinition(str));

            var genericMethod = method as GenericInstanceMethod;

            if (genericMethod != null)
                updatedMethod = MakeGeneric(updatedMethod, genericMethod.GenericArguments[0]);

            return method.Module.ImportReference(updatedMethod);
        }
        
        private MethodReference MakeGeneric(MethodReference method, TypeReference genericArgument)
        {
            var genericParameter = new GenericParameter(method.Parameters[0].Name, method);
            method.GenericParameters.Add(genericParameter);

            method.Parameters[0] = new ParameterDefinition(genericParameter);
            method.ReturnType = genericParameter;

            var  genericMethod = new GenericInstanceMethod(method);
            genericMethod.GenericArguments.Add(genericArgument);

            return genericMethod;
        }

        private void ImportReferences(ModuleDefinition module)
        {
            module.AssemblyReferences.Add(modTool);
            str = module.ImportReference(typeof(string));
            objectManager = module.ImportReference(objectManager);
        }

        private void CreateReferences()
        {
            modTool = new AssemblyNameDefinition("ModTool", new Version(1, 0, 0, 0));
            var assembly = AssemblyDefinition.CreateAssembly(modTool, "ModTool.dll", ModuleKind.Dll);

            var objectManager = new TypeDefinition("ModTool", "ObjectManager", TypeAttributes.AnsiClass | TypeAttributes.BeforeFieldInit | TypeAttributes.Public, assembly.MainModule.TypeSystem.Object);
            assembly.MainModule.Types.Add(objectManager);

            this.objectManager = objectManager;
        }
    }

    public class Export : ExportStep
    {
        public override string message { get { return "Exporting Files"; } }

        private string modDirectory;

        internal override void Execute(ExportData data)
        {
            modDirectory = Path.Combine(ExportSettings.outputDirectory, ExportSettings.name);

            BuildAssetBundles(data);

            bool content_has_code = false;
            ModPlatform all_platforms = 0;
            for (int i = 0; i < data.export_content.Count; i++)
            {
                if ((data.export_content[i] & ModContent.Code) == ModContent.Code)
                {
                    content_has_code = true;
                }

                if(data.export_content[i] > 0)
                {
                    all_platforms |= data.export_platforms[i];
                }
            }

            if (content_has_code)
            {
                File.Delete(Path.Combine(tempModDirectory, ExportSettings.name.Replace(" ", "") + ".dll"));
            }

            string json = "{\n\t\"_name\": \"" + ExportSettings.name + "\",\n\t\"_platforms\": " + (int)all_platforms + "\n}";

            File.WriteAllText(Path.Combine(tempModDirectory, ExportSettings.name + ".rootinfo"), json);

            CopyToOutput();
        }

        private void AdjustAssetBundleInclusions(ExportData data, int platform_index)
        {
            string modName = ExportSettings.name;

            bool include_assets = ((data.export_content[platform_index] & ModContent.Assets) == ModContent.Assets);

            foreach (Asset asset in data.assets)
            {
                if (include_assets)
                {
                    asset.SetAssetBundle(modName, "assets");
                }
                else
                {
                    asset.RemoveFromAssetBundle();
                }
            }


            bool include_scenes = ((data.export_content[platform_index] & ModContent.Scenes) == ModContent.Scenes);
            foreach (Asset scene in data.scenes)
            {
                if (include_scenes)
                {
                    scene.name = modName + "-" + scene.original_name;
                    scene.SetAssetBundle(modName, "scenes");
                }
                else
                {
                    scene.name = scene.original_name;
                    scene.RemoveFromAssetBundle();
                }
            }
        }

        private void BuildAssetBundles(ExportData data)
        {
            Dictionary<ModCompression, BuildAssetBundleOptions> compression_dict = new Dictionary<ModCompression, BuildAssetBundleOptions>()
            {
                { ModCompression.Uncompressed, BuildAssetBundleOptions.UncompressedAssetBundle },
                { ModCompression.LZ4, BuildAssetBundleOptions.ChunkBasedCompression },
                { ModCompression.LZMA, BuildAssetBundleOptions.None }
            };

            for(int i=0; i < data.export_platforms.Count; i++)
            {

                BuildTarget buildTarget = data.export_platforms[i].GetBuildTargets()[0];

                string platformSubdirectory = Path.Combine(tempModDirectory, data.export_platforms[i].ToString(), data.export_platforms[i].ToString());
                Directory.CreateDirectory(platformSubdirectory);

                ModInfo modInfo = new ModInfo(
                    ExportSettings.name,
                    ExportSettings.author,
                    ExportSettings.description,
                    ExportSettings.version,
                    Application.unityVersion,
                    data.export_platforms[i],
                    data.export_content[i]
                );

                ModInfo.Save(Path.Combine(tempModDirectory, data.export_platforms[i].ToString(), ExportSettings.name + ".info"), modInfo);

                AdjustAssetBundleInclusions(data, i);

                BuildPipeline.BuildAssetBundles(platformSubdirectory, compression_dict[data.export_compression[i]], buildTarget);

                if((data.export_content[i] & ModContent.Code) == ModContent.Code)
                {
                    File.Copy(Path.Combine(tempModDirectory, ExportSettings.name.Replace(" ", "")+".dll"), Path.Combine(tempModDirectory, data.export_platforms[i].ToString(), ExportSettings.name.Replace(" ", "") + ".dll"), true);
                }
            }

        }

        private void CopyToOutput()
        {
            try
            {
                if (Directory.Exists(modDirectory))
                    ClearModDirectory();

                CopyFiles(tempModDirectory, modDirectory);

                LogUtility.LogInfo("Export complete");
            }
            catch (Exception e)
            {
                LogUtility.LogWarning("There was an issue while copying the mod to the output folder. " + e.Message);
            }
        }

        private void ClearModDirectory()
        {            
            string modName = Path.GetFileName(modDirectory);
            string assetBundleName = modName.ToLower();

            //Note: Only delete files if we're sure they're part of a Mod.
            var files = Directory.GetFiles(modDirectory, "*", SearchOption.AllDirectories);

            foreach (var file in files)
            {                
                string fileName = Path.GetFileName(file);
                string directory = Path.GetFileName(file);

                if (fileName == directory || fileName == directory + ".manifest")
                    File.Delete(file);

                if (fileName.StartsWith(assetBundleName + ".scenes") || fileName.StartsWith(assetBundleName + ".assets"))
                    File.Delete(file);

                if (directory != modName)
                    continue;

                if (fileName.EndsWith(".dll") || fileName == modName + ".info")
                {
                    File.Delete(file);
                }
            }
        }        

        private static void CopyFiles(string sourceDirectory, string targetDirectory)
        {
            Directory.CreateDirectory(targetDirectory);

            foreach (string file in Directory.GetFiles(sourceDirectory))
            {
                string fileName = Path.GetFileName(file);
                File.Copy(file, Path.Combine(targetDirectory, fileName), true);
            }

            foreach (string subDirectory in Directory.GetDirectories(sourceDirectory))
            {
                string targetSubDirectory = Path.Combine(targetDirectory, Path.GetFileName(subDirectory));
                CopyFiles(subDirectory, targetSubDirectory);
            }
        }
    }

    public class RestoreProject : ExportStep
    {
        public override string message { get { return "Restoring Project"; } }

        internal override void Execute(ExportData data)
        {
            foreach (Asset assemblyDefinition in data.assemblyDefinitions)
                assemblyDefinition.Delete();

            foreach (Asset asset in data.assets)
                asset.Restore();

            foreach (Asset scene in data.scenes)
                scene.Restore();

            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);

            if (!string.IsNullOrEmpty(data.loadedScene))
                EditorSceneManager.OpenScene(data.loadedScene);
        }
    }
}
