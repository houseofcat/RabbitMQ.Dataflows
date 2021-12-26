using System.Collections.Concurrent;
using System.Data;
using System.Data.SqlClient;
using System.Threading;
using System.Threading.Tasks;

namespace HouseofCat.Gremlins
{
    public class SqlConnectionGremlin
    {
        private long _currentConnectionId = 0;
        private long _currentConnectionIdToRemove = 0;
        private readonly object _syncRoot = new object();

        /// <summary>
        /// Connection storage during SqlConnectionGremlin usage.
        /// </summary>
        public ConcurrentDictionary<long, SqlConnection> Connections { get; set; } = new ConcurrentDictionary<long, SqlConnection>();

        /// <summary>
        /// Adds an open SqlConnection to memory.
        /// </summary>
        /// <param name="connectionString"></param>
        /// <returns></returns>
        public Task AddOpenConnectionAsync(string connectionString)
        {
            lock (_syncRoot)
            {
                var conn = new SqlConnection(connectionString);
                conn.Open();

                Connections.TryAdd(_currentConnectionId, conn);
                Interlocked.Increment(ref _currentConnectionId);
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// Adds a number of SqlConnections to memory.
        /// </summary>
        /// <param name="connectionString"></param>
        /// <param name="connectionsToCreate"></param>
        /// <returns></returns>
        public async Task AddOpenConnectionsAsync(string connectionString, int connectionsToCreate)
        {
            for (int i = 0; i < connectionsToCreate; i++)
            {
                await AddOpenConnectionAsync(connectionString).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Closes a SqlConnection that is open in memory, in the order they were opened.
        /// </summary>
        public Task CloseOpenConnectionAsync()
        {
            lock (_syncRoot)
            {
                if (_currentConnectionId >= _currentConnectionIdToRemove)
                {
                    if (Connections.ContainsKey(_currentConnectionIdToRemove))
                    {
                        if (Connections.TryGetValue(_currentConnectionIdToRemove, out SqlConnection conn))
                        {
                            try { conn.Close(); }
                            catch { }

                            conn.Dispose();

                            Interlocked.Increment(ref _currentConnectionIdToRemove);
                        }
                    }
                }
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// Closes SqlConnections that are open in memory, in the order they were open.
        /// </summary>
        /// <param name="connectionsToClose"></param>
        public async Task CloseOpenConnectionsAsync(int connectionsToClose)
        {
            for (int i = 0; i < connectionsToClose; i++)
            {
                await CloseOpenConnectionAsync().ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Gets the current number of stored SqlConnections that are open.
        /// </summary>
        /// <returns>Current number of stored connections whose ConnectionState is Open.</returns>
        public Task<long> GetOpenConnectionsCountAsync()
        {
            var openCount = 0L;

            foreach (var kvp in Connections)
            {
                if (kvp.Value.State == ConnectionState.Open)
                { openCount++; }
            }

            return Task.FromResult(openCount);
        }

        /// <summary>
        /// Closes and disposes all connections in memory. Resets internal ConnectionId values too.
        /// </summary>
        /// <returns></returns>
        public Task ResetGremlinAsync()
        {
            lock (_syncRoot)
            {
                foreach (var kvp in Connections)
                {
                    if (kvp.Value != null)
                    {
                        kvp.Value.Close();
                        kvp.Value.Dispose();
                    }
                }

                Connections.Clear();
                _currentConnectionId = 0;
                _currentConnectionIdToRemove = 0;
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// Empties the connection pool associated with the specified connection.
        /// </summary>
        /// <remarks>
        /// ClearPool clears the connection pool that is associated with the connection. If additional connections associated 
        /// with connection are in use at the time of the call, they are marked appropriately and are discarded (instead of 
        /// being returned to the pool) when Close is called on them.
        /// </remarks>
        /// <param name="connection"></param>
        public void ClearPool(SqlConnection connection)
        {
            SqlConnection.ClearPool(connection);
        }

        /// <summary>
        /// Empties the connection pool.
        /// </summary>
        /// <remarks>
        /// ClearAllPools resets (or empties) the connection pool. If there are connections in use at the time of the call, 
        /// they are marked appropriately and will be discarded (instead of being returned to the pool) when Close is 
        /// called on them.
        /// </remarks>
        public void ClearAllPools()
        {
            SqlConnection.ClearAllPools();
        }
    }
}
