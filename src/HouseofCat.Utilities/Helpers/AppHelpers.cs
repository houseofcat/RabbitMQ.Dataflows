using System;
using System.Reflection;

namespace HouseofCat.Utilities.Helpers;

public static class AppHelpers
{
    public static bool IsDebug
    {
        get
        {
#if DEBUG
            return true;
#else
            return false;
#endif
        }
    }

    public static string Env => Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Debug";
    public static bool InDocker => Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == "true";

    public static string GetFlexibleSemVersion(AssemblyName assemblyName)
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
            1 => $"v{versionParts[0]}",
            2 => $"v{versionParts[0]}.{versionParts[1]}",
            _ => $"v{versionParts[0]}.{versionParts[1]}.{versionParts[2]}"
        };
    }
}
