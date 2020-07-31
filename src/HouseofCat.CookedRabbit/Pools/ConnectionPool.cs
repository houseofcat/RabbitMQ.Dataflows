using CookedRabbit.Core.Utils;
using HouseofCat.Logger;
using HouseofCat.Utilities.Errors;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace CookedRabbit.Core.Pools
{
    public interface IConnectionPool
    {
        Config Config { get; }

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

        public Config Config { get; }

        public ConnectionPool(Config config)
        {
            Guard.AgainstNull(config, nameof(config));
            Config = config;

            _logger = LogHelper.GetLogger<ConnectionPool>();

            _connections = Channel.CreateBounded<IConnectionHost>(Config.PoolSettings.MaxConnections);
            _connectionFactory = CreateConnectionFactory();

            CreateConnectionsAsync().GetAwaiter().GetResult();
        }

        private ConnectionFactory CreateConnectionFactory()
        {
            var cf = new ConnectionFactory
            {
                Uri = Config.FactorySettings.Uri,
                AutomaticRecoveryEnabled = true,
                TopologyRecoveryEnabled = Config.FactorySettings.TopologyRecovery,
                NetworkRecoveryInterval = TimeSpan.FromSeconds(Config.FactorySettings.NetRecoveryTimeout),
                ContinuationTimeout = TimeSpan.FromSeconds(Config.FactorySettings.ContinuationTimeout),
                RequestedHeartbeat = TimeSpan.FromSeconds(Config.FactorySettings.HeartbeatInterval),
                RequestedChannelMax = Config.FactorySettings.MaxChannelsPerConnection,
                DispatchConsumersAsync = Config.FactorySettings.EnableDispatchConsumersAsync,
            };

            if (Config.FactorySettings.SslSettings.EnableSsl)
            {
                cf.Ssl = new SslOption
                {
                    Enabled = Config.FactorySettings.SslSettings.EnableSsl,
                    AcceptablePolicyErrors = Config.FactorySettings.SslSettings.AcceptedPolicyErrors,
                    ServerName = Config.FactorySettings.SslSettings.CertServerName,
                    CertPath = Config.FactorySettings.SslSettings.LocalCertPath,
                    CertPassphrase = Config.FactorySettings.SslSettings.LocalCertPassword,
                    Version = Config.FactorySettings.SslSettings.ProtocolVersions
                };
            }

            return cf;
        }

        public IConnection CreateConnection(string connectionName) => _connectionFactory.CreateConnection(connectionName);

        private async Task CreateConnectionsAsync()
        {
            _logger.LogTrace(LogMessages.ConnectionPool.CreateConnections);

            for (int i = 0; i < Config.PoolSettings.MaxConnections; i++)
            {
                var serviceName = string.IsNullOrEmpty(Config.PoolSettings.ServiceName) ? $"CookedRabbit:{i}" : $"{Config.PoolSettings.ServiceName}:{i}";
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
                    await Task.Delay(Config.PoolSettings.SleepOnErrorInterval).ConfigureAwait(false);
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
