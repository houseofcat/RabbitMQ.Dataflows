namespace HouseofCat.Dapper
{
    public static class Enums
    {
        public enum DatabaseType
        {
            Default,
            OleDb,
            Odbc,
            LegacySqlServer,
            SqlServer,
            PostgreSql,
            MySql,
            OracleSql
        }

        public enum UnitOfTime
        {
            Millisecond,
            Second,
            Minute,
            Hour,
            Day
        }
    }
}
