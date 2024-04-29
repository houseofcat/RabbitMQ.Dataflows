# RabbitMQ.Dataflows
## Serialization

The RabbitMQ.Dataflows has Serialization, Compression, and Encryption providers to help quickly
transform your data into `byte[]` for publishing or converting your `byte[]` back into your
objects during consumption.

It really helps to have `RabbitOptions` already setup and ready to go.

I will use this as a file named `SampleRabbitOptions.json`
```json
{
  "PoolOptions": {
    "Uri": "amqp://guest:guest@localhost:5672/",
    "MaxChannelsPerConnection": 2000,
    "HeartbeatInterval": 6,
    "AutoRecovery": true,
    "TopologyRecovery": true,
    "NetRecoveryTimeout": 5
    "ServiceName": "HoC.RabbitMQ",
    "Connections": 2,
    "Channels": 5,
    "TansientChannelStartRange": 10000
  }
}
```

I will use a helper method to load the `RabbitOptions` from the file.

### Serialize (Json or MessagePack)
You can configure your own, or use one of the provided `IserializationProviders`
to help simplify code. Here we use the `JsonProvider` to take `<TIn> input` and
serialize the object to `byte[]`.
```csharp
using HouseofCat.RabbitMQ;
using HouseofCat.RabbitMQ.Pools;
using HouseofCat.Serialization;
using HouseofCat.Utilities;

var rabbitOptions = await JsonFileReader.ReadFileAsync<RabbitOptions>("SampleRabbitOptions.json");
var channelPool = new ChannelPool(rabbitOptions);
var jsonProvider = new JsonProvider();

var channelHost = await channelPool.GetChannelAsync();

var properties = channelHost.Channel.CreateBasicProperties();
properties.DeliveryMode = 2;

var myClass = new { Name = "MyName", Value = 42 };
var body = jsonProvider.Serialize(myClass);

var error = false;
try
{
    channelHost.Channel.BasicPublish("MyExchange", "MyRoutingKey", false, properties, body);
}
catch (Exception ex)
{
    // Log your exception.
    error = true;
}

await channelPool.ReturnChannelAsync(channelHost, error);
```

### Serialize And Compress
An example showing JsonSerialization and GzipCompression.
```csharp
using HouseofCat.Compression;
using HouseofCat.RabbitMQ;
using HouseofCat.RabbitMQ.Pools;
using HouseofCat.Serialization;
using HouseofCat.Utilities;

var rabbitOptions = await JsonFileReader.ReadFileAsync<RabbitOptions>("SampleRabbitOptions.json");
var channelPool = new ChannelPool(rabbitOptions);
var jsonProvider = new JsonProvider();
var gzipProvider = new GzipProvider();

var channelHost = await channelPool.GetChannelAsync();

var properties = channelHost.Channel.CreateBasicProperties();
properties.DeliveryMode = 2;

var myClass = new { Name = "MyName", Value = 42 };
var myClassAsJson = jsonProvider.Serialize(myClass);
var body = gzipProvider.Compress(myClassAsJson);

var error = false;
try
{
    channelHost.Channel.BasicPublish("MyExchange", "MyRoutingKey", false, properties, body);
}
catch (Exception ex)
{
    // Log your exception.
    error = true;
}

await channelPool.ReturnChannelAsync(channelHost, error);
```

