using System.Runtime.CompilerServices;
using HouseofCat.RabbitMQ;
using HouseofCat.RabbitMQ.Pools;

namespace IntegrationTests.RabbitMQ.Recoverable;

public class RecoverableChannelPool : ChannelPool
{
    public RecoverableChannelPool(RabbitOptions options) : this(new RecoverableConnectionPool(options)) { }

    public RecoverableChannelPool(IConnectionPool connPool) : base(connPool) { }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected override IChannelHost CreateChannelHost(ulong channelId, IConnectionHost connHost, bool ackable) =>
        connHost is IRecoverableConnectionHost
            ? new RecoverableChannelHost(channelId, connHost, ackable)
            : base.CreateChannelHost(channelId, connHost, ackable);
}