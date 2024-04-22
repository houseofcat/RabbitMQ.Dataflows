using HouseofCat.Utilities.Errors;
using System;
using System.Data;
using System.Data.Common;
using System.IO;
using System.Runtime.Versioning;

namespace HouseofCat.Data.Database;

public static class DbConnectionFactory
{
    public static IDbConnection GetConnection(ConnectionDetails connectionDetails)
    {
        if (OperatingSystem.IsWindows()) return GetConnectionWindows(connectionDetails);

        return GetConnectionAll(connectionDetails);
    }

    private static IDbConnection GetConnectionAll(ConnectionDetails connectionDetails)
    {
        return connectionDetails.Database switch
        {
            Enums.Database.SqlServer =>
                new Microsoft.Data.SqlClient.SqlConnection(
                    GetConnectionString(connectionDetails)),
            Enums.Database.PostgreSql =>
                new Npgsql.NpgsqlConnection(
                    GetConnectionString(connectionDetails)),
            Enums.Database.MySql =>
                new MySql.Data.MySqlClient.MySqlConnection(
                    GetConnectionString(connectionDetails)),
            Enums.Database.OracleSql =>
                new Oracle.ManagedDataAccess.Client.OracleConnection(
                GetConnectionString(connectionDetails)),
            Enums.Database.LegacySqlServer =>
                new System.Data.SqlClient.SqlConnection(
                    GetConnectionString(connectionDetails)),
            Enums.Database.SQLite =>
                new System.Data.SQLite.SQLiteConnection(
                    GetConnectionString(connectionDetails)),
            Enums.Database.Odbc or
            _ =>
                new System.Data.Odbc.OdbcConnection(
                    GetConnectionString(connectionDetails)),
        };
    }

    // Windows only platform support will have more connectivity options like OleDb.
    [SupportedOSPlatform("windows")]
    private static IDbConnection GetConnectionWindows(ConnectionDetails connectionDetails)
    {
        return connectionDetails.Database switch
        {
            Enums.Database.SqlServer =>
                new Microsoft.Data.SqlClient.SqlConnection(
                    GetConnectionString(connectionDetails)),
            Enums.Database.PostgreSql =>
                new Npgsql.NpgsqlConnection(
                    GetConnectionString(connectionDetails)),
            Enums.Database.MySql =>
                new MySql.Data.MySqlClient.MySqlConnection(
                    GetConnectionString(connectionDetails)),
            Enums.Database.OracleSql =>
                new Oracle.ManagedDataAccess.Client.OracleConnection(
                GetConnectionString(connectionDetails)),
            Enums.Database.LegacySqlServer =>
                new System.Data.SqlClient.SqlConnection(
                    GetConnectionString(connectionDetails)),
            Enums.Database.OleDb =>
                new System.Data.OleDb.OleDbConnection(
                    GetConnectionString(connectionDetails)),
            Enums.Database.SQLite =>
                new System.Data.SQLite.SQLiteConnection(
                    GetConnectionString(connectionDetails)),
            Enums.Database.Odbc or
            _ =>
                new System.Data.Odbc.OdbcConnection(
                    GetConnectionString(connectionDetails)),
        };
    }

    public static string GetConnectionString(ConnectionDetails details)
    {
        DbConnectionStringBuilder csb;
        switch (details.Database)
        {
            case Enums.Database.SqlServer:
                csb = GetSqlServerConnectionStringBuilder(details);
                break;
            case Enums.Database.PostgreSql:
                csb = GetNpgsqlConnectionStringBuilder(details);
                break;
            case Enums.Database.MySql:
                csb = GetMySqlConnectionStringBuilder(details);
                break;
            case Enums.Database.OracleSql:
                csb = GetOracleConnectionStringBuilder(details);
                break;
            case Enums.Database.LegacySqlServer:
                csb = GetLegacySqlServerConnectionStringBuilder(details);
                break;
            case Enums.Database.OleDb:
                if (!OperatingSystem.IsWindows()) throw new InvalidOperationException("OleDb is only supported on Windows.");

                csb = GetOleDbConnectionStringBuilder(details);
                break;
            case Enums.Database.SQLite:
                csb = GetSQLiteConnectionStringBuilder(details);
                break;
            case Enums.Database.Odbc:
            case Enums.Database.Default:
            default:
                csb = GetOdbcConnectionStringBuilder(details);
                break;
        }

        return csb.ConnectionString;
    }

