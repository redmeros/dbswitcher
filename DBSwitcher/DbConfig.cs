using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Xml;
using Newtonsoft.Json;

namespace DBSwitcher
{
    public class DbConfig
    {
        private readonly NLog.Logger log = NLog.LogManager.GetCurrentClassLogger();

        #region Public Fields

        /// <summary>
        /// Zawiera listę niezbędnych baz danych
        /// </summary>
        public static readonly string[] RequiredDataSources = new string[]
        {
            "AstorRules",
            "AstorDetails",
            "AstorBase",
            "AstorSettings",
            "AstorControlStructure",
            "AstorBitmaps",
            "AstorProject",
            "AstorDetailsBase",
            "AstorKernelEnvironment",
            "AstorCurrentAddIn",
            "AstorJointsCalculation",
            "AstorGratings",
            "AstorProfiles",
            "GTCMapping",
            "AstorDatabase"
        };

        /// <summary>
        /// W tej zmiennej trzymane są datasourcy
        /// </summary>
        public List<DbDataSource> DataSources = new List<DbDataSource>();

        #endregion Public Fields

        #region Private Constructors

        private DbConfig()
        {
        }

        #endregion Private Constructors

        #region Public Properties

        /// <summary>
        /// To jest ścieżka do katalogu np: C:\ProgramData\Autodesk\Advance Steel 2019\POL
        /// </summary>
        public string AdvanceSteelConfigDir { get; set; }

        /// <summary>
        /// to jest ścieżka do pliku z konfiguracją bazy danych
        /// </summary>
        public string ConfigFileName => Path.Combine(this.AdvanceSteelConfigDir, "Configuration", "DatabaseConfiguration.xml");

        /// <summary>
        /// Zwrata true jeśli katalog support jest dowiazaniem symbolicznym
        /// </summary>
        public bool SupportDirIsLink
        {
            get;
            set;
        }

        //private readonly string _supportLinkDirTarget;
        /// <summary>
        /// To jest lokalizacja "targetu" mklinka dla katalogu support
        /// </summary>
        private string _supportDirLink;

        public string SupportDirLink
        {
            get => _supportDirLink;
            set
            {
                _supportDirLink = value;
                _supportDirLink = _supportDirLink.TrimEnd('\\');
                if (_supportDirLink.StartsWith("UNC"))
                {
                    _supportDirLink = _supportDirLink.Replace("UNC\\", "\\\\");
                }
            }
        }

        /// <summary>
        /// To jest lokalizacja katalogu support normalnie znajduje się tu
        /// C:\ProgramData\Autodesk\Advance Steel 2019\POL\Shared\Support
        /// </summary>
        public string SupportPath => Path.Combine(this.AdvanceSteelConfigDir, "Shared", "Support");

        #endregion Public Properties

        #region Public Methods

        public static DbConfig Deserialize(string inputJson)
        {
            var config = JsonConvert.DeserializeObject<DbConfig>(inputJson);
            return config;
        }

        /// <summary>
        /// Odczytuje aktualną konfigurację bazy danych z pliku DatabaseConfiguration.xml
        /// </summary>
        /// <param name="advanceSteelConfigDir"></param>
        /// <returns></returns>
        public static DbConfig ReadCurrent(string advanceSteelConfigDir = "")
        {
            if (advanceSteelConfigDir == "")
            {
                advanceSteelConfigDir = "C:\\ProgramData\\Autodesk\\Advance Steel 2019\\POL";
            }

            var cfg = new DbConfig
            {
                AdvanceSteelConfigDir = advanceSteelConfigDir,
            };
            var nativePath = NativeMethods.GetFinalPathName(cfg.SupportPath);
            cfg.SupportDirLink = Path.Combine(nativePath.Substring(4));

            FileInfo pathInfo = new FileInfo(cfg.SupportPath);
            cfg.SupportDirIsLink = pathInfo.Attributes.HasFlag(FileAttributes.ReparsePoint);

            try
            {
                cfg.ReadDataSources(cfg.ConfigFileName);
            }
            catch (DirectoryNotFoundException ex)
            {
                Console.WriteLine(ex);
                //throw ex;
            }

            return cfg;
        }

        /// <summary>
        /// Wczytuje aktualnie używany plik na podstawie ConfigFileName
        /// </summary>
        public List<DbDataSource> ReadDataSources(string configFilePath = "")
        {
            XmlDocument doc = new XmlDocument();
            doc.Load(ConfigFileName);
            DataSources = ReadDataSources(doc);
            return DataSources;
        }

        /// <summary>
        /// Z dokumentu xml odczytuje datasourcy
        /// </summary>
        /// <param name="doc"></param>
        /// <returns></returns>
        public List<DbDataSource> ReadDataSources(XmlDocument doc)
        {
            var _datasources = new List<DbDataSource>();
            var nodes = doc.DocumentElement.SelectNodes("/AdvanceSteel/DataSource");
            foreach (XmlNode node in nodes)
            {
                _datasources.Add(new DbDataSource
                {
                    Name = node.Attributes["Name"].Value,
                    Value = node.Attributes["Value"].Value
                });
            }
            return _datasources;
        }

        /// <summary>
        /// Serializuje ten obiekt do json'a
        /// </summary>
        /// <returns></returns>
        public string SerializeToJson()
        {
            string output = JsonConvert.SerializeObject(this, Newtonsoft.Json.Formatting.Indented);
            return output;
        }

