# RabbitMQ.Dataflows
## BasicPublish with ChannelPool

The RabbitMQ.Dataflows library is designed to simplify the main ways you will interact
with Channels. This guide will cover the basic usage of the `ChannelPool` and how to a
RabbitMQ style publish.

I would recommend understanding this guide before moving on to `Publishers` guide.

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
    "Channels": 5,
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

```csharp
var channelHost = await channelPool.GetChannelAsync();
```

### DeliveryMode
The `DeliveryMode` is a property on the `IBasicProperties` that allows you to set the
message to be durable. This means that the message will survive a server reboot.

[AMQP Reference](https://www.rabbitmq.com/amqp-0-9-1-reference)  
[CloudAMQP](https://www.cloudamqp.com/blog/faq-what-is-the-delivery-mode-in-amqp.html)
```csharp
properties.DeliveryMode = 2;
```

### BasicPublish
Everything together to demonstrate a very basic publish.

```csharp
using HouseofCat.RabbitMQ;
using HouseofCat.RabbitMQ.Pools;
using HouseofCat.Utilities;
using System.Text;

var rabbitOptions = await JsonFileReader.ReadFileAsync<RabbitOptions>("SampleRabbitOptions.json");
var channelPool = new ChannelPool(rabbitOptions);

var channelHost = await channelPool.GetChannelAsync();

// Now you can publish a message to a RabbitMQ Exchange our RoutingKey.
var properties = channelHost.Channel.CreateBasicProperties();
properties.DeliveryMode = 2;

// RabbitMQ publishes `byte[]` with `IBasicProperties` to the `Exchange` with a `RoutingKey`.
var messageAsBytes = Encoding.UTF8.GetBytes("Hello World");

try
{
    channelHost.Channel.BasicPublish("MyExchange", "MyRoutingKey", false, properties, messageAsBytes);
}
catch
{

}
```

### Cleanup
Now we want to return our channel to the ChannelPool and indicate an error (or not) occured.
An error on Publish is good indicator the Channel can't be recovered (not always but usually
true).
```csharp
using HouseofCat.RabbitMQ;
using HouseofCat.RabbitMQ.Pools;
using HouseofCat.Utilities;
using System.Text;

var rabbitOptions = await JsonFileReader.ReadFileAsync<RabbitOptions>("SampleRabbitOptions.json");
var channelPool = new ChannelPool(rabbitOptions);
var channelHost = await channelPool.GetChannelAsync();

// Now you can publish a message to a RabbitMQ Exchange our RoutingKey.
var properties = channelHost.Channel.CreateBasicProperties();
properties.DeliveryMode = 2;

// RabbitMQ publishes `byte[]` with `IBasicProperties` to the `Exchange` with a `RoutingKey`.
var messageAsBytes = Encoding.UTF8.GetBytes("Hello World");

var error = false;
try
{
    channelHost.Channel.BasicPublish("MyExchange", "MyRoutingKey", false, properties, messageAsBytes);
}
catch (Exception ex)
{
    // Log your exception.
    error = true;
}

await channelPool.ReturnChannelAsync(channelHost, error);
```

### Transient Channel Example
These are very similar steps but necessary when you want to build a `ChannelHost` (and RabbitMQ.Client.Model Channel). It is intended for you
to then manage the life cycle of the `ChannelHost`.

Do avoid rapid creation and disposal of `ChannelHost` / `IModel` objects. It is unperformant both with the RabbitMQ.Client and the RabbitMQ server.

```csharp
using HouseofCat.RabbitMQ;
using HouseofCat.RabbitMQ.Pools;
using HouseofCat.Utilities;
using System.Text;

var rabbitOptions = await JsonFileReader.ReadFileAsync<RabbitOptions>("SampleRabbitOptions.json");
var channelPool = new ChannelPool(rabbitOptions);
var channelHost = await channelPool.GetTransientChannelAsync(true);

var properties = channelHost.Channel.CreateBasicProperties();
properties.DeliveryMode = 2;

var messageAsBytes = Encoding.UTF8.GetBytes("Hello World");

try
{
    channelHost.Channel.BasicPublish("YourExchangeName", "YourRoutingKey", false, properties, messageAsBytes);
}
catch
{
    // Log your exception.
}

channelHost.Close();
```

