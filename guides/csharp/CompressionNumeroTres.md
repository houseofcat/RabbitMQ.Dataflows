### The Challenge
#### How can I write an EVEN better implementation of those Gzip compression examples?
        
##### Additional Asks/Constraints 
1) Continue to use `Stream` as a core mechanic.
2) Go nuts

#### Intro
Let's continue our work from the last [guide](https://houseofcat.io/guides/csharp/net/compressionpartdeux).

We will be taking those well-done examples and bring in something used in high performance systems.

The `RecyclableMemoryStream`.

Let's go ahead and acquire package
```csharp
Microsoft.IO.RecyclableMemoryStream
```


```csharp
using Microsoft.IO;
```

Let's begin with a global static wrapper I used around RecyclableMemoryStreamManager. This isn't super necessary for you to do but
since I have a copy of one lying around, let's snag it.

```csharp
using Microsoft.IO;
using System;
using System.IO;

public static class RecyclableManager
{
    private static RecyclableMemoryStreamManager _manager = new RecyclableMemoryStreamManager();

    /// <summary>
    /// ConfigureStaticManager completely rebuilds the <c>RecyclableMemoryStreamManager</c> so try to call it only once, and on startup.
    /// </summary>
    /// <param name="blockSize"></param>
    /// <param name="largeBufferMultiple"></param>
    /// <param name="maximumBufferSize"></param>
    /// <param name="useExponentialLargeBuffer"></param>
    /// <param name="maximumSmallPoolFreeBytes"></param>
    /// <param name="maximumLargePoolFreeBytes"></param>
    public static void ConfigureNewStaticManager(
        int blockSize,
        int largeBufferMultiple,
        int maximumBufferSize,
        bool useExponentialLargeBuffer,
        long maximumSmallPoolFreeBytes,
        long maximumLargePoolFreeBytes)
    {
        _manager = new RecyclableMemoryStreamManager(blockSize, largeBufferMultiple, maximumBufferSize, useExponentialLargeBuffer, maximumSmallPoolFreeBytes, maximumLargePoolFreeBytes);
    }

    /// <summary>
    /// ConfigureStaticManagerWithDefaults completely rebuilds the <c>RecyclableMemoryStreamManager</c> so try to call it only once, and on startup.
    /// </summary>
    /// <param name="useExponentialLargeBuffer"></param>
    public static void ConfigureNewStaticManagerWithDefaults(bool useExponentialLargeBuffer = false)
    {
        var blockSize = 1024;
        var largeBufferMultiple = 4 * blockSize * blockSize;
        var maximumBufferSize = 2 * largeBufferMultiple;
        var maximumFreeLargePoolBytes = 32 * maximumBufferSize;
        var maximumFreeSmallPoolBytes = 256 * blockSize;

        _manager = new RecyclableMemoryStreamManager(blockSize, largeBufferMultiple, maximumBufferSize, useExponentialLargeBuffer, maximumFreeSmallPoolBytes, maximumFreeLargePoolBytes);
    }

    public static void SetGenerateCallStacks(bool input = true)
    {
        _manager.GenerateCallStacks = input;
    }

    public static void SetAggressiveBufferReturn(bool input = true)
    {
        _manager.GenerateCallStacks = input;
    }

    public static RecyclableMemoryStream GetStream()
    {
        return _manager.GetStream() as RecyclableMemoryStream;
    }

    public static RecyclableMemoryStream GetStream(string tag)
    {
        return _manager.GetStream(tag) as RecyclableMemoryStream;
    }

    public static RecyclableMemoryStream GetStream(string tag, int desiredSize)
    {
        return _manager.GetStream(tag, desiredSize) as RecyclableMemoryStream;
    }

    public static RecyclableMemoryStream GetStream(Memory<byte> buffer)
    {
        return _manager.GetStream(buffer) as RecyclableMemoryStream;
    }

    public static RecyclableMemoryStream GetStream(string tag, Memory<byte> buffer)
    {
        return _manager.GetStream(tag, buffer) as RecyclableMemoryStream;
    }

    public static RecyclableMemoryStream GetStream(string tag, Memory<byte> buffer, int start, int length)
    {
        return _manager.GetStream(tag, buffer.Slice(start, length)) as RecyclableMemoryStream;
    }

    public static void ReturnStream(MemoryStream stream)
    {
        stream.Dispose();
    }
}
```

