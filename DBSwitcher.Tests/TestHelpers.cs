using System;
using System.IO;
using System.Text.Json;

namespace DBSwitcher.Tests
{
    /// <summary>
    /// Katalog tymczasowy udający C:\ProgramData z ustawieniami Advance Steel 2023
    /// </summary>
    public sealed class FakeProgramData : IDisposable
    {
        public FakeProgramData()
        {
            Root = Path.Combine(Path.GetTempPath(), "dbsw_tests_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path.Combine(AsDir, "Shared", "Support"));
            File.WriteAllText(Path.Combine(SupportDir, "marker.txt"), "orig");
        }

        public string Root { get; }

        public string AsDir => Path.Combine(
            Root, "Autodesk", "Advance Steel 2023", Properties.Settings.Default.ASLanguagePrefix);

        public string SupportDir => Path.Combine(AsDir, "Shared", "Support");
        public string ConfigDir => Path.Combine(AsDir, "Configuration");
        public string ConfigFile => Path.Combine(ConfigDir, "DatabaseConfiguration.xml");

        public void CreateConfig(string content = "<old/>")
        {
            Directory.CreateDirectory(ConfigDir);
            File.WriteAllText(ConfigFile, content);
        }

        public void Dispose()
        {
            try
            {
                // dowiązania trzeba usunąć zanim Directory.Delete wejdzie do celu
                if (Directory.Exists(SupportDir) && IsLink(SupportDir))
                {
                    Directory.Delete(SupportDir);
                }
                Directory.Delete(Root, true);
            }
            catch (IOException)
            {
            }
        }

        public static bool IsLink(string path)
        {
            return new DirectoryInfo(path).Attributes.HasFlag(FileAttributes.ReparsePoint);
        }

        public ZDbConfig MakeConfig(
            bool supportIsLink = false,
            string supportLink = "",
            ASVersion version = ASVersion.v2023,
            string revitConfigFile = "")
        {
            var json = "{\"Version\":\"" + version + "\",\"Name\":\"test\","
                + "\"SupportDirIsLink\":" + (supportIsLink ? "true" : "false") + ","
                + "\"SupportDirLink\":" + JsonSerializer.Serialize(supportLink) + ","
                + "\"RevitConfigFileName\":" + JsonSerializer.Serialize(revitConfigFile) + ","
                + "\"DataSources\":[{\"Name\":\"A\",\"Value\":\"Server=x\"}]}";
            var config = ZDbConfig.Deserialize(json);
            config.ProgramDataPathOverride = Root;
            return config;
        }
    }
}
