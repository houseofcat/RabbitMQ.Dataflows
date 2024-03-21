# RabbitMQ.Dataflows
## Serialization

The RabbitMQ.Dataflows has Serialization, Compression, and Encryption providers to help quickly
transform your data into `byte[]` for publishing or converting your `byte[]` back into your
objects during consumption.

It really helps to have `RabbitOptions` already setup and ready to go.

I will use this as a file named `SampleRabbitOptions.json`
```json
{
  "FactoryOptions": {
    "Uri": "amqp://guest:guest@localhost:5672/",
    "MaxChannelsPerConnection": 2000,
    "HeartbeatInterval": 6,
    "AutoRecovery": true,
    "TopologyRecovery": true,
    "NetRecoveryTimeout": 5
  },
  "PoolOptions": {
    "ServiceName": "HoC.RabbitMQ",
    "MaxConnections": 2,
    "MaxChannels": 5,
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
using HouseofCat.Serialization;

var rabbitOptions = JsonFileReader.ReadFileAsync<RabbitOptions>("SampleRabbitOptions.json");
var channelPool = new ChannelPool(rabbitOptions);
var jsonProvider = new JsonProvider();

var channelHost = await channelPool.GetChannelAsync();
var channel = channelHost.GetChannel();

var properties = channel.CreateBasicProperties();
properties.DeliveryMode = 2;

var data = jsonProvider.Serialize(input);

var error = false;
try
{
    channel.BasicPublish("MyExchange", "MyRoutingKey", properties, data);
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
using HouseofCat.Serialization;
using HouseofCat.Compression;

var rabbitOptions = JsonFileReader.ReadFileAsync<RabbitOptions>("SampleRabbitOptions.json");
var channelPool = new ChannelPool(rabbitOptions);
var jsonProvider = new JsonProvider();
var gzipProvider = new GzipProvider();

var channelHost = await channelPool.GetChannelAsync();
var channel = channelHost.GetChannel();

var properties = channel.CreateBasicProperties();
properties.DeliveryMode = 2;

var dataAsJson = jsonProvider.Serialize(input);
var compressedJson = gzipProvider.Compress(dataAsJson);

var error = false;
try
{
    channel.BasicPublish("MyExchange", "MyRoutingKey", properties, compressedJson);
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
using HouseofCat.Serialization;
using HouseofCat.Compression;

var rabbitOptions = JsonFileReader.ReadFileAsync<RabbitOptions>("SampleRabbitOptions.json");
var channelPool = new ChannelPool(rabbitOptions);
var jsonProvider = new JsonProvider();
var gzipProvider = new GzipProvider();
var argonProvider = new ArgonHashingProvider();

var aes256Key = argonProvider.GetHashKey("MySuperSecretPassword", "MySaltySaltSalt", size: 32);
var aesProvider = new AesGcmEncryptionProvider(aes256Key);

var channelHost = await channelPool.GetChannelAsync();
var channel = channelHost.GetChannel();

var properties = channel.CreateBasicProperties();
properties.DeliveryMode = 2;

var dataAsJson = jsonProvider.Serialize(input);
var compressedJson = gzipProvider.Compress(dataAsJson);
var encryptedCompressedJson = aesProvider.Encrypt(encryptedCompressedJson);
var error = false;

try
{
    channel.BasicPublish("MyExchange", "MyRoutingKey", properties, compressedJson);
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
var hashingProvider = new ArgonHashingProvider();
var hashKey = hashingProvider.GetHashKey(Passphrase, Salt, 32);

var dataTransformer = new DataTransformer(
    new JsonProvider(),
    new AesGcmEncryptionProvider(hashKey),
    new GzipProvider());

var data = dataTransformer.Serialize(input)
```

### DataTransfomer Alternative #2
You can also use the `DataTransformer` to simplify the process of serialization, compression, and/or encryption.
This constructor assumes you want to use Json, Gzip, and AesGcm 256 bit encryption.
```csharp
var hashingProvider = new ArgonHashingProvider();
var hashKey = hashingProvider.GetHashKey(Passphrase, Salt, 32);

// Json, Gzip, and AesGcm 256 bit encryption.
_middleware = new DataTransformer(Passphrase, Salt, 32);

// Json and Gzip only.
_middleware = new DataTransformer();

_serializedData = _middleware.Serialize(input)
```

### Deserializing
This is kind of simple. You want to deserialize, decompress, and decrypt your `byte[]` back into your object
in the opposite manner you performed the serialization, compression, and encryption.
```csharp
var dataAsJson = jsonProvider.Serialize(input);
var compressedJson = gzipProvider.Compress(dataAsJson);
var encryptedCompressedJson = aesProvider.Encrypt(encryptedCompressedJson);

...

var decryptedCompressedJson = aesProvider.Decrypt(encryptedCompressedJson);
var decompressedJson = gzipProvider.Decompress(decryptedCompressedJson);
var myObject = _jsonProvider.Deserialize<TOut>(decompressedJson);
```

### Deserializing With DataTransformer
```csharp
// Json, Gzip, and AesGcm 256 bit encryption.
var dataTransformer = new DataTransformer(Passphrase, Salt, 32);

var data = dataTransformer.Serialize(input)

var myObject = dataTransformer.Deserialize<TOut>(data);
```