# RabbitMQ.Dataflows
## ConsumerDataflow

The `ConsumerDataflow` is really just a fancy combination of TaskParallelLibrary Blocks attached to
RabbitMQ message consumption.

It makes sense to understand `Publisher`, `Consumer`, and the helper service `IRabbitService`
before diving into the `ConsumerDataflow`.

## ConsumerDataflow - Overview
The best way to describe a `ConsumerDataflow` is that it's a Workflow that consists of supplied and
built-in functions that are executed in a specific order.

The input and output of each of these functions is intended to be a `TState` class that implements the
`IRabbitWorkState` interface.

The `ConsumerDataflow` is a very powerful tool that can be used to process messages in a very efficient
manner. At first glance, it may seem like a lot of overhead. That's because it is overkill for a single
consumer, firing one method and then acking on that message.

When you have a complex workflow that needs to be executed fast, possibly in a specific order, need the
ability to customize the parallelism (without code), the ability to create and use more than one
consumer (without code), async handle errors and not crashing.... you may struggle to find anything out
there that can do what the `ConsumerDataflow` can do.

It theory, has total machine scalability. It scales to 100% of your systems hardware with every setting
available to you via `RabbitOptions` to tweak.

1. Unlimited degrees of Parallelism.
2. Unlimited number of Consumer clones.
3. Customizeable bounded capacity to lower or increase memory consumption.
4. Async error handling with an anti-fragility pattern.

And when combined with the other tools... it's a total winner winner chicken dinner!

1. RabbitMQ connection durability via `IChannelPool`/`IConnectionPool`.
2. Native message body envelopes (`IMessage`).
3. Easy Autopublishing.
4. Seamless Serialization/Deserialization
5. Seamless (low allocation) Compression/Decompression.
6. Seamless Encryption/Decryption with modern encryption like AesGcm and Argon2ID for GPU proof hashing.
7. OpenTelemetry support for processing, producing, and consuming. This includes distributed tracing.
8. LoggerFactory support for logging (DEBUG, TRACE, INFO, WARN, ERROR).
9. Ability to customize and subsitution many mechanisms of your own design.

### ConsumerDataflow - What is TState, IRabbitWorkState, or IWorkState?

The `RabbitWorkState` implements the `IRabbitWorkState`. It's a fairly bare bones class and includes
the data coming out of a `Consumer` in the `IReceivedMessage` property and potentially outbound data
held in the SendMessage `IMessage` property. This class is how you communicate between steps.

```csharp
public interface IRabbitWorkState : IWorkState
{
    IReceivedMessage ReceivedMessage { get; set; }
    IMessage SendMessage { get; set; }
    bool SendMessageSent { get; set; }
}

public abstract class RabbitWorkState : IRabbitWorkState
{
    [IgnoreDataMember]
    public virtual IReceivedMessage ReceivedMessage { get; set; }

    public virtual ReadOnlyMemory<byte> SendData { get; set; }
    public virtual IMessage SendMessage { get; set; }
    public virtual bool SendMessageSent { get; set; }

    public virtual IDictionary<string, object> Data { get; set; }

    public virtual IDictionary<string, bool> StepSuccess { get; set; }
    public virtual string StepIdentifier { get; set; }

    public bool IsFaulted { get; set; }
    public ExceptionDispatchInfo EDI { get; set; }

    public TelemetrySpan WorkflowSpan { get; set; }
}
```

The `IWorkState` class is yet another base interface that handles what's needed in both 
ConsumerPiplines and ConsumerDataflows.

```csharp
public interface IWorkState
{
    IDictionary<string, object> Data { get; set; }

    // Routing Logic
    IDictionary<string, bool> StepSuccess { get; set; }
    string StepIdentifier { get; set; }

    // Error Handling
    bool IsFaulted { get; set; }
    ExceptionDispatchInfo EDI { get; set; }

    // Outbound
    ReadOnlyMemory<byte> SendData { get; set; }

    // RootSpan or ChildSpan derived from TraceParentHeader
    TelemetrySpan WorkflowSpan { get; set; }
}
```

The generic constraints of `TState` are as follows:

```csharp
public class ConsumerDataflow<TState> : BaseDataflow<TState> where TState : class, IRabbitWorkState, new()
```

That means your custom `TState` is a class that at least implements `IRabbitWorkState`. Be sure to
review the code to get the most up to date information. For your `AddStep()` methods, this will be the
class you pass in and return out of at each step. This is how you communicate between steps.

### What is BaseDataflow?

