using Dapper;
using HouseofCat.Data.Database;
using Microsoft.SqlServer.Types;
using System.Collections.Generic;
using System.Data;
using System.Reflection;
using System.Runtime.Loader;
using System.Threading.Tasks;

namespace HouseofCat.Database.Dapper;

public static class DapperHelper
{
    public static IDbConnection GetConnection(ConnectionDetails connectionDetails)
    {
        return DbConnectionFactory.GetConnection(connectionDetails);
    }

    public static IDataReader GetDataReader(ConnectionDetails connectionDetails, QueryPlan queryPlan)
    {
        var connection = DbConnectionFactory.GetConnection(connectionDetails);

        if (queryPlan.Parameters?.Count > 0)
        {
            var parameters = new DynamicParameters();
            foreach (var parameter in queryPlan.Parameters)
            {
                parameters.Add(
                    parameter.Name,
                    parameter.Value,
                    parameter.DbType,
                    parameter.Direction,
                    parameter.Size,
                    parameter.Precision,
                    parameter.Scale);
            }

            return ExecuteReader(connection, queryPlan.Query, parameters);
        }

        return ExecuteReader(connection, queryPlan.Query, null);
    }

    public static async Task<IDataReader> GetDataReaderAsync(ConnectionDetails connectionDetails, QueryPlan queryPlan)
    {
        var connection = DbConnectionFactory.GetConnection(connectionDetails);

        if (queryPlan.Parameters?.Count > 0)
        {
            var parameters = new DynamicParameters();
            foreach (var parameter in queryPlan.Parameters)
            {
                parameters.Add(
                    parameter.Name,
                    parameter.Value,
                    parameter.DbType,
                    parameter.Direction,
                    parameter.Size,
                    parameter.Precision,
                    parameter.Scale);
            }

            return await ExecuteReaderAsync(connection, queryPlan.Query, parameters)
                .ConfigureAwait(false);
        }

        return await ExecuteReaderAsync(connection, queryPlan.Query, null)
            .ConfigureAwait(false);
    }

    public static IDataReader GetDataReader(IDbConnection connection, QueryPlan queryPlan)
    {
        if (queryPlan.Parameters?.Count > 0)
        {
            var parameters = new DynamicParameters();
            foreach (var parameter in queryPlan.Parameters)
            {
                parameters.Add(
                    parameter.Name,
                    parameter.Value,
                    parameter.DbType,
                    parameter.Direction,
                    parameter.Size,
                    parameter.Precision,
                    parameter.Scale);
            }

            return ExecuteReader(connection, queryPlan.Query, parameters);
        }

        return ExecuteReader(connection, queryPlan.Query, null);
    }

    public static async Task<IDataReader> GetDataReaderAsync(IDbConnection connection, QueryPlan queryPlan)
    {
        if (queryPlan.Parameters?.Count > 0)
        {
            var parameters = new DynamicParameters();
            foreach (var parameter in queryPlan.Parameters)
            {
                parameters.Add(
                    parameter.Name,
                    parameter.Value,
                    parameter.DbType,
                    parameter.Direction,
                    parameter.Size,
                    parameter.Precision,
                    parameter.Scale);
            }

            return await ExecuteReaderAsync(connection, queryPlan.Query, parameters)
                .ConfigureAwait(false);
        }

        return await ExecuteReaderAsync(connection, queryPlan.Query, null)
            .ConfigureAwait(false);
    }

    public static IDataReader GetDataReader(ConnectionDetails connectionDetails, string sqlText, List<Parameter> parameters = null)
    {
        var connection = DbConnectionFactory.GetConnection(connectionDetails);

        if (parameters?.Count > 0)
        {
            var dynamicParameters = new DynamicParameters();
            foreach (var parameter in parameters)
            {
                dynamicParameters.Add(
                    parameter.Name,
                    parameter.Value,
                    parameter.DbType,
                    parameter.Direction,
                    parameter.Size,
                    parameter.Precision,
                    parameter.Scale);
            }

            return ExecuteReader(connection, sqlText, dynamicParameters);
        }

        return ExecuteReader(connection, sqlText, null);
    }

    public static async Task<IDataReader> GetDataReaderAsync(ConnectionDetails connectionDetails, string sqlText, List<Parameter> parameters = null)
    {
        var connection = DbConnectionFactory.GetConnection(connectionDetails);

        if (parameters?.Count > 0)
        {
            var dynamicParameters = new DynamicParameters();
            foreach (var parameter in parameters)
            {
                dynamicParameters.Add(
                    parameter.Name,
                    parameter.Value,
                    parameter.DbType,
                    parameter.Direction,
                    parameter.Size,
                    parameter.Precision,
                    parameter.Scale);
            }

            return await ExecuteReaderAsync(connection, sqlText, dynamicParameters)
                .ConfigureAwait(false);
        }

        return await ExecuteReaderAsync(connection, sqlText, null)
            .ConfigureAwait(false);
    }

    public static IDataReader GetDataReader(IDbConnection connection, string sqlText, List<Parameter> parameters = null)
    {
        if (parameters?.Count > 0)
        {
            var dynamicParameters = new DynamicParameters();
            foreach (var parameter in parameters)
            {
                dynamicParameters.Add(
                    parameter.Name,
                    parameter.Value,
                    parameter.DbType,
                    parameter.Direction,
                    parameter.Size,
                    parameter.Precision,
                    parameter.Scale);
            }

            return ExecuteReader(connection, sqlText, dynamicParameters);
        }

        return ExecuteReader(connection, sqlText, null);
    }

