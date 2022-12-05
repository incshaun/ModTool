using System;
using System.Collections.Generic;
using UnityEngine;

namespace ModTool.Shared
{
    /// <summary>
    /// Represents a compression method or a combination of compression methods.
    /// </summary>
    [Flags]
    [Serializable]
    public enum ModCompression { Uncompressed = 1, LZ4 = 2, LZMA = 4 }

    
}