The `BaseDataflow` is a class that is the foundation of the `ConsumerDataflow` or `Dataflow` customizations
you would wish to make on your own. It's lot of code that handles the wiring up of TPL Datablocks.

## ConsumerDataflow - Getting Started

To get started, we will need some `RabbitOptions` and a `RabbitService` to work with.

```csharp
using HouseofCat.Compression.Recyclable;
using HouseofCat.Encryption;
using HouseofCat.Hashing;
using HouseofCat.RabbitMQ;
using HouseofCat.RabbitMQ.Extensions;
using HouseofCat.Serialization;

// Step 0: Setup Logging
var loggerFactory = LogHelpers.CreateConsoleLoggerFactory(LogLevel.Information);
LogHelpers.LoggerFactory = loggerFactory;
var logger = loggerFactory.CreateLogger<Program>();

// Step 1: Load RabbitOptions from a file.
var rabbitOptions = await RabbitOptionsExtensions.GetRabbitOptionsFromJsonFileAsync("./SampleRabbitOptions.json");

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
```

### ConsumerDataflow - Setup - Creating a CustomWorkState

I have forced the need to crate your own class here. This is to prevent any confusion on behalf of
developers. You are supposed to handle your state/implementation which allows you to transition between
steps. You can start with a blank workstate and only add properties as you need to extend behavior or
functionality.

```csharp
using HouseofCat.RabbitMQ.Dataflows;

namespace ConsumerDataflowExample;

public sealed class CustomWorkState : RabbitWorkState
{
    // Leaving blank
}
```

### ConsumerDataflow - Setup - - CustomWorkState

Now that we have defined our CustomWorkState, we can construct a ConsumerDataflow.

```csharp
using HouseofCat.Compression.Recyclable;
using HouseofCat.Encryption;
using HouseofCat.Hashing;
using HouseofCat.RabbitMQ;
using HouseofCat.RabbitMQ.Extensions;
using HouseofCat.Serialization;

// Step 0: Setup Logging
var loggerFactory = LogHelpers.CreateConsoleLoggerFactory(LogLevel.Information);
LogHelpers.LoggerFactory = loggerFactory;
var logger = loggerFactory.CreateLogger<Program>();

// Step 1: Load RabbitOptions from a file.
var rabbitOptions = await RabbitOptionsExtensions.GetRabbitOptionsFromJsonFileAsync("./SampleRabbitOptions.json");

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

// Step 4: Get our ConsumerOptions
var consumerOptions = rabbitOptions.GetConsumerOptions("TestConsumer");

// Step 5: Create a ConsumerDataflow using CustomWorkState (the class that inherits/implements RabbitWorkState/IRabbitWorkState).
var dataflow = new ConsumerDataflow<CustomWorkState>(
    rabbitService,
    consumerOptions,
    TaskScheduler.Current);
```

### ConsumerDataflow - Setup - BuildState

The purpose of this mechanism is to essentially convert the `IReceivedMessage` into a `TState` object but
since this behavior is highly customizable, I have only included one possible implementation and this
tries to handle both `IReceivedMessage` and inner payload of `IMessage`. This is a very simple step so
that your supplied methods/functions have only the state parameter to juggle.

```csharp
var dataflow = new ConsumerDataflow<CustomWorkState>(
    rabbitService,
    consumerOptions,
    TaskScheduler.Current)
    .WithBuildState();
```

Other than `WithBuildState()`, all other methods except Finalize() are optional. That being said, if you
are using `IReceivedMessage` and internally `IMessage` envelopes, we have built-in steps to help streamline
your Dataflow bootstrap. You just need to include the providers you wish to use.

### ConsumerDataflow - Setup - ISerialization Provider

The `ISerializationProvider` is already assigned to the same one inside the `IRabbitService` you supplied.
You can also override this by supplying a different one or even unsetting it.

```csharp
var dataflow = new ConsumerDataflow<CustomWorkState>(
    rabbitService,
    consumerOptions,
    TaskScheduler.Current)
    .SetSerializationProvider(jsonProvider)
    .WithBuildState();

// OR
var dataflow = new ConsumerDataflow<CustomWorkState>(
    rabbitService,
    consumerOptions,
    TaskScheduler.Current)
    .UnsetSerializationProvider()
    .WithBuildState();
```

### ConsumerDataflow - Setup - IEncryptionProvider

If you wish to decrypt inbound data though with the built-in steps, you will need to assign
`IEncryptionProvider` here. This is not assumed because it is unknown at this point what your intention
is with the data coming out of the consumer. Maybe you are sending it to a database, saving it to S3,
or even sending it as is to another service etc.,

