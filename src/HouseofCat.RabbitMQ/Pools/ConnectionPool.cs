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
    public interface IConnectionPool : IAsyncDisposable
    {
        RabbitOptions Options { get; }
        bool IsRunning { get; }

        Task<bool> StartAsync();
        Task ShutdownAsync();
        IChannelPool GetTransientChannelPool();
        ValueTask<IModel> GetTransientChannelAsync(bool ack = false, CancellationToken cancellationToken = default);
        ValueTask<(IModel channel, Func<IModel, bool, ValueTask> returnFunc)> GetChannelAsync(bool ack = false, CancellationToken cancellationToken = default);
    }

    public class ConnectionPool : IConnectionPool
    {
        public static long GlobalConnectionId = 0;
        public RabbitOptions Options { get; }
        public bool IsRunning { get; private set; }

        private readonly ILogger<ConnectionPool> _logger;
        private readonly IConnectionFactory _connectionFactory;
        private readonly SemaphoreSlim _poolLock = new SemaphoreSlim(1, 1);
        private readonly TimeSpan _recoveryDelay = TimeSpan.FromSeconds(1);

        private Channel<IChannelPool> _healthyPools;
        private Channel<IChannelPool> _unhealthyPools;
        private Task _recoveryTask;

        public ConnectionPool(
            RabbitOptions options)
        {
            Guard.AgainstNull(options, nameof(options));

            Options = options;

            _logger = LogHelper.GetLogger<ConnectionPool>();
            _connectionFactory = CreateConnectionFactory();
        }

        public ConnectionPool(
            RabbitOptions options,
            IConnectionFactory connectionFactory)
        {
            Guard.AgainstNull(options, nameof(options));

            Options = options;

            _logger = LogHelper.GetLogger<ConnectionPool>();
            _connectionFactory = connectionFactory;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ConnectionFactory CreateConnectionFactory()
        {
            var cf = new ConnectionFactory
            {
                AutomaticRecoveryEnabled = true,
                TopologyRecoveryEnabled = Options.FactoryOptions.TopologyRecovery,
                NetworkRecoveryInterval = TimeSpan.FromSeconds(Options.FactoryOptions.NetRecoveryTimeout),
                ContinuationTimeout = TimeSpan.FromSeconds(Options.FactoryOptions.ContinuationTimeout),
                RequestedHeartbeat = TimeSpan.FromSeconds(Options.FactoryOptions.HeartbeatInterval),
                RequestedChannelMax = Options.FactoryOptions.MaxChannelsPerConnection,
                DispatchConsumersAsync = Options.FactoryOptions.EnableDispatchConsumersAsync,
            };

            if (Options.FactoryOptions.Uri != null)
            {
                cf.Uri = Options.FactoryOptions.Uri;
            }
            else
            {
                cf.VirtualHost = Options.FactoryOptions.VirtualHost;
                cf.HostName = Options.FactoryOptions.HostName;
                cf.UserName = Options.FactoryOptions.UserName;
                cf.Password = Options.FactoryOptions.Password;
                if (Options.FactoryOptions.Port != AmqpTcpEndpoint.UseDefaultPort)
                {
                    cf.Port = Options.FactoryOptions.Port;
                }
            }

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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private IConnection CreateConnecton()
        {
            var connectionName = $"{Options.PoolOptions.ServiceName}:{Interlocked.Increment(ref GlobalConnectionId)}";
            return _connectionFactory.CreateConnection(connectionName);
        }

        public async Task<bool> StartAsync()
        {
            await _poolLock.WaitAsync().ConfigureAwait(false);

            try
            {
                if (IsRunning) return IsRunning;

                IsRunning = true;
                _healthyPools = Channel.CreateBounded<IChannelPool>(Options.PoolOptions.MaxConnections);
                _unhealthyPools = Channel.CreateBounded<IChannelPool>(Options.PoolOptions.MaxConnections);
                _recoveryTask = RecoverConnections();

                for (int i = 0; i < Options.PoolOptions.MaxConnections; i++)
                {
                    await _healthyPools
                        .Writer
                        .WriteAsync(GetTransientChannelPool())
                        .ConfigureAwait(false);
                }
            }
            finally
            {
                _poolLock.Release();
            }

            return IsRunning;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private async Task RecoverConnections()
        {
            try
            {
                await foreach (var pool in _unhealthyPools.Reader.ReadAllAsync().ConfigureAwait(false))
                {
                    if (pool.IsHealthy)
                    {
                        await _healthyPools.Writer.WriteAsync(pool).ConfigureAwait(false);
                    }
                    else
                    {
                        await _unhealthyPools.Writer.WriteAsync(pool).ConfigureAwait(false);
                        await Task.Delay(_recoveryDelay);
                    }
                }
            }
            catch { }
        }

        public async Task ShutdownAsync()
        {
            await _poolLock.WaitAsync().ConfigureAwait(false);

            try
            {
                if (!IsRunning) return;

                IsRunning = false;

                await _recoveryTask.ConfigureAwait(false);
                _recoveryTask = null;

                _healthyPools.Writer.TryComplete();
                _unhealthyPools.Writer.TryComplete();

                await foreach (var pool in _healthyPools.Reader.ReadAllAsync())
                {
                    await pool.DisposeAsync().ConfigureAwait(false);
                }

                await foreach (var pool in _unhealthyPools.Reader.ReadAllAsync())
                {
                    await pool.DisposeAsync().ConfigureAwait(false);
                }
            }
            finally
            {
                _poolLock.Release();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IChannelPool GetTransientChannelPool()
        {
            return new ChannelPool(Options, CreateConnecton());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public async ValueTask<IModel> GetTransientChannelAsync(
            bool ack,
            CancellationToken cancellationToken = default)
        {
            if (IsRunning) throw new InvalidOperationException(ExceptionMessages.ChannelPoolValidationMessage);

            while (await _healthyPools.Reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false))
            {
                if (_healthyPools.Reader.TryRead(out var pool))
                {
                    try
                    {
                        var channel = pool.GetTransientChannel(ack);
                        return channel;
                    }
                    finally
                    {
                        // Immediately return the pool
                        await _healthyPools.Writer.WriteAsync(pool).ConfigureAwait(false);
                    }
                }
            }

            throw new InvalidOperationException(ExceptionMessages.ChannelPoolValidationMessage);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public async ValueTask<(IModel, Func<IModel, bool, ValueTask>)> GetChannelAsync(
            bool ack,
            CancellationToken cancellationToken = default)
        {
            if (IsRunning) throw new InvalidOperationException(ExceptionMessages.ChannelPoolValidationMessage);

            while (await _healthyPools.Reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false))
            {
                if (_healthyPools.Reader.TryRead(out var pool))
                {
                    try
                    {
                        var channel = await pool.GetChannelAsync(ack, cancellationToken).ConfigureAwait(false);
                        if (ack) return (channel, pool.ReturnAckChannelAsync);
                        return (channel, pool.ReturnChannelAsync);
                    }
                    finally
                    {
                        // Immediately return the pool
                        await _healthyPools.Writer.WriteAsync(pool).ConfigureAwait(false);
                    }
                }
            }

            throw new InvalidOperationException(ExceptionMessages.ChannelPoolValidationMessage);
        }

        public async ValueTask DisposeAsync()
        {
            // TODO
        }
    }
}
