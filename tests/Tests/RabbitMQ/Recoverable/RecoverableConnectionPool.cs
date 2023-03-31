using HouseofCat.RabbitMQ;
using HouseofCat.RabbitMQ.Pools;
using RabbitMQ.Client;

namespace IntegrationTests.RabbitMQ.Recoverable;

public class RecoverableConnectionPool : ConnectionPool
{
    public RecoverableConnectionPool(RabbitOptions options) : base(options) { }

    protected override IConnectionHost CreateConnectionHost(ulong connectionId, IConnection connection) =>
        connection is IAutorecoveringConnection
            ? new RecoverableConnectionHost(connectionId, connection)
            : base.CreateConnectionHost(connectionId, connection);
}