```csharp
    .SetEncryptionProvider(rabbitService.EncryptionProvider)
    .WithDecryptionStep();
```

### ConsumerDataflow - Setup - ICompressionProvider

Just like `IEncryptionProvider`, iff you intend to decompress the data and wish to use you the built-in
step, you will need to assign a `ICompressionProvider` here.

```csharp
    .SetCompressionProvider(rabbitService.CompressionProvider)
    .WithDecompressionStep();
```

### ConsumerDataflow - Setup - SendMessage

If you are intending to output the data to another RabbitMQ queue, I simplify this by allowing you to
assign values to WorkState and then outbound data will be sent to the queue you specify in the 
ConsumerOptions.

```csharp
using HouseofCat.Compression.Recyclable;
using HouseofCat.Encryption;
using HouseofCat.Hashing;
using HouseofCat.RabbitMQ;
using HouseofCat.RabbitMQ.Dataflows;
using HouseofCat.RabbitMQ.Extensions;
using HouseofCat.Serialization;
using HouseofCat.Utilities.Helpers;
using Microsoft.Extensions.Logging;
using System.Text;

// Step 0: Setup Logging
var loggerFactory = LogHelpers.CreateConsoleLoggerFactory(LogLevel.Information);
LogHelpers.LoggerFactory = loggerFactory;
var logger = loggerFactory.CreateLogger<Program>();

// Step 1: Load RabbitOptions from a file.
var rabbitOptions = await RabbitOptionsExtensions.GetRabbitOptionsFromJsonFileAsync("./SampleRabbitOptions.json");

// Step 2: Setup your Providers (only ISerializationProvider is required)
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

// Step 4: Get our ConsumerOptions
var consumerOptions = rabbitOptions.GetConsumerOptions("TestConsumer");

// Step 5: Create a ConsumerDataflow using CustomWorkState (the class that inherits/implements RabbitWorkState/IRabbitWorkState).
var dataflow = new ConsumerDataflow<CustomWorkState>(
    rabbitService,
    Options,
    taskScheduler)
    .SetSerializationProvider(rabbitService.SerializationProvider)
    .SetCompressionProvider(rabbitService.CompressionProvider)
    .SetEncryptionProvider(rabbitService.EncryptionProvider)
    .WithBuildState()
    .WithDecompressionStep()
    .WithDecryptionStep();

// Optional Step 6: Add SendMessage steps
if (!string.IsNullOrWhiteSpace(Options.SendQueueName))
{
    if (rabbitService.CompressionProvider is not null && Options.WorkflowSendCompressed)
    {
        dataflow = dataflow.WithSendCompressedStep();
    }
    if (rabbitService.EncryptionProvider is not null && Options.WorkflowSendEncrypted)
    {
        dataflow = dataflow.WithSendEncryptedStep();
    }

    dataflow = dataflow.WithSendStep();
}
```

### ConsumerDataflow - Setup - WithCreateSendMessage
Let's say when we are finishing processing our added steps, we want to create some outbound steps. Here
we want to send another message to another queue but use the built-in steps. All we must do is build the
outbound message and assign it to the State.

```csharp
dataflow.WithCreateSendMessage(
    async (state) =>
    {
        var message = new Message
        {
            Exchange = "",
            RoutingKey = state.ReceivedMessage?.Message?.RoutingKey ?? "TestQueue",
            Body = Encoding.UTF8.GetBytes("New Secret Message"),
            Metadata = new Metadata
            {
                PayloadId = Guid.NewGuid().ToString(),
            },
            ParentSpanContext = state.WorkflowSpan?.Context,
        };

        // You can manually compress and encrypt the message here. If your
        // RabbitOptions.PublishOptions are set to Encrypt/Compress, then
        // it will be done automatically there too. You want to be careful
        // and not double compress/encrypt the message.
        // await rabbitService.ComcryptAsync(message);

        state.SendMessage = message;
        return state;
    });
```

### ConsumerDataflow - Setup - Add Steps
The most powerful part is now the ability to `AddSteps`. You can add as many custom steps as you
wish to perform on the data. The only requirement is that the method signature must match the
`Func<TState, Task<TState>>` or `Func<TState, TState>` delegate.

The optional parameters are what you can use to modify the execution options, such increase or
decrease the max degrees of parallelism, limit the total number executions at once, or slow things
down by ensuring the order of execution. By default, it will use the options assigned in the
`ConsumerOptions`.

