using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChedVX.Core
{
    /// <summary>
    /// A class that represents a chart file.
    /// </summary>
    [Newtonsoft.Json.JsonObject(Newtonsoft.Json.MemberSerialization.OptIn)]
    public class ScoreBook
    {
        protected static readonly Version CurrentVersion = typeof(ScoreBook).Assembly.GetName().Version;

        internal static JsonSerializerSettings SerializerSettings = new JsonSerializerSettings()
        {
            ReferenceLoopHandling = ReferenceLoopHandling.Serialize,
            PreserveReferencesHandling = PreserveReferencesHandling.Objects,
            TypeNameHandling = TypeNameHandling.Auto,
            Formatting = Formatting.Indented
        };

        [Newtonsoft.Json.JsonProperty]
        private Version version = CurrentVersion;
        [Newtonsoft.Json.JsonProperty]
        private string title = "";
        [Newtonsoft.Json.JsonProperty]
        private string artistName = "";
        [Newtonsoft.Json.JsonProperty]
        private string effector = "";
        [Newtonsoft.Json.JsonProperty]
        private string illustrator = "";
        [Newtonsoft.Json.JsonProperty]
        private uint volume = 0;
        [Newtonsoft.Json.JsonProperty]
        private uint bpm_max = 0;
        [Newtonsoft.Json.JsonProperty]
        private uint bpm_min = 0;
        [Newtonsoft.Json.JsonProperty]
        private uint distDate = 0;
        [Newtonsoft.Json.JsonProperty]
        private uint level = 0;
        [Newtonsoft.Json.JsonProperty]
        private uint backgroundID = 0;
        [Newtonsoft.Json.JsonProperty]
        private Score score = new Score();
        [Newtonsoft.Json.JsonProperty]
        private Dictionary<string, string> exportArgs = new Dictionary<string, string>();

        #region
        public string Path { get; set; }

        /// <summary>
        /// Sets the version of the application that created the file.
        /// </summary>
        public Version Version
        {
            get { return version; }
            set { version = value; }
        }

        /// <summary>
        /// Set the song title.
        /// </summary>
        public string Title
        {
            get { return title; }
            set { title = value; }
        }

        /// <summary>
        /// Set the artist of the song.
        /// </summary>
        public string ArtistName
        {
            get { return artistName; }
            set { artistName = value; }
        }

        /// <summary>
        /// Set the chart effector name.
        /// </summary>
        public string Effector
        {
            get { return effector; }
            set { effector = value; }
        }

        /// <summary>
        /// Set the illustrator name.
        /// </summary>
        public string Illustrator
        {
            get { return illustrator; }
            set { illustrator = value; }
        }

        /// <summary>
        /// Set the music volume.
        /// </summary>
        public uint Volume
        {
            get { return volume; }
            set { volume = value; }
        }

        /// <summary>
        /// Set the min bpm.
        /// </summary>
        public uint BPM_MIN
        {
            get { return bpm_min; }
            set { bpm_min = value; }
        }

        /// <summary>
        /// Set the max bpm.
        /// </summary>
        public uint BPM_MAX
        {
            get { return bpm_max; }
            set { bpm_max = value; }
        }

        /// <summary>
        /// Set the distributed date.
        /// </summary>
        public uint DistDate
        {
            get { return distDate; }
            set { distDate = value; }
        }

        /// <summary>
        /// Set the level number.
        /// </summary>
        public uint Level
        {
            get { return level; }
            set { level = value; }
        }
        /// <summary>
        /// Set the BackgroundID.
        /// </summary>
        public uint BackgroundID
        {
            get { return backgroundID; }
            set { backgroundID = value; }
        }

        /// <summary>
        /// Set the chart data.
        /// </summary>
        public Score Score
        {
            get { return score; }
            set { score = value; }
        }

        /// <summary>
        /// エクスポート用の設定を格納します。
        /// </summary>
        public Dictionary<string, string> ExportArgs
        {
            get { return exportArgs; }
            set { exportArgs = value; }
        }
        #endregion

        public void Save(string path)
        {
            Path = path;
            Save();
        }

        public void Save()
        {
            string data = JsonConvert.SerializeObject(this, SerializerSettings);
            byte[] bytes = Encoding.UTF8.GetBytes(data);
            using (var stream = new MemoryStream(bytes))
            {
                using (var file = new FileStream(Path, FileMode.Create))
                using (var gz = new GZipStream(file, CompressionMode.Compress))
                {
                    stream.CopyTo(gz);
                }
            }
        }

        public ScoreBook Clone() => JsonConvert.DeserializeObject<ScoreBook>(JsonConvert.SerializeObject(this, SerializerSettings));

        /// <summary>
        /// Create an instance of <see cref="ScoreBook"/> from the specified file.
        /// Files from older versions will be converted for the current version.
        /// Currently this is useless.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static ScoreBook LoadFile(string path)
        {
            string data = GetDecompressedData(path);
            var doc = JObject.Parse(data);
            var fileVersion = GetFileVersion(path);

            doc["version"] = JObject.FromObject(CurrentVersion);

            var res = doc.ToObject<ScoreBook>(JsonSerializer.Create(SerializerSettings));

            if (res.Score.Events.TimeSignatureChangeEvents.Count == 0)
            {
                res.Score.Events.TimeSignatureChangeEvents.Add(new Events.TimeSignatureChangeEvent() { Tick = 0, Numerator = 4, DenominatorExponent = 2 });
            }

            res.Path = path;
            return res;
        }

        /// <summary>
        /// Check if the specified file is compatible with the current version.
        /// </summary>
        /// <param name="path">Path to the file</param>
        /// <returns>True if compatible, false if not</returns>
        public static bool IsCompatible(string path)
        {
            return GetFileVersion(path).Major <= CurrentVersion.Major;
        }

        /// <summary>
        /// Check whether the specified file needs to be updated.
        /// </summary>
        /// <param name="path">Path to the file</param>
        /// <returns>True if version upgrade is required, false if not</returns>
        public static bool IsUpgradeNeeded(string path)
        {
            return GetFileVersion(path).Major < CurrentVersion.Major;
        }

        private static string GetDecompressedData(string path)
        {
            using (var gz = new GZipStream(new FileStream(path, FileMode.Open), CompressionMode.Decompress))
            using (var stream = new MemoryStream())
            {
                gz.CopyTo(stream);
                return Encoding.UTF8.GetString(stream.ToArray());
            }
        }

        /// <summary>
        /// Get the version of the data saved in the specified file.
        /// </summary>
        /// <param name="path">Path to the file</param>
        /// <returns>The version in which the file was generated</returns>
        private static Version GetFileVersion(string path)
        {
            var doc = JObject.Parse(GetDecompressedData(path));
            return doc["version"].ToObject<Version>();
        }
    }
}
