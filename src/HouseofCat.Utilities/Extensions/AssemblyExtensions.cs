using HouseofCat.Utilities.Helpers;
using System.Collections.Concurrent;
using System.Reflection;

namespace HouseofCat.Utilities.Extensions;

public static class AssemblyExtensions
{
    private static readonly ConcurrentDictionary<Assembly, string> _assemblyVersion = new ConcurrentDictionary<Assembly, string>();

    public static string GetSemanticVersion(this Assembly assembly)
    {
        if (_assemblyVersion.TryGetValue(assembly, out var cachedVersion))
        {
            return cachedVersion;
        }

        var version = AppHelpers.GetFlexibleSemVersion(Assembly.GetEntryAssembly().GetName());
        _assemblyVersion.TryAdd(assembly, version);
        return version;
    }
}
