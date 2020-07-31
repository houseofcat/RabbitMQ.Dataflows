using FastMember;
using System.Collections.Generic;
using System.Linq;

namespace HouseofCat.Reflection
{
    public static class Extensions
    {
        public static Dictionary<string, object> ToDictionary<TIn>(this TIn input, bool readOnlyProperties = false) where TIn : class, new()
        {
            var acessor = TypeAccessor.Create(input.GetType());

            if (readOnlyProperties)
            {
                return acessor
                    .GetMembers()
                    .Where(x => x.CanRead)
                    .ToDictionary(x => x.Name, x => acessor[input, x.Name]);
            }

            return acessor
                .GetMembers()
                .ToDictionary(x => x.Name, x => acessor[input, x.Name]);
        }
    }
}