The follow example writes the IReceivedMessage.IMessage.Body to the console and gives the function
a name. The name ends up being appeneded to the OpenTelemetry name to label the trace.

```csharp
dataflow.AddStep(
    (state) =>
    {
        var message = Encoding.UTF8.GetString(state.ReceivedMessage.Body.Span);
        if (message == "throw")
              {
            throw new Exception("Throwing an exception!");
        }

        Console.WriteLine(message);

        return state;
    },
    "write_message_to_log");
```

### ConsumerDataflow - Setup - Add Steps - WithExecutionOptions
```csharp
dataflow.AddStep(
    (state) =>
    {
        var message = Encoding.UTF8.GetString(state.ReceivedMessage.Body.Span);
        if (message == "throw")
              {
            throw new Exception("Throwing an exception!");
        }

        Console.WriteLine(message);

        return state;
    },
    "write_message_to_log",
    maxDoP: 32,
    ensureOrdered: true,
    boundedCapacity: 100);
```

### ConsumerDataflow - Setup - Error Handling (aka Why You No Try/Catch?!)
You can wire up your own `try {} catch {}` blocks in your `AddStep()`, however, I have included the
ability to asynchronously process any thrown exceptions. The `Exception` and `StackTrace` are retained by
using ExceptionDispatchInfo.

You still must supply a method that will handle the exception that eventually reaches that destination.

Here we log the exception, then dynamically route the message to a different queue. First, we check the
`ConsumerOptions` for any `QueueArgs` that correlate with the DeadLetterQueue/Exchange. Those are first
priority for rejection message with `requeue: false`. After that, here we check if the `ErrorQueueName`
has value. If it does and we are dealing with an `IMessage` we can simply change the `RoutingKey` to
the error queue, and queue for publishing. If not, we have to manually publish the message to next queue
using the standard publish method. Last, here we decide to `Nack` and requeue the message, but this could
be end up block the queue if the message can't be processed due to invalid data/processing. This is just
an example of the logic involved in error handling. Feel free to adjust this example to suit your needs.

One last thing, you always have to send the BoundedCapacity for the ErrorHandling step. I recommend you
keep a lower value than what you set in the `ConsumerOptions.WorkflowBatchSize`. This can help ensure
that if your error processing gets overwhelmed, the Dataflow will begin to slow down all procesing
by backing up. This is an incredibly useful thing because it will prevent your system from crashing or
overloading downstream systems.

TL:DR; Set a healthy boundedCapacity on ErrorHandling. As the Dataflow begins to exhibit issues, it will
throttle itself and naturally slow down. This is an anti-fragility pattern in action.

```csharp
dataflow.WithErrorHandling(
    async (state) =>
    {
        logger.LogError(state?.EDI?.SourceException, "Exception Occured");

        // First, check if DLQ is configured in QueueArgs.
        // Second, check if ErrorQueue is set in Options.
        // Lastly, decide if you want to Nack with requeue, or anything else.

        if (consumerOptions.RejectOnError())
        {
            state.ReceivedMessage?.RejectMessage(requeue: false);
        }
        else if (!string.IsNullOrEmpty(consumerOptions.ErrorQueueName))
        {
            // If type is currently an IMessage, republish with new RoutingKey.
            if (state.ReceivedMessage.Message is not null)
            {
                state.ReceivedMessage.Message.RoutingKey = consumerOptions.ErrorQueueName;
                await rabbitService.Publisher.QueueMessageAsync(state.ReceivedMessage.Message);
            }
            else
            {
                await rabbitService.Publisher.PublishAsync(
                    exchangeName: "",
                    routingKey: consumerOptions.ErrorQueueName,
                    body: state.ReceivedMessage.Body,
                    headers: state.ReceivedMessage.Properties.Headers,
                    messageId: Guid.NewGuid().ToString(),
                    deliveryMode: 2,
                    mandatory: false);
            }

            // Don't forget to Ack the original message when sending it to a different Queue.
            state.ReceivedMessage?.AckMessage();
        }
        else
        {
            state.ReceivedMessage?.NackMessage(requeue: true);
        }
    },
    boundedCapacity: 100,
    ensureOrdered: false,
    maxDoP: 1);
```

### ConsumerDataflow - Setup - Finalization Step
Very easy to explain, this is the last action in the Dataflow (other than ErrorHandling). It's the very
last thing that will be executed and allows you to perform any additional operations/cleanup.

The lifecycle of objects inside TPL can be somewhat confusing.