    // ConnectionString - OleDb
    // https://docs.microsoft.com/en-us/dotnet/framework/data/adonet/connection-string-syntax
    [SupportedOSPlatform("windows")]
    public static System.Data.OleDb.OleDbConnectionStringBuilder GetOleDbConnectionStringBuilder(ConnectionDetails details)
    {
        var csb = new System.Data.OleDb.OleDbConnectionStringBuilder
        {
            DataSource = details.Host,
        };

        if (details.Metadata.OleDbOptions != null)
        {
            if (!string.IsNullOrWhiteSpace(details.Metadata.OleDbOptions.FileName))
            {
                if (!File.Exists(details.Metadata.OleDbOptions.FileName)) throw new FileNotFoundException(details.Metadata.OleDbOptions.FileName);
                csb.FileName = details.Metadata.OleDbOptions.FileName;
            }

            if (details.Metadata.OleDbOptions.OleDbServices > 0)
            { csb.OleDbServices = details.Metadata.OleDbOptions.OleDbServices; }

            if (!string.IsNullOrWhiteSpace(details.Metadata.OleDbOptions.Provider))
            { csb.Provider = details.Metadata.OleDbOptions.Provider; }

            if (details.Metadata.OleDbOptions.PersistSecurityInfo)
            { csb.PersistSecurityInfo = details.Metadata.OleDbOptions.PersistSecurityInfo; }

            // Directly Transfer custom OdbcOptions Properties to the OdbcConnectionStringBuilder.
            // Username / Password go into the KeyValuePairs.
            foreach (var keyValuePair in details.Metadata.OleDbOptions.Properties)
            {
                csb[keyValuePair.Key] = keyValuePair.Value;
            }
        }

        return csb;
    }

    // ConnectionString - ODBC
    // https://www.connectionstrings.com/net-framework-data-provider-for-odbc/
    public static System.Data.Odbc.OdbcConnectionStringBuilder GetOdbcConnectionStringBuilder(ConnectionDetails details)
    {
        Guard.AgainstNull(details.Metadata.OdbcOptions, nameof(details.Metadata.OdbcOptions));

        var csb = new System.Data.Odbc.OdbcConnectionStringBuilder();

        if (!string.IsNullOrWhiteSpace(details.Metadata.OdbcOptions.Driver))
        { csb.Driver = details.Metadata.OdbcOptions.Driver; }

        if (details.Metadata.OdbcOptions != null)
        {
            if (details.Metadata.OdbcOptions.UseDataSourceName)
            {
                if (details.Metadata.OdbcOptions.Properties.TryGetValue("DSN", out object value))
                {
                    csb.Dsn = (string)value;
                }
                else
                { throw new InvalidOperationException("Odbc UseDataSourceName option enabled but key 'DSN' was not in the OdbcOptions Properties object."); }
            }
            else if (details.Port < 1 && !string.IsNullOrWhiteSpace(details.Host))
            { csb["Server"] = details.Host; }
            else if (!string.IsNullOrWhiteSpace(details.Host))
            { csb["Server"] = $"{details.Host},{details.Port}"; }

            if (!string.IsNullOrWhiteSpace(details.DatabaseName))
            { csb["Database"] = details.DatabaseName; }

            foreach (var keyValuePair in details.Metadata.OdbcOptions.Properties)
            {
                csb[keyValuePair.Key] = keyValuePair.Value;
            }
        }

        return csb;
    }

