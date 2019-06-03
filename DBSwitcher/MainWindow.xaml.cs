using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;

namespace DBSwitcher
{
    public enum ASVersion
    {
        v2018 = 2018,
        v2019 = 2019
    }

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        #region Private Fields

        private NLog.Logger log = NLog.LogManager.GetCurrentClassLogger();
        public string AppVersion = Assembly.GetEntryAssembly().GetName().Version.ToString();

        private string _applicationPath;

        #endregion Private Fields

        #region Public Constructors

        /// <summary>
        /// Konstruktor
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();
            this.Title = "ASDbSwitcher - " + this.AppVersion;

            _applicationPath = AppDomain.CurrentDomain.BaseDirectory;
            WorkingDir = Path.Combine(_applicationPath, "configs");
            if (!Directory.Exists(WorkingDir))
            {
                Directory.CreateDirectory(WorkingDir);
            }
            else
            {
                log.Info(string.Format("Directory {0} already exists, not creating new one", WorkingDir));
            }
            log.Info(string.Format("App directory is {0}", _applicationPath));
            log.Info("AppStarted");

            ComposeUi();
            log.Info("Ui composed");
            this.Visibility = Visibility.Visible;
            this.Show();
        }

        ~MainWindow()
        {
            log.Info("Zamykam aplikację ... ");
            NLog.LogManager.Flush();
        }

        private void ComposeUi()
        {
            var vals = Enum.GetValues(typeof(ASVersion)).Cast<ASVersion>();
            foreach (var val in vals)
            {
                var ctrl = new DbSwitcherControl(val);
                zStackPanel.Children.Add(ctrl);
            }
        }

        public ASVersion CurrentVersion { get; set; } = ASVersion.v2019;

        #endregion Public Constructors

        #region Public Properties

        /// <summary>
        /// Katalog w ktorym sa ustawienia konfiguracyjne
        /// </summary>
        public string WorkingDir { get; set; }

        #endregion Public Properties

        #region Private Methods

        /// <summary>
        /// Zamyka okno główne
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CloseMemuItemClick(object sender, RoutedEventArgs e)
        {
            Close();
        }

        /// <summary>
        /// Otwiera w explorerze katalog z ustawieniami
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OpenConfigDirMenuItemClick(object sender, RoutedEventArgs e)
        {
            Process.Start(WorkingDir);
        }

        #endregion Private Methods
    }
}