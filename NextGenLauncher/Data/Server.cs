using Newtonsoft.Json;

namespace NextGenLauncher.Data
{
    public class ServerStats
    {
        public enum ServerStatus
        {
            Online,
            Offline,
            Unknown
        }

        [JsonProperty("status")]
        public ServerStatus Status { get; set; }
        [JsonProperty("online")]
        public int NumOnline { get; set; }
        [JsonProperty("registered")]
        public int NumRegistered { get; set; }
    }

    public class Server
    {
        [JsonProperty("name")]
        public string Name { get; set; }
        [JsonProperty("category")]
        public string Category { get; set; }
        [JsonProperty("gameEndpoint")]
        public string ServerAddress { get; set; }
        [JsonProperty("countryCode")]
        public string CountryCode { get; set; }
        [JsonProperty("key")]
        public string Key { get; set; }
        [JsonProperty("stats")]
        public ServerStats Stats { get; set; }

        [JsonIgnore]
        public string BannerUrl { get; set; }
    }
}