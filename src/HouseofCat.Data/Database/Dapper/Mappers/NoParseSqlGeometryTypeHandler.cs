using Dapper;
using Microsoft.SqlServer.Types;
using System.Data;

namespace HouseofCat.Database.Dapper;

public class NoParseSqlGeometryTypeHandler : SqlMapper.TypeHandler<SqlGeometry>
{
    public override SqlGeometry Parse(object value)
    { return default; }

    public override void SetValue(IDbDataParameter parameter, SqlGeometry value)
    { /* NO-OP */ }
}
