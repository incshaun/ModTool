using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

namespace ModTool.Shared
{
    /// <summary>
    /// Represents a directory that is monitored for Mods.
    /// </summary>
    public class ModSearchDirectory : IDisposable
    {
        /// <summary>
        /// Occurs when a new Mod has been found.
        /// </summary>
        public event Action<string> ModFound;
        /// <summary>
        /// Occurs when a Mod has been removed.
        /// </summary>
        public event Action<string> ModRemoved;
        /// <summary>
        /// Occurs when a change to a Mod's directory has been detected.
        /// </summary>
        public event Action<string> ModChanged;
        /// <summary>
        /// Occurs when any change was detected for any Mod in this search directory.
        /// </summary>
        public event Action ModsChanged;

        /// <summary>
        /// This ModSearchDirectory's path.
        /// </summary>
        public string path { get; private set; }

        private Dictionary<string, long> _modPaths; // The .info files.

        private bool refreshEvent;
        private bool disposed;

        /// <summary>
        /// Initialize a new ModSearchDirectory with a path.
        /// </summary>
        /// <param name="path">The path to the search directory.</param>
        public ModSearchDirectory(string path)
        {
            if (!path.StartsWith ("http"))
            {
              this.path = Path.GetFullPath(path);
            

                if (!Directory.Exists(this.path))            
                    throw new DirectoryNotFoundException(this.path);            
            }
            else
            {
                this.path = path;
            }
            
            _modPaths = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);

            refreshEvent = false;

            ModInfo.WebCoroutine._StartCoroutine(BackgroundRefresh ());
        }

        /// <summary>
        /// Refresh the collection of mod paths. Remove all missing paths and add all new paths.
        /// </summary>
        public void Refresh()
        {
            refreshEvent = true;
        }
        
        private IEnumerator BackgroundRefresh()
        {
            yield return new WaitUntil (() => refreshEvent);
            refreshEvent = false;

            while(!disposed)
            {
                yield return DoRefresh();

                yield return new WaitUntil (() => refreshEvent);
                refreshEvent = false;
            }
            
            yield return null;
        }

        private IEnumerator DoRefresh()
        {
            bool changed = false;            

            // ModCurrentPaths contains the latest view of any files.
            Dictionary<string, long> modCurrentPaths = null;
            yield return ModInfo.WebCoroutine._StartCoroutine (ModInfo.GetModPaths(path, (r) => { modCurrentPaths = r; }, ""));
            
            // _ModPaths contains the previous view of any info files.
            // Scan for changes, and signal where differences exist.            
            foreach (string path in _modPaths.Keys.ToArray())
            {
                // Remove any files in previous that no longer exist in current.
                if (!modCurrentPaths.Keys.Contains(path))
                {
                    changed = true;
                    RemoveModPath(path);
                    continue;
                }
            }

            foreach (string path in modCurrentPaths.Keys)
            {
                if (_modPaths.Keys.Contains (path))
                {
                    long lastWriteTime = _modPaths[path];

                    // Update any files that have changed relative to previous.
                    if (modCurrentPaths[path] > lastWriteTime)
                    {
                        changed = true;
                        _modPaths[path] = modCurrentPaths[path];
                        UpdateModPath(path);
                        continue;
                    }
                }
                
                // Check if any files in the same directory are more recent.
                foreach (string prevpath in _modPaths.Keys)
                {
                    string dirname = Path.GetDirectoryName (path);
                    if (prevpath.StartsWith (dirname) && modCurrentPaths[path] > _modPaths[prevpath])
                    {
                        changed = true;
                        _modPaths[prevpath] = modCurrentPaths[path];
                        UpdateModPath(prevpath);
                        break;
                    }
                }

                // Add any new files.
                if (!_modPaths.ContainsKey(path) && (path.EndsWith (".info")))
                {
                    changed = true;
                    AddModPath(path, modCurrentPaths[path]);
                }
            }

            if (changed)
                ModsChanged?.Invoke();
            
            yield return null;
        }

        private void AddModPath(string path, long timestamp)
        {
            if (_modPaths.ContainsKey(path))
                return;

            _modPaths.Add(path, timestamp);

            ModFound?.Invoke(path);
        }

        private void RemoveModPath(string path)
        {
            if (!_modPaths.ContainsKey(path))
                return;

            _modPaths.Remove(path);
            ModRemoved?.Invoke(path);
        }

        private void UpdateModPath(string path)
        {
            if (!File.Exists(path))
            {
                RemoveModPath(path);
                return;
            }

            ModChanged?.Invoke(path);
        }
             
        /// <summary>
        /// Releases all resources used by the ModSearchDirectory.
        /// </summary>
        public void Dispose()
        {
            ModFound = null;
            ModRemoved = null;
            ModChanged = null;

            disposed = true;
            refreshEvent = true;
        }
    }
}
