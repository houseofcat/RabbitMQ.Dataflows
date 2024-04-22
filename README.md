# RabbitMQ & Dataflows

A RabbitMQ library of `.NET` tools to help rapidly develop well performant services or
to just help manage durable connectivity with the `RabbitMQ.Client`!  

## Why Make A RabbitMQ Powered Dataflow?  

`Dataflows` have configurable concurrency, serialization, monitoring, compression, and
encryption all as first class citizens. This paradigm allows developers to just focus on
the important stuff - getting work done

Here are some features ready with RabbitMQ and Dataflows today!

## Status

Test Server: `Windows 11`  
RabbitMQ Server: `v3.13`  
Erlang: `v26.2.3`  

![Release](https://img.shields.io/github/v/release/houseofcat/RabbitMQ.Dataflows)
[![build](https://github.com/houseofcat/RabbitMQ.Dataflows/workflows/build/badge.svg)](https://github.com/houseofcat/RabbitMQ.Dataflows/actions/workflows/build.yml)  
[![Codacy Badge](https://app.codacy.com/project/badge/Grade/2ac2a6f51a8c4efd88135bcb835e3a97)](https://app.codacy.com/gh/houseofcat/RabbitMQ.Dataflows/dashboard?utm_source=gh&utm_medium=referral&utm_content=&utm_campaign=Badge_grade)   



### Queueing
* Async Processing, batch processing, consumer cloning and more!    
* Queue-based Retriability via Ack/Nack.
* Async Error Handling (simplify functional error handling by allowing it to throw.)
* A RabbitMQ ConnectionPool and ChannelPool (connection durability) provided by
`namespace HouseofCat.RabbitMQ.Pools;`.  

### Built-Ins
* Supports `ILogger<T>` via LogHelpers. 
* Configurable concurrency/parallelism, no code changes required.  
* Contracted `IWorkState` simplifies functional generic returns and integration.  
* Has `Json` (System.Text.Json and Newtonsoft) and `MessagePack` serialization providers.
* Allows seamless encryption/decryption steps.  
* Allows seamless compression/decompression steps.  
* Async Error Handling with Predicate triggers and an actionable callback.  
* OpenTelemetry with native Distributed Tracing for Publish/Consumer. 

### Core Interchangeability
* Allows you to replace serialization provider with `ISerializationProvider` and have basic 
implementations.  
* Allows you to replace encryption provider with `IEncryptionProvider` and have basic 
implementations.  
* Allows you to replace compression provider with `ICompressionProvider` and have basic 
implementations.   

### Business Logic
* All steps process in the order provided allowing you to still control order of execution.  
* All automatically subscribed to Async Error handling by `WorkState.IsFaulted` flag.  

### Testing
* All built-in steps will have integration tests that should remove concerns from end-user 
developer.   
* Future case will include much more complex abstract UnitTesting as time allows.  
* The developer should only need to unit test their functional business code.  

## Implicit Benefits

The benefits of a dataflow pattern extend beyond fancy machine learning and Tensorflows or
high throughput GCP Dataflow for mass computation. When brought to the service level, it
helps organize your code into more manageable blocks. You can still write monolithic
functions, but you would be hamstringing yourself and scarificing concurrency and
parallelism. By designing code into small functional steps, you always write better,
cleaner, code reduced with cyclomatic complexity. That very same code is easier to
UnitTest. The orchestration of the function calls are the order they are added allowing
you extend the original functionality infinitely. You don't have to write deserialization
or post-processing encryption/compression as they all baked in. Designing from the ground
up with concurrency and parallelism, you stay nimble and fast - able to scale up internally,
before horizontally and vertically, saving costs. All without needing code changed or
refactored.

Lastly, after everything is said and done, all your business code is re-usable. Should
you decide to abandon this workflow (:worried:) for a different mechanism, engine, or
what not, all of your code will happily port to whatever other project / flow you are
working with and so will all your testing making it a win win.  

## Help & Guides
 * Getting started with *RabbitMQ.Dataflows* [ConnectionPool](https://github.com/houseofcat/RabbitMQ.Dataflows/blob/main/guides/rabbitmq/ConnectionPools.md).
 * Getting started with *RabbitMQ.Dataflows* [ChannelPool](https://github.com/houseofcat/RabbitMQ.Dataflows/blob/main/guides/rabbitmq/ChannelPools.md).
 * Getting started with *RabbitMQ.Dataflows* [ChannelPool and BasicPublish](https://github.com/houseofcat/RabbitMQ.Dataflows/blob/main/guides/rabbitmq/BasicPublish.md).
 * Getting started with *RabbitMQ.Dataflows* [ChannelPool and BasicGet](https://github.com/houseofcat/RabbitMQ.Dataflows/blob/main/guides/rabbitmq/BasicGet.md).
 * Getting started with *RabbitMQ.Dataflows* [ChannelPool and BasicConsume](https://github.com/houseofcat/RabbitMQ.Dataflows/blob/main/guides/rabbitmq/BasicConsume.md).
 * Getting started with *RabbitMQ.Dataflows* [Serialization](https://github.com/houseofcat/RabbitMQ.Dataflows/blob/main/guides/rabbitmq/Serialization.md).  

More to come!

You can also find various library examples inside the `tests/UnitTests` or the `tests/RabbitMQ.Console.Test` project.

# Main RabbitMQ Library

## HouseofCat.RabbitMQ
[![NuGet](https://img.shields.io/nuget/v/HouseofCat.RabbitMQ.svg)](https://www.nuget.org/packages/HouseofCat.RabbitMQ/)  
[![NuGet](https://img.shields.io/nuget/dt/HouseofCat.RabbitMQ.svg)](https://www.nuget.org/packages/HouseofCat.RabbitMQ/)  

A library that focuses on RabbitMQ connection and channel management to create fault tolerant Publishers and Consumers.  
Formerly called CookedRabbit.Core/Tesseract.

# DataFlow Library

## HouseofCat.Dataflows
[![NuGet](https://img.shields.io/nuget/v/HouseofCat.Dataflows.svg)](https://www.nuget.org/packages/HouseofCat.Dataflows/)  
[![NuGet](https://img.shields.io/nuget/dt/HouseofCat.Dataflows.svg)](https://www.nuget.org/packages/HouseofCat.Dataflows/)  

A library that provides the base magic Dataflows for RabbitMQ.Dataflows. 

 * Custom TPL Block - ChannelBock used as a Channel-based `BufferBlock<TIn>`
 * Has DataFlowEngine and ChannelBlockEngine.
 * Has Pipelines (Dataflow alternative).


# Core Productivity Libraries
These libraries are here to help you build powerful Dataflows for your messages.

## HouseofCat.Serialization
[![NuGet](https://img.shields.io/nuget/v/HouseofCat.Serialization.svg)](https://www.nuget.org/packages/HouseofCat.Serialization/)  
[![NuGet](https://img.shields.io/nuget/dt/HouseofCat.Serialization.svg)](https://www.nuget.org/packages/HouseofCat.Serialization/)  

A library that has a collection of .NET `ISerializationProvider` or the interface to make your own.  
 * Supports MessagePack and System.Text.Json and Newtonsoft.Json.  

## HouseofCat.Compression
[![NuGet](https://img.shields.io/nuget/v/HouseofCat.Compression.svg)](https://www.nuget.org/packages/HouseofCat.Compression/)  
[![NuGet](https://img.shields.io/nuget/dt/HouseofCat.Compression.svg)](https://www.nuget.org/packages/HouseofCat.Compression/)  

A library that has a collection of .NET `ICompressionProvider` or the interface to make your own.

 * Supports LZ4, Gzip, Brotli, and Deflate.  
 * Supports RecyclableMemoryStream variants. 

## HouseofCat.Hashing
[![NuGet](https://img.shields.io/nuget/v/HouseofCat.Hashing.svg)](https://www.nuget.org/packages/HouseofCat.Hashing/)  
[![NuGet](https://img.shields.io/nuget/dt/HouseofCat.Hashing.svg)](https://www.nuget.org/packages/HouseofCat.Hashing/)  

A library that focuses on implementing hashing.  

 * Supports Argon2.

## HouseofCat.Encryption
[![NuGet](https://img.shields.io/nuget/v/HouseofCat.Encryption.svg)](https://www.nuget.org/packages/HouseofCat.Encryption/)  
[![NuGet](https://img.shields.io/nuget/dt/HouseofCat.Encryption.svg)](https://www.nuget.org/packages/HouseofCat.Encryption/)  

A library that provides encryption contracts and the base `AesGCM`/`AesCBC` .NET `IEncryptionProvider` as
well as the interface to make your own.  

 * Supports AesCbc via CryptoStream (good for encrypted file/memorystreams).  
 * Supports .NET AesGcm 128, 192, 256 (non-streams).  
 * Supports BouncyCastle AesGcm 128/192/256.  
 * Supports RecyclableMemoryStream variants.  

# Non-Critical Library Integrations

## HouseofCat.Data
[![NuGet](https://img.shields.io/nuget/v/HouseofCat.Data.svg)](https://www.nuget.org/packages/HouseofCat.Data/)  
[![NuGet](https://img.shields.io/nuget/dt/HouseofCat.Data.svg)](https://www.nuget.org/packages/HouseofCat.Data/)  

A library that provides the provides helper classes for data manipulation and transformation. 

Also provides Database abstractions, a simple Dapper integration, and SqlKata integration SQL query generation.

### Database Connection Factory Support
 * System.Data.SqlClient
 * Microsoft.Data.SqlClient
 * MySql.Data.MySqlClient
 * Npgsq
 * MySql.Data
 * Oracle
 * SQLite

## HouseofCat.Utilities
[![NuGet](https://img.shields.io/nuget/v/HouseofCat.Utilities.svg)](https://www.nuget.org/packages/HouseofCat.Utilities/)  
[![NuGet](https://img.shields.io/nuget/dt/HouseofCat.Utilities.svg)](https://www.nuget.org/packages/HouseofCat.Utilities/)  

A library that focuses on general purpose utilities and functions that simplify the coding experience.  

# [HouseofCat.io](https://houseofcat.io)