I generally put my `Ack/Nack` logic here.

```csharp
dataflow.WithFinalization(
    (state) =>
    {
        logger.LogInformation("Finalization Step!");

        state.ReceivedMessage?.AckMessage();
    });
```

## ConsumerDataflow - Full Example All Together
```csharp
using HouseofCat.Compression.Recyclable;
using HouseofCat.Encryption;
using HouseofCat.Hashing;
using HouseofCat.RabbitMQ;
using HouseofCat.RabbitMQ.Dataflows;
using HouseofCat.RabbitMQ.Extensions;
using HouseofCat.Serialization;
using HouseofCat.Utilities.Helpers;
using Microsoft.Extensions.Logging;
using System.Text;

// Step 0: Setup Logging
var loggerFactory = LogHelpers.CreateConsoleLoggerFactory(LogLevel.Information);
LogHelpers.LoggerFactory = loggerFactory;
var logger = loggerFactory.CreateLogger<Program>();

// Step 1: Load RabbitOptions from a file.
var rabbitOptions = await RabbitOptionsExtensions.GetRabbitOptionsFromJsonFileAsync("./SampleRabbitOptions.json");

// Step 2: Setup your Providers (only ISerializationProvider is required)
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

// Step 4: Get our ConsumerOptions
var consumerOptions = rabbitOptions.GetConsumerOptions("TestConsumer");

// Step 5: Create a ConsumerDataflow using CustomWorkState (the class that inherits/implements RabbitWorkState/IRabbitWorkState).
var dataflow = new ConsumerDataflow<CustomWorkState>(
    rabbitService,
    consumerOptions,
    TaskScheduler.Current)
    .SetSerializationProvider(rabbitService.SerializationProvider)
    .SetEncryptionProvider(rabbitService.EncryptionProvider)
    .SetCompressionProvider(rabbitService.CompressionProvider)
    .WithBuildState()
    .WithDecompressionStep();

dataflow.WithCreateSendMessage(
    (state) =>
    {
        var message = new Message
        {
            Exchange = "",
            RoutingKey = state.ReceivedMessage?.Message?.RoutingKey ?? "TestQueue",
            Body = Encoding.UTF8.GetBytes("New Secret Message"),
            Metadata = new Metadata
            {
                PayloadId = Guid.NewGuid().ToString(),
            },
            ParentSpanContext = state.WorkflowSpan?.Context,
        };

        // You can manually compress and encrypt the message here. If your
        // RabbitOptions.PublishOptions are set to Encrypt/Compress, then
        // it will be done automatically there too. You want to be careful
        // and not double compress/encrypt the message.
        // await rabbitService.ComcryptAsync(message);

        state.SendMessage = message;
        return state;
    });

dataflow.AddStep(
    (state) =>
    {
        var message = Encoding.UTF8.GetString(state.ReceivedMessage.Body.Span);

        Console.WriteLine(message);

        return state;
    },
    "write_message_to_log");

dataflow.WithErrorHandling(
    async (state) =>
    {
        logger.LogError(state?.EDI?.SourceException, "Exception Occured");

        // First, check if DLQ is configured in QueueArgs.
        // Second, check if ErrorQueue is set in Options.
        // Lastly, decide if you want to Nack with requeue, or anything else.

        if (consumerOptions.RejectOnError())
        {
            state.ReceivedMessage?.RejectMessage(requeue: false);
        }
        else if (!string.IsNullOrEmpty(consumerOptions.ErrorQueueName))
        {
            // If type is currently an IMessage, republish with new RoutingKey.
            if (state.ReceivedMessage.Message is not null)
            {
                state.ReceivedMessage.Message.RoutingKey = consumerOptions.ErrorQueueName;
                await rabbitService.Publisher.QueueMessageAsync(state.ReceivedMessage.Message);
            }
            else
            {
                await rabbitService.Publisher.PublishAsync(
                    exchangeName: "",
                    routingKey: consumerOptions.ErrorQueueName,
                    body: state.ReceivedMessage.Body,
                    headers: state.ReceivedMessage.Properties.Headers,
                    messageId: Guid.NewGuid().ToString(),
                    deliveryMode: 2,
                    mandatory: false);
            }

            // Don't forget to Ack the original message when sending it to a different Queue.
            state.ReceivedMessage?.AckMessage();
        }
        else
        {
            state.ReceivedMessage?.NackMessage(requeue: true);
        }
    },
    boundedCapacity: 100,
    ensureOrdered: false,
    maxDoP: 1);

dataflow.WithFinalization(
    (state) =>
    {
        logger.LogInformation("Finalization Step!");

        state.ReceivedMessage?.AckMessage();
    });
```

