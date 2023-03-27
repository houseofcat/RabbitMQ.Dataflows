using System.Runtime.CompilerServices;
using HouseofCat.RabbitMQ;
using HouseofCat.RabbitMQ.Pools;
using RabbitMQ.Client;

namespace IntegrationTests.RabbitMQ.Recoverable;

public class RecoverableChannelPool : ChannelPool
{
    public RecoverableChannelPool(RabbitOptions options) : base(options)
    {
    }

    public RecoverableChannelPool(IConnectionPool connPool) : base(connPool)
    {
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected override IChannelHost CreateChannelHost(ulong channelId, IConnectionHost connHost, bool ackable) =>
        connHost.Connection is IAutorecoveringConnection
            ? new RecoverableChannelHost(channelId, connHost, ackable)
            : base.CreateChannelHost(channelId, connHost, ackable);
}