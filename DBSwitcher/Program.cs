using System;
using Velopack;

namespace DBSwitcher
{
    public static class Program
    {
        [STAThread]
        public static void Main()
        {
            // must run first: handles install/update/uninstall hooks before any UI starts
            VelopackApp.Build().Run();

            var app = new App();
            app.InitializeComponent();
            app.Run();
        }
    }
}
