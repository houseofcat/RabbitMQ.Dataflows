### The Challenge
#### How can I write a better implementation of those Gzip compression examples?
        
##### Additional Asks/Constraints 
1) Continue to use `Stream` as a core mechanic.

#### Intro
Let's continue our work from the last [guide](https://houseofcat.io/guides/csharp/net/compression).

We will be taking those basic examples and finding ways to improve them in terms of memory allocation
and general performance.

Let's start with a code reference below.

```csharp
using System;
using System.IO;
using System.IO.Compression;

public byte[] Compress(byte[] data)
{
    using var compressedStream = new MemoryStream();
    using (var gzipStream = new GZipStream(compressedStream, CompressionLevel.Optimal, false))
    {
        gzipStream.Write(data);
    }

    return compressedStream.ToArray();
}

public byte[] Decompress(byte[] compressedData)
{
    using var uncompressedStream = new MemoryStream();

    using (var compressedStream = new MemoryStream(compressedData))
    using (var gzipStream = new GZipStream(compressedStream, CompressionMode.Decompress, false))
    {
        gzipStream.CopyTo(uncompressedStream);
    }

    return uncompressedStream.ToArray();
}
```
We have a couple of ways of improving some of that code.

Let's go ahead and return `ArraySegment<byte>`. This will enable us to extract the buffer out of the `MemoryStream` and
defer allocate it.

```csharp
public CompressionLevel CompressionLevel { get; set; } = CompressionLevel.Optimal;

public ArraySegment<byte> Compress(ReadOnlyMemory<byte> inputData)
{
    using var compressedStream = new MemoryStream();
    using (var gzipStream = new GZipStream(compressedStream, CompressionLevel, false))
    {
        gzipStream.Write(inputData.Span);
    }

    if (compressedStream.TryGetBuffer(out var buffer))
    { return buffer; }
    else
    { return compressedStream.ToArray(); }
}
```

For the decompress, we will do the same. But we are going to add a 3rd party library from Microsoft to keep the code
clean and `SAFE`!

### Code Detour 3 - A Safer Way To Decompress
If you remember last time, it wasn't easy to convert `ReadOnlyMemory<byte>` to a Stream. I found a work around using 
`fixed` keyword and `unsafe` compilation.

That looked like this.
```csharp
public unsafe byte[] UnsafeDecompress(ReadOnlyMemory<byte> compressedData)
{
    fixed (byte* pBuffer = &compressedData.Span[0])
    {
        using var uncompressedStream = new MemoryStream();
        using (var compressedStream = new UnmanagedMemoryStream(pBuffer, compressedData.Length))
        using (var gzipStream = new GZipStream(compressedStream, CompressionMode.Decompress, false))
        {
            gzipStream.CopyTo(uncompressedStream);
        }
    }

    return uncompressedStream.ToArray();
}
```
It's not the prettiest or the most straightforward code, but it did work well.

What works better is this NuGet package (document links at the bottom).
```csharp
using Microsoft.Toolkit.HighPerformance;
```

The library has awesome pre-coded helpers for these "stuck between a rock and a hard place" scenarios. Specifically, for our
needs though, you need to visit the `ReadOnlyMemory<T>` extensions section. It gives us this beauty.
```csharp
ReadOnlyMemory<byte> compressedData;
...
compressedData.AsStream()
```

With this new library/tooling, it allows us to write the code like so.
```csharp
public ArraySegment<byte> Decompress(ReadOnlyMemory<byte> compressedData)
{
    using var uncompressedStream = new MemoryStream();
    using (var gzipStream = new GZipStream(compressedData.AsStream(), CompressionMode.Decompress, false))
    {
        gzipStream.CopyTo(uncompressedStream);
    }

    if (uncompressedStream.TryGetBuffer(out var buffer))
    { return buffer; }
    else
    { return uncompressedStream.ToArray(); }
}
```
The best part is that its not just for `Microsoft/Windows` as I confirmed it working on `*nix` OS.

Next up we are going to improve performance on decompress by using an old Gzip trick by finding the original uncompressed
data length. Now this one won't improve performance for a single call, but it will for thousands of decompressions.

