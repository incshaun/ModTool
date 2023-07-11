using System;
using System.IO;
using UnityEngine;

using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using System.Linq;
using System.Globalization;

namespace ModTool.Shared
{
    /// <summary>
    /// Class that stores a Mod's name, author, description, version, path and supported platforms.
    /// </summary>
    [Serializable]
    public class ModInfo
    {
        /// <summary>
        /// Name
        /// </summary>
        public string name
        {
            get
            {
                return _name;
            }
        }

        /// <summary>
        /// Supported platforms for this mod.
        /// </summary>
        public ModPlatform platforms
        {
            get
            {
                return _platforms;
            }
        }
        
        /// <summary>
        /// The Mod's available content types.
        /// </summary>
        public ModContent content
        {
            get
            {
                return _content;
            }
        }

        /// <summary>
        /// Mod author.
        /// </summary>
        public string author
        {
            get
            {
                return _author;
            }
        }

        /// <summary>
        /// Mod description.
        /// </summary>
        public string description
        {
            get
            {
                return _description;
            }
        }

        /// <summary>
        /// Mod version.
        /// </summary>
        public string version
        {
            get
            {
                return _version;
            }
        }

        /// <summary>
        /// The version of Unity that was used to export this mod.
        /// </summary>
        public string unityVersion
        {
            get
            {
                return _unityVersion;
            }
        }
        
        /// <summary>
        /// Should this mod be enabled.
        /// </summary>
        public bool isEnabled
        {
            get
            {
                return _isEnabled;
            }
            set
            {
                _isEnabled = value;
            }
        }

        /// <summary>
        /// Location of mod
        /// </summary>
        public string path { get; private set; }

        [SerializeField]
        private string _name;

        [SerializeField]
        private string _author;

        [SerializeField]
        private string _description;

        [SerializeField]
        private string _version;

        [SerializeField]
        private string _unityVersion;

        [SerializeField]
        private ModPlatform _platforms;

        [SerializeField]
        private ModContent _content;

        [SerializeField]
        private bool _isEnabled;

        /// <summary>
        /// Initialize a new ModInfo.
        /// </summary>
        /// <param name="name">The Mod's name.</param>
        /// <param name="author">The Mod's author.</param>
        /// <param name="description">The Mod's description.</param>
        /// <param name="platforms">The Mod's supported platforms.</param>
        /// <param name="content">The Mod's available content types.</param>
        /// <param name="version">The Mod's version</param>
        /// <param name="unityVersion"> The version of Unity that the Mod was exported with.</param>
        public ModInfo(
            string name,
            string author,
            string description,
            string version,
            string unityVersion,
            ModPlatform platforms,
            ModContent content)
        {
            _author = author;
            _description = description;
            _name = name;
            _platforms = platforms;
            _content = content;
            _version = version;
            _unityVersion = unityVersion;

            isEnabled = false;
        }
        
        /// <summary>
        /// Save this ModInfo.
        /// </summary>
        public void Save()
        {
            if (!string.IsNullOrEmpty(path))
                Save(path, this);
        }

        /// <summary>
        /// Save a ModInfo.
        /// </summary>
        /// <param name="path">The path to save the ModInfo to.</param>
        /// <param name="modInfo">The ModInfo to save.</param>
        public static void Save(string path, ModInfo modInfo)
        {
            string json = JsonUtility.ToJson(modInfo, true);

            File.WriteAllText(path, json);         
        }

        /// <summary>
        /// Load a ModInfo.
        /// </summary>
        /// <param name="path">The path to load the ModInfo from.</param>
        /// <returns>The loaded Modinfo, if succeeded. Null otherwise.</returns>
        public static ModInfo Load(string path)
        {
            path = Path.GetFullPath(path);

            if (File.Exists(path))
            {
                try
                {
                    string json = File.ReadAllText(path);

                    ModInfo modInfo = JsonUtility.FromJson<ModInfo>(json);

                    modInfo.path = path;

                    return modInfo;
                }
                catch(Exception e)
                {
                    LogUtility.LogWarning("There was an issue while loading the ModInfo from " + path + " - " + e.Message);                    
                }
            }
            
            return null;
        }     

        // Expects to find ModTool in a defined location.
        public static string modToolDirectory ()
        {
            return Path.Combine("Assets", "ModTool");
        }
        
        class UnityTrustCertificate : CertificateHandler
        {
            protected override bool ValidateCertificate(byte[] certificateData)
            {
                return true;
            }
        }
        
        private static IEnumerator readWeb (string path, System.Action<string> callback)
        {
            using (UnityWebRequest webRequest = UnityWebRequest.Get(path))
            {
                webRequest.certificateHandler = new UnityTrustCertificate ();
                Debug.Log ("rW" + path);
                yield return webRequest.SendWebRequest();
                Debug.Log ("rW3 " + path + " " + webRequest.result);
                if (webRequest.result == UnityWebRequest.Result.Success)
                {
                Debug.Log ("rW2 " + path + " " + webRequest.downloadHandler.text);
                    callback (webRequest.downloadHandler.text);
                    yield break;
                }
            }
            callback ("");
        }

