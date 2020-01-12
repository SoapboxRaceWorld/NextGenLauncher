using System.Collections.Generic;
using NextGenLauncher.Data;

namespace NextGenLauncher.Services.Servers
{
    /// <summary>
    /// Contract for a server list provider
    /// </summary>
    public interface IServerListSource
    {
        /// <summary>
        /// Obtains the servers from the source
        /// </summary>
        /// <returns></returns>
        IEnumerable<Server> FetchServers();
    }
}