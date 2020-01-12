using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Windows.Documents;
using System.Xml.Serialization;
using Flurl;
using Flurl.Http;
using GalaSoft.MvvmLight.Messaging;
using NextGenLauncher.Data;
using NextGenLauncher.Exceptions;
using NextGenLauncher.Messages;
using NextGenLauncher.ServerModels;
using NextGenLauncher.Services.Servers;
using NextGenLauncher.Utils;

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

        public async Task<AuthenticationInfo> LogIn(Server server, string email, string password)
        {
            var response = await new Url(server.ServerAddress).AppendPathSegments("User", "authenticateUser")
                .SetQueryParam("email", email).SetQueryParam("password", AuthUtil.Hash(password).ToLower())
                .WithHeader("X-UserAgent",
                    "GameLauncherReborn 2.0.8.8 WinForms (+https://github.com/worldunitedgg/GameLauncher_NFSW)")
                .WithHeader("User-Agent", "GameLauncher (+https://github.com/SoapboxRaceWorld/GameLauncher_NFSW)")
                .WithHeader("X-HWID", "aaaaa")
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

        private AuthenticationInfo ParseToAuthenticationInfo(string data)
        {
            XmlSerializer serializer =
                new XmlSerializer(typeof(LoginStatusVO));

            using StringReader reader = new StringReader(data);
            LoginStatusVO loginStatus = (LoginStatusVO) serializer.Deserialize(reader);

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