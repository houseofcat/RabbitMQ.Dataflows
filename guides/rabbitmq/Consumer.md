# RabbitMQ.Dataflows
## Consumer Backed By A ChannelPool

I would recommend understanding the `BasicConsume` guide before moving on to this one. A lot of what a `Consumer`
does is the same thing most engineers would have to do on their own. There is one slight difference. The main
`BasicConsume` functionality is event driven. You have to subscribe to events to process messages. This leads to
a bit of complexity when scaling or throttling sometimes. Furthermore, it's not always clear why a `BasicConsume`
call stops working and have to mitigate outages. That's essentially all this `Consumer` does but with some
added functionality in tandem with reading `X-RD` headers and using `IMessage` objects. That last thing we have
added is the ability to process Consumer messages in Parallel with a ConsumerPipeline or ConsumerDataflow.

You can read more details on RabbitMQ.Client Consumers [here](https://www.rabbitmq.com/docs/consumers).

It really helps to have `RabbitOptions` already setup and ready to go. I will use the following in a file named
`SampleRabbitOptions.json`. Take note of the `ConsumerOptions` as a `Dictionary<string, ConsumerOption>` here.
This is a quality of life feature to configure a Consumer, to configure a `ConsumerPipeline`/`ConsumerDataflow`,
and to leverage a `RabbitService` to pre-build the `Consumer` classes.

```json
  "PoolOptions": {
    "Uri": "amqp://guest:guest@localhost:5672/",
    "MaxChannelsPerConnection": 2000,
    "HeartbeatInterval": 6,
    "AutoRecovery": true,
    "TopologyRecovery": true,
    "NetRecoveryTimeout": 5,
    "ContinuationTimeout": 10,
    "EnableDispatchConsumersAsync": true,
    "ServiceName": "HoC.RabbitMQ",
    "Connections": 2,
    "Channels": 2,
    "AckableChannels": 0,
    "SleepOnErrorInterval": 1000,
    "TansientChannelStartRange": 10000,
    "UseTransientChannels": false
  },
  "PublisherOptions": {
    "MessageQueueBufferSize": 100,
    "BehaviorWhenFull": 0,
    "CreatePublishReceipts": true,
    "Compress": false,
    "Encrypt": false,
    "WaitForConfirmationTimeoutInMilliseconds": 500
  },
  "ConsumerOptions": {
    "TestConsumer": {
      "Enabled": true,
      "ConsumerName": "TestConsumer",
      "BatchSize": 5,
      "BehaviorWhenFull": 0,
      "UseTransientChannels": true,
      "AutoAck": false,
      "NoLocal": false,
      "Exclusive": false,
      "QueueName": "TestQueue",
      "QueueArguments": null,
      "TargetQueueName": "TestTargetQueue",
      "TargetQueueArgs": null,
      "ErrorQueueName": "TestQueue.Error",
      "ErrorQueueArgs": null,
      "BuildQueues": true,
      "BuildQueueDurable": true,
      "BuildQueueExclusive": false,
      "BuildQueueAutoDelete": false,
      "WorkflowName": "TestConsumerWorkflow",
      "WorkflowMaxDegreesOfParallelism": 1,
      "WorkflowConsumerCount": 1,
      "WorkflowBatchSize": 5,
      "WorkflowEnsureOrdered": false,
      "WorkflowWaitForCompletion": false
    }
  }
```

Here is an example of a Top Level RabbitMQ consumer using the `RabbitService` and `Consumer` classes.

```csharp
using HouseofCat.Compression.Recyclable;
using HouseofCat.Encryption;
using HouseofCat.Hashing;
using HouseofCat.RabbitMQ;
using HouseofCat.RabbitMQ.Services.Extensions;
using HouseofCat.Serialization;

// Step 1: Load RabbitOptions from a file.
var rabbitOptions = await RabbitExtensions.GetRabbitOptionsFromJsonFileAsync("./SampleRabbitOptions.json");

// Step 2: Setup your Providers (all but ISerializationProvider is optional)
var jsonProvider = new JsonProvider();
var hashProvider = new ArgonHashingProvider();
var aes256Key = hashProvider.GetHashKey("PasswordMcPassword", "SaltySaltSalt", 32);
var aes256Provider = new AesGcmEncryptionProvider(aes256Key);
var gzipProvider = new RecyclableGzipProvider();

// Step 3: Using RabbitOptions extension method to create a ready to use RabbitService
// (rabbitService.StartAsync() is already called).
var rabbitService = await rabbitOptions.BuildRabbitServiceAsync(
    jsonProvider,
    aes256Provider,
    gzipProvider,
    null);

// Step 4: Get Consumer
var consumer = rabbitService.GetConsumer("TestConsumer");

// Step 5: Start Consumer (leveraging the options configured for it in the RabbitOptions).
await consumer.StartConsumerAsync();

// Step 6: Process the messages from the internal buffer.
await foreach (var receivedMessage in await consumer.ReadUntilStopAsync())
{
    // Do something with the IReceivedMessage.
    receivedMessage.AckMessage();
}
```

The `IReceivedMessage` event handler is a very quick operation to insert the values into our internal buffer.
The internal buffer should match the BatchSize set in ConsumerOptions. The Workflow BatchSize can be
different from the Consumer BatchSize meaning you can manage the flow via ingestion, or manage the flow via
throttling execution.

The `IReceivedMessage` is a simple class that is wrapping your bits for processing. Internally, it may be your
raw data or `IMessage` object. The Consumer will automatically deserialize, decompress, and decrypt your data
provided its given the right RabbitMQ headers that indicate ContentType, Compression, and Encryption.

This is what the `IMessage` inside the queue looks like. The `Metadata` should help indicate the state of the
inner body. This means the `IMessage` acts as envelope to your data. You won't need to worry about setting any
of these values yourself when using the `IMessage` object.

The RabbitMQ headers added when using `IMessage` in our internal Publisher will look like this. You can modify
some of them by changing the Keys/Values in the `Constants` class.

![Imessage Header](misc/imessage_header.png)

The `IMessage` object will look like this.
```json
{
    "MessageId": "6522c080-3d63-484c-995c-5e57a2a9ef47",
    "Body": "UnQ2PCYH0m6duw5mtxv+0wf9s049xQGG0a2FEFsmFm6k77rhd2E0QcEKKxSuea1ok3RuEDkPCCqbDRzaDqLrG/6VeX2+xXE=",
    "Metadata": {
        "PayloadId": "5ad9664e-6312-4f6e-ab97-17cf59802bd0",
        "Fields": {
            "X-RD-COMPRESSED": true,
            "X-RD-COMPRESSION": "GZIP",
            "X-RD-ENCRYPTED": true,
            "X-RD-ENCRYPTION": "AESGCM-256",
            "X-RD-ENCRYPTDATE": "2024-04-25T09:01:28.312541-05:00"
        }
    }
}
```

As you can see, the internal `Body` was your original data, serialized, compressed, and encrypted. A
consumer with the corresponding `ISerializationProvider`, `ICompressionProvider`, and `IEncryptionProvider`
will automatically decrypt, decompress, and deserialize your data back into your `Body`, leaving you to
deserialize the inner `Body` back into your original object/data. This will only occur though if the
headers are in place. If you published manually without `IMessage` wrapped object, you simply have to
perform these steps yourself. If you serialize, compress, and encrypt, then perform those steps in reverse;
decrypt, decompress, and deserialize.

If it was a raw string you could the following.
```csharp
using System.Text;

await foreach (var receivedMessage in await consumer.ReadUntilStopAsync())
{
    var message = Encoding.UTF8.GetString(receivedMessage.Body.Span);
    receivedMessage.AckMessage();
}
```

If it was a JsonSerialized data (internally) you could do the following.
```csharp
using System.Text;

await foreach (var receivedMessage in await consumer.ReadUntilStopAsync())
{
    var myClass = jsonProvider.Deserialize<MyClass>(receivedMessage.Body);

    // Do something with `myClass` here.

    receivedMessage.AckMessage();
}
```

If you needed to decompress and decrypt the IMessage manually you could do the following.
```csharp
await foreach (var receivedMessage in await consumer.ReadUntilStopAsync())
{
    await rabbitService.DecomcryptAsync(receivedMessage.Message);

    // Do something with the receivedMessage.Message.

    receivedMessage.AckMessage();
}
```

If you needed to decompress and decrypt the raw data manually you could do the following.
```csharp
await foreach (var receivedMessage in await consumer.ReadUntilStopAsync())
{
    var decryptedData = aes256Provider.Decrypt(receivedMessage.Message.Body);
    var decompresseData = gzipProvider.Decompress(decryptedData);
    var myClass = jsonProvider.Deserialize<MyClass>(decompresseData);


    // Do something with the myClass.

    receivedMessage.AckMessage();
}
```

These examples are obviously trivial and single threaded for the most part. This is where the Pipeline and
Dataflow allow you to level things up. The `ConsumerPipeline` and `ConsumerDataflow` are designed to allow
to scale to the number of cores on your machine or to the number of threads you want to use.

Once you AckMessage (or NackMessage) the message is removed from Consumer capacity and a new (very next)
message will be consumed to fill it's place. The BatchSize of the Consumer indicates how many of these
messages you can have pending. For long operations without parallelization, I would recommend a small
batchsize and many consumers. For short operations with parallelization, I would recommend a large
batchsize and one or more identical consumers. ConsumerDataflows allow you clone Consumers and consume in
parallel. This is a very high performance scenario. It's best to start conservative and adjust one setting
at a time for performance tuning.
