using Dapper;
using FastMember;
using Microsoft.Data.SqlClient;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Transactions;

namespace HouseofCat.Dapper
{
    public static class SqlServerDapper
    {
        public static T GetFirst<T>(string connectionString, string storedProc, object parameters)
        {
            using var connection = new SqlConnection(connectionString);

            return connection.QueryFirstOrDefault<T>(storedProc, parameters, commandType: CommandType.StoredProcedure);
        }

        public static async Task<T> GetFirstAsync<T>(string connectionString, string storedProc, object parameters)
        {
            using var connection = new SqlConnection(connectionString);

            return await connection.QueryFirstOrDefaultAsync<T>(storedProc, parameters, commandType: CommandType.StoredProcedure).ConfigureAwait(false);
        }

        public static T GetSingle<T>(string connectionString, string storedProc, object parameters)
        {
            using var connection = new SqlConnection(connectionString);

            return connection.QuerySingleOrDefault<T>(storedProc, parameters, commandType: CommandType.StoredProcedure);
        }

        public static async Task<T> GetSingleAsync<T>(string connectionString, string storedProc, object parameters)
        {
            using var connection = new SqlConnection(connectionString);

            return await connection.QuerySingleOrDefaultAsync<T>(storedProc, parameters, commandType: CommandType.StoredProcedure).ConfigureAwait(false);
        }

        public static async Task<T> GetSingleAsync<T>(string connectionString, string storedProc, DynamicParameters dynamicParameters)
        {
            using var connection = new SqlConnection(connectionString);

            return await connection.QuerySingleOrDefaultAsync<T>(storedProc, dynamicParameters, commandType: CommandType.StoredProcedure).ConfigureAwait(false);
        }

        public static IEnumerable<T> GetMany<T>(string connectionString, string storedProc, object parameters)
        {
            using var connection = new SqlConnection(connectionString);

            return connection.Query<T>(storedProc, parameters, commandType: CommandType.StoredProcedure);
        }

        public static IEnumerable<T> GetMany<T>(string connectionString, string storedProc, DynamicParameters dynamicParameters)
        {
            using var connection = new SqlConnection(connectionString);

            return connection.Query<T>(storedProc, dynamicParameters, commandType: CommandType.StoredProcedure);
        }

        public static async Task<IEnumerable<T>> GetManyAsync<T>(string connectionString, string storedProc, object parameters)
        {
            using var connection = new SqlConnection(connectionString);

            return await connection.QueryAsync<T>(storedProc, parameters, commandType: CommandType.StoredProcedure).ConfigureAwait(false);
        }

        public static async Task<IEnumerable<T>> GetManyAsync<T>(string connectionString, string storedProc, DynamicParameters dynamicParameters)
        {
            using var connection = new SqlConnection(connectionString);

            return await connection.QueryAsync<T>(storedProc, dynamicParameters, commandType: CommandType.StoredProcedure).ConfigureAwait(false);
        }

        public static SqlServerGridReader GetGridReader(string connectionString, string storedProc, object parameters)
        {
            var connection = new SqlConnection(connectionString);

            return new SqlServerGridReader(
                connection,
                connection.QueryMultiple(storedProc, parameters, commandType: CommandType.StoredProcedure));
        }

        public static async Task<SqlServerGridReader> GetGridReaderAsync(string connectionString, string storedProc, object parameters)
        {
            var connection = new SqlConnection(connectionString);

            return new SqlServerGridReader(
                connection,
                await connection
                    .QueryMultipleAsync(storedProc, parameters, commandType: CommandType.StoredProcedure)
                    .ConfigureAwait(false));
        }

        public static void Execute(string connectionString, string storedProc, object parameters)
        {
            using var connection = new SqlConnection(connectionString);
            connection.Execute(storedProc, parameters, commandType: CommandType.StoredProcedure);
        }

        public static async Task ExecuteAsync(string connectionString, string storedProc, object parameters)
        {
            using var connection = new SqlConnection(connectionString);
            await connection.ExecuteAsync(storedProc, parameters, commandType: CommandType.StoredProcedure).ConfigureAwait(false);
        }

        public static async Task ExecuteAsync(string connectionString, string storedProc, DynamicParameters dynamicParameters)
        {
            using var connection = new SqlConnection(connectionString);
            await connection.ExecuteAsync(storedProc, dynamicParameters, commandType: CommandType.StoredProcedure).ConfigureAwait(false);
        }

        public static T GetValue<T>(string connectionString, string storedProc, object parameters) where T : struct
        {
            using var connection = new SqlConnection(connectionString);

            return connection.ExecuteScalar<T>(storedProc, parameters, commandType: CommandType.StoredProcedure);
        }

