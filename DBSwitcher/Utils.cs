using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DBSwitcher
{
    public static class Utils
    {
        public static bool IsRevit(ASVersion version)
        {
            return (int)version > 3000;
        }
        public static int RevitVersion(ASVersion version)
        {
            return (int)version - 90000;
        }
    }
}
