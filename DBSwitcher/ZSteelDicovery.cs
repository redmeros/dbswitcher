using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Win32;
using System.Diagnostics;

namespace DBSwitcher
{
    static class ZSteelDicovery
    {
        public static bool IsSoftwareInstalled(string softwareName)
        {
            var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall", false);
            //var subkeys = key.GetSubKeyNames()
            //    .Select(keyname => key.OpenSubKey(keyname))
            //    .Select(subkey => subkey.GetValue("DisplayName") as string)
            //    .Where(o => {
            //        if (o == null)
            //        {
            //            return false;
            //        }
            //        return o.Contains("Revit"); 
            //    });

            return key.GetSubKeyNames()
                .Select(keyName => key.OpenSubKey(keyName))
                .Select(subkey => subkey.GetValue("DisplayName") as string)
                .Any(displayName => displayName != null && displayName == softwareName);
        }
    }
}
