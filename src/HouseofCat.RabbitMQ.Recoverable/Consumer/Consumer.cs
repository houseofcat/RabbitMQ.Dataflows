using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HouseofCat.RabbitMQ.Pools;
using ChannelPool = HouseofCat.RabbitMQ.Recoverable.Pools.ChannelPool;
using IChannelHost = HouseofCat.RabbitMQ.Pools.IChannelHost;

namespace HouseofCat.RabbitMQ.Recoverable;

public class Consumer : HouseofCat.RabbitMQ.Consumer
{
    public Consumer(RabbitOptions options, string consumerName) : this(new ChannelPool(options), consumerName) { }

    public Consumer(IChannelPool channelPool, string consumerName) : base(channelPool, consumerName) { }

    public Consumer(IChannelPool channelPool, ConsumerOptions consumerOptions) : base(channelPool, consumerOptions) { }

    protected override IDictionary<string, object> CreateConsumerArguments(IChannelHost chanHost) =>
        chanHost is Pools.IChannelHost recoverableChannelHost
            ? new Dictionary<string, object>{ { "ChanHostRecoveryId", recoverableChannelHost.RecoveryId } }
            : base.CreateConsumerArguments(chanHost);

    protected override ValueTask<bool> RestartConsumingAsync(IChannelHost chanHost) =>
        chanHost is Pools.IChannelHost recoverableChannelHost
            ? recoverableChannelHost.RecoverChannelAsync(StartConsumingAsync)
            : base.RestartConsumingAsync(chanHost);

    protected override async Task<bool> TryEnterLockAsync(int millisecondsTimeout = Timeout.Infinite)
    {
        if (!await base.TryEnterLockAsync(millisecondsTimeout).ConfigureAwait(false))
        {
            return false;
        }
        if (millisecondsTimeout != Timeout.Infinite)
        {
            // lock was entered within the timeout, so pause for the timeout instead to prevent CPU churn
            await Task.Delay(millisecondsTimeout).ConfigureAwait(false);
        }
        return true;
    }
}