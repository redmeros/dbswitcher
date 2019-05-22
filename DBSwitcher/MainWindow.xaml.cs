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
            log.Warn("AppStarted");

            this.ComposeUi();
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

        public string GetAdvanceSteelDir(ASVersion version)
        {
            string res = "";
            switch (version)
            {
                case ASVersion.v2018:
                    res = "C:\\ProgramData\\Autodesk\\Advance Steel 2018\\POL";
                    break;

                case ASVersion.v2019:
                    res = "C:\\ProgramData\\Autodesk\\Advance Steel 2019\\POL";
                    break;

                default:
                    throw (new ArgumentException("Nie mogę rozpoznać wersji steela"));
            }
            return res;
        }

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
            this.Close();
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