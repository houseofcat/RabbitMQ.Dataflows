using System.Collections.Generic;
using HouseofCat.RabbitMQ.Pools;
using ChannelPool = HouseofCat.RabbitMQ.Recoverable.Pools.ChannelPool;
using IChannelHost = HouseofCat.RabbitMQ.Pools.IChannelHost;

namespace HouseofCat.RabbitMQ.Recoverable.Consumer;

public class Consumer : HouseofCat.RabbitMQ.Consumer
{
    public Consumer(RabbitOptions options, string consumerName) : this(new ChannelPool(options), consumerName)
    {
    }

    public Consumer(IChannelPool channelPool, string consumerName) : base(channelPool, consumerName)
    {
    }

    public Consumer(IChannelPool channelPool, ConsumerOptions consumerOptions) : base(channelPool, consumerOptions)
    {
    }
    
    protected override IDictionary<string, object> CreateConsumerArguments(IChannelHost chanHost) =>
        chanHost is Pools.IChannelHost recoverableChannelHost
            ? new Dictionary<string, object>{ { "RecoverableChanHostId", recoverableChannelHost.Id } }
            : base.CreateConsumerArguments(chanHost);
}