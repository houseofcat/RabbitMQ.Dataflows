using HouseofCat.Logger;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Threading.Channels;
using System.Threading.Tasks;
using static HouseofCat.RabbitMQ.LogMessages;

namespace HouseofCat.RabbitMQ.Pools
{
    public interface IChannelPool
    {
        RabbitOptions Options { get; }
        ulong CurrentChannelId { get; }
        bool Shutdown { get; }

        /// <summary>
        /// This pulls an ackable <see cref="IChannelHost"/> out of the <see cref="IChannelPool"/> for usage.
        /// <para>If the <see cref="IChannelHost"/> was previously flagged on error, multi-attempts to recreate it before returning an open channel back to the user.
        /// If you only remove channels and never add them, you will drain your <see cref="IChannelPool"/>.</para>
        /// <para>Use <see cref="ReturnChannelAsync"/> to return Channels.</para>
        /// <para><em>Note: During an outage event, you will pause here until a viable channel can be acquired.</em></para>
        /// </summary>
        /// <returns><see cref="IChannelHost"/></returns>
        ValueTask<IChannelHost> GetAckChannelAsync();

        /// <summary>
        /// This pulls a <see cref="IChannelHost"/> out of the <see cref="IChannelPool"/> for usage.
        /// <para>If the <see cref="IChannelHost"/> was previously flagged on error, multi-attempts to recreate it before returning an open channel back to the user.
        /// If you only remove channels and never add them, you will drain your <see cref="IChannelPool"/>.</para>
        /// <para>Use <see cref="ReturnChannelAsync"/> to return the <see cref="IChannelHost"/>.</para>
        /// <para><em>Note: During an outage event, you will pause here until a viable channel can be acquired.</em></para>
        /// </summary>
        /// <returns><see cref="IChannelHost"/></returns>
        ValueTask<IChannelHost> GetChannelAsync();

        /// <summary>
        /// <para>Gives user a transient <see cref="IChannelHost"/> is simply a channel not managed by this library.</para>
        /// <para><em>Closing and disposing the <see cref="IChannelHost"/> is the responsiblity of the user.</em></para>
        /// </summary>
        /// <param name="ackable"></param>
        /// <returns><see cref="IChannelHost"/></returns>
        ValueTask<IChannelHost> GetTransientChannelAsync(bool ackable);

        ValueTask ReturnChannelAsync(IChannelHost chanHost, bool flagChannel = false);
        Task ShutdownAsync();
    }

    public class ChannelPool : TransientChannelPool
    {
        private Channel<IChannelHost> _channels;
        private Channel<IChannelHost> _ackChannels;
        private ConcurrentDictionary<ulong, bool> _flaggedChannels;

        public ChannelPool(RabbitOptions options) : this(new ConnectionPool(options)) { }

        public ChannelPool(IConnectionPool connPool) : base(connPool, LogHelper.GetLogger<ChannelPool>())
        {
            CurrentChannelId = 1;
            _flaggedChannels = new ConcurrentDictionary<ulong, bool>();
            _channels = Channel.CreateBounded<IChannelHost>(Options.PoolOptions.MaxChannels);
            _ackChannels = Channel.CreateBounded<IChannelHost>(Options.PoolOptions.MaxChannels);

            CreateChannelsAsync().GetAwaiter().GetResult();
        }

        private async Task CreateChannelsAsync()
        {
            for (var i = 0; i < Options.PoolOptions.MaxChannels; i++)
            {
                var chanHost = await CreateChannelAsync(CurrentChannelId++, false).ConfigureAwait(false);
                _flaggedChannels[chanHost.ChannelId] = false;

                await _channels
                    .Writer
                    .WriteAsync(chanHost)
                    .ConfigureAwait(false);
            }

            for (var i = 0; i < Options.PoolOptions.MaxChannels; i++)
            {
                var chanHost = await CreateChannelAsync(CurrentChannelId++, true).ConfigureAwait(false);
                _flaggedChannels[chanHost.ChannelId] = false;

                await _ackChannels
                    .Writer
                    .WriteAsync(chanHost)
                    .ConfigureAwait(false);
            }
        }

