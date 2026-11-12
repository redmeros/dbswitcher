using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using Velopack;

namespace DBSwitcher
{
    public static class Updater
    {
        private static readonly NLog.Logger log = NLog.LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Katalog z wydaniami, ten sam do ktorego publikuje "make release"
        /// </summary>
        private const string FeedPath = "//ad03/DCS/02-DCS-DESIGN/DCS-STAL/ASDBSWITCHER_NEW";

        public static async Task CheckForUpdatesAsync()
        {
            try
            {
                var manager = new UpdateManager(FeedPath);
                if (!manager.IsInstalled)
                {
                    log.Info("Aplikacja nie jest zainstalowana, pomijam sprawdzanie aktualizacji");
                    return;
                }

                if (!Directory.Exists(FeedPath))
                {
                    log.Warn($"Brak dostępu do katalogu z aktualizacjami {FeedPath}");
                    return;
                }

                var update = await manager.CheckForUpdatesAsync();
                if (update == null)
                {
                    log.Info("Brak nowej wersji");
                    return;
                }

                var version = update.TargetFullRelease.Version;
                log.Info($"Dostępna nowa wersja {version}");
                var answer = MessageBox.Show(
                    $"Dostępna jest nowa wersja {version}. Zaktualizować teraz i uruchomić ponownie?",
                    "ASDbSwitcher",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);
                if (answer != MessageBoxResult.Yes)
                {
                    return;
                }

                await manager.DownloadUpdatesAsync(update);
                manager.ApplyUpdatesAndRestart(update);
            }
            catch (Exception ex)
            {
                // brak dostepu do udzialu nie moze blokowac pracy aplikacji
                log.Warn(ex, "Nie udało się sprawdzić aktualizacji");
            }
        }
    }
}
