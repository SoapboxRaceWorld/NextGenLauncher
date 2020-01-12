using System.Collections.Generic;
using Flurl;
using Flurl.Http;
using NextGenLauncher.Data;

namespace NextGenLauncher.Services.Servers
{
    public class RemoteListSource : IServerListSource
    {
        public IEnumerable<Server> FetchServers()
        {
            return new Url("https://api.sbrw.io/api/servers")
                .WithHeader("User-Agent", "NextGenLauncher/1.0.0.0 (SBRW)")
                .GetJsonAsync<List<Server>>().Result;
        }
    }
}