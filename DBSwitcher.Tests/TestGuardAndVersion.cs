using System;
using System.IO;
using System.Threading.Tasks;
using NUnit.Framework;

namespace DBSwitcher.Tests
{
    [TestFixture]
    public class TestOperationGuard
    {
        [Test]
        public void IsIdleByDefault()
        {
            Assert.That(OperationGuard.IsBusy, Is.False);
            Assert.That(OperationGuard.WaitUntilIdleAsync().IsCompleted, Is.True);
        }

        [Test]
        public void IsBusyBetweenEnterAndDispose()
        {
            var operation = OperationGuard.Enter();
            Assert.That(OperationGuard.IsBusy, Is.True);

            operation.Dispose();

            Assert.That(OperationGuard.IsBusy, Is.False);
        }

        [Test]
        public void WaitUntilIdle_CompletesOnlyAfterTheOperationEnds()
        {
            var operation = OperationGuard.Enter();
            var wait = OperationGuard.WaitUntilIdleAsync();

            Assert.That(wait.Wait(100), Is.False, "nie powinno się zakończyć w trakcie operacji");
            operation.Dispose();

            Assert.That(wait.Wait(2000), Is.True);
        }

        [Test]
        public void NestedOperations_KeepGuardBusyUntilTheLastEnds()
        {
            var outer = OperationGuard.Enter();
            var inner = OperationGuard.Enter();

            inner.Dispose();
            Assert.That(OperationGuard.IsBusy, Is.True);

            outer.Dispose();
            Assert.That(OperationGuard.IsBusy, Is.False);
        }

        [Test]
        public void DisposingTwice_DoesNotReleaseSomeoneElsesOperation()
        {
            var first = OperationGuard.Enter();
            var second = OperationGuard.Enter();

            first.Dispose();
            first.Dispose();

            Assert.That(OperationGuard.IsBusy, Is.True);
            second.Dispose();
            Assert.That(OperationGuard.IsBusy, Is.False);
        }

        [Test]
        public void WaiterIsReleasedAndGuardCanBeEnteredAgain()
        {
            var first = OperationGuard.Enter();
            var wait = OperationGuard.WaitUntilIdleAsync();
            first.Dispose();
            wait.Wait(2000);

            var second = OperationGuard.Enter();
            Assert.That(OperationGuard.IsBusy, Is.True);
            Assert.That(OperationGuard.WaitUntilIdleAsync().IsCompleted, Is.False);
            second.Dispose();
        }
    }

    [TestFixture]
    public class TestVersion
    {
        private static string FindVersionFile()
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir != null)
            {
                var candidate = Path.Combine(dir.FullName, "VERSION");
                if (File.Exists(candidate))
                {
                    return candidate;
                }
                dir = dir.Parent;
            }
            return null;
        }

        [Test]
        public void AssemblyVersion_ComesFromTheVersionFile()
        {
            var file = FindVersionFile();
            Assert.That(file, Is.Not.Null, "brak pliku VERSION w katalogach nadrzędnych");

            var expected = Version.Parse(File.ReadAllText(file).Trim());
            var actual = typeof(MainWindow).Assembly.GetName().Version;

            Assert.That(actual.Major, Is.EqualTo(expected.Major));
            Assert.That(actual.Minor, Is.EqualTo(expected.Minor));
            Assert.That(actual.Build, Is.EqualTo(expected.Build));
        }
    }
}
