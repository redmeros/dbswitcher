using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using NUnit.Framework;

namespace DBSwitcher.Tests
{
    [TestFixture]
    public class TestUtils
    {
        [TestCase(ASVersion.v2023, false)]
        [TestCase(ASVersion.v2027, false)]
        [TestCase(ASVersion.rvt2023, true)]
        public void IsRevit_DistinguishesRevitFromAdvanceSteel(ASVersion version, bool expected)
        {
            Assert.That(Utils.IsRevit(version), Is.EqualTo(expected));
        }

        [Test]
        public void RevitVersion_ReturnsYear()
        {
            Assert.That(Utils.RevitVersion(ASVersion.rvt2023), Is.EqualTo(2023));
        }

        [TestCase(ASVersion.v2023, "Advance Steel 2023")]
        [TestCase(ASVersion.v2027, "Advance Steel 2027")]
        [TestCase(ASVersion.rvt2023, "Revit 2023")]
        public void VersionString_IsReadable(ASVersion version, string expected)
        {
            Assert.That(DbSwitcherControl.VersionString(version), Is.EqualTo(expected));
        }
    }

    [TestFixture]
    public class TestPathBuilder
    {
        [Test]
        public void Paths_AreBuiltUnderProgramData()
        {
            var builder = new PathBuilder(ASVersion.v2023, @"C:\PD");
            var asPath = Path.Combine(
                @"C:\PD", "Autodesk", "Advance Steel 2023", Properties.Settings.Default.ASLanguagePrefix);

            Assert.That(builder.ASPath, Is.EqualTo(asPath));
            Assert.That(
                builder.ConfigPath,
                Is.EqualTo(Path.Combine(asPath, "Configuration", "DatabaseConfiguration.xml")));
            Assert.That(builder.SupportPath, Is.EqualTo(Path.Combine(asPath, "Shared", "Support")));
        }

        [Test]
        public void RevitConfigPath_UsesRevitYear()
        {
            var builder = new PathBuilder(ASVersion.rvt2023, @"C:\PD");

            Assert.That(
                builder.RevitConfigPath,
                Is.EqualTo(@"C:\PD\Autodesk\Revit Steel Connections 2023\pl-PL\DatabaseConfiguration.xml"));
        }

