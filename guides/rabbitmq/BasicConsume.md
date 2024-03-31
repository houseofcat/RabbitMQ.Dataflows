# RabbitMQ.Dataflows
## BasicConsume with ChannelPool

I would recommend understanding the `BasicPublish` and `BasicGet` guide before moving on to this one.

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
    "NetRecoveryTimeout": 5,
    "ContinuationTimeout": 10,
    "EnableDispatchConsumersAsync": true
    "ServiceName": "HoC.RabbitMQ",
    "MaxConnections": 2,
    "MaxChannels": 2,
    "MaxAckableChannels": 0,
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

### Get TransientChannel For Consumer
I recommend only using TransientChannels for consumers. They are intended to sort of be dedicated to the `BasicConsumer`
for the life of the consumer. Having many consumers using a `IChannelPool` will quickly drain your available Channel count.
Simply put, use on demand or transient `IChannelHost` for consumers.

```csharp
var channelHost = await channelPool.GetTransientChannelAsync(true);

// Get access to the internal RabbitMQ Channel. 
var channel = channelHost.GetChannel();
```

### BasicConsume With ChannelPool
Consumers are a bit more complicated than Publishers. They are primarily event driven and require a bit more setup. Even then
you have to be pretty thorough with the Shutdown event. It's not always clear why a consumer has stopped. It could be transient
network conditions or it could be an actual consumer shutdown. You have to handle both from one event with out very clear
indicators which is which. AutoRecovery enabled means that the IConnection and IModel can recover, but if the `IModel`
had an error during a `BasicPublish` it is most likely permanently closed and you have to start all over.

You can find more information about that [here](https://www.rabbitmq.com/client-libraries/dotnet-api-guide#consuming-async).

***Note: The type of consumer has to be configured in the ConnectionFactory in the RabbitMQ.Client. See the
`EnableDispatchConsumersAsync` in the `PoolOptions` in the `RabbitOptions`.***

```csharp
using HouseofCat.RabbitMQ.Pools;
using HouseofCat.Utilities;
using RabbitMQ.Client;

var rabbitOptions = JsonFileReader.ReadFileAsync<RabbitOptions>("SampleRabbitOptions.json");
var channelPool = new ChannelPool(rabbitOptions);
var channelHost = await channelPool.GetTransientChannelAsync(true);

var channel = channelHost.GetChannel();

// BasicQos can't be modified after a consumer is started. So set it before.
channel.BasicQos(prefetchCount: 0, prefetchSize: 10, global: false);

AsyncEventingBasicConsumer consumer = new AsyncEventingBasicConsumer(channel);

// Have to subscribe to events.
consumer.Received += ReceiveHandlerAsync;
consumer.Shutdown += ConsumerShutdownAsync;

var consumerOptions = new ConsumerOptions
{
    ConsumerName = "YourConsumerName",
    QueueName = "YourQueueName",
    AutoAck = false,
    NoLocal = false,
    Exclusive = false
};

var consumerTag = channelHost.StartConsuming(consumer, consumerOptions);

private async Task ReceiveHandlerAsync(object _, BasicDeliverEventArgs bdea)
{
    // Do something with the message.
    // Then Ack/Nack
}

private async Task ConsumerShutdownAsync(object sender, ShutdownEventArgs e)
{
    // Handle a network outage vs. consumer cancelation/stoppage.
}

// Sleep until you want to stop consuming. Recommend stopping consumer and disposing channel when done.
channel.BasicCancel(consumerTag);
```
