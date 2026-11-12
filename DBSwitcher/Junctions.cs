using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

public static class NativeMethods
{
    #region Private Fields

    private const uint FILE_FLAG_BACKUP_SEMANTICS = 0x2000000;
    private const uint FILE_READ_EA = 0x0008;
    private static readonly IntPtr INVALID_HANDLE_VALUE = new IntPtr(-1);

    #endregion Private Fields

    #region Public Methods

    public static string NormalizePath(string path)
    {
        path = path.TrimEnd('\\');
        path = path.Replace("\\\\?\\UNC\\", "\\\\");
        path = path.Replace("\\\\?\\", "");
        return path;
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CreateSymbolicLink(string lpSymLinkFileName, string lpTargetFileName, SymbolicLink dwFlags);

    [Flags]
    public enum SymbolicLink
    {
        File = 0,
        Directory = 1,
        AllowUnprivilegedCreate = 2
    };

    /// <summary>
    /// Tworzy dowiązanie do katalogu, rzuca Win32Exception gdy się nie uda (brak uprawnień, tryb dewelopera wyłączony)
    /// </summary>
    public static void CreateDirectorySymbolicLink(string link, string target)
    {
        if (!CreateSymbolicLink(link, target, SymbolicLink.Directory | SymbolicLink.AllowUnprivilegedCreate))
        {
            throw new Win32Exception(Marshal.GetLastWin32Error(), $"Nie można utworzyć dowiązania symbolicznego {link} -> {target}");
        }
    }

    /// <summary>
    /// Sprawdza czy da się tworzyć dowiązania zanim cokolwiek zostanie zmienione
    /// </summary>
    public static void EnsureCanCreateSymbolicLinks()
    {
        var probe = Path.Combine(Path.GetTempPath(), "dbswitcher_probe_" + Guid.NewGuid().ToString("N"));
        CreateDirectorySymbolicLink(probe, Path.GetTempPath().TrimEnd('\\'));
        Directory.Delete(probe);
    }

    public static bool IsSymbolicLink(string path)
    {
        FileInfo pathInfo = new FileInfo(path);
        return pathInfo.Attributes.HasFlag(FileAttributes.ReparsePoint);
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    public static extern IntPtr CreateFile(
            [MarshalAs(UnmanagedType.LPTStr)] string filename,
            [MarshalAs(UnmanagedType.U4)] uint access,
            [MarshalAs(UnmanagedType.U4)] FileShare share,
            IntPtr securityAttributes, // optional SECURITY_ATTRIBUTES struct or IntPtr.Zero
            [MarshalAs(UnmanagedType.U4)] FileMode creationDisposition,
            [MarshalAs(UnmanagedType.U4)] uint flagsAndAttributes,
            IntPtr templateFile);

    public static string GetFinalPathName(string path)
    {
        var log = NLog.LogManager.GetCurrentClassLogger();
        //log.Info(string.Format("Sciezka ktora sprawdzam: {0}", path));
        var h = CreateFile(path,
            FILE_READ_EA,
            FileShare.ReadWrite | FileShare.Delete,
            IntPtr.Zero,
            FileMode.Open,
            FILE_FLAG_BACKUP_SEMANTICS,
            IntPtr.Zero);
        log.Info(h);
        if (h == INVALID_HANDLE_VALUE)
            throw new Win32Exception();

        try
        {
            var sb = new StringBuilder(1024);
            var res = GetFinalPathNameByHandle(h, sb, 1024, 0);
            if (res == 0)
                throw new Win32Exception();

            return sb.ToString();
        }
        finally
        {
            CloseHandle(h);
        }
    }

    #endregion Public Methods

    #region Private Methods

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CloseHandle(IntPtr hObject);

    [DllImport("Kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    private static extern uint GetFinalPathNameByHandle(IntPtr hFile, [MarshalAs(UnmanagedType.LPTStr)] StringBuilder lpszFilePath, uint cchFilePath, uint dwFlags);

    #endregion Private Methods
}