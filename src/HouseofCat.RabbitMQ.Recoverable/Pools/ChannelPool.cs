using System.Runtime.CompilerServices;
using HouseofCat.RabbitMQ.Pools;

namespace HouseofCat.RabbitMQ.Recoverable.Pools;

public class ChannelPool : HouseofCat.RabbitMQ.Pools.ChannelPool
{
    public ChannelPool(RabbitOptions options) : this(new ConnectionPool(options)) { }

    public ChannelPool(IConnectionPool connPool) : base(connPool) { }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected override HouseofCat.RabbitMQ.Pools.IChannelHost CreateChannelHost(
        ulong channelId, HouseofCat.RabbitMQ.Pools.IConnectionHost connHost, bool ackable) =>
        connHost is IConnectionHost recoverableConnectionHost
            ? new ChannelHost(channelId, recoverableConnectionHost, ackable)
            : base.CreateChannelHost(channelId, connHost, ackable);
}