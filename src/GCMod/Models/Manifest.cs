using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace GCMod
{
    /// <summary>
    /// 翻译清单数据结构，对应远程 manifest.json 格式。
    /// </summary>
    public class Manifest
    {
        [JsonPropertyName("hash")]
        public string Hash { get; set; }

        [JsonPropertyName("names")]
        public string Names { get; set; }

        [JsonPropertyName("master")]
        public string Master { get; set; }

        [JsonPropertyName("novels")]
        public Dictionary<string, string> Novels { get; set; }
    }
}
