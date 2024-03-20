using static HouseofCat.Data.Database.Enums;

namespace HouseofCat.Data.Database;

public interface IQueryBuildingService
{
    string BuildQuery(
        Enums.Database database,
        Statement statement,
        Case casing = Case.AsIs,
        bool sqlWithParameters = true);
}

public class QueryBuildingService : IQueryBuildingService
{
    private readonly SqlServerQueryBuildingService _sqlServer;
    private readonly PostgreSqlQueryBuildingService _postgreSql;
    private readonly MySqlQueryBuildingService _mySql;
    private readonly OracleSqlQueryBuildingService _oracle;
    private readonly FirebirdQueryBuildingService _firebird;
    private readonly SQLiteQueryBuildingService _sqlite;

    public QueryBuildingService()
    {
        _sqlServer = new SqlServerQueryBuildingService();
        _postgreSql = new PostgreSqlQueryBuildingService();
        _mySql = new MySqlQueryBuildingService();
        _oracle = new OracleSqlQueryBuildingService();
        _firebird = new FirebirdQueryBuildingService();
        _sqlite = new SQLiteQueryBuildingService();
    }

    public string BuildQuery(
        Enums.Database database,
        Statement statement,
        Case casing = Case.AsIs,
        bool sqlWithParameters = true)
    {
        return database switch
        {
            Enums.Database.LegacySqlServer or
            Enums.Database.SqlServer => _sqlServer.BuildSqlQueryString(statement, casing, sqlWithParameters),
            Enums.Database.PostgreSql => _postgreSql.BuildSqlQueryString(statement, casing, sqlWithParameters),
            Enums.Database.MySql => _mySql.BuildSqlQueryString(statement, casing, sqlWithParameters),
            Enums.Database.OracleSql => _oracle.BuildSqlQueryString(statement, casing, sqlWithParameters),
            Enums.Database.Firebird => _firebird.BuildSqlQueryString(statement, casing, sqlWithParameters),
            Enums.Database.SQLite => _sqlite.BuildSqlQueryString(statement, casing, sqlWithParameters),
            _ => string.Empty,
        };
    }
}