        [Test]
        public void ProgramDataPath_DefaultsToSystemCommonAppData()
        {
            var builder = new PathBuilder(ASVersion.v2023);

            Assert.That(
                builder.ProgramDataPath,
                Is.EqualTo(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData)));
        }
    }

    [TestFixture]
    public class TestCompare
    {
        private static ZDbConfig Config(
            string name = "x",
            bool link = false,
            string linkTarget = "",
            ASVersion version = ASVersion.v2023,
            params (string Name, string Value)[] sources)
        {
            if (sources.Length == 0)
            {
                sources = new[] { ("A", "Server=x;Integrated Security=true") };
            }
            var json = JsonSerializer.Serialize(new
            {
                Version = version.ToString(),
                Name = name,
                SupportDirIsLink = link,
                SupportDirLink = linkTarget,
                DataSources = sources.Select(s => new { s.Name, s.Value }),
            });
            return ZDbConfig.Deserialize(json);
        }

        [Test]
        public void Null_IsNeverEqual()
        {
            Assert.That(ZDbConfig.Compare(null, Config()), Is.False);
            Assert.That(ZDbConfig.Compare(Config(), null), Is.False);
        }

        [Test]
        public void SameContent_IsEqual()
        {
            Assert.That(ZDbConfig.Compare(Config(), Config()), Is.True);
        }

        [Test]
        public void DifferentNumberOfSources_IsNotEqual()
        {
            var a = Config();
            var b = Config(sources: new[] { ("A", "Server=x"), ("B", "Server=y") });

            Assert.That(ZDbConfig.Compare(a, b), Is.False);
        }

        [Test]
        public void ConnectionStringsAreComparedSemantically()
        {
            var a = Config(sources: new[] { ("A", "Server=x;Integrated Security=true") });
            var b = Config(sources: new[] { ("A", "integrated security=true;server=x") });

            Assert.That(ZDbConfig.Compare(a, b), Is.True);
        }

        [Test]
        public void SourceOrderDoesNotMatter()
        {
            var a = Config(sources: new[] { ("A", "Server=x"), ("B", "Server=y") });
            var b = Config(sources: new[] { ("B", "Server=y"), ("A", "Server=x") });

            Assert.That(ZDbConfig.Compare(a, b), Is.True);
        }

        [Test]
        public void ReorderedSourcesWithDifferentValue_IsNotEqual()
        {
            var a = Config(sources: new[] { ("A", "Server=x"), ("B", "Server=y") });
            var b = Config(sources: new[] { ("B", "Server=y"), ("A", "Server=OTHER") });

            Assert.That(ZDbConfig.Compare(a, b), Is.False);
        }

        [Test]
        public void DuplicateSourceNames_AreMatchedOneToOne()
        {
            var a = Config(sources: new[] { ("A", "Server=x"), ("A", "Server=x") });
            var b = Config(sources: new[] { ("A", "Server=x"), ("A", "Server=y") });

            Assert.That(ZDbConfig.Compare(a, b), Is.False);
            Assert.That(ZDbConfig.Compare(b, a), Is.False);
        }

        [Test]
        public void DifferentSourceName_IsNotEqual()
        {
            var a = Config(sources: new[] { ("A", "Server=x") });
            var b = Config(sources: new[] { ("B", "Server=x") });

            Assert.That(ZDbConfig.Compare(a, b), Is.False);
        }

        [Test]
        public void DifferentSupportLink_IsNotEqual()
        {
            Assert.That(
                ZDbConfig.Compare(Config(link: true, linkTarget: @"C:\a"), Config(link: true, linkTarget: @"C:\b")),
                Is.False);
            Assert.That(
                ZDbConfig.Compare(Config(link: true, linkTarget: @"C:\a"), Config(link: false)),
                Is.False);
        }

        [Test]
        public void SupportLinkIsIgnoredForRevit()
        {
            var a = Config(version: ASVersion.rvt2023, link: true, linkTarget: @"C:\a");
            var b = Config(version: ASVersion.rvt2023, link: false);

            Assert.That(ZDbConfig.Compare(a, b), Is.True);
        }

        [Test]
        public void NameMattersOnlyWhenRequested()
        {
            var a = Config(name: "one");
            var b = Config(name: "two");

            Assert.That(ZDbConfig.Compare(a, b), Is.True);
            Assert.That(ZDbConfig.Compare(a, b, withName: true), Is.False);
        }
    }

    [TestFixture]
    public class TestConfigEntries
    {
        private string _dir;

        [SetUp]
        public void SetUp()
        {
            _dir = Path.Combine(Path.GetTempPath(), "dbsw_cfg_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_dir);
        }

        [TearDown]
        public void TearDown()
        {
            Directory.Delete(_dir, true);
        }

        private void Write(string fileName, string json = "{\"Version\":\"2023\",\"Name\":\"n\",\"DataSources\":[]}")
        {
            File.WriteAllText(Path.Combine(_dir, fileName), json);
        }

        [Test]
        public void ReturnsOnlyFilesOfRequestedVersion()
        {
            Write("AS2023_local.config.json");
            Write("AS2023_dcs_net-domain.config.json");
            Write("AS2027_local.config.json");
            Write("RVT2023_local.config.json");
            Write("readme.txt", "x");

            var entries = ZConfigEntry.GetEntries(_dir, ASVersion.v2023);

            Assert.That(
                entries.Select(e => e.Name),
                Is.EquivalentTo(new[] { "AS2023_local.config", "AS2023_dcs_net-domain.config" }));
        }

        [Test]
        public void FileNamesAreMatchedStrictly()
        {
            Write("AS2023_local2.config.json");
            Write("AS2023_a1_b2.config.json");
            Write("AS2023_local.configXjson");
            Write("AS2023_local.config.json.bak");
            Write("XAS2023_local.config.json");
            Write("AS20231_local.config.json");

            var entries = ZConfigEntry.GetEntries(_dir, ASVersion.v2023);

            Assert.That(
                entries.Select(e => Path.GetFileName(e.FileName)),
                Is.EquivalentTo(new[] { "AS2023_local2.config.json", "AS2023_a1_b2.config.json" }));
        }

        [Test]
        public void RevitVersion_UsesRvtPrefix()
        {
            Write("AS2023_local.config.json");
            Write("RVT2023_local.config.json");

            var entries = ZConfigEntry.GetEntries(_dir, ASVersion.rvt2023);

            Assert.That(entries, Has.Count.EqualTo(1));
            Assert.That(entries[0].FileName, Does.EndWith("RVT2023_local.config.json"));
        }

        [Test]
        public void InvalidJson_IsSkippedAndReported()
        {
            Write("AS2023_local.config.json", "{ not json");
            Write("AS2023_dcs_net-domain.config.json");
            var errors = new List<string>();

            var entries = ZConfigEntry.GetEntries(_dir, ASVersion.v2023, errors);

            Assert.That(entries, Has.Count.EqualTo(1));
            Assert.That(entries[0].FileName, Does.EndWith("AS2023_dcs_net-domain.config.json"));
            Assert.That(errors, Has.Count.EqualTo(1));
            Assert.That(errors[0], Does.StartWith("AS2023_local.config.json"));
        }

        [Test]
        public void JsonNull_IsReportedAsError()
        {
            Write("AS2023_local.config.json", "null");
            var errors = new List<string>();

            var entries = ZConfigEntry.GetEntries(_dir, ASVersion.v2023, errors);

            Assert.That(entries, Is.Empty);
            Assert.That(errors, Has.Count.EqualTo(1));
        }

        [Test]
        public void InvalidJson_WithoutErrorList_DoesNotThrow()
        {
            Write("AS2023_local.config.json", "{ not json");

            Assert.That(() => ZConfigEntry.GetEntries(_dir, ASVersion.v2023), Throws.Nothing);
        }
    }

    [TestFixture]
    public class TestSoftwareDiscovery
    {
        [TestCase(null)]
        [TestCase("")]
        public void EmptyName_IsNotInstalled(string name)
        {
            Assert.That(ZSteelDiscovery.IsSoftwareInstalled(name), Is.False);
        }

        [Test]
        public void UnknownSoftware_IsNotInstalled()
        {
            Assert.That(ZSteelDiscovery.IsSoftwareInstalled("Advance Steel 1900 " + Guid.NewGuid()), Is.False);
        }

        [Test]
        public void ReadInstalledNames_FindsProgramsOnThisMachine()
        {
            var names = ZSteelDiscovery.ReadInstalledNames();

            Assert.That(names, Is.Not.Empty);
            Assert.That(names, Has.None.Null);
        }

        [Test]
        public void IsSoftwareInstalled_RecognizesAnInstalledProgram()
        {
            var some = ZSteelDiscovery.ReadInstalledNames().First();

            Assert.That(ZSteelDiscovery.IsSoftwareInstalled(some), Is.True);
        }
    }

    [TestFixture]
    public class TestShippedConfigs
    {
        private static string ConfigsDir => Path.Combine(AppContext.BaseDirectory, "configs");

        public static IEnumerable<ASVersion> AllVersions => Enum.GetValues(typeof(ASVersion)).Cast<ASVersion>();

        [TestCaseSource(nameof(AllVersions))]
        public void EveryVersionHasAtLeastOneConfig(ASVersion version)
        {
            Assert.That(ZConfigEntry.GetEntries(ConfigsDir, version), Is.Not.Empty);
        }

        [TestCaseSource(nameof(AllVersions))]
        public void ConfigsDeclareTheVersionOfTheirFileName(ASVersion version)
        {
            foreach (var entry in ZConfigEntry.GetEntries(ConfigsDir, version))
            {
                Assert.That(entry.Config.Version, Is.EqualTo(version), entry.FileName);
            }
        }

        [Test]
        public void EveryConfigFileIsPickedUpByAVersion()
        {
            var matched = AllVersions
                .SelectMany(v => ZConfigEntry.GetEntries(ConfigsDir, v))
                .Select(e => e.FileName)
                .ToList();

            var all = Directory.GetFiles(ConfigsDir, "*.config.json");

            Assert.That(matched, Is.EquivalentTo(all));
        }

        [Test]
        public void ConfigsHaveNamedDataSources()
        {
            foreach (var version in AllVersions)
            {
                foreach (var entry in ZConfigEntry.GetEntries(ConfigsDir, version))
                {
                    Assert.That(entry.Config.Name, Is.Not.Empty, entry.FileName);
                    Assert.That(entry.Config.DataSources, Is.Not.Empty, entry.FileName);
                    Assert.That(
                        entry.Config.DataSources.All(d => !string.IsNullOrEmpty(d.Name) && !string.IsNullOrEmpty(d.Value)),
                        Is.True,
                        entry.FileName);
                }
            }
        }
    }
}
