using System;
using System.Collections.Generic;
using System.IO;
using System.Security;
using Microsoft.Win32;

namespace DBSwitcher
{
    static class ZSteelDiscovery
    {
        private const string UninstallKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall";

        // lista programow jest czytana raz, a nie przy kazdym przycisku i odswiezeniu
        private static readonly Lazy<HashSet<string>> installedNames = new Lazy<HashSet<string>>(ReadInstalledNames);

        public static bool IsSoftwareInstalled(string softwareName)
        {
            return !string.IsNullOrEmpty(softwareName) && installedNames.Value.Contains(softwareName);
        }

        /// <summary>
        /// DisplayName wszystkich zainstalowanych programów, z widoku 64 i 32 bitowego
        /// </summary>
        internal static HashSet<string> ReadInstalledNames()
        {
            var names = new HashSet<string>(StringComparer.Ordinal);
            foreach (var view in new[] { RegistryView.Registry64, RegistryView.Registry32 })
            {
                using (var baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, view))
                using (var key = baseKey.OpenSubKey(UninstallKey))
                {
                    if (key == null)
                    {
                        continue;
                    }

                    foreach (var subKeyName in key.GetSubKeyNames())
                    {
                        try
                        {
                            using (var subKey = key.OpenSubKey(subKeyName))
                            {
                                if (subKey?.GetValue("DisplayName") is string displayName)
                                {
                                    names.Add(displayName);
                                }
                            }
                        }
                        catch (Exception ex) when (ex is SecurityException || ex is IOException || ex is UnauthorizedAccessException)
                        {
                            // pojedynczy nieczytelny wpis nie moze zepsuc wykrywania pozostalych
                        }
                    }
                }
            }
            return names;
        }
    }
}
