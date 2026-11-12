using System;
using System.Windows;

namespace DBSwitcher
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private static readonly NLog.Logger log = NLog.LogManager.GetCurrentClassLogger();

        public App()
        {
            log.Info("App started main object created");
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            // hook on error before app really starts
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            log.Info("Log started properly");
            base.OnStartup(e);
            _ = Updater.CheckForUpdatesAsync();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            log.Info("Exiting app");
            base.OnExit(e);
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject is System.IO.FileNotFoundException fnf)
            {
                MessageBox.Show(fnf.FileName);
            }
            MessageBox.Show(e.ExceptionObject.ToString());
        }

        ~App()
        {
            log.Info("Zamykam aplikację");

            NLog.LogManager.Flush();
            NLog.LogManager.Shutdown();
        }
    }
}