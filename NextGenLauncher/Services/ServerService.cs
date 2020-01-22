using Flurl;
using Flurl.Http;
using GalaSoft.MvvmLight.Messaging;
using NextGenLauncher.Data;
using NextGenLauncher.Exceptions;
using NextGenLauncher.Messages;
using NextGenLauncher.ServerModels;
using NextGenLauncher.Services.Servers;
using NextGenLauncher.Utils;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace NextGenLauncher.Services
{
    /// <summary>
    /// Keeps track of server data provided by the SBRW API
    /// </summary>
    public class ServerService
    {
        private List<Server> Servers { get; set; }

        private List<IServerListSource> Sources { get; }

        public ServerService()
        {
            Sources = new List<IServerListSource>
            {
                new RemoteListSource()
            };
        }

        /// <summary>
        /// Downloads the list of servers.
        /// </summary>
        public void FetchServers()
        {
            Servers = Sources.SelectMany(s => s.FetchServers()).ToList();
            TriggerListMessage();
        }

        /// <summary>
        /// Attempts to log in to a server with the given email address and password.
        /// </summary>
        /// <param name="server">The server to log in to</param>
        /// <param name="email">The user's email address</param>
        /// <param name="password">The user's password</param>
        /// <returns>A <see cref="AuthenticationInfo"/> instance with token/userID</returns>
        /// <exception cref="AuthenticationException">if there is an error while attempting to log in</exception>
        public async Task<AuthenticationInfo> LogInAsync(Server server, string email, string password)
        {
            var response = await new Url(server.ServerAddress).AppendPathSegments("User", "authenticateUser")
                .SetQueryParam("email", email).SetQueryParam("password", HashUtil.HashSha1(password).ToLower())
                .WithHeader("X-UserAgent",
                    "GameLauncherReborn 2.0.8.8 WinForms (+https://github.com/worldunitedgg/GameLauncher_NFSW)")
                .WithHeader("User-Agent", "GameLauncher (+https://github.com/SoapboxRaceWorld/GameLauncher_NFSW)")
                .WithHeader("X-HWID", HardwareIdGenerator.GetHardwareIdentifier())
                .WithHeader("X-GameLauncherHash", "aaaaa")
                .AllowAnyHttpStatus()
                .GetAsync();
            return response.StatusCode switch
            {
                HttpStatusCode.OK => ParseToAuthenticationInfo(await response.Content.ReadAsStringAsync()),
                HttpStatusCode.Unauthorized => throw new AuthenticationException("Server rejected login from this launcher."),
                HttpStatusCode.InternalServerError => throw new AuthenticationException("Credentials rejected by server"),
                _ => throw new AuthenticationException("Cannot handle status code: " + response.StatusCode),
            };
        }

        /// <summary>
        /// Fetches information about the given server
        /// </summary>
        /// <param name="server">The server</param>
        /// <returns></returns>
        public void FetchServerInfo(Server server)
        {
            if (server.Stats.Status != ServerStats.ServerStatus.Online) return;

            ServerInformation serverInformation = new Url(server.ServerAddress).AppendPathSegment("GetServerInformation")
                .AllowAnyHttpStatus()
                .GetJsonAsync<ServerInformation>().Result;
            server.BannerUrl = serverInformation.BannerUrl;
        }

        private AuthenticationInfo ParseToAuthenticationInfo(string data)
        {
            XmlSerializer serializer =
                new XmlSerializer(typeof(LoginStatusVO));

            using StringReader reader = new StringReader(data);
            LoginStatusVO loginStatus = (LoginStatusVO)serializer.Deserialize(reader);

            return new AuthenticationInfo
            {
                LoginToken = loginStatus.LoginToken,
                UserId = loginStatus.UserID
            };
        }

        private void TriggerListMessage()
        {
            Messenger.Default.Send(new ServerListUpdatedMessage(Servers));
        }
    }
}