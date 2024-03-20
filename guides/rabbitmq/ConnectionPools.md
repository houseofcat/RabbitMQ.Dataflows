# RabbitMQ.Dataflows
## ConnectionPools

Primary purpose of the `ConnectionPool` is to manage RabbitMQ `Connections` that
are each wrapped inside a class called `ConnectionHost`. This is just to track the
`Connection` and various states the Connection may be in by subscribing to the
EventHandlers of the internal `IConnection`. You would want to use these yourself
in your own integration very similarly to the `ConnectionHost`.

```plaintext  
Json -> RabbitOptions -> ConnectionPool  
ConnectionPool .ctor -> ConnectionFactory -> Create RabbitMQ Connections
```

The ConnectionPool is pretty bare bones. The reason for that is, that RabbitMQ does
most of it's heavy lifting with a RabbitMQ channel. This is called a `Model`. We
will cover that in the ChannelPool guide. For now, we need a connection to build a
channel.

For now here is the `IConnectionPool` interface:
```csharp
public interface IConnectionPool
{
    RabbitOptions Options { get; }

    IConnection CreateConnection(string connectionName);
    ValueTask<IConnectionHost> GetConnectionAsync();
    ValueTask ReturnConnectionAsync(IConnectionHost connHost);

    Task ShutdownAsync();
}
```

Here is some a simple ConnectionPool setup and basic usage.

It really helps to have `RabbitOptions` already setup and ready to go.
I will use this as a file named `SampleRabbitOptions.json`:
```
{
  "FactoryOptions": {
    "Uri": "amqp://guest:guest@localhost:5672/",
    "MaxChannelsPerConnection": 2000,
    "HeartbeatInterval": 6,
    "AutoRecovery": true,
    "TopologyRecovery": true,
    "NetRecoveryTimeout": 5,
    "ContinuationTimeout": 10,
    "EnableDispatchConsumersAsync": true
  },
  "PoolOptions": {
    "ServiceName": "HoC.RabbitMQ",
    "MaxConnections": 2,
    "MaxChannels": 0,
    "MaxAckableChannels": 0,
    "SleepOnErrorInterval": 1000,
    "TansientChannelStartRange": 10000,
    "UseTransientChannels": false
  }
}
```

I will use a helper method to load the `RabbitOptions` from the file.

```csharp
using HouseofCat.RabbitMQ.Pools;
using HouseofCat.Utilities;

var rabbitOptions = JsonFileReader.ReadFileAsync<RabbitOptions>("SampleRabbitOptions.json");
var pool = new ConnectionPool(rabbitOptions);
```

Per the config above you will see that we should have `2` connections. Here's how
you get one!

```csharp
var connectionHost = await pool.GetConnectionAsync();

// Create a Channel for communication using a RabbitMQ Connection.
var channel = connectionHost.Connection.CreateModel();

// When you are done with the `Connection` you can return it to the pool.
await pool.ReturnConnectionAsync(connectionHost);
```

If you prefer to use the `ConnectionPool` directly just to create Connections on demand
you can do this manually.
`CreateConnection`:

```csharp
var connectionHost = pool.CreateConnection();
```