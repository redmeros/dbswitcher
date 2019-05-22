namespace DBSwitcher
{
    public class DbDataSource
    {
        #region Public Properties

        public string Name { get; set; }
        public string Value { get; set; }

        public static bool Compare(DbDataSource a, DbDataSource b)
        {
            if (a.Name != b.Name)
            {
                return false;
            }
            if (a.Value != b.Value)
            {
                return false;
            }
            return true;
        }

        #endregion Public Properties
    }
}