    // ConnectionString - System.Data.SqlClient
    // https://www.connectionstrings.com/microsoft-data-sqlclient/
    public static System.Data.SqlClient.SqlConnectionStringBuilder GetLegacySqlServerConnectionStringBuilder(
        ConnectionDetails details)
    {
        var csb = new System.Data.SqlClient.SqlConnectionStringBuilder
        {
            InitialCatalog = details.DatabaseName,
            ApplicationIntent = System.Data.SqlClient.ApplicationIntent.ReadOnly,
        };

        if (!string.IsNullOrWhiteSpace(details.Username))
        { csb.UserID = details.Username; }
        else // If username is not provided, use built-in current Windows user Account.
        { csb.IntegratedSecurity = true; }

        if (!string.IsNullOrWhiteSpace(details.Password))
        { csb.Password = details.Password; }

        // Assumed if 0 (and below) it is the default 1433, therefore, not needed in string.
        if (details.Port < 1)
        { csb.DataSource = details.Host; }
        else
        { csb.DataSource = $"{details.Host},{details.Port}"; }

        if (details.Metadata.SqlServerOptions != null)
        {
            // These are all the same. Use built-in Window Authentication.
            // Trusted_Connection=true
            // IntegratedSecurity=true
            // IntegratedSecurity=SSPI 
            if (csb.IntegratedSecurity)
            { csb.IntegratedSecurity = details.Metadata.SqlServerOptions.IntegratedSecurity; }

            csb.MultipleActiveResultSets = details.Metadata.SqlServerOptions.MultipleActiveResultSets;
            csb.MultiSubnetFailover = details.Metadata.SqlServerOptions.MultiSubnetFailover;
            csb.Pooling = details.Metadata.SqlServerOptions.Pooling;
            csb.MaxPoolSize = details.Metadata.SqlServerOptions.MaxPoolSize;
            csb.MinPoolSize = details.Metadata.SqlServerOptions.MinPoolSize;

            // Switches from NamedPipes to TCP/IP communication.
            if (details.Metadata.SqlServerOptions.SwitchToTcp)
            { csb["Network Library"] = "DBMSSOCN"; }

            // Directly Transfer custom SqlServerOptions Properties to the SqlConnectionStringBuilder.
            foreach (var keyValuePair in details.Metadata.SqlServerOptions.Properties)
            {
                csb[keyValuePair.Key] = keyValuePair.Value;
            }
        }

        return csb;
    }

    // ConnectionString - Microsoft.Data.SqlClient
    // https://www.connectionstrings.com/microsoft-data-sqlclient/
    // https://www.connectionstrings.com/the-new-microsoft-data-sqlclient-explained/
    public static Microsoft.Data.SqlClient.SqlConnectionStringBuilder GetSqlServerConnectionStringBuilder(
        ConnectionDetails details)
    {
        var csb = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder
        {
            InitialCatalog = details.DatabaseName,
            ApplicationIntent = Microsoft.Data.SqlClient.ApplicationIntent.ReadOnly,
        };

        if (!string.IsNullOrWhiteSpace(details.Username))
        { csb.UserID = details.Username; }
        else // If username is not provided, use built-in current Windows user Account.
        { csb.IntegratedSecurity = true; }

        if (!string.IsNullOrWhiteSpace(details.Password))
        { csb.Password = details.Password; }

        // Assumed if 0 (and below) it is the default 1433, therefore, not needed in string.
        if (details.Port < 1)
        { csb.DataSource = details.Host; }
        else
        { csb.DataSource = $"{details.Host},{details.Port}"; }

        if (details.Metadata.SqlServerOptions != null)
        {
            // These are all the same. Use built-in Window Authentication.
            // Trusted_Connection=true
            // IntegratedSecurity=true
            // IntegratedSecurity=SSPI 
            if (csb.IntegratedSecurity)
            { csb.IntegratedSecurity = details.Metadata.SqlServerOptions.IntegratedSecurity; }

            csb.MultipleActiveResultSets = details.Metadata.SqlServerOptions.MultipleActiveResultSets;
            csb.MultiSubnetFailover = details.Metadata.SqlServerOptions.MultiSubnetFailover;
            csb.Pooling = details.Metadata.SqlServerOptions.Pooling;
            csb.MaxPoolSize = details.Metadata.SqlServerOptions.MaxPoolSize;
            csb.MinPoolSize = details.Metadata.SqlServerOptions.MinPoolSize;

            // Switches from NamedPipes to TCP/IP communication.
            if (details.Metadata.SqlServerOptions.SwitchToTcp)
            { csb["Network Library"] = "DBMSSOCN"; }

            // Directly Transfer custom SqlServerOptions Properties to the SqlConnectionStringBuilder.
            foreach (var keyValuePair in details.Metadata.SqlServerOptions.Properties)
            {
                csb[keyValuePair.Key] = keyValuePair.Value;
            }
        }

        return csb;
    }

