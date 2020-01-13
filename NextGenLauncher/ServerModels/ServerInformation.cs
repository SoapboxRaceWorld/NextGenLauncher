using Newtonsoft.Json;

namespace NextGenLauncher.ServerModels
{
    /*
     * {
"messageSrv": "WorldUnited.gg: A new era of Need for Speed: World.",
"homePageUrl": "https://soapboxrace.world",
"facebookUrl": "https://facebook.com/SoapboxRaceWorld",
"discordUrl": "https://discord.gg/Sv3zT9A",
"serverName": "WorldUnited.gg",
"country": "US",
"timezone": 0,
"bannerUrl": "https://i.imgur.com/EmxYSGu.png",
"adminList": "heyitsleo",
"ownerList": "heyitsleo",
"numberOfRegistered": 4972,
"allowedCountries": null,
"activatedHolidaySceneryGroups": [
"SCENERY_GROUP_CHRISTMAS"
],
"disactivatedHolidaySceneryGroups": [
"SCENERY_GROUP_CHRISTMAS_DISABLE"
],
"onlineNumber": 109,
"requireTicket": true,
"serverVersion": "0.0.8"
}
     */

    [JsonObject]
    public class ServerInformation
    {
        [JsonProperty("bannerUrl")]
        public string BannerUrl { get; set; }
    }
}