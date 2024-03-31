# RabbitMQ.Dataflows
## BasicGet with ChannelPool

I would recommend understanding the `BasicPublish` guide before moving on to this one.

It really helps to have `RabbitOptions` already setup and ready to go.

I will use this as a file named `SampleRabbitOptions.json`

```json
{
  "PoolOptions": {
    "Uri": "amqp://guest:guest@localhost:5672/",
    "MaxChannelsPerConnection": 2000,
    "HeartbeatInterval": 6,
    "AutoRecovery": true,
    "TopologyRecovery": true,
    "NetRecoveryTimeout": 5
    "ServiceName": "HoC.RabbitMQ",
    "MaxConnections": 2,
    "MaxAckableChannels": 5,
    "TansientChannelStartRange": 10000
  }
}
```

I will use a helper method to load the `RabbitOptions` from the file.

```csharp
using HouseofCat.RabbitMQ.Pools;
using HouseofCat.Utilities;

var rabbitOptions = JsonFileReader.ReadFileAsync<RabbitOptions>("SampleRabbitOptions.json");
var channelPool = new ChannelPool(rabbitOptions);
```

Per the config above you will see that we should have `2` connections and `5` non-ackable
channels inside of `ChannelHost` objects. Here's how you get one!

### Get Channel
```csharp
var channelHost = await channelPool.GetChannelAsync();

// Get access to the internal RabbitMQ Channel. 
var channel = channelHost.GetChannel();
```

### BasicGet With ChannelPool
```csharp
using HouseofCat.RabbitMQ.Pools;
using HouseofCat.Utilities;
using RabbitMQ.Client;

var rabbitOptions = JsonFileReader.ReadFileAsync<RabbitOptions>("SampleRabbitOptions.json");
var channelPool = new ChannelPool(rabbitOptions);

var channelHost = await channelPool.GetAckableChannelAsync();
var channel = channelHost.GetChannel();

var error = false;

try
{
	BasicGetResult result = channel.BasicGet("YourQueueName", autoAck: false);

	// Do something with the `result.Body` ...
    // var message = Encoding.UTF8.GetString(result.Body.Span);
    // Console.WriteLine(message);

	// Then ack or reject (nack) the message.
	channel.BasicAck(result.DeliveryTag, multiple: false);
}
catch (Exception ex)
{
	// Log your exception.
    error = true;
    channel.BasicNack(result.DeliveryTag, multiple: false, requeue: true);
}
finally
{
	await channelPool.ReturnChannelAsync(channelHost, error);
}
```

### BasicGet With ChannelPool (Transient Channel)
These are very similar steps but necessary when you want to build a `ChannelHost` (and RabbitMQ.Client.Model Channel) on demand. It is intended for you
to manage the life cycle of the `ChannelHost`.

Do avoid rapid creation and disposal of `ChannelHost` / `IModel` objects. It is unperformant both with the RabbitMQ.Client and the RabbitMQ server.

```csharp
using HouseofCat.RabbitMQ.Pools;
using HouseofCat.Utilities;
using RabbitMQ.Client;

var rabbitOptions = JsonFileReader.ReadFileAsync<RabbitOptions>("SampleRabbitOptions.json");
var channelPool = new ChannelPool(rabbitOptions);

try
{
    using var channelHost = await channelPool.GetTransientChannelAsync(true);

    var error = false;
	BasicGetResult result = channel.BasicGet("YourQueueName", autoAck: false);

	// Do something with the `result.Body` ...
    // var message = Encoding.UTF8.GetString(result.Body.Span);
    // Console.WriteLine(message);

	// Then ack or reject (nack) the message.
	channel.BasicAck(result.DeliveryTag, multiple: false);
}
catch (Ecxeption ex)
{
	// Log your exception.
    error = true;
    channel.BasicNack(result.DeliveryTag, multiple: false, requeue: true);
}
```
