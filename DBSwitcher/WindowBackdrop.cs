using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;

namespace DBSwitcher
{
    /// <summary>
    /// Półprzezroczyste tło okna (Mica) na Windows 11, na starszych systemach nic nie robi.
    /// </summary>
    internal static class WindowBackdrop
    {
        private const int DWMWA_SYSTEMBACKDROP_TYPE = 38;
        private const int DWMSBT_MAINWINDOW = 2;
        private const int MinBuild = 22621;

        [StructLayout(LayoutKind.Sequential)]
        private struct Margins
        {
            public int Left, Right, Top, Bottom;
        }

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);

        [DllImport("dwmapi.dll")]
        private static extern int DwmExtendFrameIntoClientArea(IntPtr hwnd, ref Margins margins);

        public static bool TryApplyMica(Window window)
        {
            if (Environment.OSVersion.Version.Build < MinBuild)
            {
                return false;
            }

            var source = (HwndSource)PresentationSource.FromVisual(window);
            if (source == null)
            {
                return false;
            }

            var backdrop = DWMSBT_MAINWINDOW;
            if (DwmSetWindowAttribute(source.Handle, DWMWA_SYSTEMBACKDROP_TYPE, ref backdrop, sizeof(int)) != 0)
            {
                return false;
            }

            var margins = new Margins { Left = -1, Right = -1, Top = -1, Bottom = -1 };
            if (DwmExtendFrameIntoClientArea(source.Handle, ref margins) != 0)
            {
                return false;
            }

            source.CompositionTarget.BackgroundColor = Colors.Transparent;
            window.Background = Brushes.Transparent;
            return true;
        }
    }
}