### Serialize, Compression, And Encrypt
An example showing JsonSerialization, Gzip compression, and AesGcm 256 bit encryption.
```csharp
using HouseofCat.Compression;
using HouseofCat.Encryption;
using HouseofCat.Hashing;
using HouseofCat.RabbitMQ;
using HouseofCat.RabbitMQ.Pools;
using HouseofCat.Serialization;
using HouseofCat.Utilities;

var rabbitOptions = await JsonFileReader.ReadFileAsync<RabbitOptions>("SampleRabbitOptions.json");
var channelPool = new ChannelPool(rabbitOptions);
var jsonProvider = new JsonProvider();
var gzipProvider = new GzipProvider();
var argonProvider = new ArgonHashingProvider();

var aes256Key = argonProvider.GetHashKey("MySuperSecretPassword", "MySaltySaltSalt", size: 32);
var aesProvider = new AesGcmEncryptionProvider(aes256Key);

var channelHost = await channelPool.GetChannelAsync();
var properties = channelHost.Channel.CreateBasicProperties();
properties.DeliveryMode = 2;

var myClass = new { Name = "MyName", Value = 42 };
var myClassAsJson = jsonProvider.Serialize(myClass);
var compressedJson = gzipProvider.Compress(myClassAsJson);
var body = aesProvider.Encrypt(compressedJson);
var error = false;

try
{
    channelHost.Channel.BasicPublish("MyExchange", "MyRoutingKey", false, properties, compressedJson);
}
catch (Exception ex)
{
    // Log your exception.
    error = true;
}

await channelPool.ReturnChannelAsync(channelHost, error);
```

### DataTransfomer Alternative #1
You can also use the `DataTransformer` to simplify the process of serialization, compression, and/or encryption.
```csharp
using HouseofCat.Compression;
using HouseofCat.Data;
using HouseofCat.Encryption;
using HouseofCat.Hashing;
using HouseofCat.Serialization;

var hashingProvider = new ArgonHashingProvider();
var hashKey = hashingProvider.GetHashKey("PasswordMcPassword", "SaltySaltSalt", 32);

var dataTransformer = new DataTransformer(
    new JsonProvider(),
    new AesGcmEncryptionProvider(hashKey),
    new GzipProvider());

var myClass = new { Name = "MyName", Value = 42 };
var data = dataTransformer.Serialize(myClass);
```

### DataTransfomer Alternative #2
You can also use the `DataTransformer` to simplify the process of serialization, compression, and/or encryption.
This constructor assumes you want to use Json, Gzip, and AesGcm 256 bit encryption.
```csharp
using HouseofCat.Data;

// Json, Gzip, and AesGcm 256 bit encryption.
var transformer = new DataTransformer("PasswordMcPassword", "SaltySaltSalt", 32);

// Json and Gzip only.
// var transformer = new DataTransformer();

var myClass = new { Name = "MyName", Value = 42 };
var body = transformer.Serialize(myClass);
```

### Deserializing
This is kind of simple. You want to deserialize, decompress, and decrypt your `byte[]` back into your object
in the opposite manner you performed the serialization, compression, and encryption.
```csharp
using HouseofCat.Compression;
using HouseofCat.Encryption;
using HouseofCat.Hashing;
using HouseofCat.Serialization;

var jsonProvider = new JsonProvider();
var gzipProvider = new GzipProvider();
var argonProvider = new ArgonHashingProvider();

var aes256Key = argonProvider.GetHashKey("MySuperSecretPassword", "MySaltySaltSalt", size: 32);
var aesProvider = new AesGcmEncryptionProvider(aes256Key);

// Making encrypted compressed json.
var myClass = new MyClass { Name = "MyName", Value = 42 };
var dataAsJson = jsonProvider.Serialize(myClass);
var compressedJson = gzipProvider.Compress(dataAsJson);
var encryptedCompressedJson = aesProvider.Encrypt(compressedJson);


// Decrypting and decompressing json into your object.
var decryptedCompressedJson = aesProvider.Decrypt(encryptedCompressedJson);
var decompressedJson = gzipProvider.Decompress(decryptedCompressedJson);
var myObject = jsonProvider.Deserialize<MyClass>(decompressedJson);
```

### Deserializing With DataTransformer
```csharp
using HouseofCat.Data;

var transformer = new DataTransformer("PasswordMcPassword", "SaltySaltSalt", 32);

// Make encrypted and compressed json from object.
var myClass = new MyClass { Name = "MyName", Value = 42 };
var body = transformer.Serialize(myClass);

// Get object from encrypted compressed json.
var myObject = transformer.Deserialize<MyClass>(body);
```