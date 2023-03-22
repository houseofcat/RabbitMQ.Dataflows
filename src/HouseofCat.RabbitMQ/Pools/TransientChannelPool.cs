using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using HouseofCat.Logger;
using HouseofCat.Utilities.Errors;
using Microsoft.Extensions.Logging;
using static HouseofCat.RabbitMQ.LogMessages;

namespace HouseofCat.RabbitMQ.Pools;

public class TransientChannelPool : IChannelPool, IDisposable
{
    private readonly IConnectionPool _connectionPool;
    private readonly SemaphoreSlim _poolLock = new(1, 1);

    protected ILogger Logger { get; }
    protected bool DisposedValue { get; private set; }

    public RabbitOptions Options => _connectionPool.Options;
    // A 0 indicates TransientChannels.
    public ulong CurrentChannelId { get; protected set; } = 0;
    public bool Shutdown { get; private set; }

    public TransientChannelPool(RabbitOptions options) : this(new ConnectionPool(options)) { }

    public TransientChannelPool(IConnectionPool connPool) : this(connPool, LogHelper.GetLogger<TransientChannelPool>())
    {
    }

    protected TransientChannelPool(IConnectionPool connPool, ILogger logger)
    {
        Guard.AgainstNull(connPool, nameof(connPool));
        _connectionPool = connPool;
        Logger = logger;
    }

    protected virtual Task CloseChannelsAsync() => Task.CompletedTask;

    // Allows overriding the mechanism for creating ChannelHosts while a base one was implemented.
    protected virtual IChannelHost CreateChannelHost(ulong channelId, IConnectionHost connHost, bool ackable) =>
        new ChannelHost(channelId, connHost, ackable);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected async ValueTask<IChannelHost> CreateChannelAsync(ulong channelId, bool ackable)
    {
        IConnectionHost connHost = null;

        while (true)
        {
            Logger.LogTrace(ChannelPools.CreateChannel, channelId);

            // Get ConnectionHost
            try
            {
                connHost = await _connectionPool.GetConnectionAsync().ConfigureAwait(false);
            }
            catch
            {
                Logger.LogTrace(ChannelPools.CreateChannelFailedConnection, channelId);
                await ReturnConnectionWithOptionalSleep(
                    connHost, channelId, Options.PoolOptions.SleepOnErrorInterval).ConfigureAwait(false);
                continue;
            }

            // Create a Channel Host
            try
            {
                var chanHost = CreateChannelHost(channelId, connHost, ackable);
                await ReturnConnectionWithOptionalSleep(connHost, channelId).ConfigureAwait(false);
                Logger.LogDebug(ChannelPools.CreateChannelSuccess, channelId);

                return chanHost;
            }
            catch
            {
                Logger.LogTrace(ChannelPools.CreateChannelFailedConstruction, channelId);
                await ReturnConnectionWithOptionalSleep(
                    connHost, channelId, Options.PoolOptions.SleepOnErrorInterval).ConfigureAwait(false);
            }
        }
    }
    
    private async ValueTask ReturnConnectionWithOptionalSleep(
        IConnectionHost connHost, ulong channelId, int sleep = 0)
    {
        if (connHost != null)
        {
            // Return Connection (or lose them.)
            await _connectionPool.ReturnConnectionAsync(connHost).ConfigureAwait(false);
        }

        if (sleep > 0)
        {
            Logger.LogDebug(ChannelPools.CreateChannelSleep, channelId);

            await Task.Delay(sleep).ConfigureAwait(false);
        }
    }

    public virtual ValueTask<IChannelHost> GetChannelAsync() => throw new NotSupportedException();
    public virtual ValueTask<IChannelHost> GetAckChannelAsync() => throw new NotSupportedException();
    public virtual ValueTask ReturnChannelAsync(IChannelHost chanHost, bool flagChannel) => 
        throw new NotSupportedException();
    
    /// <summary>
    /// <para>Gives user a transient <see cref="IChannelHost"/> is simply a channel not managed by this library.</para>
    /// <para><em>Closing and disposing the <see cref="IChannelHost"/> is the responsiblity of the user.</em></para>
    /// </summary>
    /// <param name="ackable"></param>
    /// <returns><see cref="IChannelHost"/></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ValueTask<IChannelHost> GetTransientChannelAsync(bool ackable) => CreateChannelAsync(0, ackable);

    public async Task ShutdownAsync()
    {
        Logger.LogTrace(ChannelPools.Shutdown);

        await _poolLock
            .WaitAsync()
            .ConfigureAwait(false);

        if (!Shutdown)
        {
            await CloseChannelsAsync()
                .ConfigureAwait(false);

            Shutdown = true;

            await _connectionPool
                .ShutdownAsync()
                .ConfigureAwait(false);
        }

        _poolLock.Release();
        Logger.LogTrace(ChannelPools.ShutdownComplete);
    }
        
    protected virtual void Dispose(bool disposing)
    {
        if (DisposedValue)
        {
            return;
        }
        
        if (disposing)
        {
            _poolLock.Dispose();
        }

        DisposedValue = true;
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}