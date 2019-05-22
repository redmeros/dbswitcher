using System.IO;

namespace DBSwitcher
{
    //public class AddNewConfigEntry : ConfigEntry
    //{
    //    #region Public Properties

    //    public override string ConfigName => "Dodaj nową...";

    //    #endregion Public Properties
    //}

    /// <summary>
    /// Klasa macierzysta dla wszystkich ConfigEntry
    /// </summary>
    public class ConfigEntry
    {
        #region Public Properties

        /// <summary>
        /// Nazwa Konfiguracji
        /// </summary>
        public virtual string ConfigName { get; }

        #endregion Public Properties
    }

    /// <summary>
    /// Aktualnie załadowany configEntry
    /// </summary>
    public class CurrentConfigEntry : ConfigEntry
    {
        #region Public Properties

        /// <summary>
        /// Nazwa Konfiguracji
        /// </summary>
        public override string ConfigName => "Aktualna konfiguracja";

        #endregion Public Properties
    }

    /// <summary>
    /// Odczytany z pliku configEntry
    /// </summary>
    public class FileConfigEntry : ConfigEntry
    {
        #region Public Properties

        /// <summary>
        /// Nazwa Konfiguracji
        /// </summary>
        public override string ConfigName => Path.GetFileNameWithoutExtension(FileFullPath);

        /// <summary>
        /// Nazwa pliku w ktorym jest konfiguracja
        /// </summary>
        public string FileFullPath { get; set; }

        #endregion Public Properties
    }

    /// <summary>
    /// Separator config entry
    /// </summary>
    public class SeparatorConfigEntry : ConfigEntry
    {
        #region Public Properties

        /// <summary>
        /// Wyswietla sie '--------'
        /// </summary>
        public override string ConfigName => "-------------";

        #endregion Public Properties
    }
}