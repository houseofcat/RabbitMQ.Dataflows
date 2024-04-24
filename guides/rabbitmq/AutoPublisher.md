# RabbitMQ.Dataflows
## Publisher using AutoPublish

I would recommend understanding the `Publisher` guide before moving on to this one.

Publisher can create it's own `IChannelPool` for simplification or if you want a separate one
for Publishers and one for Consumers. This can be a great way of keeping throughput at it's
highest when inside the same process. Inside the `RabbitService` it contains the `IPublisher`
and all your pre-configured `Consumers`. RabbitService isn't a particularly fancy or necessary
service. It's just a way to keep all your RabbitMQ related objects in one place and allow you
to dependency inject them where needed or to use directly.

The following is an example of building a `Publisher, IPublisher` directly for using both normal Publish
methods and queueing messages for auto-publishing. The AutoPublishing is only useful if you intend
to use the `IMessage` object otherwise you are better off managing how you publish your raw data.

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
        Connections = 2,
        Channels = 10,
        AckableChannels = 0
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

// Step 2: Instantiate the Publisher (this example means it will have it's own internal ChannelPool)
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
    var data = Encoding.UTF8.GetBytes("Hello World, from RabbitMQ!");
    var message = new Message(Shared.ExchangeName, Shared.RoutingKey, data, Guid.NewGuid().ToString());

    // Step 5: Queue Message (async publish).
    await publisher.QueueMessageAsync(message);

    // Step 6: Stop Auto Publishing (this will wait for all messages to be published before stopping)
    await publisher.StopAutoPublishAsync();
}
catch (Exception ex)
{
    Console.WriteLine($"Error: {ex.Message}");
}
```

This example uses the `IRabbitService` more traditionally. You could imagine that you have injected
`IRabbitService` to the constructor of your BusinsessLogicClass and you just need to drop a message
into a RabbitMQ queue. The added benefit of using AutoPublisher is to allow you to publish asynchronously
from your code and not slowing down the current flow. This is a great way to keep your code nimble and
responsive.

```csharp
using HouseofCat.Compression;
using HouseofCat.Encryption;
using HouseofCat.RabbitMQ;
using HouseofCat.RabbitMQ.Pools;
using HouseofCat.Serialization;
using Microsoft.Extensions.Logging;
using System.Text;

// Step 1: Configure RabbitOptions (or load from file or IConfiguration).
var rabbitOptions = await RabbitExtensions.GetRabbitOptionsFromJsonFileAsync(configFileNamePath);

// Step 2: Setup your Providers (all but ISerializationProvider is optional)
var jsonProvider = new JsonProvider();
var hashProvider = new ArgonHashingProvider();
var aes256Key = hashProvider.GetHashKey(EncryptionPassword, EncryptionSalt, KeySize);
var aes256Provider = new AesGcmEncryptionProvider(aes256Key);
var gzipProvider = new RecyclableGzipProvider();

// Step 3: Using extension method to create a ready to use RabbitService (StartAsync is called already).
var rabbitService = await rabbitOptions.BuildRabbitServiceAsync(
    jsonProvider,
    aes256Provider,
    gzipProvider,
    loggerFactory);

// Step 4: Create IMessage and Payload
var message = new Message(
    exchange: Shared.ExchangeName,
    routingKey: Shared.RoutingKey,
    body: dataAsBytes,
    payloadId: Guid.NewGuid().ToString());

// Step 5: Queue Message (async publish).
await rabbitService.Publisher.QueueMessageAsync(message);
```

The `Publisher` also has a function that listens for Receipts of an AutoPublish. You can override the default
behavior by providing your own function when calling `StartAutoPublish`. This version is used when `null` is
provided by the user but the `Options.PublisherOptions.CreatePublishReceipts` is `true`. It is looking for
failed receipts (failure to publish) and this means we can requeue them for an additional attempt. This helps
prevent the loss of messages if a background publish fails.

```csharp
private async ValueTask ProcessReceiptAsync(IPublishReceipt receipt)
{
    if (AutoPublisherStarted
        && receipt.IsError
        && receipt.OriginalMessage != null)
    {
        _logger.LogWarning($"Failed publish for message ({receipt.OriginalMessage.MessageId}). Retrying with AutoPublishing...");

        try
        { await QueueMessageAsync(receipt.OriginalMessage); }
        catch (Exception ex) /* No-op */
        { _logger.LogDebug("Error ({0}) occurred on retry, most likely because retry during shutdown.", ex.Message); }
    }
    else if (receipt.IsError)
    {
        _logger.LogError($"Failed publish for message ({receipt.OriginalMessage.MessageId}). Unable to retry as the original message was not received.");
    }
}
```

The method signature for `StartAutoPublish` with the optional `processReceiptAsync` is as follows:

```csharp
void StartAutoPublish(Func<IPublishReceipt, ValueTask> processReceiptAsync = null);
Task StartAutoPublishAsync(Func<IPublishReceipt, ValueTask> processReceiptAsync = null);
```