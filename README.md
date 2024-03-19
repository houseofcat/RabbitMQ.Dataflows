# RabbitDataflows

A library of `.NET` tools to help rapidly develop well performant micro/macroservices. 

Prototypes you could send to production!  

## Why Make A RabbitMQ Powered Dataflow?  

`Dataflows` have configurable concurrency, serialization, monitoring, compression, and encryption all as first class citizens. This paradigm allows developers to just focus on the important stuff - getting work done. Dataflows pay attention to the extra dimensions so you don't have to!

Here are some features ready with RabbitMQ today, tomorrow - the world!

### Queueing
* Async Processing    
* Retriability  
* Chaos Engineering  
* Connection/Channel Durability provided by `HouseofCat.RabbitMQ` formerly `CookedRabbit.Core`.  

### Built-Ins
* Supports `ILogger<T>`  
* Concurrency/Parallelism - baked in from the ground up.  
* Contracted `WorkState`/WorkObject simplifies development and integration.  
* Has `Json` (2 flavors) and `MessagePack` serialization providers.
* Allow transparent encryption/decryption steps.  
* Allow compression/decompression steps to reduce trip time over the wire.  
* Async Error Handling with Predicate triggers and an actionable callback.  

### Interchangeability
* Allows you to replace serialization provider with `HouseofCat` Provider wrappers.  
* Allows you to replace encryption provider with `HouseofCat` Provider wrappers.  
* Allows you to replace compression provider with `HouseofCat` Provider wrappers.   
* Constructed to fully support Inversion of Control.  

### Business Logic
* All steps process in the order provided allowing you to still control order of execution.  
* All automatically subscribed to Async Error handling by `WorkState.IsFaulted` flag.  

### Testing
* All built-in steps will have integration tests that should remove concerns from end-user developer.   
* Future case will include much more complex abstract UnitTesting as time allows.  
* The developer should only need to unit test their functional business code.  

## Implicit Benefits

The benefits of a dataflow pattern extend beyond fancy machine learning and Tensorflows or high throughput GCP Dataflow for mass computation. When brought to the service level, it helps organize your code into more manageable blocks. You can still write monolithic functions, but you would be hamstringing yourself and scarificing concurrency and parallelism. By designing code into small functional steps, you always write better, cleaner, code reduced with cyclomatic complexity. That very same code is easier to UnitTest. The orchestration of the function calls are the order they are added allowing you extend the original functionality infinitely. You don't have to write deserialization or post-processing encryption/compression as they all baked in. Designing from the ground up with concurrency and parallelism, you stay nimble and fast - able to scale up internally, before horizontally and vertically, saving costs. All without needing code changed or refactored.

Lastly, after everything is said and done, all your business code is re-usable. Should you decide to abandon this workflow (:worried:) for a different mechanism, engine, or what not, all of your code will happily port to whatever other project / flow you are working with and so will all your testing making it a win win.

## Help
You will find library usage examples in the `old\examples` folder. You also can find generic NetCore how-tos and tutorials located in there. The code quality of the entire library will improve over time. Codacy allows me to review code and openly share any pain points so submit a PR to help keep this an A rated library!

Check out each project for additional `README.md`. They will provide additional instructions/examples.

## Status

Test Server: `Windows 11`  
RabbitMQ Server: `v3.13`  
Erlang: `v26.2.3`  

[![Codacy Badge](https://api.codacy.com/project/badge/Grade/9dbb20a30ada48caae4b92a83628f45e)](https://app.codacy.com/gh/houseofcat/RabbitDataflows/dashboard)  

[![build](https://github.com/houseofcat/HouseofCat.Library/workflows/build/badge.svg)](https://github.com/houseofcat/RabbitDataflows/actions/workflows/build.yml)

[![Gitter](https://badges.gitter.im/HoC-Tesseract/community.svg)](https://gitter.im/HoC-Tesseract/community?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge)

# Main RabbitMQ Library

## HouseofCat.RabbitMQ
[![NuGet](https://img.shields.io/nuget/v/HouseofCat.RabbitMQ.svg)](https://www.nuget.org/packages/HouseofCat.RabbitMQ/)  
[![NuGet](https://img.shields.io/nuget/dt/HouseofCat.RabbitMQ.svg)](https://www.nuget.org/packages/HouseofCat.RabbitMQ/)  

A library that focuses on RabbitMQ connection and channel management to create fault tolerant Publishers and Consumers.  
Formerly called CookedRabbit.Core/Tesseract.

# DataFlow Libraries

## HouseofCat.Dataflows
[![NuGet](https://img.shields.io/nuget/v/HouseofCat.Dataflows.svg)](https://www.nuget.org/packages/HouseofCat.Dataflows/)  
[![NuGet](https://img.shields.io/nuget/dt/HouseofCat.Dataflows.svg)](https://www.nuget.org/packages/HouseofCat.Dataflows/)  

A library that provides the base magic Dataflows for RabbitDataflows. 

 * Custom TPL Block - ChannelBock used as a Channel-based `BufferBlock<TIn>`
 * Has DataFlowEngine and ChannelBlockEngine.
 * Has Pipelines (Dataflow alternative).


# Core Productivity Libraries
These libraries are here to help you build powerful Dataflows for your messages.

## HouseofCat.Serialization
[![NuGet](https://img.shields.io/nuget/v/HouseofCat.Serialization.svg)](https://www.nuget.org/packages/HouseofCat.Serialization/)  
[![NuGet](https://img.shields.io/nuget/dt/HouseofCat.Serialization.svg)](https://www.nuget.org/packages/HouseofCat.Serialization/)  

A library that has a collection of .NET ISerializationProvider or the interface to make your own.  
 * Supports MessagePack and System.Text.Json and Newtonsoft.Json.  

## HouseofCat.Compression
[![NuGet](https://img.shields.io/nuget/v/HouseofCat.Compression.svg)](https://www.nuget.org/packages/HouseofCat.Compression/)  
[![NuGet](https://img.shields.io/nuget/dt/HouseofCat.Compression.svg)](https://www.nuget.org/packages/HouseofCat.Compression/)  

A library that has a collection of .NET ICompressionProvider or the interface to make your own.

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

A library that provides encryption contracts and base AesGCM/AesCBC NetCore encryption providers.  

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

## Example Integration Project Ideas

### HouseofCat.RabbitMQ.Twilio
An example project/library to simplify using Twilio (SMS/TextMessages) with the HouseofCat.RabbitMQ library. 

### HouseofCat.RabbitMQ.Mailkit
An example project/library to simplify using Mailkit (Email) with the HouseofCat.RabbitMQ library. 


# [HouseofCat.io](https://houseofcat.io)
