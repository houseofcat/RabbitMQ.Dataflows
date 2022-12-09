# TESSERACT

![TesseractLogo](https://raw.githubusercontent.com/houseofcat/Tesseract/main/TesseractLogo.svg)

A library of `NetCore` tools to help rapidly develop well performant micro/macroservices. 

Prototypes you could send to production!  

## Why Make A Tesseract Powered Dataflow  

`Dataflows` have concurrency, serialization, monitoring, compression, and encryption all as first class citizens. This paradigm allows developers to just focus on the important stuff - getting work done. Dataflows pay attention to the extra dimensions so you don't have to!

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
* Has `Json` (3 flavors) and `MessagePack` serialization providers.
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
You will find library usage examples in the `examples` folder. You also can find generic NetCore how-tos and tutorials located in there. The code quality of the entire library will improve over time. Codacy allows me to review code and openly share any pain points so submit a PR to help keep this an A rated library!

Check out each project for additional `README.md`. They will provide additional instructions/examples.

## Status

[![Codacy Badge](https://api.codacy.com/project/badge/Grade/9dbb20a30ada48caae4b92a83628f45e)](https://app.codacy.com/gh/houseofcat/Tesseract/dashboard)  

[![build](https://github.com/houseofcat/HouseofCat.Library/workflows/build/badge.svg)](https://github.com/houseofcat/Tesseract/actions/workflows/build.yml)

[![Gitter](https://badges.gitter.im/HoC-Tesseract/community.svg)](https://gitter.im/HoC-Tesseract/community?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge)

## HouseofCat.Algorithms
[![NuGet](https://img.shields.io/nuget/v/HouseofCat.Algorithms.svg)](https://www.nuget.org/packages/HouseofCat.Algorithms/)  
[![NuGet](https://img.shields.io/nuget/dt/HouseofCat.Algorithms.svg)](https://www.nuget.org/packages/HouseofCat.Algorithms/)  

A library that has a collection of algorithms as I have time to learn and play with them.  


## HouseofCat.Compression
[![NuGet](https://img.shields.io/nuget/v/HouseofCat.Compression.svg)](https://www.nuget.org/packages/HouseofCat.Compression/)  
[![NuGet](https://img.shields.io/nuget/dt/HouseofCat.Compression.svg)](https://www.nuget.org/packages/HouseofCat.Compression/)  

A library that has a collection of builtin NetCore compression providers.  


## HouseofCat.Compression.LZ4
[![NuGet](https://img.shields.io/nuget/v/HouseofCat.Compression.LZ4.svg)](https://www.nuget.org/packages/HouseofCat.Compression.LZ4/)  
[![NuGet](https://img.shields.io/nuget/dt/HouseofCat.Compression.LZ4.svg)](https://www.nuget.org/packages/HouseofCat.Compression.LZ4/)  

A library that focuses on implementing the LZ4 compression provider.  


## HouseofCat.Compression.Recyclable
[![NuGet](https://img.shields.io/nuget/v/HouseofCat.Compression.Recyclable.svg)](https://www.nuget.org/packages/HouseofCat.Compression.Recyclable/)  
[![NuGet](https://img.shields.io/nuget/dt/HouseofCat.Compression.Recyclable.svg)](https://www.nuget.org/packages/HouseofCat.Compression.Recyclable/)  

A library that has a collection of builtin NetCore compression providers that uses object pools and RecyclableMemoryStreams. 


## HouseofCat.Dapper
[![NuGet](https://img.shields.io/nuget/v/HouseofCat.Dapper.svg)](https://www.nuget.org/packages/HouseofCat.Dapper/)  
[![NuGet](https://img.shields.io/nuget/dt/HouseofCat.Dapper.svg)](https://www.nuget.org/packages/HouseofCat.Dapper/)  

A library that provides helper methods for integrating Dapper.


## HouseofCat.Data
[![NuGet](https://img.shields.io/nuget/v/HouseofCat.Data.svg)](https://www.nuget.org/packages/HouseofCat.Data/)  
[![NuGet](https://img.shields.io/nuget/dt/HouseofCat.Data.svg)](https://www.nuget.org/packages/HouseofCat.Data/)  

A library that provides the provides helper classes for data manipulation and transformation. 


## HouseofCat.Data.Database
[![NuGet](https://img.shields.io/nuget/v/HouseofCat.Data.Database.svg)](https://www.nuget.org/packages/HouseofCat.Data.Database/)  
[![NuGet](https://img.shields.io/nuget/dt/HouseofCat.Data.Database.svg)](https://www.nuget.org/packages/HouseofCat.Data.Database/)  

A library that provides the provides a Database Connection Factory and Sql Generation.

### Database Connection Factory Support
 * System.Data.SqlClient
 * Microsoft.Data.SqlClient
 * MySql.Data.MySqlClient
 * Npgsq
 * MySql.Data
 * Oracle
 * SQLite

### Sql Query Generation
 * SqlServer
 * MySql
 * PostgreSql
 * Firebird
 * Oracle
 * SQLite


## HouseofCat.Data.Parquet
[![NuGet](https://img.shields.io/nuget/v/HouseofCat.Data.Parquet.svg)](https://www.nuget.org/packages/HouseofCat.Data.Parquet/)  
[![NuGet](https://img.shields.io/nuget/dt/HouseofCat.Data.Parquet.svg)](https://www.nuget.org/packages/HouseofCat.Data.Parquet/)  

A library that helps extract databases to parquet file (IDataReader -> Snappy compressed Parquet files.)


## HouseofCat.Data.Recyclable
[![NuGet](https://img.shields.io/nuget/v/HouseofCat.Data.Recyclable.svg)](https://www.nuget.org/packages/HouseofCat.Data.Recyclable/)  
[![NuGet](https://img.shields.io/nuget/dt/HouseofCat.Data.Recyclable.svg)](https://www.nuget.org/packages/HouseofCat.Data.Recyclable/)  

A library that provides the provides helper classes for data manipulation and transformation that uses object pooling and RecyclableMemoryStreams.


## HouseofCat.Dataflows
[![NuGet](https://img.shields.io/nuget/v/HouseofCat.Dataflows.svg)](https://www.nuget.org/packages/HouseofCat.Dataflows/)  
[![NuGet](https://img.shields.io/nuget/dt/HouseofCat.Dataflows.svg)](https://www.nuget.org/packages/HouseofCat.Dataflows/)  

A library that provides the base magic Dataflows for Tesseract. 


## HouseofCat.Dataflows.Pipelines
[![NuGet](https://img.shields.io/nuget/v/HouseofCat.Dataflows.Pipelines.svg)](https://www.nuget.org/packages/HouseofCat.Dataflows.Pipelines/)  
[![NuGet](https://img.shields.io/nuget/dt/HouseofCat.Dataflows.Pipelines.svg)](https://www.nuget.org/packages/HouseofCat.Dataflows.Pipelines/)  

A library that provides the base magic Pipelines for Tesseract. 


## HouseofCat.Encryption
[![NuGet](https://img.shields.io/nuget/v/HouseofCat.Encryption.svg)](https://www.nuget.org/packages/HouseofCat.Encryption/)  
[![NuGet](https://img.shields.io/nuget/dt/HouseofCat.Encryption.svg)](https://www.nuget.org/packages/HouseofCat.Encryption/)  

A library that provides encryption contracts and a base AesGCM NetCore provider. 


## HouseofCat.Encryption.BouncyCastle
[![NuGet](https://img.shields.io/nuget/v/HouseofCat.Encryption.BouncyCastle.svg)](https://www.nuget.org/packages/HouseofCat.Encryption.BouncyCastle/)  
[![NuGet](https://img.shields.io/nuget/dt/HouseofCat.Encryption.BouncyCastle.svg)](https://www.nuget.org/packages/HouseofCat.Encryption.BouncyCastle/)  

A library that provides encryption from the BouncyCastle provider.  


## HouseofCat.Encryption.Recyclable
[![NuGet](https://img.shields.io/nuget/v/HouseofCat.Encryption.Recyclable.svg)](https://www.nuget.org/packages/HouseofCat.Encryption.Recyclable/)  
[![NuGet](https://img.shields.io/nuget/dt/HouseofCat.Encryption.Recyclable.svg)](https://www.nuget.org/packages/HouseofCat.Encryption.Recyclable/)  

A library that provides encryption from base AesGcm class in NetCore but with ArrayPools and RecyclableMemoryStreams.


## HouseofCat.Extensions
[![NuGet](https://img.shields.io/nuget/v/HouseofCat.Extensions.svg)](https://www.nuget.org/packages/HouseofCat.Extensions/)  
[![NuGet](https://img.shields.io/nuget/dt/HouseofCat.Extensions.svg)](https://www.nuget.org/packages/HouseofCat.Extensions/)  

A library that focuses on extending functionality to other objects.  


## HouseofCat.Framing
[![NuGet](https://img.shields.io/nuget/v/HouseofCat.Framing.svg)](https://www.nuget.org/packages/HouseofCat.Framing/)  
[![NuGet](https://img.shields.io/nuget/dt/HouseofCat.Framing.svg)](https://www.nuget.org/packages/HouseofCat.Framing/)  

A library that focuses on simplifying reading groups of byte[] (designated as frames).  


## HouseofCat.Gremlins
[![NuGet](https://img.shields.io/nuget/v/HouseofCat.Gremlins.svg)](https://www.nuget.org/packages/HouseofCat.Gremlins/)  
[![NuGet](https://img.shields.io/nuget/dt/HouseofCat.Gremlins.svg)](https://www.nuget.org/packages/HouseofCat.Gremlins/)  

A library that focuses on Chaos Engineering. Currently targets Windows OS.  


## HouseofCat.Gremlins.SqlServer
[![NuGet](https://img.shields.io/nuget/v/HouseofCat.Gremlins.SqlServer.svg)](https://www.nuget.org/packages/HouseofCat.Gremlins.SqlServer/)  
[![NuGet](https://img.shields.io/nuget/dt/HouseofCat.Gremlins.SqlServer.svg)](https://www.nuget.org/packages/HouseofCat.Gremlins.SqlServer/)  

A library that focuses on Chaos Engineering with SqlServer. Currently targets System.Data.SqlClient.  


## HouseofCat.Hashing
[![NuGet](https://img.shields.io/nuget/v/HouseofCat.Hashing.svg)](https://www.nuget.org/packages/HouseofCat.Hashing/)  
[![NuGet](https://img.shields.io/nuget/dt/HouseofCat.Hashing.svg)](https://www.nuget.org/packages/HouseofCat.Hashing/)  

A library that focuses on implementing hashing.  


## HouseofCat.Hashing.Argon
[![NuGet](https://img.shields.io/nuget/v/HouseofCat.Hashing.Argon.svg)](https://www.nuget.org/packages/HouseofCat.Hashing.Argon/)  
[![NuGet](https://img.shields.io/nuget/dt/HouseofCat.Hashing.Argon.svg)](https://www.nuget.org/packages/HouseofCat.Hashing.Argon/)  

A library that focuses on implementing Argon hashing.  


## HouseofCat.Logger
[![NuGet](https://img.shields.io/nuget/v/HouseofCat.Logger.svg)](https://www.nuget.org/packages/HouseofCat.Logger/)  
[![NuGet](https://img.shields.io/nuget/dt/HouseofCat.Logger.svg)](https://www.nuget.org/packages/HouseofCat.Logger/)  

A library that focuses on getting Microsoft.Extensions.LoggerFactory implemented adhoc globally.  


## HouseofCat.Network
[![NuGet](https://img.shields.io/nuget/v/HouseofCat.Network.svg)](https://www.nuget.org/packages/HouseofCat.Network/)  
[![NuGet](https://img.shields.io/nuget/dt/HouseofCat.Network.svg)](https://www.nuget.org/packages/HouseofCat.Network/)  

A library that focuses on making it easier to deal with systems networking.  


## HouseofCat.RabbitMQ
[![NuGet](https://img.shields.io/nuget/v/HouseofCat.RabbitMQ.svg)](https://www.nuget.org/packages/HouseofCat.RabbitMQ/)  
[![NuGet](https://img.shields.io/nuget/dt/HouseofCat.RabbitMQ.svg)](https://www.nuget.org/packages/HouseofCat.RabbitMQ/)  

A library that focuses on RabbitMQ connection and channel management to create fault tolerant Publishers and Consumers.  

Formerly found at [RabbitMQ.Core](https://github.com/houseofcat/RabbitMQ.Core/tree/master/CookedRabbit.Core)  
[![NuGet](https://img.shields.io/nuget/dt/CookedRabbit.Core.svg)](https://www.nuget.org/packages/CookedRabbit.Core/)    
[![NuGet](https://img.shields.io/nuget/v/CookedRabbit.Core.svg)](https://www.nuget.org/packages/CookedRabbit.Core/)   


# Deprecated - Using Official Pivotal/VMWare client again.
## HouseofCat.RabbitMQ.Client
[![NuGet](https://img.shields.io/nuget/v/HouseofCat.RabbitMQ.Client.svg)](https://www.nuget.org/packages/HouseofCat.RabbitMQ.Client/)  
[![NuGet](https://img.shields.io/nuget/dt/HouseofCat.RabbitMQ.Client.svg)](https://www.nuget.org/packages/HouseofCat.RabbitMQ.Client/)  

A library that focuses on cloning the official Pivotal/VMWare RabbitMQ DotNetClient but ported to pure NetCore 3.1/5.x with small code enhancements.  

Formerly found at [RabbitMQ.Core](https://github.com/houseofcat/RabbitMQ.Core)  
[![NuGet](https://img.shields.io/nuget/dt/RabbitMQ.Core.Client.svg)](https://www.nuget.org/packages/RabbitMQ.Core.Client/)   
[![NuGet](https://img.shields.io/nuget/v/RabbitMQ.Core.Client.svg)](https://www.nuget.org/packages/RabbitMQ.Core.Client/)  


## HouseofCat.RabbitMQ.Dataflows
[![NuGet](https://img.shields.io/nuget/v/HouseofCat.RabbitMQ.Dataflows.svg)](https://www.nuget.org/packages/HouseofCat.RabbitMQ.Dataflows/)  
[![NuGet](https://img.shields.io/nuget/dt/HouseofCat.RabbitMQ.Dataflows.svg)](https://www.nuget.org/packages/HouseofCat.RabbitMQ.Dataflows/)  

A library that extends HouseofCat.RabbitMQ functionality by providing epic TPL Dataflow usage for Tesseract.  


## HouseofCat.RabbitMQ.Mailkit
[![NuGet](https://img.shields.io/nuget/v/HouseofCat.RabbitMQ.Services.Mailkit.svg)](https://www.nuget.org/packages/HouseofCat.RabbitMQ.Services.Mailkit/)  
[![NuGet](https://img.shields.io/nuget/dt/HouseofCat.RabbitMQ.Services.Mailkit.svg)](https://www.nuget.org/packages/HouseofCat.RabbitMQ.Services.Mailkit/)  

A library that extends HouseofCat.RabbitMQ.Services to simplify using Mailkit (Email) with the HouseofCat.RabbitMQ library. 


## HouseofCat.RabbitMQ.Pipelines
[![NuGet](https://img.shields.io/nuget/v/HouseofCat.RabbitMQ.Pipelines.svg)](https://www.nuget.org/packages/HouseofCat.RabbitMQ.Pipelines/)  
[![NuGet](https://img.shields.io/nuget/dt/HouseofCat.RabbitMQ.Pipelines.svg)](https://www.nuget.org/packages/HouseofCat.RabbitMQ.Pipelines/)  

A library that extends HouseofCat.RabbitMQ functionality by providing simplified TPL Dataflow usage called Pipelines.  


## HouseofCat.RabbitMQ.Services
[![NuGet](https://img.shields.io/nuget/v/HouseofCat.RabbitMQ.Services.svg)](https://www.nuget.org/packages/HouseofCat.RabbitMQ.Services/)  
[![NuGet](https://img.shields.io/nuget/dt/HouseofCat.RabbitMQ.Services.svg)](https://www.nuget.org/packages/HouseofCat.RabbitMQ.Services/)  

A library that extends HouseofCat.RabbitMQ to simplify using the HouseofCat.RabbitMQ library.   


## HouseofCat.RabbitMQ.Twilio
[![NuGet](https://img.shields.io/nuget/v/HouseofCat.RabbitMQ.Twilio.svg)](https://www.nuget.org/packages/HouseofCat.RabbitMQ.Twilio/)  
[![NuGet](https://img.shields.io/nuget/dt/HouseofCat.RabbitMQ.Twilio.svg)](https://www.nuget.org/packages/HouseofCat.RabbitMQ.Twilio/)  

A library that extends HouseofCat.RabbitMQ.Services to simplify using Twilio (SMS/TextMessages) with the HouseofCat.RabbitMQ library.  


## HouseofCat.RabbitMQ.WorkState
[![NuGet](https://img.shields.io/nuget/v/HouseofCat.RabbitMQ.WorkState.svg)](https://www.nuget.org/packages/HouseofCat.RabbitMQ.WorkState/)  
[![NuGet](https://img.shields.io/nuget/dt/HouseofCat.RabbitMQ.WorkState.svg)](https://www.nuget.org/packages/HouseofCat.RabbitMQ.WorkState/)  

A library that creates the shared WorkState classes for HouseofCat.RabbitMQ.


## HouseofCat.Recyclable
[![NuGet](https://img.shields.io/nuget/v/HouseofCat.Recyclable.svg)](https://www.nuget.org/packages/HouseofCat.Recyclable/)  
[![NuGet](https://img.shields.io/nuget/dt/HouseofCat.Recyclable.svg)](https://www.nuget.org/packages/HouseofCat.Recyclable/)  

A library that focuses on Recyclable classes and pooling. 


## HouseofCat.Reflection
[![NuGet](https://img.shields.io/nuget/v/HouseofCat.Reflection.svg)](https://www.nuget.org/packages/HouseofCat.Reflection/)  
[![NuGet](https://img.shields.io/nuget/dt/HouseofCat.Reflection.svg)](https://www.nuget.org/packages/HouseofCat.Reflection/)  

A library that focuses on Reflection hackery.  


## HouseofCat.Serialization
[![NuGet](https://img.shields.io/nuget/v/HouseofCat.Serialization.svg)](https://www.nuget.org/packages/HouseofCat.Serialization/)  
[![NuGet](https://img.shields.io/nuget/dt/HouseofCat.Serialization.svg)](https://www.nuget.org/packages/HouseofCat.Serialization/)  

A library that focuses on making it easier to deal with Serialization.  


## HouseofCat.Serialization.Json.Newtonsoft
[![NuGet](https://img.shields.io/nuget/v/HouseofCat.Serialization.Json.Newtonsoft.svg)](https://www.nuget.org/packages/HouseofCat.Serialization.Json.Newtonsoft/)  
[![NuGet](https://img.shields.io/nuget/dt/HouseofCat.Serialization.Json.Newtonsoft.svg)](https://www.nuget.org/packages/HouseofCat.Serialization.Json.Newtonsoft/)  

A library that focuses on making it easier to deal with Newtonsoft Json Serialization.  


## HouseofCat.Serialization.Json.Utf8Json
[![NuGet](https://img.shields.io/nuget/v/HouseofCat.Serialization.Json.Utf8Json.svg)](https://www.nuget.org/packages/HouseofCat.Serialization.Json.Utf8Json/)  
[![NuGet](https://img.shields.io/nuget/dt/HouseofCat.Serialization.Json.Utf8Json.svg)](https://www.nuget.org/packages/HouseofCat.Serialization.Json.Utf8Json/)  

A library that focuses on making it easier to deal with Utf8Json Json Serialization.  


## HouseofCat.Serialization.MessagePack
[![NuGet](https://img.shields.io/nuget/v/HouseofCat.Serialization.MessagePack.svg)](https://www.nuget.org/packages/HouseofCat.Serialization.MessagePack/)  
[![NuGet](https://img.shields.io/nuget/dt/HouseofCat.Serialization.MessagePack.svg)](https://www.nuget.org/packages/HouseofCat.Serialization.MessagePack/)  

A library that focuses on making it easier to deal with MessagePack Serialization.  


## HouseofCat.Serilog
[![NuGet](https://img.shields.io/nuget/v/HouseofCat.Serilog.svg)](https://www.nuget.org/packages/HouseofCat.Serilog/)  
[![NuGet](https://img.shields.io/nuget/dt/HouseofCat.Serilog.svg)](https://www.nuget.org/packages/HouseofCat.Serilog/)  

A library that focuses on extending IHost functionality to quickly setup Serilog.  


## HouseofCat.Sockets
[![NuGet](https://img.shields.io/nuget/v/HouseofCat.Sockets.svg)](https://www.nuget.org/packages/HouseofCat.Sockets/)  
[![NuGet](https://img.shields.io/nuget/dt/HouseofCat.Sockets.svg)](https://www.nuget.org/packages/HouseofCat.Sockets/)  

A library that focuses on making it easier to deal with socket communication.  


## HouseofCat.Sockets.Utf8Json
[![NuGet](https://img.shields.io/nuget/v/HouseofCat.Sockets.Utf8Json.svg)](https://www.nuget.org/packages/HouseofCat.Sockets.Utf8Json/)  
[![NuGet](https://img.shields.io/nuget/dt/HouseofCat.Sockets.Utf8Json.svg)](https://www.nuget.org/packages/HouseofCat.Sockets.Utf8Json/)  

A library that focuses on making it easier to deal with sockets communication with Utf8Json.  


## HouseofCat.Utilities
[![NuGet](https://img.shields.io/nuget/v/HouseofCat.Utilities.svg)](https://www.nuget.org/packages/HouseofCat.Utilities/)  
[![NuGet](https://img.shields.io/nuget/dt/HouseofCat.Utilities.svg)](https://www.nuget.org/packages/HouseofCat.Utilities/)  

A library that focuses on general purpose utilities and functions that simplify the coding experience.  


## HouseofCat.Windows.Keyboard
[![NuGet](https://img.shields.io/nuget/v/HouseofCat.Windows.Keyboard.svg)](https://www.nuget.org/packages/HouseofCat.Windows.Keyboard/)  
[![NuGet](https://img.shields.io/nuget/dt/HouseofCat.Windows.Keyboard.svg)](https://www.nuget.org/packages/HouseofCat.Windows.Keyboard/)  

A library that focuses on interacting, filtering, and/or replaying user inputs on Windows, specifically Keyboard.  


## HouseofCat.Windows.NativeMethods
[![NuGet](https://img.shields.io/nuget/v/HouseofCat.Windows.NativeMethods.svg)](https://www.nuget.org/packages/HouseofCat.Windows.NativeMethods/)  
[![NuGet](https://img.shields.io/nuget/dt/HouseofCat.Windows.NativeMethods.svg)](https://www.nuget.org/packages/HouseofCat.Windows.NativeMethods/)  

A library that focuses on consolidating Windows NativeMethods calls used by my libaries.  


## HouseofCat.Windows.Threading
[![NuGet](https://img.shields.io/nuget/v/HouseofCat.Windows.Threading.svg)](https://www.nuget.org/packages/HouseofCat.Windows.Threading/)  
[![NuGet](https://img.shields.io/nuget/dt/HouseofCat.Windows.Threading.svg)](https://www.nuget.org/packages/HouseofCat.Windows.Threading/)  

A library that focuses on simplifying affinity and thread management.  


## HouseofCat.Windows.WMI
[![NuGet](https://img.shields.io/nuget/v/HouseofCat.Windows.WMI.svg)](https://www.nuget.org/packages/HouseofCat.Windows.WMI/)  
[![NuGet](https://img.shields.io/nuget/dt/HouseofCat.Windows.WMI.svg)](https://www.nuget.org/packages/HouseofCat.Windows.WMI/)  

A library that focuses on performing System.Management (Windows.Compatibility.Pack) WMI Queries.  


# [HouseofCat.io](https://houseofcat.io)
