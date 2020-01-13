using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace NextGenLauncher.ServerModels
{
    public class ModIndexEntry
    {
        [JsonProperty("Name")]
        public string Name { get; set; }
        [JsonProperty("Checksum")]
        public string Checksum { get; set; }
    }

    public class ModIndex
    {
        [JsonProperty("built_at")]
        public DateTimeOffset BuiltAt { get; set; }

        [JsonProperty("entries")]
        public List<ModIndexEntry> Entries { get; set; }
    }
}