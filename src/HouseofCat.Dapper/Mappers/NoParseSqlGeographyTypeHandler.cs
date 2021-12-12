using Dapper;
using System.Data;

namespace HouseofCat.Dapper
{
    public class NoParseSqlGeographyTypeHandler : SqlMapper.TypeHandler<Microsoft.SqlServer.Types.SqlGeography>
    {
        public override Microsoft.SqlServer.Types.SqlGeography Parse(object value)
        { return default; }

        public override void SetValue(IDbDataParameter parameter, Microsoft.SqlServer.Types.SqlGeography value)
        { /* NO-OP */ }
    }
}
