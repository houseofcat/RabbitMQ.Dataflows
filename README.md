# Tesseract - The Library

A library of NetCore tools to help quickly get rapid and well performant development going in micro/macroservices.  

Prototypes you could send to production!  

## Why Make A Tesseract Powered Workflow  
Here are some features ready you can use today.

*Note: These are all available today, out of the box with RabbitMQ.  
The goal is to continue enabling features with other Queue-based providers. NoSql/DocumentDB data crawlers are also future slated.   

### RabbitMQ Queueing Allows 
* Async Processing    
* Retriability  
* Chaos Engineering  
* Connection/Channel Durability provided by HouseofCat.RabbitMQ.  

### Built-Ins
* Supports ILogger&lt;T&gt;  
* Concurrency/Parallelism - baked in from the ground up.  
* Predetermine WorkState/WorkObject simplifies development and integration.  
* Has built in Json (3 flavors) and MessagePack serialization providers.
* Allow transparent encryption/decryption steps.  
* Allow compression/decompression steps to reduce trip time over the wire.  
* Async Error Handling with Predicate Triggers and actionable callback.  

### Interchangeable Parts  
* Allows you to replace serialization provider with HouseofCat Provider wrappers.  
* Allows you to replace encryption provider with HouseofCat Provider wrappers.  
* Allows you to replace compression provider with HouseofCat Provider wrappers.   
* All using Interfaces to allow you to implement your own flavor and providers.  

### Business Logic
* All steps process in the order provided allowing you to still control order of execution.  
* All automatically subscribed to Async Error handling by WorkState.IsFaulted flag.  

### Testing
* All built-in steps will have integration tests removing concerns from end-user developer.  
* Future case will include much more complex abstract UnitTesting as time allows.  
* The developer should only need to unit test their functional business code.  

## Non-Technical Benefits

The benefits of a dataflow pattern extend beyond fancy Tensorflows or high throughput GCP Dataflow for mass computation. At the service level, it helps mentally organize your code into manageable blocks. You can still write monolithic functions, but you would be hamstringing yourself and scarificing concurrency and parallelism. By designing code into small functional steps, you always write better, cleaner, code. That same code then is easier to UnitTest and less prone to bugs. The orchestration of the function calls are implicit, working out deserialization or post processing/egress is baked in and out of sight out of mind. Concurrency, parallelism, all baked into a "it just works" package.

Lastly, after everything is said and done, all your business code is re-usable. Should you decide to abandon this workflow (:worried:) for a different mechanim, engine, or what not, all of your code will happily port to whatever other project / flow you are working with and so will all your testing. All an all, it seems very much like a win win.

## Help
You will find library usage examples in the `examples` folder. You also can find generic NetCore how-tos and tutorials located in there. The code quality of the entire library will improve over time. Codacy allows me to review code and openly share any pain points so submit a PR to help out keeping this an A rated library!

Check out each project for a `README.md` to see if there are additional instructions/examples.

## Status

