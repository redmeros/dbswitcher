using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Xml;
using Newtonsoft.Json;

namespace DBSwitcher
{
    public class PathBuilder
    {
        #region Private Fields

        /// <summary>
        /// Wersja AS dla której budowane są ścieżki
        /// </summary>
        private readonly ASVersion Version;

        #endregion Private Fields

        #region Public Constructors

        /// <summary>
        /// Konstruktor wymaga wersji AS dla ktorej beda budowoane sciezki
        /// </summary>
        /// <param name="version"></param>
        public PathBuilder(ASVersion version)
        {
            Version = version;
        }

        #endregion Public Constructors

        #region Public Properties

        /// <summary>
        /// Normalnie to jest
        /// C:\ProgramData\Autodesk\Advance Steel 2019\POL
        ///
        /// </summary>
        public string ASPath => Path.Combine(AutodeskPath, "Advance Steel " + ((int)Version).ToString(), Properties.Settings.Default.ASLanguagePrefix);

        /// <summary>
        /// normalnie to jest:
        /// C:\ProgramData\Autodesk
        /// </summary>
        public string AutodeskPath => Path.Combine(ProgramDataPath, "Autodesk");

        /// <summary>
        /// C:\ProgramData\Autodesk\Advance Steel 2019\POL\Configuration\DatabaseConfiguration.xml
        /// </summary>
        public string ConfigPath => Path.Combine(ASPath, "Configuration", "DatabaseConfiguration.xml");

        /// <summary>
        /// C:\ProgramData
        /// </summary>
        public string ProgramDataPath => Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);

        /// <summary>
        /// C:\ProgramData\Autodesk\Advance Steel 2019\POL\Shared\Support
        /// </summary>
        public string SupportPath => Path.Combine(ASPath, "Shared", "Support");

