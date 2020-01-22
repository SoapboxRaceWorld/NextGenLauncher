using Flurl;
using Flurl.Http;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.CommandWpf;
using Ionic.Zip;
using Microsoft.Win32;
using Microsoft.WindowsAPICodePack.Dialogs;
using Newtonsoft.Json;
using NextGenLauncher.Data;
using NextGenLauncher.Exceptions;
using NextGenLauncher.ViewModel.Installer;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Xml;

namespace NextGenLauncher.ViewModel
{
    public class InstallerViewModel : ViewModelBase
    {
        private string _installationDirectory;
        private bool _isInstallationHappening;
        private GameLanguageOption _selectedLanguageOption;
        private bool _canChangeOptions;

        private bool IsInstallationHappening
        {
            get => _isInstallationHappening;
            set
            {
                Set(ref _isInstallationHappening, value);
                CanChangeOptions = !value;
                InstallCommand.RaiseCanExecuteChanged();
                ChangeInstallationDirectoryCommand.RaiseCanExecuteChanged();
            }
        }

        public bool CanChangeOptions
        {
            get => _canChangeOptions;
            set => Set(ref _canChangeOptions, value);
        }

        public string InstallationDirectory
        {
            get => _installationDirectory;
            set
            {
                Set(ref _installationDirectory, value);
                InstallCommand.RaiseCanExecuteChanged();
            }
        }

        public RelayCommand ChangeInstallationDirectoryCommand { get; }

        public List<GameLanguageOption> LanguageOptions { get; }

        public GameLanguageOption SelectedLanguageOption
        {
            get => _selectedLanguageOption;
            set => Set(ref _selectedLanguageOption, value);
        }

        public ProgressState InstallProgress { get; }

        public RelayCommand InstallCommand { get; }

        public InstallerViewModel()
        {
            ChangeInstallationDirectoryCommand = new RelayCommand(ExecuteChangeInstallationDirectoryCommand, () => !IsInstallationHappening);
            InstallCommand = new RelayCommand(ExecuteInstallCommand, () => !string.IsNullOrWhiteSpace(InstallationDirectory) && !IsInstallationHappening);
            InstallationDirectory =
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                    @"SoapDev\Soapbox Race World");
            InstallProgress = new ProgressState { ProgressText = "Waiting..." };
            LanguageOptions = new List<GameLanguageOption>
            {
                new GameLanguageOption(GameLanguageEnum.English, "English", "en", "EN"),
                new GameLanguageOption(GameLanguageEnum.Spanish, "Spanish (Español)", "es", "ES"),
                new GameLanguageOption(GameLanguageEnum.German, "German (Deutsch)", "de", "DE"),
                new GameLanguageOption(GameLanguageEnum.Russian, "Russian (русский)", "ru", "RU"),
                new GameLanguageOption(GameLanguageEnum.Portuguese, "Portuguese (Português) (Brasil)", "en", "PT"),
                new GameLanguageOption(GameLanguageEnum.French, "French (Français)", "en", "FR"),
                new GameLanguageOption(GameLanguageEnum.SimplifiedChinese, "Simplified Chinese (简体中文)", "en", "SC"),
                new GameLanguageOption(GameLanguageEnum.TraditionalChinese, "Traditional Chinese (繁體中文)", "en", "TC"),
                new GameLanguageOption(GameLanguageEnum.Polish, "Polski", "en", "PL"),
            };
            SelectedLanguageOption = LanguageOptions[0];
            IsInstallationHappening = false;
        }

        private void ExecuteChangeInstallationDirectoryCommand()
        {
            CommonOpenFileDialog folderBrowser = new CommonOpenFileDialog();
            folderBrowser.Title = "Select game folder";
            folderBrowser.AllowNonFileSystemItems = false;
            folderBrowser.EnsurePathExists = true;
            folderBrowser.EnsureReadOnly = false;
            folderBrowser.EnsureValidNames = true;
            folderBrowser.Multiselect = false;
            folderBrowser.IsFolderPicker = true;
            folderBrowser.InitialDirectory = InstallationDirectory;

            if (folderBrowser.ShowDialog() == CommonFileDialogResult.Ok)
            {
                InstallationDirectory = folderBrowser.FileName;
            }
        }

        private async void ExecuteInstallCommand()
        {
            IsInstallationHappening = true;

            DirectoryInfo file = new DirectoryInfo(InstallationDirectory);
            DriveInfo drive = new DriveInfo(file.Root.FullName);

            // some validation

            if (!drive.IsReady)
            {
                throw new InstallerException("drive.IsReady returned false!");
            }

            // 8GB requirement
            if (drive.AvailableFreeSpace < 8589934592)
            {
                throw new InstallerException("At least 8 GB of space must be available on drive: " + drive.Name);
            }

            if (!string.Equals(drive.DriveFormat, "NTFS", StringComparison.InvariantCulture))
            {
                throw new InstallerException("Only NTFS drives are supported. DriveFormat=" + drive.DriveFormat);
            }

            if (Directory.Exists(InstallationDirectory))
            {
                Directory.Delete(InstallationDirectory, true);
            }

            SecurityIdentifier identity = new SecurityIdentifier(WellKnownSidType.WorldSid, null);
            DirectorySecurity accessControl = new DirectorySecurity();
            accessControl.AddAccessRule(new FileSystemAccessRule(identity, FileSystemRights.FullControl,
                InheritanceFlags.ObjectInherit | InheritanceFlags.ContainerInherit, PropagationFlags.NoPropagateInherit,
                AccessControlType.Allow));
            Directory.CreateDirectory(InstallationDirectory, accessControl);

            await Task.Run(() =>
            {
                IProgress<DownloadState> progress = new Progress<DownloadState>(HandleDownloadProgress);

                DownloadGameFiles(progress);

                InstallProgress.ProgressValue = 0;
                InstallProgress.ProgressText = "";
                InstallProgress.IsIndeterminate = true;

                CreateFirewallRules();
                CreateRegistryData();
                UpdateUserSettings();
            });

            InstallProgress.ProgressValue = 0;
            InstallProgress.IsIndeterminate = false;
            InstallProgress.ProgressText = "Done! Launcher will restart in 3 seconds.";

            await Task.Delay(3000);
            Process.Start(Application.ResourceAssembly.Location);
            Application.Current.Shutdown();
        }

