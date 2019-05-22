using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace DBSwitcher
{
    /// <summary>
    /// Interaction logic for DbConfigControl.xaml
    /// </summary>
    public partial class DbConfigControl : UserControl
    {
        #region Public Fields

        private NLog.Logger log = NLog.LogManager.GetCurrentClassLogger();

        public static readonly DependencyProperty DbConfigProperty = DependencyProperty.Register(
            "DbConfig",
            typeof(DbConfig),
            typeof(DbConfigControl),
            new PropertyMetadata(new PropertyChangedCallback(OnDbConfigChanged))
            );

        public event EventHandler DbConfigChanged;

        #endregion Public Fields

        #region Public Constructors

        public DbConfigControl()
        {
            InitializeComponent();
            DbConfigChanged += this.OnDbConfigChanged;
            DataContext = DbConfig;
            log.Info(string.Format("DbConfigControl loaded"));
        }

        protected void CreateDataSourcesUi(List<DbDataSource> datasources)
        {
            Console.WriteLine();
            DataSourcesStackPanel.Children.Clear();
            foreach (var ds in datasources)
            {
                StackPanel panel = new StackPanel
                {
                    Orientation = Orientation.Horizontal
                };
                TextBlock txt1 = new TextBlock
                {
                    Text = ds.Name,
                    Margin = new Thickness(10, 0, 10, 0),
                };
                TextBlock txt2 = new TextBlock()
                {
                    Text = ds.Value,
                    Margin = new Thickness(10, 0, 10, 0)
                };

                panel.Children.Add(txt1);
                panel.Children.Add(txt2);

                DataSourcesStackPanel.Children.Add(panel);
            }
        }

        private StackPanel CreateLine(string label, string value)
        {
            var margin = new Thickness(10, 0, 10, 0);
            var txt1 = new TextBlock()
            {
                Text = label,
                Margin = margin
            };
            var txt2 = new TextBlock()
            {
                Text = value,
                Margin = margin
            };
            var panel = new StackPanel()
            {
                Orientation = Orientation.Horizontal
            };
            panel.Children.Add(txt1);
            panel.Children.Add(txt2);
            return panel;
        }

        protected void CreateDirsUi(DbConfig config)
        {
            var line = CreateLine("Katalog z ustawieniami AS", DbConfig.AdvanceSteelConfigDir);
            var line2 = CreateLine("Plik z ustawieniami baz danych", DbConfig.ConfigFileName);
            var line3 = CreateLine("Katalog support jest dowiązany", DbConfig.SupportDirIsLink.ToString());
            var line4 = CreateLine("Lokalizacja katalog support", DbConfig.SupportPath);
            var line5 = CreateLine("Lokalizacja dowiązania support", DbConfig.SupportDirLink);

            DirStackPanel.Children.Clear();
            DirStackPanel.Children.Add(line);
            DirStackPanel.Children.Add(line2);
            DirStackPanel.Children.Add(line3);
            DirStackPanel.Children.Add(line4);
            DirStackPanel.Children.Add(line5);
        }

        protected void OnDbConfigChanged(object sender, EventArgs e)
        {
            log.Info("OnDbConfigChanged");
            CreateDataSourcesUi(this.DbConfig.DataSources);
            CreateDirsUi(this.DbConfig);
        }

        #endregion Public Constructors

        #region Public Properties

        public DbConfig DbConfig
        {
            get => GetValue(DbConfigProperty) as DbConfig;
            set => SetValue(DbConfigProperty, value);
        }

        #endregion Public Properties

        #region Private Methods

        private static void OnDbConfigChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var ctrl = d as DbConfigControl;
            if ((e.NewValue as DbConfig) != null)
            {
                ctrl.DataContext = e.NewValue;
                ctrl.DbConfigChanged(ctrl, new EventArgs());
            }
            else
            {
                ctrl.DataContext = null;
                ctrl.DbConfigChanged(ctrl, new EventArgs());
            }
        }

        #endregion Private Methods
    }
}