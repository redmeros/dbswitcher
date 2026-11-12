using System;
using System.Threading;
using System.Threading.Tasks;

namespace DBSwitcher
{
    /// <summary>
    /// Zaznacza że trwa operacja której nie wolno przerwać (zmiana konfiguracji),
    /// dzięki temu aktualizacja nie zrestartuje aplikacji w środku.
    /// </summary>
    public static class OperationGuard
    {
        private static readonly object gate = new object();
        private static int active;
        private static TaskCompletionSource<bool> idle = CompletedSource();

        public static bool IsBusy
        {
            get
            {
                lock (gate)
                {
                    return active > 0;
                }
            }
        }

        /// <summary>
        /// Rozpoczyna operację, Dispose ją kończy. Można zagnieżdżać.
        /// </summary>
        public static IDisposable Enter()
        {
            lock (gate)
            {
                if (active++ == 0)
                {
                    idle = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                }
            }
            return new Releaser();
        }

        /// <summary>
        /// Kończy się gdy żadna operacja nie trwa. Po wznowieniu trzeba sprawdzić IsBusy,
        /// bo w międzyczasie mogła wystartować następna.
        /// </summary>
        public static Task WaitUntilIdleAsync()
        {
            lock (gate)
            {
                return idle.Task;
            }
        }

        private static void Exit()
        {
            TaskCompletionSource<bool> finished = null;
            lock (gate)
            {
                if (--active == 0)
                {
                    finished = idle;
                }
            }
            finished?.SetResult(true);
        }

        private static TaskCompletionSource<bool> CompletedSource()
        {
            var source = new TaskCompletionSource<bool>();
            source.SetResult(true);
            return source;
        }

        private sealed class Releaser : IDisposable
        {
            private int released;

            public void Dispose()
            {
                if (Interlocked.Exchange(ref released, 1) == 0)
                {
                    Exit();
                }
            }
        }
    }
}
