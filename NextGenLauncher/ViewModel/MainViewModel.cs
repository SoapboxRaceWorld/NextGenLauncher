using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using System.Windows;
using Flurl;
using Flurl.Http;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.CommandWpf;
using Microsoft.Win32;
using NextGenLauncher.Data;
using NextGenLauncher.Exceptions;
using NextGenLauncher.Messages;
using NextGenLauncher.Proxy;
using NextGenLauncher.Services;
using NextGenLauncher.Services.Servers;
using NextGenLauncher.Utils;

namespace NextGenLauncher.ViewModel
{
    /// <summary>
    /// This class contains properties that the main View can data bind to.
    /// <para>
    /// Use the <strong>mvvminpc</strong> snippet to add bindable properties to this ViewModel.
    /// </para>
    /// <para>
    /// You can also use Blend to data bind with the tool's support.
    /// </para>
    /// <para>
    /// See http://www.galasoft.ch/mvvm
    /// </para>
    /// </summary>
    public class MainViewModel : ViewModelBase
    {
        private readonly ServerService _serverService;
        private readonly ServerModService _serverModService;
        private Server _selectedServer;
        private string _email;
        private string _password;
        private bool _inAuthentication;
        private AuthenticationInfo _authenticationInfo;
        private bool _canPlay;

        /// <summary>
        /// The list of available servers
        /// </summary>
        public ObservableCollection<Server> Servers { get; }

        public Server SelectedServer
        {
            get => _selectedServer;
            set
            {
                _serverService.FetchServerInfo(value);
                Set(ref _selectedServer, value);
                LoginCommand.RaiseCanExecuteChanged();
            }
        }

        public string Email
        {
            get => _email;
            set
            {
                Set(ref _email, value);
                LoginCommand.RaiseCanExecuteChanged();
            }
        }

        public string Password
        {
            get => _password;
            set
            {
                Set(ref _password, value);
                LoginCommand.RaiseCanExecuteChanged();
            }
        }

        public bool InAuthentication
        {
            get => _inAuthentication;
            set => Set(ref _inAuthentication, value);
        }

        public RelayCommand LoginCommand { get; }

        public RelayCommand PlayCommand { get; }

        public ProgressState PlayProgress { get; }

        /// <summary>
        /// Initializes a new instance of the MainViewModel class.
        /// </summary>
        /// <param name="serverService">The server service</param>
        /// <param name="serverModService"></param>
        public MainViewModel(ServerService serverService, ServerModService serverModService)
        {
            _serverService = serverService;
            _serverModService = serverModService;

            // Start critical services
            ServerProxy.Instance.Start();

            // Setup
            Servers = new ObservableCollection<Server>();
            LoginCommand = new RelayCommand(ExecuteLoginCommand, CanExecuteLoginCommand);
            PlayCommand = new RelayCommand(ExecutePlayCommand, CanExecutePlayCommand);
            MessengerInstance.Register<ServerListUpdatedMessage>(this, HandleServerListUpdatedMessage);
            MessengerInstance.Register<AuthenticationInfoUpdatedMessage>(this, HandleAuthenticationInfoUpdatedMessage);

            // Data
            serverService.FetchServers();

            PlayProgress = new ProgressState { ProgressText = "Waiting..." };
            InAuthentication = true;
        }

        private bool CanExecutePlayCommand()
        {
            return _canPlay;
        }

        private async void ExecutePlayCommand()
        {
            _canPlay = false;
            PlayCommand.RaiseCanExecuteChanged();
            var gameDataPath = Path.Combine((string)Registry.GetValue(@"HKEY_LOCAL_MACHINE\software\SoapDev\Soapbox Race World",
                "GameInstallDir", ""), "Data");
            string fileName =
                Path.Combine(
                    gameDataPath, "nfsw.exe");
            if (!File.Exists(fileName))
            {
                throw new PlayException("Game executable does not exist: " + fileName);
            }

            // Download mods
            await InstallModSystem(gameDataPath);
            await DownloadServerMods(gameDataPath);
            LaunchGame(fileName);
        }

        private void LaunchGame(string fileName)
        {
            PlayProgress.IsIndeterminate = false;
            PlayProgress.ProgressValue = 100;
            PlayProgress.ProgressText = "Launching game...";

            Process process = new Process();
            process.StartInfo.FileName = fileName;

            if (_authenticationInfo == null)
            {
                throw new PlayException("PlayCommand somehow executed without authentication info.");
            }

            if (_selectedServer == null)
            {
                throw new PlayException("PlayCommand somehow executed without a selected server. Great.");
            }

            process.StartInfo.Arguments =
                $"US http://127.0.0.1:4080/nfsw/Engine.svc {_authenticationInfo.LoginToken} {_authenticationInfo.UserId}";

            // Set up handlers
            process.EnableRaisingEvents = true;
            process.Exited += HandleGameExited;

            // Send signal to proxy
            ServerProxy.Instance.SetCurrentServer(_selectedServer);

            if (!process.Start())
            {
                throw new PlayException("Failed to start game process");
            }

            int processorAffinity = 0;
            for (int i = 0; i < Math.Min(Math.Max(1, Environment.ProcessorCount), 8); i++)
            {
                processorAffinity |= 1 << i;
            }

            process.ProcessorAffinity = (IntPtr) processorAffinity;

            //process.ProcessorAffinity = (IntPtr)0b11;
        }

        private void HandleGameExited(object sender, EventArgs e)
        {
            Process process = (Process) sender;

            if (process.ExitCode != 0)
            {
                var betterCode = unchecked((uint) process.ExitCode);

                throw new PlayException($"Game exited abnormally. Exit code: {process.ExitCode} (0x{betterCode:X8}). Report this to an administrator!");
            }

            PlayProgress.ProgressText = "Game is done. Restart the launcher to play again.";
        }

        private async Task DownloadServerMods(string gameDataPath)
        {
            PlayProgress.ProgressText = "Downloading mods...";
            await _serverModService.DownloadServerModsAsync(SelectedServer, gameDataPath);
        }

        private async Task InstallModSystem(string gameDataPath)
        {
            PlayProgress.IsIndeterminate = true;
            PlayProgress.ProgressText = "Installing mod system...";
            await _serverModService.InstallModSystemAsync(gameDataPath);
        }

        private void HandleAuthenticationInfoUpdatedMessage(AuthenticationInfoUpdatedMessage obj)
        {
            InAuthentication = false;
            _authenticationInfo = obj.Info;
            _canPlay = true;
            PlayCommand.RaiseCanExecuteChanged();
        }

        private async void ExecuteLoginCommand()
        {
            try
            {
                AuthenticationInfo authenticationInfo = await _serverService.LogInAsync(SelectedServer, Email, Password);
                MessengerInstance.Send(new AuthenticationInfoUpdatedMessage(authenticationInfo));
            }
            catch (AuthenticationException e)
            {
                MessageBox.Show(e.Message, "Login error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool CanExecuteLoginCommand()
        {
            return SelectedServer != null && !string.IsNullOrWhiteSpace(Email) && !string.IsNullOrWhiteSpace(Password);
        }

        private void HandleServerListUpdatedMessage(ServerListUpdatedMessage message)
        {
            Servers.Clear();

            foreach (var server in message.NewList)
            {
                Servers.Add(server);
            }

            if (Servers.Count == 0)
            {
                throw new InvalidServerListException("Empty server list is not permitted.");
            }

            // set SelectedServer to first server if null
            SelectedServer ??= Servers[0];
        }
    }
}