### ConsumerDataflow - Start
Now that everything is wired up and configured, just start the Dataflow! This will kick start the Consumer
internally and start processing all the messages in your queque and executing your methods.

```csharp
await dataflow.StartAsync();
```

## ConsumerDataflowService - Full Example All Together
The `ConsumerDataflowService` is merely a helping hand in bootstraping up ConsumerDataflows exactly like
you see it above.

```csharp
using HouseofCat.Compression.Recyclable;
using HouseofCat.Encryption;
using HouseofCat.Hashing;
using HouseofCat.RabbitMQ;
using HouseofCat.RabbitMQ.Extensions;
using HouseofCat.RabbitMQ.Services;
using HouseofCat.Serialization;
using HouseofCat.Utilities.Helpers;
using Microsoft.Extensions.Logging;
using System.Text;

// Step 0: Setup Logging
var loggerFactory = LogHelpers.CreateConsoleLoggerFactory(LogLevel.Information);
LogHelpers.LoggerFactory = loggerFactory;
var logger = loggerFactory.CreateLogger<Program>();

// Step 1: Load RabbitOptions from a file.
var rabbitOptions = await RabbitOptionsExtensions.GetRabbitOptionsFromJsonFileAsync("./SampleRabbitOptions.json");

// Step 2: Setup your Providers (only ISerializationProvider is required)
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
    loggerFactory);

// Step 4: Create a ConsumerDataflowService that automatically builds a ConsumerDataflow.
var dataflowService = new ConsumerDataflowService<CustomWorkState>(rabbitService, "TestConsumer");

// Manually modify the internal Dataflow.
dataflowService.Dataflow.WithCreateSendMessage(
    async (state) =>
    {
        var message = new Message
        {
            Exchange = "",
            RoutingKey = state.ReceivedMessage?.Message?.RoutingKey ?? "TestQueue",
            Body = Encoding.UTF8.GetBytes("New Secret Message"),
            Metadata = new Metadata
            {
                PayloadId = Guid.NewGuid().ToString(),
            },
            ParentSpanContext = state.WorkflowSpan?.Context,
        };

        await rabbitService.ComcryptAsync(message);

        state.SendMessage = message;
        return state;
    });

// Add custom step to Dataflow using Service helper methods.
dataflowService.AddStep(
    "write_message_to_log",
    (state) =>
    {
        var message = Encoding.UTF8.GetString(state.ReceivedMessage.Body.Span);
        if (message == "throw")
        {
            throw new Exception("Throwing an exception!");
        }

        logger.LogInformation(message);

        return state;
    });

// Add finalization step to Dataflow using Service helper method.
dataflowService.AddFinalization(
    (state) =>
    {
        logger.LogInformation("Finalization Step!");

        state.ReceivedMessage?.AckMessage();
    });

// Add error handling to Dataflow using Service helper method.
dataflowService.AddErrorHandling(
    async (state) =>
    {
        logger.LogError(state?.EDI?.SourceException, "Exception Occured");

        // First, check if DLQ is configured in QueueArgs.
        // Second, check if ErrorQueue is set in Options.
        // Lastly, decide if you want to Nack with requeue, or anything else.

        if (dataflowService.Options.RejectOnError())
        {
            state.ReceivedMessage?.RejectMessage(requeue: false);
        }
        else if (!string.IsNullOrEmpty(dataflowService.Options.ErrorQueueName))
        {
            // If type is currently an IMessage, republish with new RoutingKey.
            if (state.ReceivedMessage.Message is not null)
            {
                state.ReceivedMessage.Message.RoutingKey = dataflowService.Options.ErrorQueueName;
                await rabbitService.Publisher.QueueMessageAsync(state.ReceivedMessage.Message);
            }
            else
            {
                await rabbitService.Publisher.PublishAsync(
                    exchangeName: "",
                    routingKey: dataflowService.Options.ErrorQueueName,
                    body: state.ReceivedMessage.Body,
                    headers: state.ReceivedMessage.Properties.Headers,
                    messageId: Guid.NewGuid().ToString(),
                    deliveryMode: 2,
                    mandatory: false);
            }

            // Don't forget to Ack the original message when sending it to a different Queue.
            state.ReceivedMessage?.AckMessage();
        }
        else
        {
            state.ReceivedMessage?.NackMessage(requeue: true);
        }
    });

await dataflowService.StartAsync();
```
