using System.Data.Common;

namespace DBSwitcher
{
    public class DbDataSource
    {
        #region Public Properties

        public string Name { get; set; }
        public string Value { get; set; }

        public static bool Compare(DbDataSource a, DbDataSource b)
        {
            // a - to jest odczytany z pliku json
            // b - to jest config odczytany z pliku xml
            if (a.Name != b.Name) 
            {
                return false;
            }
            
            // tutaj powinno być inne porównanie
            // np wczytać string connection buildera
            // i porównać output

            var builderA = new DbConnectionStringBuilder();
            builderA.ConnectionString = a.Value;
            var builderB = new DbConnectionStringBuilder();
            builderB.ConnectionString = b.Value;
            return builderA.EquivalentTo(builderB);
        }

        #endregion Public Properties
    }
}