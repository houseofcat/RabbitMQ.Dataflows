using FastMember;
using System.Collections.Generic;
using System.Linq;

namespace HouseofCat.Reflection;

public static class Extensions
{
    public static Dictionary<string, object> ToDictionary<TIn>(this TIn input, bool readOnlyProperties = false) where TIn : class, new()
    {
        var acessor = TypeAccessor.Create(input.GetType());

        if (readOnlyProperties)
        {
            return acessor
                .GetMembers()
                .Where(x => x.CanRead && x.CanWrite == false)
                .ToDictionary(x => x.Name, x => acessor[input, x.Name]);
        }

        return acessor
            .GetMembers()
            .ToDictionary(x => x.Name, x => acessor[input, x.Name]);
    }

    public static TOut ToObject<TOut>(this IDictionary<string, object> data) where TOut : class, new()
    {
        var t = new TOut();
        var acessor = TypeAccessor.Create(t.GetType());

        foreach (var kvp in data)
        {
            acessor[t, kvp.Key] = kvp.Value;
        }

        return t;
    }
}