    // ConnectionString - Npgsql
    // https://www.connectionstrings.com/postgresql/
    // https://www.npgsql.org/doc/connection-string-parameters.html
    public static Npgsql.NpgsqlConnectionStringBuilder GetNpgsqlConnectionStringBuilder(
        ConnectionDetails details)
    {
        var csb = new Npgsql.NpgsqlConnectionStringBuilder
        {
            Database = details.DatabaseName,
        };

        if (!string.IsNullOrWhiteSpace(details.Username))
        { csb.Username = details.Username; }

        if (!string.IsNullOrWhiteSpace(details.Password))
        { csb.Password = details.Password; }

        // Assumed if 0 (and below) it is the default 5432, therefore, not needed in string.
        if (details.Port < 1)
        { csb.Host = details.Host; }
        else
        {
            csb.Host = details.Host;
            csb.Port = details.Port;
        }

        if (details.Metadata.NpgsqlOptions != null)
        {
            if (Enum.TryParse<Npgsql.SslMode>(
                details.Metadata.NpgsqlOptions.SslMode,
                out var mode))
            {
                csb.SslMode = mode;
            }

            csb.Pooling = details.Metadata.NpgsqlOptions.Pooling;
            csb.MaxPoolSize = details.Metadata.NpgsqlOptions.MaxPoolSize;
            csb.MinPoolSize = details.Metadata.NpgsqlOptions.MinPoolSize;

            // Directly Transfer custom NpgsqlOptions Properties to the NpgsqlConnectionStringBuilder.
            foreach (var keyValuePair in details.Metadata.NpgsqlOptions.Properties)
            {
                csb[keyValuePair.Key] = keyValuePair.Value;
            }
        }

        return csb;
    }

    private static readonly string _mysqlAllowZeroDateTimeKey = "Allow Zero Datetime";
    private static readonly string _mysqlAllowZeroDateTimeValue = "True";

    // ConnectionString - Mysql
    // https://www.connectionstrings.com/mysql/
    public static MySql.Data.MySqlClient.MySqlConnectionStringBuilder GetMySqlConnectionStringBuilder(
        ConnectionDetails details)
    {
        var csb = new MySql.Data.MySqlClient.MySqlConnectionStringBuilder
        {
            Database = details.DatabaseName,
        };

        if (!string.IsNullOrWhiteSpace(details.Username))
        { csb.UserID = details.Username; }
        else // If username is not provided, use built-in current Windows user Account.
        { csb.IntegratedSecurity = true; }

        if (!string.IsNullOrWhiteSpace(details.Password))
        { csb.Password = details.Password; }

        // Assumed if 0 (and below) it is the default 3306, therefore, not needed in string.
        if (details.Port < 1)
        { csb.Server = details.Host; }
        else
        {
            csb.Server = details.Host;
            csb.Port = (uint)details.Port;
        }

        if (details.Metadata.MySqlOptions != null)
        {
            // These are all the same. Use built-in Window Authentication.
            // Trusted_Connection=true
            // IntegratedSecurity=true
            // IntegratedSecurity=SSPI 
            if (csb.IntegratedSecurity)
            { csb.IntegratedSecurity = details.Metadata.MySqlOptions.IntegratedSecurity; }

            if (!string.IsNullOrWhiteSpace(details.Metadata.MySqlOptions.SslMode)
                && Enum.TryParse<MySql.Data.MySqlClient.MySqlConnectionProtocol>(
                    details.Metadata.MySqlOptions.Protocol,
                    out var protocol))
            {
                csb.ConnectionProtocol = protocol;
            }

            if (!string.IsNullOrWhiteSpace(details.Metadata.MySqlOptions.SslMode)
                && Enum.TryParse<MySql.Data.MySqlClient.MySqlSslMode>(
                    details.Metadata.MySqlOptions.SslMode,
                    out var mode))
            {
                csb.SslMode = mode;
            }

            csb.Pooling = details.Metadata.MySqlOptions.Pooling;
            csb.MaximumPoolSize = details.Metadata.MySqlOptions.MaxPoolSize;
            csb.MinimumPoolSize = details.Metadata.MySqlOptions.MinPoolSize;

            // Directly Transfer custom MySqlOptions Properties to the MySqlConnectionStringBuilder.
            foreach (var keyValuePair in details.Metadata.MySqlOptions.Properties)
            {
                csb[keyValuePair.Key] = keyValuePair.Value;
            }
        }

        csb[_mysqlAllowZeroDateTimeKey] = _mysqlAllowZeroDateTimeValue;

        return csb;
    }

