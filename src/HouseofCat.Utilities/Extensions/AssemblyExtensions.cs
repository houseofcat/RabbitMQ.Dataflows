using System;
using System.Collections.Concurrent;
using System.Reflection;

namespace HouseofCat.Utilities.Extensions;

public static class AssemblyExtensions
{
    private static readonly ConcurrentDictionary<Assembly, string> _assemblyVersion = new ConcurrentDictionary<Assembly, string>();

    public static string GetExecutingSemanticVersion(this Assembly assembly)
    {
        if (_assemblyVersion.TryGetValue(assembly, out var cachedVersion))
        {
            return cachedVersion;
        }

        var version = Assembly.GetExecutingAssembly().GetName().GetFlexibleSemVersion();
        _assemblyVersion.TryAdd(assembly, version);
        return version;
    }

    public static string GetFlexibleSemVersion(this AssemblyName assemblyName)
    {
        if (assemblyName == null)
        {
            return null;
        }

        var versionParts = assemblyName
             .Version
             .ToString()
             .Split('.', StringSplitOptions.RemoveEmptyEntries);

        return (versionParts?.Length ?? 0) switch
        {
            0 => null,
            1 => versionParts[0],
            2 => $"{versionParts[0]}.{versionParts[1]}",
            _ => $"{versionParts[0]}.{versionParts[1]}.{versionParts[2]}"
        };
    }
}