We are going to look at the last `8 bytes` of `Gzipped` data, which is appended after the Compressed data payload. The
first `4 bytes` (of 8) are used for [CRC32](https://en.wikipedia.org/wiki/Cyclic_redundancy_check). The last `4 bytes`
is the `ISIZE`, a little endian integer of the original payload size.


```csharp
// RFC GZIP - The Last 8 Bytes
// https://datatracker.ietf.org/doc/html/rfc1952
// [ 0 , 1 , 2 , 3 , 4 , 5 , 6 , 7 ]
// +---+---+---+---+---+---+---+---+
// |     CRC32     |     ISIZE     |
// +---+---+---+---+---+---+---+---+
//
// ISIZE - This contains the size of the original (uncompressed) input data modulo 2^32.
// Due to Little Endian format of ISIZE, its better to mentally re-arrange the bytes.
// ex.) [ 3, 2, 1, 0 ]
// Viable strategies for reading this value with respect to little endian byte ordering:
// var length = ([3] << 24) | ([2] << 24) + ([1] << 8) + [0];
// var length ((((([3] << 8) | [2]) << 8) | [1]) << 8) | [0];
// var length = BitConverter.ToInt32(lastFour); // not sure why this works, must internally check
```

So let's write some of our own helper methods that allow us to anticipate the Gzip compressed size. There are
some gotchas to this but they most likely aren't going to apply at this level or to the general use case. Some notable
ones are file size limitations (2^32) and will not work without modification for archive/cab/library systems
(i.e. WinRAR file breakup.)

```csharp
using System;
using System.IO;
using System.Runtime.CompilerServices;

public static class CompressionHelpers
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetGzipUncompressedLength(ReadOnlyMemory<byte> compressedData)
    {
        return BitConverter.ToInt32(compressedData.Slice(compressedData.Length - 4, 4).Span);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetGzipUncompressedLength(Stream stream)
    {
        Span<byte> uncompressedLength = stackalloc byte[4];
        stream.Position = stream.Length - 4;
        stream.Read(uncompressedLength);
        stream.Seek(0, SeekOrigin.Begin);
        return BitConverter.ToInt32(uncompressedLength);
    }
}
```

### Code Detour 1
We used `stackalloc` as this ends up being a low-level performance optimization at the cost of creating 4 additional bytes.
A stack allocated memory block created during the method execution is automatically discarded when that method returns and is
not subject to Garbarge Collection and doesn't need to be pinned with a fix statement either.

### Code Detour 2
We also added `[MethodImpl(MethodImplOptions.AggressiveInlining)]` to our helping functions after testing with it on
and off. It makes near no difference on a single call, but hundreds and thousands of calls led to some savings in
execution time.

MS document links for both are at the bottom of the page.

Now, we will apply it to the `Decompress` method. This will allow us to initialize that `uncompressedStream` with the correct
size array and prevent the need for any size increases.

```csharp
public ArraySegment<byte> Decompress(ReadOnlyMemory<byte> compressedData)
{
    using var uncompressedStream = new MemoryStream();
    ...
}
```

Turns into this.

```csharp
public ArraySegment<byte> Decompress(ReadOnlyMemory<byte> compressedData)
{
    using var uncompressedStream = new MemoryStream(CompressionHelpers.GetGzipUncompressedLength(compressedData));
    ...
}
```

That will get us to our final destination for a better decompress.

```csharp
public ArraySegment<byte> Decompress(ReadOnlyMemory<byte> compressedData)
{
    using var uncompressedStream = new MemoryStream(CompressionHelpers.GetGzipUncompressedLength(compressedData));
    using (var gzipStream = new GZipStream(compressedData.AsStream(), CompressionMode.Decompress, false))
    {
        gzipStream.CopyTo(uncompressedStream);
    }

    if (uncompressedStream.TryGetBuffer(out var buffer))
    { return buffer; }
    else
    { return uncompressedStream.ToArray(); }
}
```

Let's create a Stream based Compress/Decompress too! This time around let's allow the function the option to close the
Stream and guard against a Stream being given to us with the Position at the wrong place (in this example, the position was
at the end already.) The Stream being at the end is one of the most common head scratching issues I run into with
developers. There is no error, but they basically end up compressing 0 bytes and no one way to tell what's wrong at first
glance. There is some inherent danger of always setting the Stream to the beginning. It may not be the developer's
intention to zip everything in the stream (i.e. offset -> stream length).

```csharp
public MemoryStream Compress(Stream inputStream, bool leaveStreamOpen = false)
{
    if (inputStream.Position == inputStream.Length) { inputStream.Seek(0, SeekOrigin.Begin); }

    var compressedStream = new MemoryStream();
    using (var gzipStream = new GZipStream(compressedStream, CompressionLevel, true))
    {
        inputStream.CopyTo(gzipStream);
    }
    if (!leaveStreamOpen) { inputStream.Close(); }

    compressedStream.Seek(0, SeekOrigin.Begin);
    return compressedStream;
}

public MemoryStream Decompress(Stream compressedStream, bool leaveStreamOpen = false)
{
    if (compressedStream.Position == compressedStream.Length) { compressedStream.Seek(0, SeekOrigin.Begin); }

    var uncompressedStream = new MemoryStream(CompressionHelpers.GetGzipUncompressedLength(compressedStream));
    using (var gzipStream = new GZipStream(compressedStream, CompressionMode.Decompress, leaveStreamOpen))
    {
        gzipStream.CopyTo(uncompressedStream);
    }

    return uncompressedStream;
}
```

### Benchmarks
This the end result of all the improvements over the basic versions of the code.

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
|                                      |      |             |           |             |             |       |         |          |         |       |           |          |
|      BasicCompressDecompress_5KBytes |  100 |  2,188.8 us |  42.41 us |    47.14 us |  2,195.4 us |  1.00 |    0.00 |  66.4063 |       - |     - |  1,088 KB |      - % |
|                 GzipProvider_5KBytes |  100 |  1,909.6 us |  29.74 us |    27.82 us |  1,911.0 us |  0.87 |    0.02 |  35.1563 |       - |     - |    588 KB | 45.956 % |   
|           GzipProviderStream_5KBytes |  100 |  1,897.6 us |  22.44 us |    18.73 us |  1,892.8 us |  0.86 |    0.02 |  35.1563 |       - |     - |    583 KB | 46.415 % |
|                                      |      |             |           |             |             |       |         |          |         |       |           |          |
|      BasicCompressDecompress_5KBytes |  500 | 11,190.1 us | 213.12 us |   218.86 us | 11,148.9 us |  1.00 |    0.00 | 328.1250 |       - |     - |  5,438 KB |      - % |
|                 GzipProvider_5KBytes |  500 | 10,144.5 us | 214.08 us |   631.21 us | 10,449.1 us |  0.84 |    0.06 | 171.8750 |       - |     - |  2,938 KB | 45.973 % |
|           GzipProviderStream_5KBytes |  500 | 11,105.7 us | 143.25 us |   126.99 us | 11,110.2 us |  0.99 |    0.03 | 171.8750 |       - |     - |  2,914 KB | 46.414 % |
|                                      |      |             |           |             |             |       |         |          |         |       |           |          |
|      BasicCompressDecompress_5KBytes | 1000 | 20,582.4 us | 422.06 us | 1,237.82 us | 21,169.3 us |  1.00 |    0.00 | 656.2500 |       - |     - | 10,875 KB |      - % |
|                 GzipProvider_5KBytes | 1000 | 20,492.8 us | 481.45 us | 1,419.57 us | 21,292.6 us |  1.00 |    0.07 | 343.7500 |       - |     - |  5,875 KB | 45.977 % |
|           GzipProviderStream_5KBytes | 1000 | 20,491.4 us | 440.79 us | 1,299.68 us | 20,990.3 us |  1.00 |    0.05 | 343.7500 |       - |     - |  5,828 KB | 46.641 % |
```

### Conclusion
There you have it, some significant reductions in byte allocations by making some tweaks. You maybe wondering... whats the gain over the last articles numbers? Full disclosure,
I wasn't going to do this guide as separate piece... but I saw the length was getting longer than expected. I also didn't want to really remove any content as it was exposing
some really handy dandy informations.

I changed my mind and decided not to combine the above with `RecyclableMemoryStream` how-to. Thus I didn't get those number differences and frankly... it won't matter to you
after the next set of improvments... so head there now if you want me to throw down the gauntlet!