        /// <summary>
        /// This pulls a <see cref="IChannelHost"/> out of the <see cref="IChannelPool"/> for usage.
        /// <para>If the <see cref="IChannelHost"/> was previously flagged on error, multi-attempta to recreate it before returning an open channel back to the user.
        /// If you only remove channels and never add them, you will drain your <see cref="IChannelPool"/>.</para>
        /// <para>Use <see cref="ReturnChannelAsync"/> to return the <see cref="IChannelHost"/>.</para>
        /// <para><em>Note: During an outage event, you will pause here until a viable channel can be acquired.</em></para>
        /// </summary>
        /// <returns><see cref="IChannelHost"/></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override async ValueTask<IChannelHost> GetChannelAsync()
        {
            if (Shutdown) throw new InvalidOperationException(ExceptionMessages.ChannelPoolValidationMessage);

            if (!await _channels
                .Reader
                .WaitToReadAsync()
                .ConfigureAwait(false))
            {
                throw new InvalidOperationException(ExceptionMessages.ChannelPoolGetChannelError);
            }

            var chanHost = await _channels
                .Reader
                .ReadAsync()
                .ConfigureAwait(false);

            var healthy = await chanHost.HealthyAsync().ConfigureAwait(false);
            var flagged = _flaggedChannels.ContainsKey(chanHost.ChannelId) && _flaggedChannels[chanHost.ChannelId];
            if (flagged || !healthy)
            {
                Logger.LogWarning(ChannelPools.DeadChannel, chanHost.ChannelId);

                var success = false;
                while (!success)
                {
                    success = await chanHost.MakeChannelAsync().ConfigureAwait(false);
                    await Task.Delay(Options.PoolOptions.SleepOnErrorInterval).ConfigureAwait(false);
                }
            }

            return chanHost;
        }

        /// <summary>
        /// This pulls an ackable <see cref="IChannelHost"/> out of the <see cref="IChannelPool"/> for usage.
        /// <para>If the <see cref="IChannelHost"/> was previously flagged on error, multi-attempta to recreate it before returning an open channel back to the user.
        /// If you only remove channels and never add them, you will drain your <see cref="IChannelPool"/>.</para>
        /// <para>Use <see cref="ReturnChannelAsync"/> to return Channels.</para>
        /// <para><em>Note: During an outage event, you will pause here until a viable channel can be acquired.</em></para>
        /// </summary>
        /// <returns><see cref="IChannelHost"/></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override async ValueTask<IChannelHost> GetAckChannelAsync()
        {
            if (Shutdown) throw new InvalidOperationException(ExceptionMessages.ChannelPoolValidationMessage);

            if (!await _ackChannels
                .Reader
                .WaitToReadAsync()
                .ConfigureAwait(false))
            {
                throw new InvalidOperationException(ExceptionMessages.ChannelPoolGetChannelError);
            }

            var chanHost = await _ackChannels
                .Reader
                .ReadAsync()
                .ConfigureAwait(false);

            var healthy = await chanHost.HealthyAsync().ConfigureAwait(false);
            var flagged = _flaggedChannels.ContainsKey(chanHost.ChannelId) && _flaggedChannels[chanHost.ChannelId];
            if (flagged || !healthy)
            {
                Logger.LogWarning(ChannelPools.DeadChannel, chanHost.ChannelId);

                var success = false;
                while (!success)
                {
                    await Task.Delay(Options.PoolOptions.SleepOnErrorInterval).ConfigureAwait(false);
                    success = await chanHost.MakeChannelAsync().ConfigureAwait(false);
                }
            }

            return chanHost;
        }

        /// <summary>
        /// Returns the <see cref="ChannelHost"/> back to the <see cref="ChannelPool"/>.
        /// <para>All Aqmp IModel Channels close server side on error, so you have to indicate to the library when that happens.</para>
        /// <para>The library does its best to listen for a dead <see cref="ChannelHost"/>, but nothing is as reliable as the user flagging the channel for replacement.</para>
        /// <para><em>Users flag the channel for replacement (e.g. when an error occurs) on it's next use.</em></para>
        /// </summary>
        /// <param name="chanHost"></param>
        /// <param name="flagChannel"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override async ValueTask ReturnChannelAsync(IChannelHost chanHost, bool flagChannel)
        {
            if (Shutdown) throw new InvalidOperationException(ExceptionMessages.ChannelPoolValidationMessage);

            _flaggedChannels[chanHost.ChannelId] = flagChannel;

            Logger.LogDebug(ChannelPools.ReturningChannel, chanHost.ChannelId, flagChannel);

            if (chanHost.Ackable)
            {
                await _ackChannels
                    .Writer
                    .WriteAsync(chanHost)
                    .ConfigureAwait(false);
            }
            else
            {
                await _channels
                    .Writer
                    .WriteAsync(chanHost)
                    .ConfigureAwait(false);
            }
        }

        protected override async Task CloseChannelsAsync()
        {
            // Signal to Channel no more data is coming.
            _channels.Writer.Complete();
            _ackChannels.Writer.Complete();

            await _channels.Reader.WaitToReadAsync().ConfigureAwait(false);
            while (_channels.Reader.TryRead(out IChannelHost chanHost))
            {
                try
                { chanHost.Close(); }
                catch { /* SWALLOW */ }
            }

            await _ackChannels.Reader.WaitToReadAsync().ConfigureAwait(false);
            while (_ackChannels.Reader.TryRead(out IChannelHost chanHost))
            {
                try
                { chanHost.Close(); }
                catch { /* SWALLOW */ }
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (DisposedValue)
            {
                base.Dispose(disposing);
                return;
            }

            _channels = null;
            _ackChannels = null;
            _flaggedChannels = null;
            base.Dispose(disposing);
        }
    }
}
