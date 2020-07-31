using Dapper;
using FastMember;
using System.Collections.Generic;
using System.Linq;
using HouseofCat.Reflection;

namespace HouseofCat.Dapper
{
    public static class DapperHelper
    {
        public static DynamicParameters ToDynamicParameters<TIn>(this TIn input, bool readOnlyProperties) where TIn : class, new()
        {
            return new DynamicParameters(input.ToDictionary(readOnlyProperties));
        }
    }
}
