using System;
using System.Threading;
using System.Threading.Tasks;
using HouseofCat.Logger;
using HouseofCat.Utilities.Errors;
using Microsoft.Extensions.Logging;
using static HouseofCat.RabbitMQ.LogMessages;

namespace HouseofCat.RabbitMQ.Pools;

public class TransientChannelPool : IChannelPool, IDisposable
{
    public RabbitOptions Options => _connectionPool.Options;
    public ulong CurrentChannelId => 0;
    public bool Shutdown { get; private set; }

    private readonly ILogger<TransientChannelPool> _logger;
    private readonly IConnectionPool _connectionPool;
    private readonly SemaphoreSlim _poolLock = new SemaphoreSlim(1, 1);
    private bool _disposedValue;

    public TransientChannelPool(RabbitOptions options) : this(new ConnectionPool(options)) { }

    public TransientChannelPool(IConnectionPool connPool)
    {
        Guard.AgainstNull(connPool, nameof(connPool));

        _logger = LogHelper.GetLogger<TransientChannelPool>();
        _connectionPool = connPool;
    }

    public ValueTask<IChannelHost> GetAckChannelAsync() => throw new NotSupportedException();

    public ValueTask<IChannelHost> GetChannelAsync() => throw new NotSupportedException();

    public ValueTask<IChannelHost> GetTransientChannelAsync(bool ackable) =>
        _connectionPool.CreateChannelAsync(0, ackable);

    public ValueTask ReturnChannelAsync(IChannelHost chanHost, bool flagChannel) => throw new NotSupportedException();

    public async Task ShutdownAsync()
    {
        _logger.LogTrace(ChannelPools.Shutdown);

        await _poolLock
            .WaitAsync()
            .ConfigureAwait(false);

        if (!Shutdown)
        {
            Shutdown = true;

            await _connectionPool
                .ShutdownAsync()
                .ConfigureAwait(false);
        }

        _poolLock.Release();
        _logger.LogTrace(ChannelPools.ShutdownComplete);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposedValue)
        {
            return;
        }

        if (disposing)
        {
            _poolLock.Dispose();
        }

        _disposedValue = true;
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}