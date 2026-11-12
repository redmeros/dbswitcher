using System;
using System.Windows;
using System.Windows.Threading;

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
            DispatcherUnhandledException += App_DispatcherUnhandledException;
            log.Info("Log started properly");
            base.OnStartup(e);
            _ = Updater.CheckForUpdatesAsync();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            log.Info("Exiting app");
            NLog.LogManager.Flush();
            NLog.LogManager.Shutdown();
            base.OnExit(e);
        }

        private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            log.Error(e.Exception, "Nieobsłużony wyjątek w wątku interfejsu");
            MessageBox.Show(e.Exception.Message, "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
            e.Handled = true;
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject is System.IO.FileNotFoundException fnf)
            {
                MessageBox.Show(fnf.FileName);
            }
            MessageBox.Show(e.ExceptionObject.ToString());
        }
    }
}