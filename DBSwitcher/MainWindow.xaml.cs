using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;

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
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        #region Private Fields

        private NLog.Logger log = NLog.LogManager.GetCurrentClassLogger();
        public string AppVersion = Assembly.GetEntryAssembly().GetName().Version.ToString();

        /// <summary>
        /// To jest aktualny config załadowany na komputerze
        /// </summary>
        private DbConfig _currentConfig;

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

            CurrentVersion = ASVersion.v2019;
            foreach (var item in AsVersionMenu.Items.Cast<MenuItem>())
            {
                if (int.Parse(item.Tag.ToString()) == 2019)
                {
                    item.IsChecked = true;
                }
            }

            CurrentConfigReload();
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

        /// <summary>
        /// Wczytuje na nowo aktualnie zaladowany config
        /// </summary>
        public void CurrentConfigReload()
        {
            var dir = GetAdvanceSteelDir(CurrentVersion);
            _currentConfig = DbConfig.ReadCurrent(dir);
            log.Info(string.Format("Załadowano aktualną konfigurację z katalogu: {0}", dir));
        }

        #endregion Public Constructors

        #region Public Events

        /// <summary>
        /// Implementacja INotifyPropertyChanged
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        #endregion Public Events

        #region Public Properties

        /// <summary>
        /// To jest komunikat wyswietlany na podstawie IsDbConfigTheSameAsCurrent
        /// </summary>
        public string IsTheSameConfigAsCurrentText
        {
            get
            {
                if (IsDbConfigTheSameAsCurrent)
                {
                    return "To jest akutalnie używana konfiguracja";
                }
                else
                {
                    return "Wybrana konfiguracja jest inna niż aktualnie używana";
                }
            }
        }

        /// <summary>
        /// Zwraca true jesli aktualny konfig jest tozsamy z aktualnym configiem
        /// </summary>
        public bool IsDbConfigTheSameAsCurrent
        {
            get
            {
                if (_currentConfig == null || SelectedConfig == null)
                {
                    return false;
                }
                var _serializedCurrent = _currentConfig.SerializeToJson();
                var serialzedChosen = SelectedConfig.SerializeToJson();
                var res = _serializedCurrent == serialzedChosen;
                this.ActivateSettingsMenuItem.IsEnabled = !res;
                return res;
            }
        }

        /// <summary>
        /// ApplicationData dir for current system
        /// </summary>
        //public string AppDataDir { get; } = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

        /// <summary>
        /// Lista wejściowa konfiguracji zbudowana na podstawie ConfigFiles
        /// </summary>
        public List<ConfigEntry> ConfigEntries
        {
            get
            {
                var files = ConfigFiles;
                var entries = new List<ConfigEntry>
                {
                    new CurrentConfigEntry(),
                    //new AddNewConfigEntry(),
                    new SeparatorConfigEntry()
                };

                foreach (var file in files)
                {
                    entries.Add(
                            new FileConfigEntry()
                            {
                                FileFullPath = file
                            });
                }
                return entries;
            }
        }

        /// <summary>
        /// Lista plikow z rozszerzeniem *.config.json
        /// </summary>
        public List<string> ConfigFiles
        {
            get
            {
                var files = Directory.GetFiles(WorkingDir, "*.config.json").ToList();
                return files;
            }
        }

        /// <summary>
        /// Nazwa pliku aktualnie wybranego configu
        /// </summary>
        public string CurrentConfigFile { get; set; }

        /// <summary>
        /// Aktualnie wybrany przez uzytkownika config
        /// </summary>
        public DbConfig SelectedConfig { get; set; }

        /// <summary>
        /// Katalog w ktorym sa ustawienia konfiguracyjne
        /// </summary>
        public string WorkingDir { get; set; }

        #endregion Public Properties

        #region Private Methods

        /// <summary>
        /// Zmienia selekcje comboboxa i podczytuje nowa konfiguracje
        /// nastepnie `invokuje` propertychange interfejs
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count != 1)
            {
                e.Handled = true;
                return;
            }

            if (!(e.AddedItems[0] is ConfigEntry item))
            {
                e.Handled = true;
                return;
            }

            DbConfig dbconfig;
            if (item is CurrentConfigEntry)
            {
                dbconfig = DbConfig.ReadCurrent(GetAdvanceSteelDir(CurrentVersion));
                SelectedConfig = dbconfig;
            }
            else if (item is FileConfigEntry)
            {
                var entry = item as FileConfigEntry;
                var json = File.ReadAllText(entry.FileFullPath);
                dbconfig = DbConfig.Deserialize(json);
                SelectedConfig = dbconfig;
            }
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("SelectedConfig"));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("IsTheSameConfigAsCurrentText"));
        }

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

        /// <summary>
        /// Zapisuje wybrane ustawienia pod inną nazwą
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SaveDbConfigAsMenuClick(object sender, RoutedEventArgs e)
        {
            if (SelectedConfig == null)
            {
                MessageBox.Show("Najpierw wybierz ustawienia do zapisania");
                return;
            }
            var dialog = new SaveFileDialog();
            dialog.Filter = "Switcher config files (*.config.json)|*.config.json";
            dialog.InitialDirectory = WorkingDir;
            if (dialog.ShowDialog() == true)
            {
                var file = dialog.FileName;
                var json = SelectedConfig.SerializeToJson();

                File.WriteAllText(file, json);
            }
        }

        private void SteelVersionItemClick(object sender, RoutedEventArgs e)
        {
            var item = sender as MenuItem;
            foreach (var i in AsVersionMenu.Items)
            {
                var ii = i as MenuItem;
                ii.IsChecked = false;
            }

            var versionNo = int.Parse(item.Tag.ToString());

            CurrentVersion = (ASVersion)versionNo;

            CurrentConfigReload();
            item.IsChecked = true;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("SelectedConfig"));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("IsTheSameConfigAsCurrentText"));
        }

        /// <summary>
        /// Przełącza na wybrane ustawienia
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MakeCurrentMenuItemClick(object sender, RoutedEventArgs e)
        {
            if (SelectedConfig == null)
            {
                MessageBox.Show("Nie wybrałeś ustawień");
                return;
            }
            SelectedConfig.MakeCurrent();
            _currentConfig = DbConfig.ReadCurrent(GetAdvanceSteelDir(CurrentVersion));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("SelectedConfig"));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("IsTheSameConfigAsCurrentText"));
        }

        #endregion Private Methods
    }

    /// <summary>
    /// Klasa pomocnicza dla wyboru stylu dla separatora
    /// </summary>
    public class SelectOrNotSelectStyleSelector : StyleSelector
    {
        #region Public Methods

        public override Style SelectStyle(object item, DependencyObject container)
        {
            if (item is SeparatorConfigEntry)
            {
                Console.WriteLine("Wybrano separator");
                var style = new Style(typeof(ComboBoxItem));
                style.Setters.Add(new Setter(UIElement.IsHitTestVisibleProperty, false));
                return style;
            }
            return base.SelectStyle(item, container);
        }

        #endregion Public Methods
    }
}