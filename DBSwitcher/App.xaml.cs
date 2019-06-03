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
        private NLog.Logger log = NLog.LogManager.GetCurrentClassLogger();

        public App()
        {
            log.Info("App started main object created");
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            // hook on error before app really starts
            //AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            log.Info("Log started properly");
            base.OnStartup(e);
        }

        protected override void OnExit(ExitEventArgs e)
        {
            log.Info("Exiting app");
            base.OnExit(e);
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            MessageBox.Show(e.ExceptionObject.ToString());
        }

        ~App()
        {
            log.Info("Zamykam aplikację");

            NLog.LogManager.Flush();
            NLog.LogManager.Shutdown();
        }

        private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            log.Info("Został wywołany nieobsłużony wyjątek");
            log.Info(e.Exception.Message);
            e.Handled = true;
        }
    }
}