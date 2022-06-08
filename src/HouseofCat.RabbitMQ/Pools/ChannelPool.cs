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
    public interface IChannelPool : IAsyncDisposable
    {
        IConnection Connection { get; }
        RabbitOptions Options { get; }
        bool IsRunning { get; }
        bool IsHealthy { get; }

        Task StartAsync();
        Task ShutdownAsync();
        IModel GetTransientChannel(bool ack = false);
        ValueTask<IModel> GetChannelAsync(bool ack = false, CancellationToken cancellationToken = default);
        ValueTask ReturnChannelAsync(IModel channel, bool channelError);
        ValueTask ReturnAckChannelAsync(IModel channel, bool channelError);
    }

    public class ChannelPool : IChannelPool
    {
        public IConnection Connection { get; }
        public RabbitOptions Options { get; }
        public bool IsRunning { get; private set; }

        private readonly ILogger<ChannelPool> _logger;
        private readonly int _healthyCount;
        private readonly SemaphoreSlim _poolLock = new SemaphoreSlim(1, 1);

        private Channel<IModel> _channels;
        private Channel<IModel> _ackChannels;
        private int _channelCount;
        private int _ackChannelCount;
        private Task _recoveryTask;

        public ChannelPool(RabbitOptions options, IConnection connection)
        {
            Guard.AgainstNull(connection, nameof(connection));
            Guard.AgainstNull(options, nameof(options));

            Options = options;
            Connection = connection;

            _logger = LogHelper.GetLogger<ChannelPool>();
            _healthyCount = Options.PoolOptions.MaxChannels / 2; // Pool is healthy enough to get channels from if > half
        }

        public bool IsHealthy => Connection.IsOpen && _channelCount >= _healthyCount && _ackChannelCount >= _healthyCount;

        public async Task StartAsync()
        {
            if (!await _poolLock.WaitAsync(0).ConfigureAwait(false)) return;

            if (IsRunning) return;

            try
            {
                _channels = Channel.CreateBounded<IModel>(Options.PoolOptions.MaxChannels);
                _ackChannels = Channel.CreateBounded<IModel>(Options.PoolOptions.MaxChannels);
                _channelCount = 0;
                _ackChannelCount = 0;
                _recoveryTask = RecoverChannels();
                IsRunning = true;
            }
            finally
            {
                _poolLock.Release();
            }
        }

        public async Task ShutdownAsync()
        {
            if (!await _poolLock.WaitAsync(0).ConfigureAwait(false)) return;

            if (!IsRunning) return;

            try
            {
                _channels.Writer.TryComplete();
                _ackChannels.Writer.TryComplete();

                await foreach (var channel in _channels.Reader.ReadAllAsync().ConfigureAwait(false))
                {
                    channel.Dispose();
                };
                await foreach (var channel in _ackChannels.Reader.ReadAllAsync().ConfigureAwait(false))
                {
                    channel.Dispose();
                };
                _channelCount = 0;
                _ackChannelCount = 0;

                await _recoveryTask.ConfigureAwait(false);
                _recoveryTask = null;

                IsRunning = false;
            }
            finally
            {
                _poolLock.Release();
            }
        }

        // Channels can sometimes error/close unexpectedly even when the connection does not close.
        // We will attempt to recover channels as long as the connection is healthy
        private readonly TimeSpan _recoveryDelay = TimeSpan.FromSeconds(1);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private async Task RecoverChannels()
        {
            try
            {
                while (IsRunning)
                {
                    // If connection is not open or at max channels delay
                    if (!Connection.IsOpen || _channelCount >= Options.PoolOptions.MaxChannels || _ackChannelCount >= Options.PoolOptions.MaxChannels)
                    {
                        await Task.Delay(_recoveryDelay).ConfigureAwait(false);
                    }

                    while (_channelCount < Options.PoolOptions.MaxChannels)
                    {
                        var channel = Connection.CreateModel();
                        await _channels
                            .Writer
                            .WriteAsync(channel)
                            .ConfigureAwait(false);
                        Interlocked.Increment(ref _channelCount);
                    }

                    while (_ackChannelCount < Options.PoolOptions.MaxChannels)
                    {
                        var channel = Connection.CreateModel();
                        channel.ConfirmSelect();
                        await _ackChannels
                            .Writer
                            .WriteAsync(channel)
                            .ConfigureAwait(false);
                        Interlocked.Increment(ref _ackChannelCount);
                    }
                }
            }
            catch { }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IModel GetTransientChannel(bool ack)
        {
            if (!Connection.IsOpen) throw new InvalidOperationException(ExceptionMessages.ChannelPoolGetChannelError);
            var channel = Connection.CreateModel();
            channel.ConfirmSelect();
            return channel;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public async ValueTask<IModel> GetChannelAsync(
            bool ack = false,
            CancellationToken cancellationToken = default)
        {
            if (!IsRunning) throw new InvalidOperationException(ExceptionMessages.ChannelPoolValidationMessage);
            if (!Connection.IsOpen) throw new InvalidOperationException(ExceptionMessages.ChannelPoolGetChannelError);

            if (ack)
            {
                return await _ackChannels
                    .Reader
                    .ReadAsync(cancellationToken)
                    .ConfigureAwait(false);
            }

            return await _channels
                .Reader
                .ReadAsync(cancellationToken)
                .ConfigureAwait(false);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public async ValueTask ReturnChannelAsync(
            IModel channel,
            bool channelError)
        {
            if (!IsRunning) throw new InvalidOperationException(ExceptionMessages.ChannelPoolValidationMessage);

            // Tristan asserts that once channels have errored, they can no longer be re-used/recovered. Test this, but
            // if true, we simply do not return a channel to the pool, and allow the recovery tasks to replenish.
            if (channelError)
            {
                Interlocked.Decrement(ref _channelCount);
                channel.Dispose();
            }
            else
            {
                await _channels
                    .Writer
                    .WriteAsync(channel)
                    .ConfigureAwait(false);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public async ValueTask ReturnAckChannelAsync(
            IModel channel,
            bool channelError)
        {
            if (!IsRunning) throw new InvalidOperationException(ExceptionMessages.ChannelPoolValidationMessage);

            // Tristan asserts that once channels have errored, they can no longer be re-used/recovered. Test this, but
            // if true, we simply do not return a channel to the pool, and allow the recovery tasks to replenish.
            if (channelError)
            {
                Interlocked.Decrement(ref _ackChannelCount);
                channel.Dispose();
            }
            else
            {
                await _ackChannels
                    .Writer
                    .WriteAsync(channel)
                    .ConfigureAwait(false);
            }
        }

        public async ValueTask DisposeAsync()
        {
            Connection.Dispose();

            // TODO
        }
    }
}