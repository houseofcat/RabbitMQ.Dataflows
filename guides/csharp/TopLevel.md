##### The Users Challenge
They want to write a few utility apps or microservices and they like C# in general but it is kind of boilerplate-y. They would rather brush
off/ignore my NetCore/C# suggestions and end up writing their app in something simpler like `golang` or `python` when they want a quick
ConsoleApp or Daemon etc.  

##### Top-Level Statements
While I whole heartedly believe people should enjoy the language they work with, I do think the basic "boilerplate" code found around C# patterns
like `Program.cs`, `Startup.cs`, and AspNetCore hosting in general, make a lot of sense. I find it quite tolerable and the complaints are kind of
hyperbolic compared to other systems. They clearly have not met `Java` huehuehuehue. A large majority of the time what that person is really saying
is I don't know why I need this. Now that is something I could totally get behind. Why do I need a `namespace`? Why do I need have a `Program.cs`?
Why is there a `static main`? Et cetera.
    
Even though I don't necessarily agree that C# is very boilerplate-y, there just happens to be a shiny, new, but more importantly - _**slimming**_ -
feature in [C#9.0](https://docs.microsoft.com/en-us/dotnet/csharp/whats-new/csharp-9) targeted at those that want to spin up tiny apps or utilities
from a single `.cs` file without a lot of ceremony. I want to try and put that idea to the test.

##### Introducing Top-Level
_**Microsoft: Top-level statements enable you to avoid the extra ceremony required by placing your program's entry point in a static method in a class.**_ 
    
`dotnet new ` Console application templates usually generate something like this.

```csharp
using System;

namespace Application
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
        }
    }
}
```

In Top-level (final?) form:

```csharp
using System;

Console.WriteLine("Hello World!");
```

That is... it? Okay... so I made a real application in a single file, but boy is that just too boring and disappointing for a guide. It doesn't even do anything! I may
not fully like the idea, but we should at least demonstrate features with pizazz and `fuego`!

_**Edit**_  
A silly [redditor](https://www.reddit.com/r/csharp/comments/mzwd4p/c90_toplevel_statement_exampleguide/gw4n047/) pointed out you could have
simplified it just a single line. Which is just a reminder you can use the fully qualified names everywhere and remove the need for `using` 
statements at the top of your file... but I actually dislike that. I always use `using` statements. Regardless, here is what that looks like.
```csharp
System.Console.WriteLine("Hello World!");
```

You can skip out on me taking this to the next level if you want, there are `Top-level` statement links below that follow the basic usage guidelines from Microsoft.

##### A Top-Level RabbitMQ Consumer
I am going to see if I can combine top-level statements with my [Tesseract/RabbitMQ library](https://github.com/houseofcat/tesseract) to create a RabbitMQ `Consumer` as
a NanoService. Oh I like that, `NanoService`. I picked RabbitMQ because I have plenty of experience with RabbitMQ, microservices, and self-contained realiable apps that
also happen to scale from micro to monolithic. I can also easily test how well it performs. So lets see what that looks like at the nano-scale.

Lets start by building an app with the categorical `ILogger<T>` and `LoggerFactory` because real apps use logging. That is today's additional challenge, building a real
world application contained in a single file that is also still somehow maintainable.

```csharp
using Microsoft.Extensions.Logging;
using System;
using System.Text;

var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Information));
var logger = loggerFactory.CreateLogger("Program");
```

That means also adding some NuGet references.

```
<PackageReference Include="Microsoft.Extensions.Logging" Version="5.0.0" />
<PackageReference Include="Microsoft.Extensions.Logging.Console" Version="5.0.0" />
```

Going to create the helper service provided for RabbitMQ.

```csharp
using HouseofCat.RabbitMQ;
using HouseofCat.RabbitMQ.Services;
using HouseofCat.Serialization;
using Microsoft.Extensions.Logging;
using System;
using System.Text;

var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Information));
var logger = loggerFactory.CreateLogger("Program");
var serializationProvider = new Utf8JsonProvider();
var rabbitService = new RabbitService(
    "HouseofCatConfig.json",
    serializationProvider,
    encryptionProvider: null,
    compressionProvider: null,
    loggerFactory);
```

Which means more NuGets. How about we just list out all the NuGets together.

```
<ItemGroup>
    <PackageReference Include="HouseofCat.RabbitMQ" Version="1.0.6" />
    <PackageReference Include="HouseofCat.RabbitMQ.Services" Version="1.0.7" />
    <PackageReference Include="HouseofCat.Serialization.Json.Utf8Json" Version="1.0.3" />
    <PackageReference Include="Microsoft.Extensions.Logging" Version="5.0.0" />
    <PackageReference Include="Microsoft.Extensions.Logging.Console" Version="5.0.0" />
</ItemGroup>
```

Let me copy in a basic HoC config with our consumer settings in it. This file needs to be copied to the bin folder, so do not forget to `Copy Always`. I named mine
`"HouseofCatConfig.json"` for the purposes of this experiment.

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
    "EnableDispatchConsumersAsync": true,
    "ServiceName": "HoC.RabbitMQ",
    "Connections": 2,
    "Channels": 10,
    "AckableChannels": 0,
    "SleepOnErrorInterval": 5000,
    "TansientChannelStartRange": 10000,
    "UseTransientChannels": false
  },
  "PublisherOptions": {
    "MessageQueueBufferSize": 100,
    "BehaviorWhenFull": 0,
    "CreatePublishReceipts": false,
    "Compress": false,
    "Encrypt": false,
    "WaitForConfirmationTimeoutInMilliseconds": 500
  },
  "ConsumerOptions": {
    "HoC-Consumer": {
      "Enabled": true,
      "ConsumerName": "HoC-Consumer",
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
}
```

The config has the `Consumer` named as `HoC-Consumer` so let me get that prebuilt `Consumer` object out of the RabbitService and then start the consuming.

```csharp
using HouseofCat.RabbitMQ;
using HouseofCat.RabbitMQ.Services;
using HouseofCat.Serialization;
using Microsoft.Extensions.Logging;
using System;
using System.Text;

var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Information));
var logger = loggerFactory.CreateLogger("Program");
var serializationProvider = new Utf8JsonProvider();
var rabbitService = new RabbitService(
    "HouseofCatConfig.json",
    serializationProvider,
    encryptionProvider: null,
    compressionProvider: null,
    loggerFactory);

await rabbitService.StartAsync();

var consumer = rabbitService.GetConsumer("HoC-Consumer");
await consumer.StartConsumerAsync();
```

Messages at this point should be sitting in the `ConsumerBuffer`. I am going to use `IAsyncEnumerable` to stream those out of the local buffer for
further processing. `ForEach ReceivedMessage` we will read the inner body and then Ack/Nack the message as a processing step (do work step).
Rather than an ugly/bulky `foreach` let us create a `local function` called `ProcessMessage` to keep things nice and clean. We are not using
auto-ack so we have to ack our messages for them be marked as finished (or nack/unfinished) with server-side.

```csharp
await foreach (var receivedMessage in consumer.StreamOutUntilClosedAsync()) // this will exit only when the internal buffer closes/exception
{
    ProcessMessage(receivedMessage);
}

void ProcessMessage(IReceivedMessage receivedMessage)
{
    try
    {
        var body = Encoding.UTF8.GetString(receivedMessage.Data);
        logger.LogInformation($"{DateTime.Now:yyyy/MM/dd hh:mm:ss.ffffff} - [Message Received]: {body}");

        if (receivedMessage.Ackable)
        { receivedMessage.AckMessage(); }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred processing messages from the consumer buffer.");

        if (receivedMessage.Ackable)
        { receivedMessage.NackMessage(requeue: true); }
    }
}
```

Adding a `ShutdownAsync` in there brings the whole thing together.
        
Lets take a look at everything.

```csharp
using HouseofCat.RabbitMQ;
using HouseofCat.RabbitMQ.Services;
using HouseofCat.Serialization;
using Microsoft.Extensions.Logging;
using System;
using System.Text;

var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Information));
var logger = loggerFactory.CreateLogger("Program");
var serializationProvider = new Utf8JsonProvider();
var rabbitService = new RabbitService(
    "HouseofCatConfig.json",
    serializationProvider,
    encryptionProvider: null,
    compressionProvider: null,
    loggerFactory);

