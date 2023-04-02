using RabbitMQ.Client;

namespace HouseofCat.RabbitMQ.Recoverable.Pools;

public class ConnectionPool : HouseofCat.RabbitMQ.Pools.ConnectionPool
{
    public ConnectionPool(RabbitOptions options) : base(options) { }

    protected override HouseofCat.RabbitMQ.Pools.IConnectionHost CreateConnectionHost(
        ulong connectionId, IConnection connection) =>
        connection is IAutorecoveringConnection
            ? new ConnectionHost(connectionId, connection)
            : base.CreateConnectionHost(connectionId, connection);
}