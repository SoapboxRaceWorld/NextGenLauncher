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
        private Server _selectedServer;
        private string _email;
        private string _password;
        private bool _inAuthentication;
        private AuthenticationInfo _authenticationInfo;
        private bool _gameRunning;

        /// <summary>
        /// The list of available servers
        /// </summary>
        public ObservableCollection<Server> Servers { get; }

        public Server SelectedServer
        {
            get => _selectedServer;
            set
            {
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

        /// <summary>
        /// Initializes a new instance of the MainViewModel class.
        /// </summary>
        /// <param name="serverService">The server service</param>
        public MainViewModel(ServerService serverService)
        {
            _serverService = serverService;

            // Setup
            Servers = new ObservableCollection<Server>();
            LoginCommand = new RelayCommand(ExecuteLoginCommand, CanExecuteLoginCommand);
            PlayCommand = new RelayCommand(ExecutePlayCommand, CanExecutePlayCommand);
            MessengerInstance.Register<ServerListUpdatedMessage>(this, HandleServerListUpdatedMessage);
            MessengerInstance.Register<AuthenticationInfoUpdatedMessage>(this, HandleAuthenticationInfoUpdatedMessage);

            // Data
            serverService.FetchServers();

            InAuthentication = true;
        }

        private bool CanExecutePlayCommand()
        {
            return !_gameRunning;
        }

        private void ExecutePlayCommand()
        {
            Process process = new Process();
            process.StartInfo.FileName =
                Path.Combine(
                    (string) Registry.GetValue(@"HKEY_LOCAL_MACHINE\software\SoapDev\Soapbox Race World",
                        "GameInstallDir", ""), "Data", "nfsw.exe");
            if (!File.Exists(process.StartInfo.FileName))
            {
                throw new PlayException("Game executable does not exist: " + process.StartInfo.FileName);
            }

            if (_authenticationInfo == null)
            {
                throw new PlayException("PlayCommand somehow executed without authentication info.");
            }

            if (_selectedServer == null)
            {
                throw new PlayException("PlayCommand somehow executed without a selected server. Great.");
            }

            process.StartInfo.Arguments = $"US {_selectedServer.ServerAddress} {_authenticationInfo.LoginToken} {_authenticationInfo.UserId}";

            if (!process.Start())
            {
                throw new PlayException("Failed to start game process");
            }

            _gameRunning = true;
            PlayCommand.RaiseCanExecuteChanged();
        }

        private void HandleAuthenticationInfoUpdatedMessage(AuthenticationInfoUpdatedMessage obj)
        {
            InAuthentication = false;
            _authenticationInfo = obj.Info;
        }

        private async void ExecuteLoginCommand()
        {
            try
            {
                AuthenticationInfo authenticationInfo = await _serverService.LogIn(SelectedServer, Email, Password);
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