        private void DownloadGameFiles(IProgress<DownloadState> progress)
        {
            string[] packages = new[] { "game_core", "game_trackshigh", $"langpack_{SelectedLanguageOption.PackageKey}" };

            foreach (var packageId in packages)
            {
                var response = new Url($"https://cdn.soapboxrace.world/game/{packageId}.zip").SendAsync(
                    HttpMethod.Get, null, completionOption: HttpCompletionOption.ResponseHeadersRead).Result;
                var dataStream = response.Content.ReadAsStreamAsync().Result;

                var tmpFile = Path.GetTempFileName();
                using (FileStream fileStream = new FileStream(tmpFile, FileMode.Create, FileAccess.Write))
                {
                    var dataStreamLength = response.Content.Headers.ContentLength ?? throw new Exception("Could not get response with Content-Length!");
                    DownloadState state = new DownloadState { BytesToGet = dataStreamLength, Message = $"Downloading package: {packageId}" };
                    byte[] buffer = new byte[1048576];
                    int bytesRead;
                    while ((bytesRead = dataStream.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        fileStream.Write(buffer, 0, bytesRead);
                        state.BytesRead = fileStream.Length;
                        progress.Report(state);
                    }
                }

                using (ZipFile zf = new ZipFile(tmpFile))
                {
                    zf.ExtractProgress += (o, args) =>
                    {
                        if (args.EventType == ZipProgressEventType.Extracting_EntryBytesWritten)
                        {
                            progress.Report(new DownloadState
                            {
                                Message = $"Extracting... {args.CurrentEntry.FileName}",
                                BytesRead = args.BytesTransferred,
                                BytesToGet = args.TotalBytesToTransfer
                            });
                        }
                    };
                    zf.ExtractAll(Path.Combine(InstallationDirectory, "Data"), ExtractExistingFileAction.OverwriteSilently);
                }

                File.Delete(tmpFile);
            }
        }

        private void HandleDownloadProgress(DownloadState dls)
        {
            if (dls.BytesToGet > 0)
                InstallProgress.ProgressValue = (float)dls.BytesRead / dls.BytesToGet * 100;
            InstallProgress.ProgressText = dls.Message;
        }

        private void CreateRegistryData()
        {
            InstallProgress.ProgressText = "Creating registry key...";
            Registry.SetValue(@"HKEY_LOCAL_MACHINE\software\SoapDev\Soapbox Race World", "GameInstallDir", InstallationDirectory);
        }

        private void CreateFirewallRules()
        {
            InstallProgress.ProgressText = "Updating firewall...";
            // netsh advfirewall firewall add rule name="My Application" dir=in action=allow program="C:\MyApp\MyApp.exe" enable=yes
            CreateFirewallRule("in");
            CreateFirewallRule("out");
        }

        private void UpdateUserSettings()
        {
            InstallProgress.ProgressText = "Updating settings...";
            string gameAppDataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Need for Speed World");
            Directory.CreateDirectory(gameAppDataFolder);

            var settingsPath = Path.Combine(gameAppDataFolder, "Settings", "UserSettings.xml");
            File.WriteAllText(settingsPath, File.ReadAllText(@"Resources\UserSettings.xml", Encoding.UTF8), Encoding.UTF8);

            XmlDocument xmlDocument = new XmlDocument();
            xmlDocument.Load(settingsPath);
            xmlDocument["Settings"]["UI"]["Language"].InnerText = SelectedLanguageOption.GameKey;
            xmlDocument.Save(settingsPath);

            File.WriteAllText("settings.json", JsonConvert.SerializeObject(new Settings()));
        }

        private void CreateFirewallRule(string direction)
        {
            string command =
                $"advfirewall firewall add rule " +
                $"name=\"Soapbox Race World (NFSW)\" " +
                $"dir={direction} " +
                $"action=allow " +
                $"program=\"{Path.Combine(InstallationDirectory, "Data", "nfsw.exe")}\" " +
                $"enable=yes ";
            if (direction == "in")
                command += "edge=yes";
            Process process = Process.Start("netsh.exe", command);

            if (process == null)
            {
                throw new InstallerException("Failed to start netsh.exe (command: netsh.exe " + command + ")");
            }

            process.EnableRaisingEvents = true;
            process.Exited += (sender, args) =>
            {
                if (process.ExitCode != 0)
                {
                    throw new InstallerException("netsh.exe exited with code " + process.ExitCode + " - attempted to execute: netsh.exe " + command);
                }
            };
        }

        private struct DownloadState
        {
            public long BytesRead;
            public long BytesToGet;
            public string Message;
        }
    }
}