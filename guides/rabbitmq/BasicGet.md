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
    "Connections": 2,
    "AckableChannels": 5,
    "TansientChannelStartRange": 10000
  }
}
```

I will use a helper method to load the `RabbitOptions` from the file.

```csharp
using HouseofCat.RabbitMQ;
using HouseofCat.RabbitMQ.Pools;
using HouseofCat.Utilities;

var rabbitOptions = await JsonFileReader.ReadFileAsync<RabbitOptions>("SampleRabbitOptions.json");
var channelPool = new ChannelPool(rabbitOptions);
```

Per the config above you will see that we should have `2` connections and `5` non-ackable
channels inside of `ChannelHost` objects. Here's how you get one!

### Get Channel
```csharp
var channelHost = await channelPool.GetChannelAsync();
```

### BasicGet With ChannelPool
```csharp
using HouseofCat.RabbitMQ;
using HouseofCat.RabbitMQ.Pools;
using HouseofCat.Utilities;
using RabbitMQ.Client;

var rabbitOptions = await JsonFileReader.ReadFileAsync<RabbitOptions>("SampleRabbitOptions.json");
var channelPool = new ChannelPool(rabbitOptions);

var channelHost = await channelPool.GetAckChannelAsync();

var error = false;

BasicGetResult result = null;
try
{
    result = channelHost.Channel.BasicGet("YourQueueName", autoAck: false);

    // Do something with the `result.Body` ...
    // var message = Encoding.UTF8.GetString(result.Body.Span);
    // Console.WriteLine(message);

    // Then ack or reject (nack) the message.
    channelHost.Channel.BasicAck(result.DeliveryTag, multiple: false);
}
catch (Exception ex)
{
    // Log your exception.
    error = true;

    if (result is not null)
    {
        channelHost.Channel.BasicNack(result.DeliveryTag, multiple: false, requeue: true);
    }
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
using HouseofCat.RabbitMQ;
using HouseofCat.RabbitMQ.Pools;
using HouseofCat.Utilities;
using RabbitMQ.Client;

var rabbitOptions = await JsonFileReader.ReadFileAsync<RabbitOptions>("SampleRabbitOptions.json");
var channelPool = new ChannelPool(rabbitOptions);

BasicGetResult result = null;
var channelHost = await channelPool.GetTransientChannelAsync(true);

try
{
    result = channelHost.Channel.BasicGet("YourQueueName", autoAck: false);

    // Do something with the `result.Body` ...
    // var message = Encoding.UTF8.GetString(result.Body.Span);
    // Console.WriteLine(message);

    // Then ack or reject (nack) the message.
    channelHost.Channel.BasicAck(result.DeliveryTag, multiple: false);
}
catch
{
    if (result is not null)
    {
        channelHost.Channel.BasicNack(result.DeliveryTag, multiple: false, requeue: true);
    }
}
```
