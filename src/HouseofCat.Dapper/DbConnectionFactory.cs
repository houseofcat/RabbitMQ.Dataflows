using System;
using System.Data;
using System.Data.Common;
using System.Runtime.Versioning;
using static HouseofCat.Dapper.Enums;

namespace HouseofCat.Dapper
{
    public static class DbConnectionFactory
    {
        public static IDbConnection GetConnection(ConnectionDetails connectionDetails)
        {
            if (OperatingSystem.IsWindows()) return GetConnectionWindows(connectionDetails);

            return GetConnectionAll(connectionDetails);
        }

        private static IDbConnection GetConnectionAll(ConnectionDetails connectionDetails)
        {
            return connectionDetails.DBType switch
            {
                DatabaseType.SqlServer =>
                    new Microsoft.Data.SqlClient.SqlConnection(
                        GetConnectionString(connectionDetails)),
                DatabaseType.PostgreSql =>
                    new Npgsql.NpgsqlConnection(
                        GetConnectionString(connectionDetails)),
                DatabaseType.MySql =>
                    new MySql.Data.MySqlClient.MySqlConnection(
                        GetConnectionString(connectionDetails)),
                DatabaseType.OracleSql =>
                    new Oracle.ManagedDataAccess.Client.OracleConnection(
                    GetConnectionString(connectionDetails)),
                DatabaseType.LegacySqlServer =>
                    new System.Data.SqlClient.SqlConnection(
                        GetConnectionString(connectionDetails)),
                _ =>
                    new System.Data.Odbc.OdbcConnection(
                        GetConnectionString(connectionDetails)),
            };
        }

        // Windows only platform support will have more connectivity options like OleDb.
        [SupportedOSPlatform("windows")]
        private static IDbConnection GetConnectionWindows(ConnectionDetails connectionDetails)
        {
            return connectionDetails.DBType switch
            {
                DatabaseType.SqlServer =>
                    new Microsoft.Data.SqlClient.SqlConnection(
                        GetConnectionString(connectionDetails)),
                DatabaseType.PostgreSql =>
                    new Npgsql.NpgsqlConnection(
                        GetConnectionString(connectionDetails)),
                DatabaseType.MySql =>
                    new MySql.Data.MySqlClient.MySqlConnection(
                        GetConnectionString(connectionDetails)),
                DatabaseType.OracleSql =>
                    new Oracle.ManagedDataAccess.Client.OracleConnection(
                    GetConnectionString(connectionDetails)),
                DatabaseType.LegacySqlServer =>
                    new System.Data.SqlClient.SqlConnection(
                        GetConnectionString(connectionDetails)),
                DatabaseType.OleDb =>
                    new System.Data.OleDb.OleDbConnection(
                        GetConnectionString(connectionDetails)),
                _ => // DatabaseType.Odbc, DatabaseType.Default, and switch "default"
                    new System.Data.Odbc.OdbcConnection(
                        GetConnectionString(connectionDetails)),
            };
        }

        public static string GetConnectionString(ConnectionDetails connectionDetails)
        {
            DbConnectionStringBuilder csb;
            switch (connectionDetails.DBType)
            {
                case DatabaseType.SqlServer:
                    csb = GetSqlServerConnectionStringBuilder(connectionDetails);
                    break;
                case DatabaseType.PostgreSql:
                    csb = GetNpgsqlConnectionStringBuilder(connectionDetails);
                    break;
                case DatabaseType.MySql:
                    csb = GetMySqlConnectionStringBuilder(connectionDetails);
                    break;
                case DatabaseType.OracleSql:
                    csb = GetOracleConnectionStringBuilder(connectionDetails);
                    break;
                case DatabaseType.LegacySqlServer:
                    csb = GetLegacySqlServerConnectionStringBuilder(connectionDetails);
                    break;
                case DatabaseType.OleDb:
                    if (!OperatingSystem.IsWindows()) throw new InvalidOperationException("OleDb is only supported on Windows.");

                    csb = GetOleDbConnectionStringBuilder(connectionDetails);
                    break;
                case DatabaseType.Odbc:
                case DatabaseType.Default:
                default:
                    csb = GetOdbcConnectionStringBuilder(connectionDetails);
                    break;
            }

            return csb.ConnectionString;
        }