        public static async Task<T> GetValueAsync<T>(string connectionString, string storedProc, object parameters) where T : struct
        {
            using var connection = new SqlConnection(connectionString);

            return await connection.ExecuteScalarAsync<T>(storedProc, parameters, commandType: CommandType.StoredProcedure).ConfigureAwait(false);
        }

        public static async Task<T> GetValueAsync<T>(string connectionString, string storedProc, DynamicParameters dynamicParameters) where T : struct
        {
            using var connection = new SqlConnection(connectionString);

            return await connection.ExecuteScalarAsync<T>(storedProc, dynamicParameters, commandType: CommandType.StoredProcedure).ConfigureAwait(false);
        }

        public static T GetFirstWithTransaction<T>(string connectionString, string storedProc, object parameters)
        {
            using var transactionScope = new TransactionScope();
            using var connection = new SqlConnection(connectionString);

            var result = connection.QueryFirstOrDefault<T>(storedProc, parameters, commandType: CommandType.StoredProcedure);
            transactionScope.Complete();

            return result;
        }

        public static async Task<T> GetFirstWithTransactionAsync<T>(string connectionString, string storedProc, object parameters)
        {
            using var transactionScope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);
            using var connection = new SqlConnection(connectionString);

            var result = await connection.QueryFirstOrDefaultAsync<T>(storedProc, parameters, commandType: CommandType.StoredProcedure).ConfigureAwait(false);
            transactionScope.Complete();

            return result;
        }

        public static T GetSingleWithTransaction<T>(string connectionString, string storedProc, object parameters)
        {
            using var transactionScope = new TransactionScope();
            using var connection = new SqlConnection(connectionString);

            var result = connection.QuerySingleOrDefault<T>(storedProc, parameters, commandType: CommandType.StoredProcedure);
            transactionScope.Complete();

            return result;
        }

        public static async Task<T> GetSingleWithTransactionAsync<T>(string connectionString, string storedProc, object parameters)
        {
            using var transactionScope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);
            using var connection = new SqlConnection(connectionString);

            var result = await connection.QuerySingleOrDefaultAsync<T>(storedProc, parameters, commandType: CommandType.StoredProcedure).ConfigureAwait(false);
            transactionScope.Complete();

            return result;
        }

        public static IEnumerable<T> GetWithTransaction<T>(string connectionString, string storedProc, object parameters)
        {
            using var transactionScope = new TransactionScope();
            using var connection = new SqlConnection(connectionString);

            var result = connection.Query<T>(storedProc, parameters, commandType: CommandType.StoredProcedure);
            transactionScope.Complete();

            return result;
        }

        public static async Task<IEnumerable<T>> GetWithTransactionAsync<T>(string connectionString, string storedProc, object parameters)
        {
            using var transactionScope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);
            using var connection = new SqlConnection(connectionString);

            var result = await connection.QueryAsync<T>(storedProc, parameters, commandType: CommandType.StoredProcedure).ConfigureAwait(false);
            transactionScope.Complete();

            return result;
        }

        public static void ExecuteWithTransaction(string connectionString, string storedProc, object parameters)
        {
            using var transactionScope = new TransactionScope();
            using var connection = new SqlConnection(connectionString);

            connection.Execute(storedProc, parameters, commandType: CommandType.StoredProcedure);
            transactionScope.Complete();
        }

        public static async Task ExecuteWithTransactionAsync(string connectionString, string storedProc, object parameters)
        {
            using var transactionScope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);
            using var connection = new SqlConnection(connectionString);

            await connection.ExecuteAsync(storedProc, parameters, commandType: CommandType.StoredProcedure).ConfigureAwait(false);
            transactionScope.Complete();
        }

        public static T GetValueWithTransaction<T>(string connectionString, string storedProc, object parameters) where T : struct
        {
            using var transactionScope = new TransactionScope();
            using var connection = new SqlConnection(connectionString);

            var result = connection.ExecuteScalar<T>(storedProc, parameters, commandType: CommandType.StoredProcedure);
            transactionScope.Complete();

            return result;
        }

        public static async Task<T> GetValueWithTransactionAsync<T>(string connectionString, string storedProc, object parameters) where T : struct
        {
            using var transactionScope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);
            using var connection = new SqlConnection(connectionString);

            var result = await connection.ExecuteScalarAsync<T>(storedProc, parameters, commandType: CommandType.StoredProcedure).ConfigureAwait(false);
            transactionScope.Complete();

            return result;
        }
    }
}
