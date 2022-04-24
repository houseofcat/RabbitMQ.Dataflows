namespace HouseofCat.Data.Database
{
    public class Metadata
    {
        public OdbcOptions OdbcOptions { get; set; }
        public OleDbOptions OleDbOptions { get; set; }
        public SqlServerOptions SqlServerOptions { get; set; }
        public NpgsqlOptions NpgsqlOptions { get; set; }
        public MySqlOptions MySqlOptions { get; set; }
        public OracleOptions OracleOptions { get; set; }
        public SQLiteOptions SQLiteOptions { get; set; }
    }
}