[![Codacy Badge](https://api.codacy.com/project/badge/Grade/9dbb20a30ada48caae4b92a83628f45e)](https://app.codacy.com/manual/cat_3/Library?utm_source=github.com&utm_medium=referral&utm_content=houseofcat/Library&utm_campaign=Badge_Grade_Dashboard)  

![master-build](https://github.com/houseofcat/HouseofCat.Library/workflows/master-build/badge.svg)  

[![Gitter](https://badges.gitter.im/Library/community.svg)](https://gitter.im/Library/community?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge)

## HouseofCat.Algorithms
[![NuGet](https://img.shields.io/nuget/v/HouseofCat.Algorithms.svg)](https://www.nuget.org/packages/HouseofCat.Algorithms/)  
[![NuGet](https://img.shields.io/nuget/dt/HouseofCat.Algorithms.svg)](https://www.nuget.org/packages/HouseofCat.Algorithms/)  

A library that has a collection of algorithms as I have time to learn and play with them.  


## HouseofCat.Compression
[![NuGet](https://img.shields.io/nuget/v/HouseofCat.Compression.svg)](https://www.nuget.org/packages/HouseofCat.Compression/)  
[![NuGet](https://img.shields.io/nuget/dt/HouseofCat.Compression.svg)](https://www.nuget.org/packages/HouseofCat.Compression/)  

A library that has a collection of builtin and 3rd party copmression providers.  


## HouseofCat.Compression.LZ4
[![NuGet](https://img.shields.io/nuget/v/HouseofCat.Compression.LZ4.svg)](https://www.nuget.org/packages/HouseofCat.Compression.LZ4/)  
[![NuGet](https://img.shields.io/nuget/dt/HouseofCat.Compression.LZ4.svg)](https://www.nuget.org/packages/HouseofCat.Compression.LZ4/)  

A library that focuses on implementing the LZ4 compression provider.  


## HouseofCat.Dapper
[![NuGet](https://img.shields.io/nuget/v/HouseofCat.Dapper.svg)](https://www.nuget.org/packages/HouseofCat.Dapper/)  
[![NuGet](https://img.shields.io/nuget/dt/HouseofCat.Dapper.svg)](https://www.nuget.org/packages/HouseofCat.Dapper/)  

A library that provides a standard for Dapper implementation.  


## HouseofCat.Dapper.LegacySqlServer
[![NuGet](https://img.shields.io/nuget/v/HouseofCat.Dapper.LegacySqlServer.svg)](https://www.nuget.org/packages/HouseofCat.Dapper.LegacySqlServer/)  
[![NuGet](https://img.shields.io/nuget/dt/HouseofCat.Dapper.LegacySqlServer.svg)](https://www.nuget.org/packages/HouseofCat.Dapper.LegacySqlServer/)  

A library that provides a standard System.Data.SqlClient implementation.  


## HouseofCat.Dapper.SqlServer
[![NuGet](https://img.shields.io/nuget/v/HouseofCat.Dapper.SqlServer.svg)](https://www.nuget.org/packages/HouseofCat.Dapper.SqlServer/)  
[![NuGet](https://img.shields.io/nuget/dt/HouseofCat.Dapper.SqlServer.svg)](https://www.nuget.org/packages/HouseofCat.Dapper.SqlServer/)  

A library that provides a standard Microsoft.Data.SqlClient implementation.  


## HouseofCat.Encryption
[![NuGet](https://img.shields.io/nuget/v/HouseofCat.Encryption.svg)](https://www.nuget.org/packages/HouseofCat.Encryption/)  
[![NuGet](https://img.shields.io/nuget/dt/HouseofCat.Encryption.svg)](https://www.nuget.org/packages/HouseofCat.Encryption/)  

A library that provides encryption contracts.  


## HouseofCat.Encryption.BouncyCastle
[![NuGet](https://img.shields.io/nuget/v/HouseofCat.Encryption.BouncyCastle.svg)](https://www.nuget.org/packages/HouseofCat.Encryption.BouncyCastle/)  
[![NuGet](https://img.shields.io/nuget/dt/HouseofCat.Encryption.BouncyCastle.svg)](https://www.nuget.org/packages/HouseofCat.Encryption.BouncyCastle/)  

A library that provides encryption from the BouncyCastle provider.  


## HouseofCat.Extensions
[![NuGet](https://img.shields.io/nuget/v/HouseofCat.Extensions.svg)](https://www.nuget.org/packages/HouseofCat.Extensions/)  
[![NuGet](https://img.shields.io/nuget/dt/HouseofCat.Extensions.svg)](https://www.nuget.org/packages/HouseofCat.Extensions/)  

A library that focuses on extending functionality to other objects.  


## HouseofCat.Extensions.Host.Serilog
[![NuGet](https://img.shields.io/nuget/v/HouseofCat.Extensions.Host.Serilog.svg)](https://www.nuget.org/packages/HouseofCat.Extensions.Host.Serilog/)  
[![NuGet](https://img.shields.io/nuget/dt/HouseofCat.Extensions.Host.Serilog.svg)](https://www.nuget.org/packages/HouseofCat.Extensions.Host.Serilog/)  

A library that focuses on extending IHost functionality to quickly setup Serilog.  


## HouseofCat.Extensions.Workflows
[![NuGet](https://img.shields.io/nuget/v/HouseofCat.Extensions.Workflows.svg)](https://www.nuget.org/packages/HouseofCat.Extensions.Workflows/)  
[![NuGet](https://img.shields.io/nuget/dt/HouseofCat.Extensions.Workflows.svg)](https://www.nuget.org/packages/HouseofCat.Extensions.Workflows/)  

A library that focuses on extending functionality to other workflow related objects.  


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


## HouseofCat.RabbitMQ.Client
[![NuGet](https://img.shields.io/nuget/v/HouseofCat.RabbitMQ.Client.svg)](https://www.nuget.org/packages/HouseofCat.RabbitMQ.Client/)  
[![NuGet](https://img.shields.io/nuget/dt/HouseofCat.RabbitMQ.Client.svg)](https://www.nuget.org/packages/HouseofCat.RabbitMQ.Client/)  

A library that focuses on cloning the official Pivotal/VMWare RabbitMQ DotNetClient but ported to pure NetCore 3.1/5.x with small code enhancements.  


## HouseofCat.RabbitMQ.Pipelines
[![NuGet](https://img.shields.io/nuget/v/HouseofCat.RabbitMQ.Pipelines.svg)](https://www.nuget.org/packages/HouseofCat.RabbitMQ.Pipelines/)  
[![NuGet](https://img.shields.io/nuget/dt/HouseofCat.RabbitMQ.Pipelines.svg)](https://www.nuget.org/packages/HouseofCat.RabbitMQ.Pipelines/)  

A library that extends HouseofCat.RabbitMQ functionality by providing simplified TPL Dataflow usage called Pipelines.  


## HouseofCat.RabbitMQ.Services
[![NuGet](https://img.shields.io/nuget/v/HouseofCat.RabbitMQ.Services.svg)](https://www.nuget.org/packages/HouseofCat.RabbitMQ.Services/)  
[![NuGet](https://img.shields.io/nuget/dt/HouseofCat.RabbitMQ.Services.svg)](https://www.nuget.org/packages/HouseofCat.RabbitMQ.Services/)  

A library that extends HouseofCat.RabbitMQ to simplify using the HouseofCat.RabbitMQ library.  


## HouseofCat.RabbitMQ.Services.Twilio
[![NuGet](https://img.shields.io/nuget/v/HouseofCat.RabbitMQ.Services.Twilio.svg)](https://www.nuget.org/packages/HouseofCat.RabbitMQ.Services.Twilio/)  
[![NuGet](https://img.shields.io/nuget/dt/HouseofCat.RabbitMQ.Services.Twilio.svg)](https://www.nuget.org/packages/HouseofCat.RabbitMQ.Services.Twilio/)  

A library that extends HouseofCat.RabbitMQ.Services to simplify using Twilio with the HouseofCat.RabbitMQ library.  


## HouseofCat.RabbitMQ.Workflows
[![NuGet](https://img.shields.io/nuget/v/HouseofCat.RabbitMQ.Workflows.svg)](https://www.nuget.org/packages/HouseofCat.RabbitMQ.Workflows/)  
[![NuGet](https://img.shields.io/nuget/dt/HouseofCat.RabbitMQ.Workflows.svg)](https://www.nuget.org/packages/HouseofCat.RabbitMQ.Workflows/)  

A library that extends HouseofCat.RabbitMQ functionality by providing robust Workflow support.  


## HouseofCat.Reflection
[![NuGet](https://img.shields.io/nuget/v/HouseofCat.Reflection.svg)](https://www.nuget.org/packages/HouseofCat.Reflection/)  
[![NuGet](https://img.shields.io/nuget/dt/HouseofCat.Reflection.svg)](https://www.nuget.org/packages/HouseofCat.Reflection/)  

A library that focuses on Reflection hackery.  


## HouseofCat.Serilization
[![NuGet](https://img.shields.io/nuget/v/HouseofCat.Serialization.svg)](https://www.nuget.org/packages/HouseofCat.Serialization/)  
[![NuGet](https://img.shields.io/nuget/dt/HouseofCat.Serialization.svg)](https://www.nuget.org/packages/HouseofCat.Serialization/)  

A library that focuses on making it easier to deal with Serialization.  


## HouseofCat.Serilization.Json.Newtonsoft
[![NuGet](https://img.shields.io/nuget/v/HouseofCat.Serialization.Json.Newtonsoft.svg)](https://www.nuget.org/packages/HouseofCat.Serialization.Json.Newtonsoft/)  
[![NuGet](https://img.shields.io/nuget/dt/HouseofCat.Serialization.Json.Newtonsoft.svg)](https://www.nuget.org/packages/HouseofCat.Serialization.Json.Newtonsoft/)  

A library that focuses on making it easier to deal with Newtonsoft Json Serialization.  

## HouseofCat.Serilization.Json.Utf8Json
[![NuGet](https://img.shields.io/nuget/v/HouseofCat.Serialization.Json.Utf8Json.svg)](https://www.nuget.org/packages/HouseofCat.Serialization.Json.Utf8Json/)  
[![NuGet](https://img.shields.io/nuget/dt/HouseofCat.Serialization.Json.Utf8Json.svg)](https://www.nuget.org/packages/HouseofCat.Serialization.Json.Utf8Json/)  

A library that focuses on making it easier to deal with Utf8Json Json Serialization.  

## HouseofCat.Serilization.MessagePack
[![NuGet](https://img.shields.io/nuget/v/HouseofCat.Serialization.MessagePack.svg)](https://www.nuget.org/packages/HouseofCat.Serialization.MessagePack/)  
[![NuGet](https://img.shields.io/nuget/dt/HouseofCat.Serialization.MessagePack.svg)](https://www.nuget.org/packages/HouseofCat.Serialization.MessagePack/)  

A library that focuses on making it easier to deal with MessagePack Serialization.  


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


## HouseofCat.Workflows
[![NuGet](https://img.shields.io/nuget/v/HouseofCat.Workflows.svg)](https://www.nuget.org/packages/HouseofCat.Workflows/)  
[![NuGet](https://img.shields.io/nuget/dt/HouseofCat.Workflows.svg)](https://www.nuget.org/packages/HouseofCat.Workflows/)  

A library that focuses on Task Parallel Library and rapid Function execution.  


## HouseofCat.Workflows
[![NuGet](https://img.shields.io/nuget/v/HouseofCat.Workflows.svg)](https://www.nuget.org/packages/HouseofCat.Workflows/)  
[![NuGet](https://img.shields.io/nuget/dt/HouseofCat.Workflows.svg)](https://www.nuget.org/packages/HouseofCat.Workflows/)  

A library that focuses on Task Parallel Library and rapid Function execution.  


## HouseofCat.Workflows.Pipelines
[![NuGet](https://img.shields.io/nuget/v/HouseofCat.Workflows.Pipelines.svg)](https://www.nuget.org/packages/HouseofCat.Workflows.Pipelines/)  
[![NuGet](https://img.shields.io/nuget/dt/HouseofCat.Workflows.Pipelines.svg)](https://www.nuget.org/packages/HouseofCat.Workflows.Pipelines/)  

A library that focuses on a narrow TPL implementation of mine called Pipelines.  

# [HouseofCat.io](https://houseofcat.io)
