# HouseofCat.Dapper

## DapperHelper
Convenience methods wrapping Dapper, mostly around acquiring DataReaders.

## DbConnectionFactory

Wired up to centralize the building of Connections / ConnectionStrings for Vendor's Drivers

 * System.Data.SqlClient
 * Microsoft.Data.SqlClient
 * Npgsql
 * Mysql.Data.Client
 * Oracle.ManagedDataAccess.Client

#### Generic

 * System.Data.Odbc

#### Extra Windows

 * System.Data.OleDb

### SqlGeography / SqlGeometry

Allows you to replace `Microsoft.SqlServer.Types` from Microsoft with NetStandard version from [dotMorten](https://github.com/dotMorten/Microsoft.SqlServer.Types)

Does not work with `Microsoft.Data.SqlClient` yet (someone would have to compile that and release a NuGet for that.)

```csharp
public static void ReplaceMicrosoftSqlServerTypeAssemblyResolution()
{
    AssemblyLoadContext.Default.Resolving += OnAssemblyResolve;
}

private static Assembly OnAssemblyResolve(
    AssemblyLoadContext assemblyLoadContext,
    AssemblyName assemblyName)
{
    try
    {
        AssemblyLoadContext.Default.Resolving -= OnAssemblyResolve;
        return assemblyLoadContext.LoadFromAssemblyName(assemblyName);
    }
    catch
    {
        // Intercept assembly load context failure
        // Check to see if it's Dapper loading a DLL from .NET framework.
        if (assemblyName.Name == MicrosoftSqlServerTypeAssembly)
        { return typeof(SqlGeography).Assembly; }

        throw; // New other error
    }
    finally
    {
        AssemblyLoadContext.Default.Resolving += OnAssemblyResolve;
    }
}
```

Includes some Mappers to not use SqlGeometry or SqlGeography etc.