await rabbitService.StartAsync();
var consumer = rabbitService.GetConsumer("HoC-Consumer");
await consumer.StartConsumerAsync();

await foreach (var receivedMessage in consumer.StreamOutUntilClosedAsync())
{
    ProcessMessage(receivedMessage);
}

await rabbitService.ShutdownAsync(immediately: false);

void ProcessMessage(IReceivedMessage receivedMessage)
{
    try
    {
        var body = Encoding.UTF8.GetString(receivedMessage.Data);
        logger.LogInformation($"{DateTime.Now:yyyy/MM/dd hh:mm:ss.ffffff} - [Message Received]: {body}");

        if (receivedMessage.Ackable)
        { receivedMessage.AckMessage(); }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred streaming out messages from the consumer.");

        if (receivedMessage.Ackable)
        { receivedMessage.NackMessage(requeue: true); }
    }
}
```

##### What Have I Done?
That was 45 lines of code. Looking at this makes me feel dirty. I am impressed and disgusted with myself at the same time. Now this is kind of cheating, durable RabbitMQ connectivity is
handled by my library. I have quite a few Quality of Life things as well that help keep this code short, but _**that is the entire point of that library**_ and I had never considered that an
application would be this small and manageable.

![Application for Ants?](https://houseofcat.blob.core.windows.net/hocblog/images/guides/top-level-ants.jpg)  

##### So now what?
In keeping with the slim theme, lets publish tiny files. I am going to publish this app as a self-contained win-x64 runtime with...

```
<PublishTrimmed>true</PublishTrimmed>
<TrimMode>Link</TrimMode>
```

...and also disabling ReadyToRun compilation to see how small I can get this single coded file (`Program.cs`) application. [ReadyToRun](https://docs.microsoft.com/en-us/dotnet/core/deploying/ready-to-run)
being disabled should in theory slow down startup time but can bloat executables for that performance.

![It's Tiny!](https://houseofcat.blob.core.windows.net/hocblog/images/guides/top-level-app-size.png)  

And just like Baby Yoda, this thing can be freakishly cute small at 7,519 KB. Just imagine speeding up Docker container deployments with this tiny fella.

##### So how does it perform?
It isn't the fastest test I have ever ran, but the code above is essentially a non-blocking sequential `for` loop. It managed to peak at around 12,000 msg/s. We could
definitely make this quite a bit faster by adding concurrent processing so it is still pretty good for a single consumer not using concurrency.
![It Performs Well!](https://houseofcat.blob.core.windows.net/hocblog/images/guides/top-level-perf.png)  
