using Flurl;
using Flurl.Http;
using Newtonsoft.Json;
using NextGenLauncher.Data;
using NextGenLauncher.Exceptions;
using NextGenLauncher.ServerModels;
using NextGenLauncher.Utils;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Threading.Tasks;

namespace NextGenLauncher.Services
{
    /// <summary>
    /// Manages server mods
    /// </summary>
    public class ServerModService
    {
        /// <summary>
        /// Installs the mod system modules into the given game folder.
        /// </summary>
        /// <param name="directory">The game folder to install the modules into</param>
        /// <returns></returns>
        public async Task InstallModSystemAsync(string directory)
        {
            // Check for old stuff
            if (File.Exists(Path.Combine(directory, "lightfx.dll")) ||
                File.Exists(Path.Combine(directory, "ModManager.asi")) ||
                File.Exists(Path.Combine(directory, "ModManager.dat")))
            {
                throw new ModSystemException("Legacy mod system files detected in game folder. Cannot continue.");
            }

            string[] modules = { "7z.dll", "PocoFoundation.dll", "PocoNet.dll", "dinput8.dll", "ModLoader.asi" };

            foreach (var module in modules)
            {
                Url url = new Url("https://cdn.soapboxrace.world/modules").AppendPathSegment(module);
                Stream responseStream = await url.GetStreamAsync();

                using FileStream fileStream = new FileStream(Path.Combine(directory, module),
                    FileMode.Create, FileAccess.Write);
                responseStream.CopyTo(fileStream);
            }
        }

        /// <summary>
        /// Downloads and installs mod packages associated with the given server.
        /// </summary>
        /// <param name="server">The server to download mods for.</param>
        /// <param name="directory">The directory to install mods into.</param>
        /// <returns></returns>
        public async Task DownloadServerModsAsync(Server server, string directory)
        {
            HttpResponseMessage modSystemInfo = await new Flurl.Url(server.ServerAddress)
                .AppendPathSegment("Modding/GetModInfo")
                .AllowAnyHttpStatus()
                .GetAsync();
            if (modSystemInfo.StatusCode == HttpStatusCode.OK)
            {
                ModSystemInfo msi =
                    JsonConvert.DeserializeObject<ModSystemInfo>(await modSystemInfo.Content.ReadAsStringAsync());

                if (string.IsNullOrWhiteSpace(msi.BasePath))
                {
                    throw new ModSystemException("No base path for mods was provided by the server.");
                }

                if (string.IsNullOrWhiteSpace(msi.ServerId))
                {
                    throw new ModSystemException("No group ID for mods was provided by the server.");
                }

                ModIndex index = await new Flurl.Url(msi.BasePath)
                    .AppendPathSegment("index.json")
                    .GetJsonAsync<ModIndex>();

                string modsDirectory = Path.Combine(directory, "MODS", HashUtil.HashMd5(msi.ServerId).ToLower());

                SecurityIdentifier identity = new SecurityIdentifier(WellKnownSidType.WorldSid, null);
                DirectorySecurity accessControl = new DirectorySecurity();
                accessControl.AddAccessRule(new FileSystemAccessRule(identity, FileSystemRights.FullControl,
                    InheritanceFlags.ObjectInherit | InheritanceFlags.ContainerInherit, PropagationFlags.NoPropagateInherit,
                    AccessControlType.Allow));
                Directory.CreateDirectory(modsDirectory, accessControl);

                foreach (var entry in index.Entries)
                {
                    await DownloadModPackage(modsDirectory, msi, entry);
                }
            }
        }

        private async Task DownloadModPackage(string modDirectory, ModSystemInfo modSystemInfo, ModIndexEntry modIndexEntry)
        {
            var request = WebRequest.Create(new Url(modSystemInfo.BasePath).AppendPathSegment(modIndexEntry.Name).ToString());
            var response = await request.GetResponseAsync();
            using var responseStream = response.GetResponseStream() ?? throw new ModSystemException("Could not get mod package stream for: " + modIndexEntry.Name);
            using var fileStream = new FileStream(Path.Combine(modDirectory, modIndexEntry.Name), FileMode.Create, FileAccess.Write);
            var buffer = new byte[1048576];
            int bytesRead;
            while ((bytesRead = await responseStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
            {
                fileStream.Write(buffer, 0, bytesRead);
            }
        }
    }
}