        private static IEnumerator retrieveModFile (string sourcePath, string destPath)
        {
            // Check if file already exists, and if it is more recent.
            DateTime sourceTime = new DateTime ();
            bool validSourceTime = false;
            using (UnityWebRequest request = UnityWebRequest.Head (sourcePath))
            {
                request.certificateHandler = new UnityTrustCertificate ();
                yield return request.SendWebRequest ();
             
                try 
                {
                    long sourceSize = Convert.ToInt64 (request.GetResponseHeader("Content-Length"));
                    sourceTime = DateTime.ParseExact(request.GetResponseHeader("Last-Modified"), "ddd, dd MMM yyyy HH:mm:ss 'GMT'", CultureInfo.InvariantCulture.DateTimeFormat, DateTimeStyles.AssumeUniversal);
                    validSourceTime = true;
                    long destSize = new FileInfo (destPath).Length;
                    DateTime destTime = File.GetLastWriteTime (destPath);
                    if (File.Exists (destPath) && (sourceTime == destTime) && (sourceSize == destSize))
                    {
                        yield break;
                    }
                }
                catch (Exception e)
                {
                  // ignore issues, just force a reload anyway.    
                  LogUtility.LogWarning("There was an issue checking header of " + sourcePath + " - " + e.Message);                    
                }
            }
            
//             Debug.Log ("Mirroring " + sourcePath + " to " + destPath);
            
            using (UnityWebRequest request = UnityWebRequest.Get (sourcePath))
            {
                request.certificateHandler = new UnityTrustCertificate ();
                yield return request.SendWebRequest ();
        
                if ((request.result != UnityWebRequest.Result.ConnectionError) && (request.result != UnityWebRequest.Result.ProtocolError))
                {
                    FileInfo file = new FileInfo(destPath);
                    file.Directory.Create(); // create directories if required.
                    File.WriteAllBytes (destPath, request.downloadHandler.data);
                    if (validSourceTime)
                    {
                        File.SetLastWriteTime (destPath, sourceTime);
                    }
                    yield break;
                }
            }
            yield return null;
        }
        
        public static IEnumerator GetModPaths(string path, System.Action<Dictionary<string, long>> callback, string endsWith)
        {
            if (path.StartsWith ("http"))
            {
                using (UnityWebRequest webRequest = UnityWebRequest.Get(Path.Combine (path, "FileList")))
                {
                    // FileList needs to be created for each mod path added. Use:
                    // rm FileList; find . -type f | cut -c3- | tee FileList
                    webRequest.certificateHandler = new UnityTrustCertificate ();
                    yield return webRequest.SendWebRequest();
                    if (webRequest.result == UnityWebRequest.Result.Success)
                    {
                        Dictionary<string, long> modpaths = new Dictionary<string, long> ();
                        
                        // The original intent was to provide web references to the mod files so they could be
                        // accessed on demand. However some assets are accessed as files by third party packages
                        // and so would have to be retrieved and stored on the filesystem anyway. This function
                        // then might as well retrieve them all in one go, which saves having to scattershot this
                        // facility across the other classes.
                        
                        string hashName = path.GetHashCode ().ToString ();
                        foreach (string fpath in webRequest.downloadHandler.text.Split (new [] { '\r', '\n' }))
                        {
                            if (fpath.Length > 0)
                            {
                                string localModName = Path.Combine (Application.persistentDataPath, Path.Combine ("ModTool", Path.Combine (hashName, fpath)));
                                yield return retrieveModFile (Path.Combine (path, fpath), localModName);
                                modpaths.Add (localModName, File.GetLastWriteTime (localModName).Ticks);
                            }                            
                        }
                        
//                         foreach (string fpath in webRequest.downloadHandler.text.Split (new [] { '\r', '\n' }).Select (x => Path.Combine (path, x)))
//                         {
//                             if (fpath.EndsWith (endsWith))
//                             {
//                               modpaths.Add (fpath, 1);
//                             }
//                         }
                        callback (modpaths);
                        yield break;
                    }
                }
                callback (new Dictionary<string, long> ());
            }
            else
            {
                Dictionary<string, long> modpaths = new Dictionary<string, long> ();
                string [] paths = Directory.GetFiles(path, "*", SearchOption.AllDirectories);
                paths = paths.Concat (Directory.GetDirectories(path, "*", SearchOption.AllDirectories)).ToArray ();
                foreach (string fpath in paths)
                {
                    if (fpath.EndsWith (endsWith))
                    {
                      modpaths.Add (fpath, File.GetLastWriteTime (fpath).Ticks);
                    }
                }
                callback (modpaths);
            }
            
            yield return null;
        }
        
        // A bit of a hack, to get coroutines running on a non-MonoBehaviour class. Relies on the scene having at
        // least one object, and that object being active.
        public class WebCoroutine : MonoBehaviour
        {
            private static WebCoroutine coroutineComponent;
            
            public static Coroutine _StartCoroutine (IEnumerator iEnumerator)
            {
                if (coroutineComponent == null)
                {
                  coroutineComponent = SceneManager.GetActiveScene ().GetRootGameObjects ()[0].AddComponent <WebCoroutine> ();
                }
                return coroutineComponent.StartCoroutine (iEnumerator);
            }
        }
    }       
}