        // ConnectionString - OleDb
        // https://docs.microsoft.com/en-us/dotnet/framework/data/adonet/connection-string-syntax
        [SupportedOSPlatform("windows")]
        public static System.Data.OleDb.OleDbConnectionStringBuilder GetOleDbConnectionStringBuilder(ConnectionDetails connectionDetails)
        {
            var csb = new System.Data.OleDb.OleDbConnectionStringBuilder
            {
                DataSource = connectionDetails.Host,
            };

            if (connectionDetails.Metadata.OleDbOptions != null)
            {
                if (!string.IsNullOrWhiteSpace(connectionDetails.Metadata.OleDbOptions.FileName))
                { csb.FileName = connectionDetails.Metadata.OleDbOptions.FileName; }

                if (connectionDetails.Metadata.OleDbOptions.OleDbServices > 0)
                { csb.OleDbServices = connectionDetails.Metadata.OleDbOptions.OleDbServices; }

                if (!string.IsNullOrWhiteSpace(connectionDetails.Metadata.OleDbOptions.Provider))
                { csb.Provider = connectionDetails.Metadata.OleDbOptions.Provider; }

                if (connectionDetails.Metadata.OleDbOptions.PersistSecurityInfo)
                { csb.PersistSecurityInfo = connectionDetails.Metadata.OleDbOptions.PersistSecurityInfo; }

                // Directly Transfer custom OdbcOptions Properties to the OdbcConnectionStringBuilder.
                // Username / Password go into the KeyValuePairs.
                foreach (var keyValuePair in connectionDetails.Metadata.OleDbOptions.Properties)
                {
                    csb[keyValuePair.Key] = keyValuePair.Value;
                }
            }

            return csb;
        }

        // ConnectionString - ODBC
        // https://www.connectionstrings.com/net-framework-data-provider-for-odbc/
        public static System.Data.Odbc.OdbcConnectionStringBuilder GetOdbcConnectionStringBuilder(ConnectionDetails connectionDetails)
        {
            var csb = new System.Data.Odbc.OdbcConnectionStringBuilder
            {
                Driver = connectionDetails.Metadata.OdbcOptions.Driver
            };

            if (connectionDetails.Metadata.OdbcOptions != null)
            {
                if (connectionDetails.Metadata.OdbcOptions.UseDataSourceName)
                {
                    if (connectionDetails.Metadata.OdbcOptions.Properties.ContainsKey("Dsn"))
                    {
                        csb.Dsn = (string)connectionDetails.Metadata.OdbcOptions.Properties["Dsn"];
                    }
                    else
                    { throw new Exception("Odbc UseDataSourceName option enabled but key 'Dsn' was not in the OdbcOptions Properties object."); }
                }
                else if (connectionDetails.Port < 1)
                { csb["Server"] = $"{connectionDetails.Host}"; }
                else
                { csb["Server"] = $"{connectionDetails.Host},{connectionDetails.Port}"; }

                if (!string.IsNullOrWhiteSpace(connectionDetails.DatabaseName))
                { csb["Database"] = connectionDetails.DatabaseName; }

                // Directly Transfer custom OdbcOptions Properties to the OdbcConnectionStringBuilder.
                // Username / Password go into the KeyValuePairs.
                foreach (var keyValuePair in connectionDetails.Metadata.OdbcOptions.Properties)
                {
                    csb[keyValuePair.Key] = keyValuePair.Value;
                }
            }

            return csb;
        }

