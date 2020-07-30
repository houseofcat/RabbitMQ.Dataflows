using Dapper;
using FastMember;
using System.Collections.Generic;
using System.Linq;

namespace HouseofCat.Library.Dapper
{
    public static class DapperHelper
    {
        public static DynamicParameters ConstructDynamicParameters<TIn>(TIn input) where TIn : new()
        {
            var objectMemberAccessor = TypeAccessor.Create(input.GetType());

            return new DynamicParameters(
                objectMemberAccessor
                    .GetMembers()
                    .ToDictionary(x => x.Name, x => objectMemberAccessor[input, x.Name]));
        }

        public static DynamicParameters ConstructDynamicParametersFromReadableProperties<TIn>(TIn input) where TIn : new()
        {
            var objectMemberAccessor = TypeAccessor.Create(input.GetType());
            var propertyDictionary = new Dictionary<string, object>();
            foreach (var member in objectMemberAccessor.GetMembers())
            {
                if (member.CanRead)
                { propertyDictionary.Add(member.Name, objectMemberAccessor[input, member.Name]); }
            }

            return new DynamicParameters(propertyDictionary);
        }
    }
}
