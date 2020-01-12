using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Win32;
using NextGenLauncher.Data;

namespace NextGenLauncher
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        //protected override void OnStartup(StartupEventArgs e)
        //{
        //    base.OnStartup(e);
        //    InstallationWindow installationWindow = new InstallationWindow();
        //    installationWindow.Show();
        //}

        public App()
        {
            if (this.Dispatcher == null)
            {
                throw new Exception("Null dispatcher. We've got a problem");
            }

            this.Dispatcher.UnhandledException += (sender, args) =>
            {
                MessageBox.Show(args.Exception.Message, "Unhandled exception", MessageBoxButton.OK,
                    MessageBoxImage.Error);
                args.Handled = true;
                File.AppendAllLines("error.log", new[]
                {
                    $"{DateTimeOffset.Now:s} - {args.Exception.Message} ({args.Exception.GetType()})",
                    args.Exception.StackTrace,
                    $"\tInner exception: {args.Exception.InnerException?.Message ?? "(none)"} {args.Exception.InnerException?.GetType()?.ToString() ?? string.Empty}"
                });
                Environment.Exit(1);
            };
        }

        private void App_OnStartup(object sender, StartupEventArgs e)
        {
            bool shouldShowInstall = false;

            if (!File.Exists("settings.json"))
                shouldShowInstall = true;
            else
            {
                string regLoc = (string) Registry.GetValue(@"HKEY_LOCAL_MACHINE\software\SoapDev\Soapbox Race World",
                    "GameInstallDir", string.Empty);

                if (!Directory.Exists(regLoc))
                {
                    shouldShowInstall = true;
                }
            }

            if (shouldShowInstall)
            {
                InstallationWindow iw = new InstallationWindow();
                iw.Show();
            }
            else
            {
                MainWindow mw = new MainWindow();
                mw.Show();
            }
        }
    }
}