    // ConnectionString - Oracle
    // https://www.connectionstrings.com/oracle/
    // Easy Connect - Oracle
    // https://www.oracle.com/webfolder/technetwork/tutorials/obe/db/dotnet/ODPNET_Core_get_started/index.html#UseEasyConnecttoSetupDatabaseConnection
    public static Oracle.ManagedDataAccess.Client.OracleConnectionStringBuilder GetOracleConnectionStringBuilder(
        ConnectionDetails details)
    {
        var csb = new Oracle.ManagedDataAccess.Client.OracleConnectionStringBuilder();

        if (!string.IsNullOrWhiteSpace(details.Username))
        {
            csb.UserID = details.Username;
            csb["Integrated Security"] = "no";

            if (!string.IsNullOrWhiteSpace(details.Password))
            { csb.Password = details.Password; }
        }
        else // If username is not provided, use built-in current Windows user Account.
        { csb["Integrated Security"] = "yes"; }

        // Assumed if 0 (and below) it is the default 1521, therefore, not needed in string.
        if (details.Port < 1)
        { csb.DataSource = $"{details.Host}/{details.DatabaseName}"; }
        else
        { csb.DataSource = $"{details.Host}:{details.Port}/{details.DatabaseName}"; }

        if (details.Metadata.OracleOptions != null)
        {
            // Use built-in Window Authentication.
            // Integrated Security=SSPI  // when TORCL
            // Integrated Security=yes
            if (details.Metadata.OracleOptions.IntegratedSecurity.Contains("yes"))
            { csb["Integrated Security"] = "yes"; }

            csb.Pooling = details.Metadata.OracleOptions.Pooling;
            csb.MaxPoolSize = details.Metadata.OracleOptions.MaxPoolSize;
            csb.MinPoolSize = details.Metadata.OracleOptions.MinPoolSize;

            // Directly Transfer custom OracleOptions Properties to the OracleConnectionStringBuilder.
            foreach (var keyValuePair in details.Metadata.OracleOptions.Properties)
            {
                csb[keyValuePair.Key] = keyValuePair.Value;
            }
        }

        return csb;
    }

    public static System.Data.SQLite.SQLiteConnectionStringBuilder GetSQLiteConnectionStringBuilder(
        ConnectionDetails details)
    {
        var csb = new System.Data.SQLite.SQLiteConnectionStringBuilder();

        if (!string.IsNullOrWhiteSpace(details.Password))
        { csb.Password = details.Password; }

        csb.DataSource = details.Host;

        if (details.Metadata.SQLiteOptions != null)
        {
            csb.Version = details.Metadata.SQLiteOptions.Version;

            if (details.Metadata.SQLiteOptions.Pooling)
            {
                csb.Pooling = details.Metadata.SQLiteOptions.Pooling;
            }
            if (details.Metadata.SQLiteOptions.MaxPoolSize > 0)
            {
                csb["Max Pool Size"] = details.Metadata.SQLiteOptions.MaxPoolSize;
            }
            if (details.Metadata.SQLiteOptions.MinPoolSize > 0)
            {
                csb["Min Pool Size"] = details.Metadata.SQLiteOptions.MinPoolSize;
            }

            csb.BinaryGUID = details.Metadata.SQLiteOptions.BinaryGuid;
            csb.UseUTF16Encoding = details.Metadata.SQLiteOptions.UTF16Encoding;

            if (details.Metadata.SQLiteOptions.CacheSizeInBytes > 0)
            {
                csb.CacheSize = details.Metadata.SQLiteOptions.CacheSizeInBytes;
            }
            if (details.Metadata.SQLiteOptions.PageSizeInBytes > 0)
            {
                csb.PageSize = details.Metadata.SQLiteOptions.PageSizeInBytes;
            }
            if (details.Metadata.SQLiteOptions.MaxPageCount > 0)
            {
                csb.MaxPageCount = details.Metadata.SQLiteOptions.MaxPageCount;
            }

            csb.ReadOnly = details.Metadata.SQLiteOptions.ReadOnly;

            // Directly Transfer custom SQLiteOptions Properties to the SQLiteConnectionStringBuilder.
            foreach (var keyValuePair in details.Metadata.SQLiteOptions.Properties)
            {
                csb[keyValuePair.Key] = keyValuePair.Value;
            }
        }

        return csb;
    }
}