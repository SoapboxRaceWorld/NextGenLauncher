using System;
using Nancy;
using Nancy.Hosting.Self;
using NextGenLauncher.Data;

namespace NextGenLauncher.Proxy
{
    /// <summary>
    /// Proxies requests from the game to the server
    /// </summary>
    public class ServerProxy
    {
        private static readonly Lazy<ServerProxy> Lazy = new Lazy<ServerProxy>(() => new ServerProxy());

        public static ServerProxy Instance => Lazy.Value;

        private Server CurrentServer { get; set; }

        private ServerProxy()
        {
            //
        }

        /// <summary>
        /// Starts the proxy server
        /// </summary>
        public void Start()
        {
            var hostConfigs = new HostConfiguration()
            {
                UrlReservations = new UrlReservations()
                {
                    CreateAutomatically = true,
                },
                AllowChunkedEncoding = false
            };

            NancyHost nancyHost = new NancyHost(new Uri("http://127.0.0.1:4080"), new ServerProxyBootstrapper(), hostConfigs);
            nancyHost.Start();
        }

        public Server GetCurrentServer()
        {
            return CurrentServer;
        }

        public void SetCurrentServer(Server server)
        {
            CurrentServer = server;
        }
    }
}