using UnityEngine;
using ModTool.Shared;

namespace ModTool.Editor.Exporting
{
    /// <summary>
    /// Stores the exporter's settings.
    /// </summary>
    public class ExportSettings : Singleton<ExportSettings>
    {
        /// <summary>
        /// The Mod's name.
        /// </summary>
        public static new string name
        {
            get
            {
                return instance._name;
            }
            set
            {
                instance._name = value;
            }
        }

        /// <summary>
        /// The Mod's author.
        /// </summary>
        public static string author
        {
            get
            {
                return instance._author;
            }
            set
            {
                instance._author = value;
            }
        }

        /// <summary>
        /// The Mod's description.
        /// </summary>
        public static string description
        {
            get
            {
                return instance._description;
            }
            set
            {
                instance._description = value;
            }
        }
                
        /// <summary>
        /// The Mod's version.
        /// </summary>
        public static string version
        {
            get
            {
                return instance._version;
            }
            set
            {
                instance._version = value;
            }
        }

        /// <summary>
        /// The selected platforms for which this mod will be exported.
        /// </summary>
        public static ModPlatform platforms
        {
            get
            {
                return instance._platforms;
            }
            set
            {
                instance._platforms = value;
            }
        }

        /// <summary>
        /// The selected content types that will be exported.
        /// </summary>
        public static int[] content
        {
            get
            {
                return instance._content;
            }
            set
            {
                instance._content = value;
            }
        }

        /// <summary>
        /// The selected compression types that will be exported.
        /// </summary>
        public static int[] compression
        {
            get
            {
                return instance._compression;
            }
            set
            {
                instance._compression = value;
            }
        }

        /// <summary>
        /// The selected lock types that will be exported.
        /// </summary>
        public static int[] locked
        {
            get
            {
                return instance._locked;
            }
            set
            {
                instance._locked = value;
            }
        }

        /// <summary>
        /// The directory to which the Mod will be exported.
        /// </summary>
        public static string outputDirectory
        {
            get
            {
                return instance._outputDirectory;
            }
            set
            {
                instance._outputDirectory = value;
            }
        }

        /// <summary>
        /// The platform content matrix that is saved to the object.
        /// </summary>
        public static bool[] platform_content_matrixSAVE
        {
            get
            {
                return instance._platform_content_matrixSAVE;
            }
            set
            {
                instance._platform_content_matrixSAVE = value;
            }
        }

        /// <summary>
        /// The platform content that is saved to the object.
        /// </summary>
        public static ModContent[] platform_contentSAVE
        {
            get
            {
                return instance._platform_contentSAVE;
            }
            set
            {
                instance._platform_contentSAVE = value;
            }
        }

        [SerializeField]
        private string _name;

        [SerializeField]
        private string _author;

        [SerializeField]
        private string _description;

        [SerializeField]
        private string _version;

        [SerializeField]
        private ModPlatform _platforms = (ModPlatform)(-1);

        [SerializeField]
        private int[] _content = new int[] { 0, 0, 0, 0, 0 };

        [SerializeField]
        private int[] _compression = new int[] { 0, 0, 0, 0, 0 };

        [SerializeField]
        private int[] _locked = new int[] { 0, 0, 0, 0, 0 };

        [SerializeField]
        private string _outputDirectory;

        [SerializeField]
        private bool[] _platform_content_matrixSAVE = new bool[] {
                                                      false, false, false, //Windows - Scene, Asset, Code
                                                      false, false, false, //Linux - Scene, Asset, Code
                                                      false, false, false, //OSX - Scene, Asset, Code
                                                      false, false, false, //Android - Scene, Asset, Code
                                                      false, false, false  //iPhone - Scene, Asset, Code
        };

        [SerializeField]
        private ModContent[] _platform_contentSAVE = new ModContent[] { 0, 0, 0, 0, 0 };
    }
}
