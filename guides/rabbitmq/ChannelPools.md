# RabbitMQ.Dataflows
## ChannelPools

Primary purpose of the `IChannelPool` is to manage RabbitMQ Channels called `IModel`. Each one is
wrapped inside a class called `ChannelHost`. This is the primary class to use when wanting
to manage a `IModel` for communicating with RabbitMQ. IModel correlates to a Channel. They are
used to to publish or to consume messages. It holds EventHandlers for the `IModel` that assist
in managing the life cycle of the internal channel.

The ChannelPool is designed to be the primary class of your RabbitMQ channel/connection management
give that it contains the `IConnectionPool` internally.

For now here is the `IChannelPool` interface:
```csharp
public interface IChannelPool
{
    RabbitOptions Options { get; }
    ulong CurrentChannelId { get; }
    bool Shutdown { get; }

    Task<IChannelHost> GetAckChannelAsync();
    Task<IChannelHost> GetChannelAsync();
    Task<IChannelHost> GetTransientChannelAsync(bool ackable);

    ValueTask ReturnChannelAsync(IChannelHost chanHost, bool flagChannel = false);
    Task ShutdownAsync();
}
```

Here is a some simple ChannelPool setup and basic usage.

It really helps to have `RabbitOptions` already setup and ready to go.
I will use this as a file named `SampleRabbitOptions.json`:
```json
{
  "PoolOptions": {
    "Uri": "amqp://guest:guest@localhost:5672/",
    "MaxChannelsPerConnection": 2000,
    "HeartbeatInterval": 6,
    "AutoRecovery": true,
    "TopologyRecovery": true,
    "NetRecoveryTimeout": 5,
    "ContinuationTimeout": 10,
    "EnableDispatchConsumersAsync": true
    "ServiceName": "HoC.RabbitMQ",
    "Connections": 2,
    "Channels": 5,
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
var connectionPool = new ConnectionPool(rabbitOptions);
var channelPool = new ChannelPool(connectionPool);
```

Or if you want it to autocreate the `ConnectionPool` internally for you:
```csharp
using HouseofCat.Utilities;

var rabbitOptions = JsonFileReader.ReadFileAsync<RabbitOptions>("SampleRabbitOptions.json");
var channelPool = new ChannelPool(rabbitOptions);
```

Per the config above you will see that we should have `2` connections and `5` non-ackable
channels inside of `ChannelHost` objects. Here's how you get one!

```csharp
var channelHost = await channelPool.GetChannelAsync();

// Get access to the RabbitMQ Channel. 
var channel = channelHost.GetChannel();
```

If we wanted an ackable channel for a consumer we would change the config to
this so we can have `5` regular channels and `5` ackable channels:
```json
  "PoolOptions": {
    "MaxChannels": 5,
    "MaxAckableChannels": 5,
  }
```

Here's how you get an ackable channel:
```csharp
var channelHost = await channelPool.GetAckableChannelAsync();

// Get access to the RabbitMQ Channel. 
var channel = channelHost.GetChannel();
```

Regardless of the quantity of each type of channel you can always get a transient channel
that is either ackable or non-ackable. This is useful for when you need to dynamically
create channels at runtime.

```csharp
var channelHost = await channelPool.GetTransientChannelAsync(ackable: true);

// Get access to the RabbitMQ Channel. 
var channel = channelHost.GetChannel();
```

***Note: You may wonder why you pool channels and not just create them on demand. This
creates a lot of RabbitMQ overhead and slows down throughput. Not just in the client but
also in the RabbitMQ server. This can be monitored in the RabbitMQ Server as
Channel/Connection churn.***

When you are done with the `Channel` you can return it to the pool.

The issue is on error with most RabbitMQ channels to a bad queue or exchange name leave
a channel permanently closed. You can't ever recover it. You also can get an error when
simply disconnecting from the server. We indicate this with flagChannel flag. This will
place the channel back in to the pool but the next time it is requested it will take as
long as is needed to get the channel healthy again. That could mean a few seconds for an
autorecovery event or just immediately making a new channel if the channel seems
permanently closed.

```csharp
await channelPool.ReturnChannelAsync(channelHost, false);
```