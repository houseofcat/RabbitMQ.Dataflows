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
    var message = new Message("TestExchange", "TestRoutingKey", data, Guid.NewGuid().ToString());

    // Step 5: Queue Message (async publish).
    await publisher.QueueMessageAsync(message);

    // Step 6: Stop Auto Publishing (this will wait for all messages to be published before stopping)
    await publisher.StopAutoPublishAsync();
}
catch (Exception ex)
{
    Console.WriteLine($"Error: {ex.Message}");
}