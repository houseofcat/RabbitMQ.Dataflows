# RabbitMQ.Dataflows
## Publisher using AutoPublish

I would recommend understanding the `Publisher` guide before moving on to this one.

Publisher can create it's own `IChannelPool` for simplification or if you want a separate one
for Publishers and one for Consumers. This can be a great way of keeping throughput at it's
highest when inside the same process. Inside the `RabbitService` it contains an `AutoPublisher`
and all your pre-configured `Consumer`.

```csharp
using HouseofCat.Compression;
using HouseofCat.Encryption;
using HouseofCat.RabbitMQ;
using HouseofCat.RabbitMQ.Pools;
using HouseofCat.Serialization;
using Microsoft.Extensions.Logging;
using System.Text;

// Step 1: Configure RabbitOptions (or load from file or IConfiguration).
var rabbitOptions = new RabbitOptions
{
    PoolOptions = new PoolOptions
    {
        Uri = new Uri("amqp://guest:guest@localhost:5672"),
        ServiceName = "TestService",
        MaxConnections = 2,
        MaxChannels = 10,
        MaxAckableChannels = 0
    },
    PublisherOptions = new PublisherOptions
    {
        CreatePublishReceipts = true,
        MessageQueueBufferSize = 10_000,
        BehaviorWhenFull = BoundedChannelFullMode.Wait,
        Compress = false,
        Encrypt = false,
        WithHeaders = true,
        WaitForConfirmationTimeoutInMilliseconds = 500
    }
};

// Step 2: Instantiate the Publisher
var publisher = new Publisher(
    rabbitOptions,
    new JsonProvider(),
    encryptionProvider: null,
    compressionProvider: null);

try
{
    // Step 3: Start Auto Publishing
    await publisher.StartAutoPublishAsync(
        async receipt =>
        {
            await Console.Out.WriteLineAsync($"Receipt Received: {receipt.MessageId}");
        });

    // Step 4: Create IMessage
    var data = Encoding.UTF8.GetBytes("Hello, RabbitMQ!");
    var message = new Message(Shared.ExchangeName, Shared.RoutingKey, data, Guid.NewGuid().ToString())
    {
        // DeliveryId for tracking/routing through Publisher/Consumer.
        MessageId = Guid.NewGuid().ToString(),
    };

    // Step 5: Queue Message (async publish).
    await publisher.QueueMessageAsync(message);

    // Step 6: Stop Auto Publishing
    await publisher.StopAutoPublishAsync();
}
catch (Exception ex)
{
    Console.WriteLine($"Error: {ex.Message}");
}
```

