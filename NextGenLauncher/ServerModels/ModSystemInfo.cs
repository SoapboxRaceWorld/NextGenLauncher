using System.Collections.Generic;
using Newtonsoft.Json;

namespace NextGenLauncher.ServerModels
{
    public class ModSystemInfo
    {
        [JsonProperty("basePath")]
        public string BasePath { get; set; }
        [JsonProperty("serverID")]
        public string ServerId { get; set; }
        [JsonProperty("features")]
        public List<string> Features { get; set; }
    }
}