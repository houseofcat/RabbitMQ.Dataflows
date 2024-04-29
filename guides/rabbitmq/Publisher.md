# RabbitMQ.Dataflows
## Publish using Publisher

I would recommend understanding the `BasicPublish` guide before moving on to this one.

Publisher can create it's own `IChannelPool` for simplification or if you want a separate one
for Publishers and one for Consumers. This can be a great way of keeping throughput at it's
highest when inside the same process. Inside the `RabbitService` it contains the `IPublisher`
and all your pre-configured `Consumers`. RabbitService isn't a particularly fancy or necessary
service. It's just a way to keep all your RabbitMQ related objects in one place and allow you
to dependency inject them where needed or to use directly.

The following is an example of building a `Publisher, IPublisher` directly for using both normal
Publish methods and queueing messages for auto-publishing.

Standard publish example with just raw data.

```csharp
using HouseofCat.RabbitMQ;
using HouseofCat.Serialization;
using System.Text;
using System.Threading.Channels;

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
    }
};

// Step 2: Instantiate the Publisher (this example means it will have it's own internal ChannelPool)
var publisher = new Publisher(
    rabbitOptions,
    new JsonProvider());

try
{
    // Step 3: Create Data
    var exchange = "TestExchange";
    var routingKey = "TestRoutingKey";
    var data = Encoding.UTF8.GetBytes("Hello, RabbitMQ!");

    // Step 4: Publish Data
    await publisher.PublishAsync(
        exchange,
        routingKey,
        data,
        headers: null);
}
catch (Exception ex)
{
    Console.WriteLine($"Error: {ex.Message}");
}
```

Standard publish example with `IMessage`.

```csharp
using HouseofCat.RabbitMQ;
using HouseofCat.Serialization;
using System.Text;
using System.Threading.Channels;

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
    new JsonProvider());

try
{
    // Step 3: Create Message
    var data = Encoding.UTF8.GetBytes("Hello, RabbitMQ!");
    var message = new Message("TestExchange", "TestRoutingKey", data);

    // Step 4: Publish IMessage
    await publisher.PublishAsync(
        message,
        createReceipt: false,
        withOptionalHeaders: true);
}
catch (Exception ex)
{
    Console.WriteLine($"Error: {ex.Message}");
}
```

If you use `createReceipt: true` then `AutoPublish` will process the messages and on error, it will
automatically retry internally. This is useful for when you want to ensure that your messages are
delivered and you don't want to manage the retries yourself.

The following example uses the `IRabbitService` more traditionally. You could imagine that you have injected
`IRabbitService` to the constructor of your BusinessLogicClass and you just need to publish a message.

```csharp
using HouseofCat.Compression.Recyclable;
using HouseofCat.Encryption;
using HouseofCat.Hashing;
using HouseofCat.RabbitMQ;
using HouseofCat.RabbitMQ.Services.Extensions;
using HouseofCat.Serialization;
using System.Text;

// Step 1: Configure RabbitOptions (or load from file or IConfiguration).
var rabbitOptions = await RabbitExtensions.GetRabbitOptionsFromJsonFileAsync("./rabbitoptions.json");

// Step 2: Setup your Providers (all but ISerializationProvider is optional)
var jsonProvider = new JsonProvider();
var hashProvider = new ArgonHashingProvider();
var aes256Key = hashProvider.GetHashKey("PasswordMcPassword", "SaltySaltSalt", 32);
var aes256Provider = new AesGcmEncryptionProvider(aes256Key);
var gzipProvider = new RecyclableGzipProvider();

// Step 3: Using extension method to create a ready to use RabbitService (StartAsync is called already).
var rabbitService = await rabbitOptions.BuildRabbitServiceAsync(
    jsonProvider,
    aes256Provider,
    gzipProvider,
    null);

// Step 4: Create IMessage and Payload
var data = Encoding.UTF8.GetBytes("Hello, RabbitMQ!");
var message = new Message(
    exchange: "TestExchange",
    routingKey: "TestRoutingKey",
    body: data,
    payloadId: Guid.NewGuid().ToString());

// Step 5: Publish
await rabbitService.Publisher.PublishAsync(
    message,
    createReceipt: false,
    withOptionalHeaders: true);
```