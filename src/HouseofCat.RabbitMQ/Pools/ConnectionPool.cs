using HouseofCat.Logger;
using HouseofCat.Utilities.Errors;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace HouseofCat.RabbitMQ.Pools
{
    public interface IConnectionPool
    {
        Options Options { get; }

        IConnection CreateConnection(string connectionName);
        ValueTask<IConnectionHost> GetConnectionAsync();
        ValueTask ReturnConnectionAsync(IConnectionHost connHost);

        Task ShutdownAsync();
    }

    public class ConnectionPool : IConnectionPool, IDisposable
    {
        private readonly ILogger<ConnectionPool> _logger;
        private readonly SemaphoreSlim _poolLock = new SemaphoreSlim(1, 1);

        private Channel<IConnectionHost> _connections;
        private ConnectionFactory _connectionFactory;
        private bool _disposedValue;
        private ulong _currentConnectionId;

        public Options Options { get; }

        public ConnectionPool(Options options)
        {
            Guard.AgainstNull(options, nameof(options));
            Options = options;

            _logger = LogHelper.GetLogger<ConnectionPool>();

            _connections = Channel.CreateBounded<IConnectionHost>(Options.PoolOptions.MaxConnections);
            _connectionFactory = CreateConnectionFactory();

            CreateConnectionsAsync().GetAwaiter().GetResult();
        }

        private ConnectionFactory CreateConnectionFactory()
        {
            var cf = new ConnectionFactory
            {
                Uri = Options.FactoryOptions.Uri,
                AutomaticRecoveryEnabled = true,
                TopologyRecoveryEnabled = Options.FactoryOptions.TopologyRecovery,
                NetworkRecoveryInterval = TimeSpan.FromSeconds(Options.FactoryOptions.NetRecoveryTimeout),
                ContinuationTimeout = TimeSpan.FromSeconds(Options.FactoryOptions.ContinuationTimeout),
                RequestedHeartbeat = TimeSpan.FromSeconds(Options.FactoryOptions.HeartbeatInterval),
                RequestedChannelMax = Options.FactoryOptions.MaxChannelsPerConnection,
                DispatchConsumersAsync = Options.FactoryOptions.EnableDispatchConsumersAsync,
            };

            if (Options.FactoryOptions.SslOptions.EnableSsl)
            {
                cf.Ssl = new SslOption
                {
                    Enabled = Options.FactoryOptions.SslOptions.EnableSsl,
                    AcceptablePolicyErrors = Options.FactoryOptions.SslOptions.AcceptedPolicyErrors,
                    ServerName = Options.FactoryOptions.SslOptions.CertServerName,
                    CertPath = Options.FactoryOptions.SslOptions.LocalCertPath,
                    CertPassphrase = Options.FactoryOptions.SslOptions.LocalCertPassword,
                    Version = Options.FactoryOptions.SslOptions.ProtocolVersions
                };
            }

            return cf;
        }

        public IConnection CreateConnection(string connectionName) => _connectionFactory.CreateConnection(connectionName);

        private async Task CreateConnectionsAsync()
        {
            _logger.LogTrace(LogMessages.ConnectionPool.CreateConnections);

            for (int i = 0; i < Options.PoolOptions.MaxConnections; i++)
            {
                var serviceName = string.IsNullOrEmpty(Options.PoolOptions.ServiceName) ? $"HoC.RabbitMQ:{i}" : $"{Options.PoolOptions.ServiceName}:{i}";
                try
                {
                    await _connections
                        .Writer
                        .WriteAsync(new ConnectionHost(_currentConnectionId++, CreateConnection(serviceName)));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, LogMessages.ConnectionPool.CreateConnectionException, serviceName);
                    throw; // Non Optional Throw
                }
            }

            _logger.LogTrace(LogMessages.ConnectionPool.CreateConnectionsComplete);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public async ValueTask<IConnectionHost> GetConnectionAsync()
        {
            if (!await _connections
                .Reader
                .WaitToReadAsync()
                .ConfigureAwait(false))
            {
                throw new InvalidOperationException(ExceptionMessages.GetConnectionErrorMessage);
            }

            while (true)
            {
                var connHost = await _connections
                    .Reader
                    .ReadAsync().ConfigureAwait(false);

                // Connection Health Check
                var healthy = await connHost.HealthyAsync().ConfigureAwait(false);
                if (!healthy)
                {
                    await ReturnConnectionAsync(connHost).ConfigureAwait(false);
                    await Task.Delay(Options.PoolOptions.SleepOnErrorInterval).ConfigureAwait(false);
                    continue;
                }

                return connHost;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public async ValueTask ReturnConnectionAsync(IConnectionHost connHost)
        {
            if (!await _connections
                    .Writer
                    .WaitToWriteAsync()
                    .ConfigureAwait(false))
            {
                throw new InvalidOperationException(ExceptionMessages.GetConnectionErrorMessage);
            }

            await _connections
                .Writer
                .WriteAsync(connHost);
        }

        public async Task ShutdownAsync()
        {
            _logger.LogTrace(LogMessages.ConnectionPool.Shutdown);

            await _poolLock
                .WaitAsync()
                .ConfigureAwait(false);

            _connections.Writer.Complete();

            await _connections.Reader.WaitToReadAsync().ConfigureAwait(false);
            while (_connections.Reader.TryRead(out IConnectionHost connHost))
            {
                try
                { connHost.Close(); }
                catch { }
            }

            _poolLock.Release();

            _logger.LogTrace(LogMessages.ConnectionPool.ShutdownComplete);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _poolLock.Dispose();
                }

                _connectionFactory = null;
                _connections = null;
                _disposedValue = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