        /// <summary>
        /// Wybraną konfigurację wprowadza w życie
        /// </summary>
        public void MakeCurrent()
        {
            log.Info("Rozpoczynam proces zmiany ustawień");
            log.Info("Rozpoczynam backup pliku z konfiguracją bazy danych");
            var newConfigFileName = Path.ChangeExtension(ConfigFileName, DateTime.Now.ToString("yyyy-MM-dd_HHmmss.x\\m\\l"));
            log.Info(string.Format("Backup pliku konfiguracyjnego będzie znajdował się tutaj: {0}", newConfigFileName));
            try
            {
                File.Copy(ConfigFileName, newConfigFileName, false);
            }
            catch (Exception e)
            {
                MessageBox.Show("Błąd podczas kopiowania pliku - przerywam");
                log.Warn("Brak możliwości skopiowania pliku:");
                log.Warn(e.Message);
                log.Warn("Przerywam");
                return;
            }

            log.Info("Stworzono plik backupu z konfiguracją");
            log.Info("Usuwam plik konfiguracyjny");
            try
            {
                File.Delete(ConfigFileName);
            }
            catch (Exception e)
            {
                MessageBox.Show("Błąd podczas usuwania starego pliku konfiguracyjnego");
                log.Warn("Błąd podczas usuwania starego pliku konfiguracyjnego");
                log.Warn(e.Message);
                log.Warn("Przerywam");
                return;
            }

            log.Info("Sprawdzam czy wymagany jest backup dla katalogu support");
            FileInfo pathInfo = new FileInfo(SupportPath);
            var supportDirIsLink = pathInfo.Attributes.HasFlag(FileAttributes.ReparsePoint);

            log.Info("Rozpoczynam tworzenie dokumentu xml");

            var doc = new XmlDocument();
            XmlDeclaration declaration = doc.CreateXmlDeclaration("1.0", "UTF-8", null);
            XmlElement root = doc.DocumentElement;
            doc.InsertBefore(declaration, root);

            var steelEl = doc.CreateElement("AdvanceSteel");
            doc.AppendChild(steelEl);

            foreach (var ds in DataSources)
            {
                var dselement = doc.CreateElement("DataSource");
                var valAtt = doc.CreateAttribute("Value");
                valAtt.Value = ds.Value;
                var nameAtt = doc.CreateAttribute("Name");
                nameAtt.Value = ds.Name;

                dselement.Attributes.Append(valAtt);
                dselement.Attributes.Append(nameAtt);

                steelEl.AppendChild(dselement);
            }

            // to jest bez bom
            var encoding = new System.Text.UTF8Encoding(false);
            try
            {
                using (StreamWriter sw = new StreamWriter(File.Open(ConfigFileName, FileMode.Create), encoding))
                {
                    doc.Save(sw);
                }
            }
            catch (Exception e)
            {
                log.Warn("Brak możliwości zapisu pliku konfiguracyjnego - przerywam");
                log.Warn(e.Message);
                MessageBox.Show("Brak możliwości zapisu pliku konfiguracyjnego - przerywam");
                return;
            }
            log.Info(string.Format("Zapisano plik konfiguracyjny w: {0}", ConfigFileName));

            log.Info("Rozpoczynam operacje dla katalogu support");
            log.Info("Backup katalogu support");
            if (supportDirIsLink)
            {
                log.Info("Docelowy katalog jest dowiązaniem symbolicznym");
                log.Info("Brak potrzeby backupu - nastąpi próba usunięcia");
                try
                {
                    Directory.Delete(SupportPath);
                }
                catch (Exception e)
                {
                    log.Warn(string.Format("Brak możliwości usunięcia katalogu support ze ścieżki {0}", SupportPath));
                    log.Warn(e.Message);
                    return;
                }
            }
            else
            {
                try
                {
                    Directory.Move(SupportPath, SupportPath + ".bak");
                }
                catch (Exception e)
                {
                    log.Warn(string.Format("Brak możliwości zmiany nazwy katalug support ze ścieżki {0}", SupportPath));
                    log.Warn(e.Message);
                    return;
                }
            }
            log.Info("Zakończono backup katalogu support");

            log.Info("Rozpoczynam przywracanie katalogu support");
            if (SupportDirIsLink)
            {
                log.Info(string.Format("Tworzę symbolic link dla z {0} do {1}", SupportPath, SupportDirLink));
                if (!NativeMethods.CreateSymbolicLink(SupportPath, SupportDirLink, NativeMethods.SymbolicLink.Directory))
                {
                    log.Warn(string.Format("Próba stworzenia linku zakończyła się niepowodzeniem - przerywam"));
                    return;
                }
            }
            else
            {
                var bakdir = SupportPath + ".bak";
                log.Info(string.Format("Zmienam nazwę katalogu z: \n {0} \nna\n {1}", bakdir, SupportPath));
                try
                {
                    Directory.Move(SupportPath + ".bak", SupportPath);
                }
                catch (Exception e)
                {
                    log.Warn(string.Format("Brak możliwości zmiany nazwy katalogu - przerywam"));
                    log.Warn(e.Message);
                    return;
                }
            }
            log.Info("Proces przywracania ustawień zakończony");
        }

        #endregion Public Methods
    }

    public class DbDataSource
    {
        #region Public Properties

        public string Name { get; set; }
        public string Value { get; set; }

        #endregion Public Properties
    }
}