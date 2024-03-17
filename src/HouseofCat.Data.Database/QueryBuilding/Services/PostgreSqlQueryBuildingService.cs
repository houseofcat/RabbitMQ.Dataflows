using SqlKata.Compilers;
using static HouseofCat.Data.Database.Enums;

namespace HouseofCat.Data.Database.QueryBuilding.Services;

public class PostgreSqlQueryBuildingService : BaseQueryBuildingService
{
    private readonly PostgresCompiler _compiler = new PostgresCompiler();

    public string BuildSqlQueryString(
        Statement statement,
        Case casing = Case.AsIs,
        bool sqlWithParameters = true)
    {
        if (sqlWithParameters)
        {
            return _compiler
                .Compile(BuildQueryFromStatement(statement, casing))
                .ToString();
        }
        else
        {
            return _compiler
                .Compile(BuildQueryFromStatement(statement, casing))
                .Sql;
        }
    }
}
