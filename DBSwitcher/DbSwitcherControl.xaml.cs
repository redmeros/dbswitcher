using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace DBSwitcher
{
    /// <summary>
    /// Interaction logic for DbSwitcherControl.xaml
    /// </summary>
    public partial class DbSwitcherControl : UserControl
    {
        #region Private Fields

        //private static readonly NLog.Logger log = NLog.LogManager.GetCurrentClassLogger();

        #endregion Private Fields

        #region Public Constructors

        public DbSwitcherControl(ASVersion version)
        {
            InitializeComponent();

            ConfigChanged += SetBtnStyles;

            Version = version;
            WorkingDir = AppDomain.CurrentDomain.BaseDirectory;

            ConfigEntries = ZConfigEntry.GetEntries(ConfigDir, Version);

            ComposeUI();
        }

        #endregion Public Constructors

        #region Public Events

        public event EventHandler ConfigChanged;

        #endregion Public Events

        #region Public Properties

        public List<ZConfigEntry> ConfigEntries { get; set; }
        public ASVersion Version { get; set; }

        #endregion Public Properties

        #region Private Properties

        private string ConfigDir => Path.Combine(WorkingDir, "configs");
        private string WorkingDir { get; set; }

        #endregion Private Properties

        #region Public Methods

        public static string VersionString(ASVersion version)
        {
            if ((int)version < 3000)
            {
                return "Advance Steel " + ((int)version).ToString();
            } else
            {
                return "Revit " + ((int)version - 90000).ToString();
            }
        }

        public void ComposeUI()
        {
            var txt = new TextBlock()
            {
                Text = VersionString(Version),
                FontSize = 16,
                FontWeight = FontWeights.SemiBold
            };

            MainGroupbox.Header = txt;

            foreach (var entry in ConfigEntries)
            {
                var btn = CreateBtn(entry);
                MainWrapPanel.Children.Add(btn);
            }

            SetBtnStyles(this, new EventArgs());
        }

        public void SetBtnStyles(object sender, EventArgs e)
        {
            foreach (var item in MainWrapPanel.Children)
            {
                if (!(item is Button btn))
                {
                    continue;
                }

                if (!(btn.Tag is ZConfigEntry entry))
                {
                    btn.IsEnabled = false;
                    continue;
                }
                if (Utils.IsRevit(entry.Version))
                {
                    Console.WriteLine("");
                }
                if (entry.Config.DisabledVersion)
                {
                    btn.IsEnabled = false;
                    continue;
                }

                if (!entry.Config.IsValid())
                {
                    btn.IsEnabled = false;
                    continue;
                }

                if (btn.Tag == null)
                {
                    btn.IsEnabled = false;
                    continue;
                }
                var res = entry.Config.IsCurrent();
                var ver = entry.Config.Version;
                var ever = entry.Version;

                if (entry.Config.IsCurrent())
                {
                    btn.Background = Brushes.LightBlue;
                }
                else
                {
                    btn.Background = SystemColors.ControlBrush;
                }
            }
        }

        #endregion Public Methods

        #region Private Methods

        private Button CreateBtn(ZConfigEntry entry)
        {
            var box = new TextBlock()
            {
                Text = entry.Config.Name,
                TextWrapping = TextWrapping.Wrap,
                TextAlignment = TextAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            var btn = new Button
            {
                Tag = entry,
                Content = box,
                Height = 50,
                Width = 120,
                Margin = new Thickness(10)
            };
            btn.Click += SettingButtonClick;
            return btn;
        }

        private void SettingButtonClick(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            var entry = btn.Tag as ZConfigEntry;
            try
            {
                entry.Config.MakeCurrent();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                MessageBox.Show("Operacja przywracania ustawień zakończona");
            }
            ConfigChanged(sender, e);
        }

        #endregion Private Methods
    }

    public class ZConfigEntry
    {
        #region Public Properties

        public ZDbConfig Config { get; set; }
        public string FileName { get; set; }
        public string Name { get; set; }
        public ASVersion Version { get; set; }

        #endregion Public Properties

        #region Public Methods

        private static readonly NLog.ILogger log = NLog.LogManager.GetCurrentClassLogger();

        public static List<ZConfigEntry> GetEntries(string dirpath, ASVersion version)
        {


            var files = Directory.GetFiles(dirpath);
            string pattern;
            string verstr;

            if ((int)version < 3000)
            {
                verstr = "AS" + ((int)version).ToString();
                pattern = "(?<asversion>" + verstr + ")_(?<name>[A-Za-z_-]+).config.json$";
            }
            else
            {
                verstr = "RVT" + ((int)version - 90000).ToString();
                pattern = "(?<asversion>" + verstr + ")_(?<name>[A-Za-z_-]+).config.json$";
            }

            var result = new List<ZConfigEntry>();
            foreach (var filename in files)
            {
                var match = Regex.Match(filename, pattern);
                if (match.Success)
                {
                    try
                    {
                        var jsonstr = File.ReadAllText(filename);
                        ZConfigEntry entry = new ZConfigEntry
                        {
                            Version = version,
                            FileName = filename,
                            Name = Path.GetFileNameWithoutExtension(filename),
                            Config = ZDbConfig.Deserialize(jsonstr)
                        };
                        result.Add(entry);
                    } catch (Exception ex)
                    {
                        log.Error(ex, $"Blad podczas odczytywania json'a z pliku {filename}");
                        throw ex;
                    }
                }
            }

            return result;
        }

        #endregion Public Methods
    }
}