I am not going to tell you exactly what works on the blockSize, bufferSize, maxes, mins, etc. I wouldn't know what works for
your code. You must figure that out for yourself based on your systems and payloads. I will say, every article's numbers I copied
while reading their guide *sucked shit* in terms of allocations and performance over just default when benchmarking.

I have included my own default one but this one primarily only works well for a segment of message sizes I was dealing with.
It's a specific use case and I am not convinced about even keeping this.

#### Important!  
If you do not set MaximumFreeLargePoolBytes and MaximumFreeSmallPoolBytes there is the possibility for unbounded memory growth!  
[Microsoft's README.md](https://github.com/microsoft/Microsoft.IO.RecyclableMemoryStream)

Once we have a reference to a centralized location for our `RecyclableManager`, we can begin using it everywhere! I am going to take
the final state of our last guide and start implementing `RecyclableMemoryStream` where feasible.

Up first is compression!
```csharp
using Microsoft.Toolkit.HighPerformance;
using System;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;

public string Type { get; } = "GZIP";
public CompressionLevel CompressionLevel { get; set; } = CompressionLevel.Optimal;

public ArraySegment<byte> Compress(ReadOnlyMemory<byte> inputData)
{
    //using var compressedStream = new MemoryStream();
    var compressedStream = RecyclableManager.GetStream(nameof(RecyclableGzipProvider));
    using (var gzipStream = new GZipStream(compressedStream, CompressionLevel, false))
    {
        gzipStream.Write(inputData.Span);
    }

    if (compressedStream.TryGetBuffer(out var buffer))
    { return buffer; }
    else
    {
        using (compressedStream) // dispose stream after using ToArray()
        {
            return compressedStream.ToArray();
        }
    }
}
```

### Code Detour 1 - Right Out The Gate
Because we are adhering to the signature, we lost an advantage here. We extract the buffer from the `MemoryStream`, which is then
used by the developer, meaning it can be returned to the buffer pool. It will get discarded eventually but that isn't
as efficient as signaling a `RecyclableMemoryStream.Dispose()`.

In addition, from time to time under pressure, `TrytGetBuffer()` may indeed fail. In that scenario, we want to make sure that we immediately
`Dispose()` the `Stream` we `rented` from the `RecyclableManager` as we don't need it after using the `.ToArray()` method.

This function, while still good for creating `MemoryStreams` backed by a buffer pool, is the second-best choice for lower memory allocations.

That would be using `Stream`.

```csharp
public MemoryStream Compress(Stream inputStream, bool leaveStreamOpen = false)
{
    // reset position check
    if (inputStream.Position == inputStream.Length) { inputStream.Seek(0, SeekOrigin.Begin); }

    // grab recycled stream
    var compressedStream = RecyclableManager.GetStream(nameof(RecyclableGzipProvider));
    using (var gzipStream = new GZipStream(compressedStream, CompressionLevel, true))
    {
        inputStream.CopyTo(gzipStream);
    }
    if (!leaveStreamOpen) { inputStream.Close(); }

    // reset the position
    compressedStream.Seek(0, SeekOrigin.Begin);
    return compressedStream;
}
```
The return now is the `MemoryStream` built from the `RecyclableMemoryStreamManager`.

Two important notes to remember.
 1. It can be cast to RecyclableMemoryStream.
 2. When finished with it, you have to remember to _**dispose**_ the `Stream`!

The disposable portion is super important to keep allocations low, as this (underneath the
covers) returns the buffer to the buffer pool.

Now that we have viable Compress methods, let's build the Decompress ones!
```csharp
public ArraySegment<byte> Decompress(ReadOnlyMemory<byte> compressedData)
{
    var uncompressedStream = RecyclableManager.GetStream(nameof(RecyclableGzipProvider), CompressionHelpers.GetGzipUncompressedLength(compressedData));
    using (var gzipStream = new GZipStream(compressedData.AsStream(), CompressionMode.Decompress, false))
    {
        gzipStream.CopyTo(uncompressedStream);
    }

    if (uncompressedStream.TryGetBuffer(out var buffer))
    { return buffer; }
    else
    {
        // dispose stream after allocation.
        using (uncompressedStream)
        {
            return uncompressedStream.ToArray();
        }
    }
}

public MemoryStream Decompress(Stream compressedStream, bool leaveStreamOpen = false)
{
    if (compressedStream.Position == compressedStream.Length) { compressedStream.Seek(0, SeekOrigin.Begin); }

    var uncompressedStream = RecyclableManager.GetStream(nameof(RecyclableGzipProvider), CompressionHelpers.GetGzipUncompressedLength(compressedStream));
    using (var gzipStream = new GZipStream(compressedStream, CompressionMode.Decompress, leaveStreamOpen))
    {
        gzipStream.CopyTo(uncompressedStream);
    }

    return uncompressedStream;
}
```

The Decompress method has the same weakness as the first Compress method. To get the lowest allocations, you have to be returning out the
`Stream` that you can then dispose of afterwards.

### Code Detour 2 - Optimal Implementation
If you are using the Recyclable classes, you will want an implementation that looks like this.

```csharp
using HouseofCat.Compression;
using HouseofCat.Encryption;
using HouseofCat.Recyclable;
using HouseofCat.Serialization;
using HouseofCat.Utilities.Errors;
using System;
using System.IO;
using System.Threading.Tasks;

namespace HouseofCat.Example
{
    public class RecyclableTransformer
    {
        public readonly RecyclableAesGcmEncryptionProvider EncryptionProvider;
        public readonly RecyclableGzipProvider CompressionProvider;
        public readonly ISerializationProvider SerializationProvider;

        public RecyclableTransformer(
            ISerializationProvider serializationProvider,
            RecyclableGzipProvider compressionProvider,
            RecyclableAesGcmEncryptionProvider encryptionProvider)
        {
            Guard.AgainstNull(serializationProvider, nameof(serializationProvider));
            Guard.AgainstNull(compressionProvider, nameof(compressionProvider));
            Guard.AgainstNull(encryptionProvider, nameof(encryptionProvider));

            SerializationProvider = serializationProvider;
            CompressionProvider = compressionProvider;
            EncryptionProvider = encryptionProvider;
        }

        public MemoryStream TransformToStream<TIn>(TIn input)
        {
            using var serializedStream = RecyclableManager.GetStream(nameof(RecyclableTransformer));
            SerializationProvider.Serialize(serializedStream, input);

            using var compressedStream = CompressionProvider.Compress(serializedStream, false);

            return EncryptionProvider.Encrypt(compressedStream, false);
        }

        public (ArraySegment<byte>, long) Transform<TIn>(TIn input)
        {
            using var serializedStream = RecyclableManager.GetStream(nameof(RecyclableTransformer));
            SerializationProvider.Serialize(serializedStream, input);

            using var compressedStream = CompressionProvider.Compress(serializedStream, false);
            var encryptedStream = EncryptionProvider.Encrypt(compressedStream, false);

            var length = encryptedStream.Length;
            if (encryptedStream.TryGetBuffer(out var buffer))
            { return (buffer, length); }
            else
            { return (encryptedStream.ToArray(), length); }
        }

        public TOut Restore<TOut>(ReadOnlyMemory<byte> data)
        {
            using var decryptStream = EncryptionProvider.DecryptToStream(data);
            using var decompressStream = CompressionProvider.Decompress(decryptStream, false);
            return SerializationProvider.Deserialize<TOut>(decompressStream);
        }

        public TOut Restore<TOut>(MemoryStream data)
        {
            using var decryptedStream = EncryptionProvider.Decrypt(data, false);
            using var decompressedStream = CompressionProvider.Decompress(decryptedStream, false);
            return SerializationProvider.Deserialize<TOut>(decompressedStream);
        }
    }
}
```

My HouseofCat libraries really streamlined taking an object `<TIn>` and outputting serialized, compressed, and encrypted bytes in a `Stream`
or `bytes` and then the ability to `Restore` that back to the object!

### Benchmarks
Now I always think that there is always room for improvement, but this seems pretty darn good at for an out of the box improvement!
This relatively painless implementation, on average, amounted to an 88% reduction in byte allocations for non-random data (xml, json, plaintext,
etc.) Amounts/variance will occur on how compressible an item is of course so your mileage will vary (be sure to test!)

```ini
// * Summary *

BenchmarkDotNet=v0.13.0, OS=Windows 10.0.19043.1165 (21H1/May2021Update)
AMD Ryzen 9 5950X, 1 CPU, 32 logical and 16 physical cores
.NET SDK=5.0.302
  [Host]   : .NET 5.0.8 (5.0.821.31504), X64 RyuJIT
  .NET 5.0 : .NET 5.0.8 (5.0.821.31504), X64 RyuJIT

Job=.NET 5.0  Runtime=.NET 5.0

|                               Method |    x |        Mean |     Error |      StdDev |      Median | Ratio | RatioSD |    Gen 0 |   Gen 1 | Gen 2 | Allocated | Decrease |
|------------------------------------- |----- |------------:|----------:|------------:|------------:|------:|--------:|---------:|--------:|------:|----------:|---------:|
|      BasicCompressDecompress_5KBytes |   10 |    213.3 us |   4.25 us |     9.14 us |    215.8 us |  1.00 |    0.00 |   6.5918 |       - |     - |    109 KB |      - % |
|                 GzipProvider_5KBytes |   10 |    203.5 us |   4.06 us |    10.98 us |    210.1 us |  0.94 |    0.05 |   3.4180 |       - |     - |     59 KB | 45.872 % |
|           GzipProviderStream_5KBytes |   10 |    206.5 us |   4.11 us |    10.97 us |    210.9 us |  0.96 |    0.06 |   3.4180 |       - |     - |     58 KB | 46.789 % |
|       RecyclableGzipProvider_5KBytes |   10 |    207.5 us |   4.10 us |     9.08 us |    211.3 us |  0.97 |    0.06 |   0.7324 |  0.2441 |     - |     13 KB | 88.073 % |
| RecyclableGzipProviderStream_5KBytes |   10 |    203.6 us |   2.16 us |     1.91 us |    203.8 us |  0.97 |    0.06 |   0.7324 |       - |     - |     13 KB | 88.073 % |
|                                      |      |             |           |             |             |       |         |          |         |       |           |          |
|      BasicCompressDecompress_5KBytes |  100 |  2,188.8 us |  42.41 us |    47.14 us |  2,195.4 us |  1.00 |    0.00 |  66.4063 |       - |     - |  1,088 KB |      - % |
|                 GzipProvider_5KBytes |  100 |  1,909.6 us |  29.74 us |    27.82 us |  1,911.0 us |  0.87 |    0.02 |  35.1563 |       - |     - |    588 KB | 45.956 % |   
|           GzipProviderStream_5KBytes |  100 |  1,897.6 us |  22.44 us |    18.73 us |  1,892.8 us |  0.86 |    0.02 |  35.1563 |       - |     - |    583 KB | 46.415 % |
|       RecyclableGzipProvider_5KBytes |  100 |  2,342.6 us |  45.13 us |    48.29 us |  2,335.8 us |  1.07 |    0.03 |   3.9063 |       - |     - |    121 KB | 88.879 % |
| RecyclableGzipProviderStream_5KBytes |  100 |  2,182.7 us |  79.15 us |   223.26 us |  2,290.1 us |  0.95 |    0.12 |   5.8594 |       - |     - |    126 KB | 88.420 % |
|                                      |      |             |           |             |             |       |         |          |         |       |           |          |
|      BasicCompressDecompress_5KBytes |  500 | 11,190.1 us | 213.12 us |   218.86 us | 11,148.9 us |  1.00 |    0.00 | 328.1250 |       - |     - |  5,438 KB |      - % |
|                 GzipProvider_5KBytes |  500 | 10,144.5 us | 214.08 us |   631.21 us | 10,449.1 us |  0.84 |    0.06 | 171.8750 |       - |     - |  2,938 KB | 45.973 % |
|           GzipProviderStream_5KBytes |  500 | 11,105.7 us | 143.25 us |   126.99 us | 11,110.2 us |  0.99 |    0.03 | 171.8750 |       - |     - |  2,914 KB | 46.414 % |
|       RecyclableGzipProvider_5KBytes |  500 | 11,653.7 us | 182.54 us |   179.28 us | 11,656.0 us |  1.04 |    0.03 |  31.2500 | 15.6250 |     - |    635 KB | 88.323 % | 
| RecyclableGzipProviderStream_5KBytes |  500 | 11,433.7 us | 171.46 us |   160.39 us | 11,420.8 us |  1.02 |    0.02 |  31.2500 |       - |     - |    629 KB | 88.433 % |
|                                      |      |             |           |             |             |       |         |          |         |       |           |          |
|      BasicCompressDecompress_5KBytes | 1000 | 20,582.4 us | 422.06 us | 1,237.82 us | 21,169.3 us |  1.00 |    0.00 | 656.2500 |       - |     - | 10,875 KB |      - % |
|                 GzipProvider_5KBytes | 1000 | 20,492.8 us | 481.45 us | 1,419.57 us | 21,292.6 us |  1.00 |    0.07 | 343.7500 |       - |     - |  5,875 KB | 45.977 % |
|           GzipProviderStream_5KBytes | 1000 | 20,491.4 us | 440.79 us | 1,299.68 us | 20,990.3 us |  1.00 |    0.05 | 343.7500 |       - |     - |  5,828 KB | 46.641 % |
|       RecyclableGzipProvider_5KBytes | 1000 | 18,568.1 us |  42.56 us |    33.23 us | 18,570.2 us |  0.94 |    0.05 |  62.5000 | 31.2500 |     - |  1,271 KB | 88.313 % |
| RecyclableGzipProviderStream_5KBytes | 1000 | 18,192.9 us | 120.91 us |   134.40 us | 18,188.5 us |  0.96 |    0.07 |  62.5000 |       - |     - |  1,258 KB | 88.432 % |
```

### Conclusion
Memory allocation optimizations can be a huge boon in terms of plain-ole throughput. When you start reaching the scale of thousands of requests,
maybe tens of thousands of requests a second, you start finding these issues fast. Speaking in generality, most devs/managers/product owners start
scaling up immediately. I know, I know, Cloud hardware solves are often cheaper than devs, but costs can be prohibitive and back in the day you
couldn't easily throw hardware at problems because it frankly didn't exist in the capacities we take for granted.

There is nothing special about these three guides or code snippets. The only potential secret sauce was taking something mundane you may see on
StackOverflow answered a thousand times and just not taking the first popularly upvoted answer. Instead, challenging oneself with some interesting
constraints. In this particular use case, I was working on a library and I want my it to be as lean and clean as humanly possible so devs
can get more free/out-of-the-box.

I hope you found it useful expedition!