        // ConnectionString - System.Data.SqlClient
        // https://www.connectionstrings.com/microsoft-data-sqlclient/
        public static System.Data.SqlClient.SqlConnectionStringBuilder GetLegacySqlServerConnectionStringBuilder(
            ConnectionDetails connectionDetails)
        {
            var csb = new System.Data.SqlClient.SqlConnectionStringBuilder
            {
                InitialCatalog = connectionDetails.DatabaseName,
                ApplicationIntent = System.Data.SqlClient.ApplicationIntent.ReadOnly,
            };

            if (!string.IsNullOrWhiteSpace(connectionDetails.Username))
            { csb.UserID = connectionDetails.Username; }
            else // If username is not provided, use built-in current Windows user Account.
            { csb.IntegratedSecurity = true; }

            if (!string.IsNullOrWhiteSpace(connectionDetails.Password))
            { csb.Password = connectionDetails.Password; }

            // Assumed if 0 (and below) it is the default 1433, therefore, not needed in string.
            if (connectionDetails.Port < 1)
            { csb.DataSource = $"{connectionDetails.Host}"; }
            else
            { csb.DataSource = $"{connectionDetails.Host},{connectionDetails.Port}"; }

            if (connectionDetails.Metadata.SqlServerOptions != null)
            {
                // These are all the same. Use built-in Window Authentication.
                // Trusted_Connection=true
                // IntegratedSecurity=true
                // IntegratedSecurity=SSPI 
                if (csb.IntegratedSecurity)
                { csb.IntegratedSecurity = connectionDetails.Metadata.SqlServerOptions.IntegratedSecurity; }

                csb.MultipleActiveResultSets = connectionDetails.Metadata.SqlServerOptions.MultipleActiveResultSets;
                csb.MultiSubnetFailover = connectionDetails.Metadata.SqlServerOptions.MultiSubnetFailover;
                csb.Pooling = connectionDetails.Metadata.SqlServerOptions.Pooling;
                csb.MaxPoolSize = connectionDetails.Metadata.SqlServerOptions.MaxPoolSize;
                csb.MinPoolSize = connectionDetails.Metadata.SqlServerOptions.MinPoolSize;

                // Switches from NamedPipes to TCP/IP communication.
                if (connectionDetails.Metadata.SqlServerOptions.SwitchToTcp)
                { csb["Network Library"] = "DBMSSOCN"; }

                // Directly Transfer custom SqlServerOptions Properties to the SqlConnectionStringBuilder.
                foreach (var keyValuePair in connectionDetails.Metadata.SqlServerOptions.Properties)
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
            ConnectionDetails connectionDetails)
        {
            var csb = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder
            {
                InitialCatalog = connectionDetails.DatabaseName,
                ApplicationIntent = Microsoft.Data.SqlClient.ApplicationIntent.ReadOnly,
            };

            if (!string.IsNullOrWhiteSpace(connectionDetails.Username))
            { csb.UserID = connectionDetails.Username; }
            else // If username is not provided, use built-in current Windows user Account.
            { csb.IntegratedSecurity = true; }

            if (!string.IsNullOrWhiteSpace(connectionDetails.Password))
            { csb.Password = connectionDetails.Password; }

            // Assumed if 0 (and below) it is the default 1433, therefore, not needed in string.
            if (connectionDetails.Port < 1)
            { csb.DataSource = $"{connectionDetails.Host}"; }
            else
            { csb.DataSource = $"{connectionDetails.Host},{connectionDetails.Port}"; }

            if (connectionDetails.Metadata.SqlServerOptions != null)
            {
                // These are all the same. Use built-in Window Authentication.
                // Trusted_Connection=true
                // IntegratedSecurity=true
                // IntegratedSecurity=SSPI 
                if (csb.IntegratedSecurity)
                { csb.IntegratedSecurity = connectionDetails.Metadata.SqlServerOptions.IntegratedSecurity; }

                if (connectionDetails.Metadata.SqlServerOptions.MultipleActiveResultSets)
                { csb.MultipleActiveResultSets = connectionDetails.Metadata.SqlServerOptions.MultipleActiveResultSets; }

                if (csb.MultiSubnetFailover = connectionDetails.Metadata.SqlServerOptions.MultiSubnetFailover)
                { csb.MultiSubnetFailover = connectionDetails.Metadata.SqlServerOptions.MultiSubnetFailover; }

                csb.Pooling = connectionDetails.Metadata.SqlServerOptions.Pooling;
                csb.MaxPoolSize = connectionDetails.Metadata.SqlServerOptions.MaxPoolSize;
                csb.MinPoolSize = connectionDetails.Metadata.SqlServerOptions.MinPoolSize;

                // Switches from NamedPipes to TCP/IP communication.
                if (connectionDetails.Metadata.SqlServerOptions.SwitchToTcp)
                { csb["Network Library"] = "DBMSSOCN"; }

                // Directly Transfer custom SqlServerOptions Properties to the SqlConnectionStringBuilder.
                foreach (var keyValuePair in connectionDetails.Metadata.SqlServerOptions.Properties)
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
            ConnectionDetails connectionDetails)
        {
            var csb = new Npgsql.NpgsqlConnectionStringBuilder()
            {
                Database = connectionDetails.DatabaseName,
            };

            if (!string.IsNullOrWhiteSpace(connectionDetails.Username))
            { csb.Username = connectionDetails.Username; }
            else // If username is not provided, use built-in current Windows user Account.
            { csb.IntegratedSecurity = true; }

            if (!string.IsNullOrWhiteSpace(connectionDetails.Password))
            { csb.Password = connectionDetails.Password; }

            // Assumed if 0 (and below) it is the default 5432, therefore, not needed in string.
            if (connectionDetails.Port < 1)
            { csb.Host = $"{connectionDetails.Host}"; }
            else
            {
                csb.Host = connectionDetails.Host;
                csb.Port = connectionDetails.Port;
            }

            if (connectionDetails.Metadata.NpgsqlOptions != null)
            {
                // These are all the same. Use built-in Window Authentication.
                // Trusted_Connection=true
                // IntegratedSecurity=true
                // IntegratedSecurity=SSPI 
                if (csb.IntegratedSecurity)
                { csb.IntegratedSecurity = connectionDetails.Metadata.NpgsqlOptions.IntegratedSecurity; }

                if (Enum.TryParse<Npgsql.SslMode>(
                    connectionDetails.Metadata.NpgsqlOptions.SslMode,
                    out var mode))
                {
                    csb.SslMode = mode;
                }

                csb.Pooling = connectionDetails.Metadata.NpgsqlOptions.Pooling;
                csb.MaxPoolSize = connectionDetails.Metadata.NpgsqlOptions.MaxPoolSize;
                csb.MinPoolSize = connectionDetails.Metadata.NpgsqlOptions.MinPoolSize;

                // Directly Transfer custom NpgsqlOptions Properties to the NpgsqlConnectionStringBuilder.
                foreach (var keyValuePair in connectionDetails.Metadata.NpgsqlOptions.Properties)
                {
                    csb[keyValuePair.Key] = keyValuePair.Value;
                }
            }

            return csb;
        }

        // ConnectionString - Mysql
        // https://www.connectionstrings.com/mysql/
        public static MySql.Data.MySqlClient.MySqlConnectionStringBuilder GetMySqlConnectionStringBuilder(
            ConnectionDetails connectionDetails)
        {
            var csb = new MySql.Data.MySqlClient.MySqlConnectionStringBuilder()
            {
                Database = connectionDetails.DatabaseName,
            };

            if (!string.IsNullOrWhiteSpace(connectionDetails.Username))
            { csb.UserID = connectionDetails.Username; }
            else // If username is not provided, use built-in current Windows user Account.
            { csb.IntegratedSecurity = true; }

            if (!string.IsNullOrWhiteSpace(connectionDetails.Password))
            { csb.Password = connectionDetails.Password; }

            // Assumed if 0 (and below) it is the default 3306, therefore, not needed in string.
            if (connectionDetails.Port < 1)
            { csb.Server = $"{connectionDetails.Host}"; }
            else
            {
                csb.Server = connectionDetails.Host;
                csb.Port = (uint)connectionDetails.Port;
            }

            if (connectionDetails.Metadata.MySqlOptions != null)
            {
                // These are all the same. Use built-in Window Authentication.
                // Trusted_Connection=true
                // IntegratedSecurity=true
                // IntegratedSecurity=SSPI 
                if (csb.IntegratedSecurity)
                { csb.IntegratedSecurity = connectionDetails.Metadata.MySqlOptions.IntegratedSecurity; }

                if (!string.IsNullOrWhiteSpace(connectionDetails.Metadata.MySqlOptions.SslMode)
                    && Enum.TryParse<MySql.Data.MySqlClient.MySqlConnectionProtocol>(
                        connectionDetails.Metadata.MySqlOptions.Protocol,
                        out var protocol))
                {
                    csb.ConnectionProtocol = protocol;
                }

                if (!string.IsNullOrWhiteSpace(connectionDetails.Metadata.MySqlOptions.SslMode)
                    && Enum.TryParse<MySql.Data.MySqlClient.MySqlSslMode>(
                        connectionDetails.Metadata.MySqlOptions.SslMode,
                        out var mode))
                {
                    csb.SslMode = mode;
                }

                csb.Pooling = connectionDetails.Metadata.MySqlOptions.Pooling;
                csb.MaximumPoolSize = connectionDetails.Metadata.MySqlOptions.MaxPoolSize;
                csb.MinimumPoolSize = connectionDetails.Metadata.MySqlOptions.MinPoolSize;

                // Directly Transfer custom MySqlOptions Properties to the MySqlConnectionStringBuilder.
                foreach (var keyValuePair in connectionDetails.Metadata.MySqlOptions.Properties)
                {
                    csb[keyValuePair.Key] = keyValuePair.Value;
                }
            }

            return csb;
        }

        // ConnectionString - Oracle
        // https://www.connectionstrings.com/oracle/
        // Easy Connect - Oracle
        // https://www.oracle.com/webfolder/technetwork/tutorials/obe/db/dotnet/ODPNET_Core_get_started/index.html#UseEasyConnecttoSetupDatabaseConnection
        public static Oracle.ManagedDataAccess.Client.OracleConnectionStringBuilder GetOracleConnectionStringBuilder(
            ConnectionDetails connectionDetails)
        {
            var csb = new Oracle.ManagedDataAccess.Client.OracleConnectionStringBuilder();

            if (!string.IsNullOrWhiteSpace(connectionDetails.Username))
            {
                csb.UserID = connectionDetails.Username;
                csb["Integrated Security"] = "no";

                if (!string.IsNullOrWhiteSpace(connectionDetails.Password))
                { csb.Password = connectionDetails.Password; }
            }
            else // If username is not provided, use built-in current Windows user Account.
            { csb["Integrated Security"] = "yes"; }

            // Assumed if 0 (and below) it is the default 1521, therefore, not needed in string.
            if (connectionDetails.Port < 1)
            { csb.DataSource = $"{connectionDetails.Host}/{connectionDetails.DatabaseName}"; }
            else
            { csb.DataSource = $"{connectionDetails.Host}:{connectionDetails.Port}/{connectionDetails.DatabaseName}"; }

            if (connectionDetails.Metadata.OracleOptions != null)
            {
                // Use built-in Window Authentication.
                // Integrated Security=SSPI  // when TORCL
                // Integrated Security=yes
                if (connectionDetails.Metadata.OracleOptions.IntegratedSecurity.Contains("yes"))
                { csb["Integrated Security"] = "yes"; }

                csb.Pooling = connectionDetails.Metadata.OracleOptions.Pooling;
                csb.MaxPoolSize = connectionDetails.Metadata.OracleOptions.MaxPoolSize;
                csb.MinPoolSize = connectionDetails.Metadata.OracleOptions.MinPoolSize;

                // Directly Transfer custom OracleOptions Properties to the OracleConnectionStringBuilder.
                foreach (var keyValuePair in connectionDetails.Metadata.OracleOptions.Properties)
                {
                    csb[keyValuePair.Key] = keyValuePair.Value;
                }
            }

            return csb;
        }
    }
}