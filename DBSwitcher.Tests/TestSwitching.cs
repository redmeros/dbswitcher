using System.ComponentModel;
using System.IO;
using NUnit.Framework;

namespace DBSwitcher.Tests
{
    [TestFixture]
    public class TestSwitching
    {
        private FakeProgramData _pd;
        private string _linkTarget;

        [SetUp]
        public void SetUp()
        {
            _pd = new FakeProgramData();
            _pd.CreateConfig();
            _linkTarget = Path.Combine(_pd.Root, "shared_support");
            Directory.CreateDirectory(_linkTarget);
            File.WriteAllText(Path.Combine(_linkTarget, "shared.txt"), "shared");
        }

        [TearDown]
        public void TearDown()
        {
            _pd.Dispose();
        }

        private static void RequireSymlinks()
        {
            try
            {
                NativeMethods.EnsureCanCreateSymbolicLinks();
            }
            catch (Win32Exception)
            {
                Assert.Ignore("Brak uprawnień do tworzenia dowiązań symbolicznych (włącz tryb dewelopera)");
            }
        }

        [Test]
        public void RealDirToLink_CreatesLinkAndKeepsOriginalInBak()
        {
            RequireSymlinks();

            _pd.MakeConfig(supportIsLink: true, supportLink: _linkTarget).MakeCurrent();

            Assert.That(FakeProgramData.IsLink(_pd.SupportDir), Is.True);
            Assert.That(File.Exists(Path.Combine(_pd.SupportDir + ".bak", "marker.txt")), Is.True);
            Assert.That(File.Exists(Path.Combine(_pd.SupportDir, "shared.txt")), Is.True);
        }

        [Test]
        public void RealDirToLink_RewritesConfigFile()
        {
            RequireSymlinks();

            _pd.MakeConfig(supportIsLink: true, supportLink: _linkTarget).MakeCurrent();

            var xml = File.ReadAllText(_pd.ConfigFile);
            Assert.That(xml, Does.Contain("Server=x"));
            Assert.That(File.Exists(_pd.ConfigFile + ".new"), Is.False, "plik tymczasowy nie powinien zostać");
        }

        [Test]
        public void LinkToRealDir_RestoresOriginalFromBak()
        {
            RequireSymlinks();
            _pd.MakeConfig(supportIsLink: true, supportLink: _linkTarget).MakeCurrent();

            _pd.MakeConfig(supportIsLink: false).MakeCurrent();

            Assert.That(FakeProgramData.IsLink(_pd.SupportDir), Is.False);
            Assert.That(File.Exists(Path.Combine(_pd.SupportDir, "marker.txt")), Is.True);
            Assert.That(Directory.Exists(_pd.SupportDir + ".bak"), Is.False);
        }

        [Test]
        public void LinkToOtherLink_ReplacesLinkTarget()
        {
            RequireSymlinks();
            var other = Path.Combine(_pd.Root, "other_support");
            Directory.CreateDirectory(other);
            File.WriteAllText(Path.Combine(other, "other.txt"), "other");
            _pd.MakeConfig(supportIsLink: true, supportLink: _linkTarget).MakeCurrent();

            _pd.MakeConfig(supportIsLink: true, supportLink: other).MakeCurrent();

            Assert.That(File.Exists(Path.Combine(_pd.SupportDir, "other.txt")), Is.True);
            Assert.That(File.Exists(Path.Combine(_pd.SupportDir, "shared.txt")), Is.False);
        }

        [Test]
        public void RealDirToRealDir_LeavesSupportUntouched()
        {
            _pd.MakeConfig(supportIsLink: false).MakeCurrent();

            Assert.That(File.Exists(Path.Combine(_pd.SupportDir, "marker.txt")), Is.True);
            Assert.That(Directory.Exists(_pd.SupportDir + ".bak"), Is.False);
            Assert.That(File.ReadAllText(_pd.ConfigFile), Does.Contain("Server=x"));
        }

        [Test]
        public void FailureAfterSupportSwap_RollsBackSupportDir()
        {
            RequireSymlinks();
            // bez katalogu Configuration zapis xml się nie uda już po zmianie katalogu support
            Directory.Delete(_pd.ConfigDir, true);

            Assert.That(
                () => _pd.MakeConfig(supportIsLink: true, supportLink: _linkTarget).MakeCurrent(),
                Throws.Exception);

            Assert.That(FakeProgramData.IsLink(_pd.SupportDir), Is.False);
            Assert.That(File.Exists(Path.Combine(_pd.SupportDir, "marker.txt")), Is.True);
            Assert.That(Directory.Exists(_pd.SupportDir + ".bak"), Is.False);
        }

        [Test]
        public void FailedConfigWrite_KeepsOldConfigFile()
        {
            // plik docelowy zablokowany: stara konfiguracja ma przetrwać
            using (new FileStream(_pd.ConfigFile, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                Assert.That(
                    () => _pd.MakeConfig(supportIsLink: false).MakeCurrent(),
                    Throws.Exception);
            }

            Assert.That(File.ReadAllText(_pd.ConfigFile), Is.EqualTo("<old/>"));
        }

        [Test]
        public void LinkToRealDirWithoutBak_RefusesAndLeavesLinkAlone()
        {
            RequireSymlinks();
            Directory.Delete(_pd.SupportDir, true);
            Directory.CreateSymbolicLink(_pd.SupportDir, _linkTarget);

            Assert.That(
                () => _pd.MakeConfig(supportIsLink: false).MakeCurrent(),
                Throws.TypeOf<DirectoryNotFoundException>());

            Assert.That(FakeProgramData.IsLink(_pd.SupportDir), Is.True);
            Assert.That(File.ReadAllText(_pd.ConfigFile), Is.EqualTo("<old/>"));
        }

        [Test]
        public void StaleBakDir_IsMovedAsideInsteadOfFailing()
        {
            RequireSymlinks();
            Directory.CreateDirectory(_pd.SupportDir + ".bak");
            File.WriteAllText(Path.Combine(_pd.SupportDir + ".bak", "old.txt"), "stale");

            _pd.MakeConfig(supportIsLink: true, supportLink: _linkTarget).MakeCurrent();

            Assert.That(FakeProgramData.IsLink(_pd.SupportDir), Is.True);
            Assert.That(File.Exists(Path.Combine(_pd.SupportDir + ".bak", "marker.txt")), Is.True);
        }

        [Test]
        public void BackupOfConfigFile_IsCreated()
        {
            _pd.MakeConfig(supportIsLink: false).MakeCurrent();

            var backups = Directory.GetFiles(_pd.ConfigDir, "DatabaseConfiguration.*.xml");
            Assert.That(backups, Has.Length.EqualTo(1));
            Assert.That(File.ReadAllText(backups[0]), Is.EqualTo("<old/>"));
        }

        [Test]
        public void Revit_BacksUpAndRewritesRevitConfigFile()
        {
            var revitFile = Path.Combine(_pd.Root, "revit", "DatabaseConfiguration.xml");
            Directory.CreateDirectory(Path.GetDirectoryName(revitFile));
            File.WriteAllText(revitFile, "<old/>");

            _pd.MakeConfig(version: ASVersion.rvt2023, revitConfigFile: revitFile).MakeCurrent();

            Assert.That(File.ReadAllText(revitFile), Does.Contain("Server=x"));
            Assert.That(
                Directory.GetFiles(Path.GetDirectoryName(revitFile), "DatabaseConfiguration.*.xml"),
                Has.Length.EqualTo(1));
        }
    }
}
