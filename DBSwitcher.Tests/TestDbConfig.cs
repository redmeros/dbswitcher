// NUnit 3 tests
// See documentation : https://github.com/nunit/docs/wiki/NUnit-Documentation
using System.Runtime.InteropServices;
using NUnit.Framework;

namespace DBSwitcher.Tests
{
    [TestFixture]
    public class TestDbConfig
    {
        [DllImport("kernel32.dll")]
        private static extern bool CreateSymbolicLink(string lpSymlinkFileName, string lpTargetFileName, SymbolicLink dwFlags);

        private enum SymbolicLink
        {
            File = 0,
            Directory = 1
        }

        [Test]
        public void TestCorrectConfigFileFromPath()
        {
            var config = DbConfig.ReadCurrent("C:\\ProgramData\\Autodesk\\Advance Steel 2019\\POL");
            Assert.That(config.ConfigFileName, Is.EqualTo("C:\\ProgramData\\Autodesk\\Advance Steel 2019\\POL\\Configuration\\DatabaseConfiguration.xml"));
        }

        [Test]
        public void TestCorrectSupportDirFromPath()
        {
            var config = DbConfig.ReadCurrent("C:\\ProgramData\\Autodesk\\Advance Steel 2019\\POL");
            Assert.That(config.SupportPath, Is.EqualTo("C:\\ProgramData\\Autodesk\\Advance Steel 2019\\POL\\Shared\\Support"));
        }

        [Test]
        public void SerializedAndDeserializedSettingsAreTheSame()
        {
            var config = DbConfig.ReadCurrent();
            var serialized = config.SerializeToJson();
            var deserialized = DbConfig.Deserialize(serialized);

            var deserializedJson = deserialized.SerializeToJson();

            Assert.That(serialized, Is.EqualTo(deserializedJson));
        }
    }
}