    public static async Task<IDataReader> GetDataReaderAsync(IDbConnection connection, string sqlText, List<Parameter> parameters = null)
    {
        if (parameters?.Count > 0)
        {
            var dynamicParameters = new DynamicParameters();
            foreach (var parameter in parameters)
            {
                dynamicParameters.Add(
                    parameter.Name,
                    parameter.Value,
                    parameter.DbType,
                    parameter.Direction,
                    parameter.Size,
                    parameter.Precision,
                    parameter.Scale);
            }

            return await ExecuteReaderAsync(connection, sqlText, dynamicParameters)
                .ConfigureAwait(false);
        }

        return await ExecuteReaderAsync(connection, sqlText, null)
            .ConfigureAwait(false);
    }

    public static T GetSingle<T>(
        IDbConnection connection,
        string sql,
        DynamicParameters parameters,
        CommandType commandType = CommandType.Text,
        int? timeout = null)
    {
        return connection.QuerySingleOrDefault<T>(sql, parameters, null, timeout, commandType);
    }

    public static Task<T> GetSingleAsync<T>(
        IDbConnection connection,
        string sql,
        DynamicParameters parameters,
        CommandType commandType = CommandType.Text,
        int? timeout = null)
    {
        return connection.QuerySingleOrDefaultAsync<T>(sql, parameters, null, timeout, commandType);
    }

    public static IEnumerable<T> GetMany<T>(
        IDbConnection connection,
        string sql,
        DynamicParameters parameters,
        CommandType commandType = CommandType.Text,
        int? timeout = null)
    {
        return connection.Query<T>(sql, parameters, null, commandTimeout: timeout, commandType: commandType);
    }

    public static Task<IEnumerable<T>> GetManyAsync<T>(
        IDbConnection connection,
        string sql,
        DynamicParameters parameters,
        CommandType commandType = CommandType.Text,
        int? timeout = null)
    {
        return connection.QueryAsync<T>(sql, parameters, null, timeout, commandType);
    }

    public static DapperGridReader GetGridReader(
        IDbConnection connection,
        string sql,
        DynamicParameters parameters,
        CommandType commandType = CommandType.Text,
        int? timeout = null)
    {
        return new DapperGridReader(
            connection,
            connection.QueryMultiple(sql, parameters, null, timeout, commandType));
    }

    public static async Task<DapperGridReader> GetGridReaderAsync(
        IDbConnection connection,
        string sql,
        DynamicParameters parameters,
        CommandType commandType = CommandType.Text,
        int? timeout = null)
    {
        return new DapperGridReader(
            connection,
            await connection
                .QueryMultipleAsync(sql, parameters, null, timeout, commandType)
                .ConfigureAwait(false));
    }

    public static void Execute(
        IDbConnection connection,
        string sql,
        DynamicParameters parameters,
        CommandType commandType = CommandType.Text,
        int? timeout = null)
    {
        connection.Execute(sql, parameters, null, timeout, commandType);
    }

    public static Task ExecuteAsync(
        IDbConnection connection,
        string sql,
        DynamicParameters parameters,
        CommandType commandType = CommandType.Text,
        int? timeout = null)
    {
        return connection
            .ExecuteAsync(sql, parameters, null, timeout, commandType);
    }

    public static IDataReader ExecuteReader(
        IDbConnection connection,
        string sql,
        DynamicParameters parameters,
        CommandType commandType = CommandType.Text,
        int? timeout = null)
    {
        var definition = new CommandDefinition(sql, parameters, null, timeout, commandType, CommandFlags.Buffered);

        return connection.ExecuteReader(definition, CommandBehavior.SequentialAccess);
    }

    public static Task<IDataReader> ExecuteReaderAsync(
        IDbConnection connection,
        string sql,
        DynamicParameters parameters,
        CommandType commandType = CommandType.Text,
        int? timeout = null)
    {
        var definition = new CommandDefinition(sql, parameters, null, timeout, commandType, CommandFlags.Buffered);

        return connection
            .ExecuteReaderAsync(definition, CommandBehavior.SequentialAccess);
    }

    #region SqlServer Mechanical Workarounds

    public static void DisableSqlGeography()
    {
        // Not currently working for new Microsoft client with dapper.
        // Something is wrong with the bytes.
        SqlMapper.AddTypeHandler(new NoParseSqlGeographyTypeHandler());
        SqlMapper.RemoveTypeMap(typeof(SqlGeography));
    }

    public static void DisableSqlGeometry()
    {
        // Not currently working for new Microsoft client with dapper.
        // Something is wrong with the bytes.
        SqlMapper.AddTypeHandler(new NoParseSqlGeometryTypeHandler());
        SqlMapper.RemoveTypeMap(typeof(SqlGeometry));
    }

    public static void ReplaceMicrosoftSqlServerTypeAssemblyResolution()
    {
        AssemblyLoadContext.Default.Resolving += OnAssemblyResolve;
    }

    private static readonly string MicrosoftSqlServerTypeAssembly = "Microsoft.SqlServer.Types";

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

    #endregion
}