        #endregion Public Properties
    }

    public class ZDbConfig
    {
        #region Private Fields

        private static readonly NLog.Logger log = NLog.LogManager.GetCurrentClassLogger();

        private PathBuilder _pathBuilder = null;

        #endregion Private Fields

        #region Private Constructors

        private ZDbConfig()
        {
        }

        private readonly Dictionary<ASVersion, string> ASNames = new Dictionary<ASVersion, string>()
        {
            { ASVersion.v2018, "Advance Steel 2018" },
            { ASVersion.v2019, "Advance Steel 2019" },
            { ASVersion.v2020, "Advance Steel 2020" },
            { ASVersion.v2023, "Advance Steel 2023" },
            { ASVersion.rvt2020, "Autodesk Revit 2020" },
            { ASVersion.rvt2023, "Autodesk Revit 2023" }
        };

        #endregion Private Constructors

        #region Public Properties

        public List<DbDataSource> DataSources { get; set; }
        public string Name { get; set; }
        public bool SupportDirIsLink { get; set; }
        public string SupportDirLink { get; set; }
        public ASVersion Version { get; set; } = ASVersion.v2019;
        public string RevitConfigFileName { get; set; } = "";
        public bool DisabledVersion { 
            get
            {
                return !ZSteelDicovery.IsSoftwareInstalled(ASNames[this.Version]);
            }
        }

        #endregion Public Properties

        #region Protected Properties

        protected PathBuilder PathBuilder
        {
            get
            {
                if (_pathBuilder == null)
                {
                    _pathBuilder = new PathBuilder(Version);
                }
                return _pathBuilder;
            }
        }

        protected string SupportDirPath => _pathBuilder.SupportPath;

        #endregion Protected Properties

        #region Public Methods

        public static ZDbConfig Deserialize(string json)
        {
            var config = JsonConvert.DeserializeObject<ZDbConfig>(json);
            return config;
        }

        public bool IsValid()
        {
            try
            {
                if (!Utils.IsRevit(Version))
                {
                    // to jest sprawdzenie nie wiem czego, jesli to nie jest katalog to rzuca wyjatkiem
                    var str = NativeMethods.GetFinalPathName(this.PathBuilder.SupportPath);
                }

                // tutaj jest sprawdzenie czy w ogole mam szukac danej wersji...
                // najlepiej by bylo z configu zczytywac jakas wartosc i sprawdzac czy jest w rejestrze
                return true;
            }
            catch (Win32Exception)
            {
                return false;
            }
        }

        public static ZDbConfig ReadCurrent(ASVersion version)
        {
            try
            {
                var config = new ZDbConfig
                {
                    Version = version,
                    Name = "Current config for AS" + version.ToString(),
                };
                if (Utils.IsRevit(version))
                {
                    config.Name = "Current config for RVT" + Utils.RevitVersion(version).ToString();
                }
                if (!Utils.IsRevit(version))
                {
                    config.SupportDirIsLink = NativeMethods.IsSymbolicLink(config.PathBuilder.SupportPath);
                    config.SupportDirLink = NativeMethods.NormalizePath(NativeMethods.GetFinalPathName(config.PathBuilder.SupportPath));
                }
                config.DataSources = ReadDataSourcesFromXml(version);
                return config;
            }
            catch (Win32Exception)
            {
                return null;
            }
        }

        public static bool Compare(ZDbConfig a, ZDbConfig b, bool withName = false)
        {
            if (Utils.IsRevit(a.Version) || Utils.IsRevit(a.Version))
            {
                Console.WriteLine("test");
            }
            if (a == null || b == null)
            {
                return false;
            }
            //Data sourcy
            if (a.DataSources.Count != b.DataSources.Count)
            {
                log.Debug("Nie zgadza się liczba datasourcow");
                return false;
            }
            for (int i = 0; i < a.DataSources.Count; i++)
            {
                var ads = a.DataSources[i];
                var bds = b.DataSources[i];
                if (!DbDataSource.Compare(ads, bds))
                {
                    log.Debug(string.Format("Datasourcy w poz {0} nie zgadzaja się", i));
                    return false;
                }
            }
            if (!Utils.IsRevit(a.Version) || !Utils.IsRevit(b.Version)) {
                if (a.SupportDirIsLink != b.SupportDirIsLink)
                {
                    log.Debug("Nie zgadza się supportdirislink");
                    return false;
                }

                if (a.SupportDirLink != b.SupportDirLink)
                {
                    log.Debug("Nie zgadza się supportdirlink");
                    return false;
                }
            }

            if (withName)
            {
                if (a.Name != b.Name)
                {
                    log.Debug("Nie zgadza się name");
                    return false;
                }
            }

            return true;
        }

        public bool IsCurrent()
        {
            try
            {
                var configCurrent = ReadCurrent(Version);
                return Compare(this, configCurrent);
            } catch (FileNotFoundException)
            {
                return false;
            }
        }

        public void MakeCurrent()
        {
            if (Utils.IsRevit(this.Version))
            {
                log.Info("Wykryto konfigurację dla revita");
                MakeCurrentRevit();
                return;
            }
            log.Info("Rozpoczynam proces zmiany ustawień");
            try
            {
                BackupConfigFile();
            } catch (Exception ex)
            {
                log.Error(ex, "Blad podczas backupu bazy, operacja bedzie kontynuowana bez backupu");
            }
            DeleteConfigFile();
            BackupSupportDir();
            CreateXMLDoc();
            PrepareSupportDir();
            log.Info("Proces przywracania ustawień zakończony");
        }

        public void MakeCurrentRevit()
        {
            log.Info("Rozpoczynam proces zmiany ustawień dla revita");
            BackupRevitConfigFile();
            CreateXMLDoc(RevitConfigFileName);
        }

        public string Serialize(bool WithName = true)
        {
            var json = JsonConvert.SerializeObject(this, Newtonsoft.Json.Formatting.Indented);
            return json;
        }

        #endregion Public Methods

        #region Protected Methods

        protected bool BackupRevitConfigFile()
        {
            log.Info("Rozpoczynam backup pliku konfiguracyjnego");
            var newConfigFileName = Path.ChangeExtension(RevitConfigFileName, DateTime.Now.ToString("yyyy-MM-dd_HHmmss.x\\m\\l"));
            log.Info(string.Format("Backup pliku konfiguracyjnego będzie znajdował się tutaj: {0}", newConfigFileName));
            try
            {
                File.Copy(RevitConfigFileName, newConfigFileName);
            } catch (Exception e)
            {
                log.Warn(e.Message);
                throw e;
            }
            return true;
        }

        protected bool BackupConfigFile()
        {
            try
            {
                log.Info("Rozpoczynam backup pliku z konfiguracyjnego");
                var newConfigFileName = Path.ChangeExtension(PathBuilder.ConfigPath, DateTime.Now.ToString("yyyy-MM-dd_HHmmss.x\\m\\l"));
                log.Info(string.Format("Backup pliku konfiguracyjnego będzie znajdował się tutaj: {0}", newConfigFileName));
                File.Copy(PathBuilder.ConfigPath, newConfigFileName, false);
            }
            catch (Exception e)
            {
                log.Warn("Brak możliwości skopiowania pliku:");
                log.Warn(e.Message);
                log.Warn("Przerywam");
                throw e;
            }
            log.Info("Stworzono plik backupu z konfiguracją");
            return true;
        }

        protected bool BackupSupportDir()
        {
            log.Info("Backup katalogu support");
            var suppDirIsLinkNow = NativeMethods.IsSymbolicLink(PathBuilder.SupportPath);
            if (suppDirIsLinkNow)
            {
                log.Info("Docelowy katalog jest dowiązaniem symbolicznym");
                Directory.Delete(PathBuilder.SupportPath);
            }
            else
            {
                try
                {
                    Directory.Move(PathBuilder.SupportPath, PathBuilder.SupportPath + ".bak");
                }
                catch (Exception e)
                {
                    log.Warn(string.Format("Brak możliwości zmiany nazwy katalogu support ze ścieżki {0}", PathBuilder.SupportPath));
                    log.Warn(e.Message);
                    throw e;
                }
            }
            log.Info("Zakończono backup katalogu support");
            return true;
        }

        protected bool CreateXMLDoc(string saveFileName = "")
        {
            if (saveFileName == "")
            {
                saveFileName = PathBuilder.ConfigPath;
            }
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

            log.Info("Stworzono xml, nastąpi próba zapisania");
            // to jest bez bom tak samo bylo w oryginalnych plikach steela
            var encoding = new System.Text.UTF8Encoding(false);
            try
            {
               
                using (StreamWriter sw = new StreamWriter(File.Open(saveFileName, FileMode.Create), encoding))
                {
                    doc.Save(sw);
                }
            }
            catch (Exception e)
            {
                log.Warn("Brak możliwości zapisu pliku konfiguracyjnego - przerywam");
                log.Warn(e.Message);
                throw e;
            }
            log.Info("Zapisano plik xml");
            return true;
        }

        protected bool DeleteConfigFile()
        {
            log.Info("Usuwam stary plik konfiguracyjny");
            try
            {
                File.Delete(PathBuilder.ConfigPath);
            }
            catch (Exception e)
            {
                log.Warn("Brak możliwości usunięcia pliku konfiguracyjnego:");
                log.Warn(e.Message);
                log.Warn("Przerywam");
                throw e;
            }
            return true;
        }

        protected bool PrepareSupportDir()
        {
            log.Info("Rozpoczynam pracę nad katalogiem support");
            if (SupportDirIsLink)
            {
                NativeMethods.CreateSymbolicLink(PathBuilder.SupportPath, SupportDirLink, NativeMethods.SymbolicLink.Directory);
            }
            else
            {
                var bakdir = PathBuilder.SupportPath + ".bak";
                try
                {
                    Directory.Move(bakdir, PathBuilder.SupportPath);
                }
                catch (Exception e)
                {
                    log.Warn(string.Format("Brak możliwości zmiany nazwy katalogu - przerywam"));
                    log.Warn(e.Message);
                    throw e;
                }
            }
            return true;
        }

        #endregion Protected Methods

        #region Private Methods

        private static List<DbDataSource> ReadDataSourcesFromXml(ASVersion version)
        {
            var pathbuilder = new PathBuilder(version);
            var doc = new XmlDocument();
            var filename = pathbuilder.ConfigPath;

            if (Utils.IsRevit(version))
            {
                filename = @"C:\ProgramData\Autodesk\Revit Steel Connections 2020\pl-PL\DatabaseConfiguration.xml";
            }

            doc.Load(filename);

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

        #endregion Private Methods
    }
}