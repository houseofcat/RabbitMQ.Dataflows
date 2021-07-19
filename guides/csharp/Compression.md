### The Challenge
#### How can I perform Compression/Decompression in NetCore3.x/Net5.x?
        
##### Additional Asks/Constraints 
1) Use `Stream`.
2) No 3rd party dependencies.

#### Intro
I will start with the most common way I have seen this written. It is with an input of `byte[]` and an 
output `byte[]` so lets just write that first as a rough draft. Most of you probably just want to
bounce after that!

These are super vanilla examples and I demonstrate them with Gzip (`GzipStream`). I did want to add
though, I think doing it with `Stream` is probably the most common/useful/clean way to do this, as
opposed to manually working with bytes and operations yourself.

I took some ADD detours here and there, so skip those if you don't really care.

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

##### Code Detour 1 - Stream Setup
In `public byte[] Compress(byte[] data)`, you take in a `byte[]` and spit out the same. This is where
things get a little confusing to beginners - because it is essentially not written down ***concisely***
what is happening.

###### Compression Breakdown
`GzipStream` only accepts `Stream` for construction. So when you construct a `GzipStream` for
***compression***, that input `MemoryStream` is where you are telling the `GzipStream` to Write its output
to! So it maybe a little in reverse of what you may expect (input data would go into a `.ctor`).
`GzipStream` is essentially a middle-man just performing operations on inputs and outputing the results
to another `Stream`.

So try to visualize Compression like this:  
```
byte, byte, byte, byte byte -> GzipStream (internal Compression operation) 
GzipStream -> byte, byte, byte -> Stream output
```

So to build a `GzipStream`, we need to start with two `Streams` actually.
```csharp
using var compressedStream = new MemoryStream();
using (var gzipStream = new GZipStream(compressedStream, CompressionLevel.Optimal, false))
```

Then funnel our data into our middle-man with `Write()`.
```csharp
using var compressedStream = new MemoryStream();
using (var gzipStream = new GZipStream(compressedStream, CompressionLevel.Optimal, false))
{
    gzipStream.Write(data);
}
```

Then get/flush the final data out.
```csharp
return compressedStream.ToArray();
```

###### So... the same for Decompression?
Well no, now you need an additional `Stream` for Decompression! Crazy! `GzipStream` still only accepts a
`Stream` for construction. So when you do construct a `GzipStream` for ***decompression***, that input
`MemoryStream` is where you are telling the `GzipStream` where the compressed data is! Hey, that's the
opposite?! Yep.

Visualize Decompression like this:  
```
Stream1 (with compressed data) byte, byte, byte -> GzipStream (internal Decompression operation)
GzipStream -> byte, byte, byte, byte, byte -> Stream2 output
```

This time though, to get it out, you have to tell `GzipStream` where to now put the data. That's where
`CopyTo()` comes into play (similar to needing the `Write()` before).

Confusing? Good. This is an ancient API that's due for an overhaul. So this is a ***great example of
learning just how to do it*** not how it works because its convoluted.

##### Code Detour 2 - Using Statement Stranger Danger
Referencing the above code, I am used to what I have written... but I have done something with the `using`
statement for cleanliness that could screw up beginners. If it is mentionable, it is manageable.
```csharp
using var compressedStream = new MemoryStream();
```
This `using` statement with out `parentheses` and with a line terminator `;` means that this `var 
compressedStream` disposes AFTER the scope has finished, in this case, after return. This is a relatively
new feature in C# since `v8.0`. This is super important to pay attention to: `using` statements with
`Stream`. It is probably the number 1 cause of why your `Stream` code isn't working. That's because - and
I think this is a major flaw - `Close()/Dispose()` on Streams performs finalization logic which means
that in a `Stream` it is performing further operations on your buffer/output.

For example:
```csharp
using (var gzipStream = new GZipStream(compressedStream, CompressionMode.Decompress, false))
{
    gzipStream.CopyTo(uncompressedStream);
}
```

The above code really only shows one method `CopyTo` localized within the standard `using` statement,
however, there is a hiding cleanup operations like `Close()` inside `Dispose()` which can hide `Flush()` or
final `Write()` to backing storage (depends on how the Stream is implemented).

So to quote [Chad Daniels](https://www.youtube.com/watch?v=tOSo0r-9Sz4), `Jelly beans can be tricky,
so watch your ass and smell your jelly beans.`

```csharp
public byte[] Compress(byte[] data)
{
    using var compressedStream = new MemoryStream();
    using (var gzipStream = new GZipStream(compressedStream, CompressionLevel.Optimal, false))
    {
        gzipStream.Write(data);
    } // this is finalizing the gzipStream and writing the final block of bytes to the compressedStream

    return compressedStream.ToArray(); // if the above second using statement is wrong, 
   // these bytes returned won't decompress (invalid format or length invalid etc.)
}
```

One last tangent, these `using` statements are above each other, but not stacked because of the `;`. This is 
a really important distinction since stacked `using` statements, dispose together. Remember, disposing with
streams means its performing operations!

This is not stacked.
```csharp
using var compressedStream = new MemoryStream();
using (var gzipStream = new GZipStream(compressedStream, CompressionLevel.Optimal, false))
{
    ...
}

return ...;
```

The `compressedStream` disposes after return. `gzipStream` disposes after the internal local scope ends
(with the ending brace `}`).

This is stacked and it is saying something 100% different.
```csharp
using (var compressedStream = new MemoryStream())
using (var gzipStream = new GZipStream(compressedStream, CompressionLevel.Optimal, false))
{
    ...
}

return ...;
```

These variables both dispose after the local scope finishes (i.e. after the `}`) but in reverse order.
It's `gzipStream` `Dispose()`, then `compressedStream` `Dispose()` before you hit `return`.

Stacked using statements are short hand for this:
```csharp
using (var compressedStream = new MemoryStream())
{
    using (var gzipStream = new GZipStream(compressedStream, CompressionLevel.Optimal, false))
    {
        ...
    }
}

return ...;
```

But I acknowledge they look similar and can add to confusion in a guide/example.

#### Back To ~~Reality~~ Compression
##### Let's Make It Async Too
This isn't faster/better than the regular use case (local memory compression) but would work better with
incoming data from `Stream`. Think `NetworkStream` or `FileStream`. That would make these
additive methods/APIs - don't replace the sync methods. It maybe tempting to `Async` all the way down,
but don't, the `sync` methods are faster and lower allocations.
```csharp
using System;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;

// because the bytes are here, and the streams are built here... this async is virtually useless
// and does nothing to help with performance nor will it really ever await.
public async Task<byte[]> CompressAsync(byte[] data)
{
    using var compressedStream = new MemoryStream();
    using (var gzipStream = new GZipStream(compressedStream, CompressionLevel.Optimal, false))
    {
        await gzipStream
            .WriteAsync(data)
            .ConfigureAwait(false);
    }

    return compressedStream.ToArray();
}

// in this case, we have the input data, but we maybe waiting to write based on the stream status,
// so writeasync could block depending on what the caller is doing with the stream.
public async Task CompressAsync(Stream outputStream, byte[] data)
{
    // Add a little safety check.
    if (!outputStream.CanWrite) throw new InvalidOperationException($"{nameof(outputStream)} is not available for writing to.");

    using (var gzipStream = new GZipStream(outputStream, CompressionLevel.Optimal, false))
    {
        await gzipStream
            .WriteAsync(data)
            .ConfigureAwait(false);
    }
}

public async Task<byte[]> DecompressAsync(byte[] compressedData)
{
    using var uncompressedStream = new MemoryStream();

    using (var compressedStream = new MemoryStream(compressedData))
    using (var gzipStream = new GZipStream(compressedStream, CompressionMode.Decompress, false))
    {
        await gzipStream
            .CopyToAsync(uncompressedStream)
            .ConfigureAwait(false);
    }

    return uncompressedStream.ToArray();
}

public async Task DecompressAsync(Stream outputStream, byte[] compressedData)
{
    if (!outputStream.CanWrite) throw new InvalidOperationException($"{nameof(outputStream)} is not available for writing to.");

    using (var compressedStream = new MemoryStream(compressedData))
    using (var gzipStream = new GZipStream(compressedStream, CompressionMode.Decompress, false))
    {
        await gzipStream
            .CopyToAsync(outputStream)
            .ConfigureAwait(false);
    }
}
```

#### Let's Modernize The API - Part 1
##### Low Memory Allocation Pattern
In the spirit of NetCore3.0+, now-a-days we try to write library-esque code for cases where users want the
option to deal in `Span<T>/Memory<T>` giving them a ton more flexibiliy on allocating data. This parameter
change will allow byte[] OR a reference to a segment of memory.  

STOP ALL THE ALLOCATIONS!  

```csharp
using System;
using System.IO;
using System.IO.Compression;

public byte[] Compress(ReadOnlyMemory<byte> data)
{
    using var compressedStream = new MemoryStream();
    using (var gzipStream = new GZipStream(compressedStream, CompressionLevel, false))
    {
        gzipStream.Write(data.Span);
    }

    return compressedStream.ToArray();
}
```

Now let's write the Decompress... Wait... the Stream API hasn't been updated to include these
`Span<T>/Memory<T>` language/library tools! Ahhhh Shit! That means we can't write a Decompress!  

##### Code Detour 3 - `ReadOnlyMemory<T>` Can't Be Used In Streams?!
It's true. Like I said previously (you may have skimmed over my bullshit) - `Stream` is an ancient API. Lucky
for you, while I am not a great coder, I am stubborn as shit. I researched some solutions doing my best to
prevent needing my own custom `Stream`. There was one way that appealed to me as it essentially fell into
still being able to use a non-library internal method and not requiring a ton of work. I am lazy.

***THIS MAY NOT WORK IN ALL USE CASES AND YOU MAY NEED TO TWEAK IT***
...but this did seem to work on most datasets I tested with.  

Let's keep the original as a reference!  
```cs
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
We need a `MemoryStream` with the `compressedData` in it. There is not a `Stream` that works with
`ReadOnlyMemory<byte>`. Looking around, there is a special `Stream` that will work with a `pointer` to
the start of a `byte` segment. Now, I frankly didn't know this before I started this, but fuck it, I am
avoiding work I don't want to do (just kidding employer :|) so let's go down the rabbit hole.

How do I convert a `ReadOnlyMemory<byte>` to essentially a buffer `pointer`? Well you can't, at least not
directly. Inside our `ReadOnlyMemory<byte>` however, there are helper Properties/Methods. The one we want to
look at is the one giving us access to the internal `Span[]`.

This is where we have to stop playing it `safe`.  

This is why I put the warning disclaimer. It's possible to have more than one `Span` in here. While I thoroughly tested this code and it
seemed to always work, I am sure there is a use case where this fails because we ignore the other `Spans`. That's why I labeled it
`UnsafeDecompress` though, so you can keep the original!

If we use `fixed` keyword, we can `fix` or `pin` a memory address and stop GC from moving it around in memory and refer to it as a type.
In this case, this gives us a `fixed` `byte*` which is a `byte` pointer at the beginning of a segment of memory holding/expecting type `byte`.

Exactly what we needed... now to use it!

```cs
// unsafe keyword means the project now needs to be marked unsafe to the compiler.
public unsafe byte[] UnsafeDecompress(ReadOnlyMemory<byte> compressedData)
{
    fixed (byte* pBuffer = &compressedData.Span[0])
    {

    }
}
```

Now what the hell do we do with that? Great question brotato chip.

Well we just need a `Stream` that will take that as input and just read the data. That's it.

So the `Stream` that can perform that is called an `UnmanagedMemoryStream`. We give it a `pointer` and
length of our segment of data we intend to use with it. Then everything else stitches together like before.

All together as a Decompress function.
```cs
using System;
using System.IO;
using System.IO.Compression;

// unsafe keyword means the project now needs to be marked unsafe to the compiler.
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

Jesus, are we done yet?  

NO, YOU ARE GOING TO TAKE THIS INFORMATION AND YOU ARE GOING TO LIKE IT!  

...or you know... just leave the page.

#### Let's Modernize The API - Part 2
##### By Using The Ancient API
You know, we have done all this work... but we still make a `byte[]` on return. I wish we didn't have to. If
my memory serves me correctly, this means you are always double allocating (2 x `byte[]`). Once in the
buffer of the `Stream` and also allocating to when calling `.ToArray()`.

Can we change that? The answer honestly is `no`, not with the way things exist today...

...but what if you could use the buffer directly from the `MemoryStream`? Sounds incredibly sketchy to me so
let's do it. This is a learning experience anyways.

`MemoryStream` doesn't provide direct access to the `Buffer`, but does have method `TryGetBuffer()` that
gives us an arraysegment of unsigned bytes from which this `Stream` was created. While it may seem new,
`ArraySegment<byte>` is actually quite old school. Kind of the grandad of `Span<T>/Memory<T>`. By returning
this, you don't perform the second allocation of memory.  

So it's all solved? Sadly no, is not a silver bullet - there are some asterisks.

Putting it to code first so we can analyze it.

First issue, it is `TryGetBuffer()`, which means it can fail in acquiring the buffer, i.e. can happen because
the `Stream` was disposed/closed etc. Now, that can't/shouldn't happen in this code (because we control type of
Stream used and it's life cycle), but lets handle the condition properly regardless - should that change in
some unforseen future.

Second off, we have to change our return type to `ArraySegment<byte>`. This will put off people not
familiar with `ArraySegment<byte>` which I would gauge is the majority of developers. Thus you might have
to call it a different method name.

So `CompressAlt` it is!
```csharp
public ArraySegment<byte> CompressAlt(ReadOnlyMemory<byte> data)
{
    using var compressedStream = new MemoryStream();
    using (var gzipStream = new GZipStream(compressedStream, CompressionLevel, false))
    {
        gzipStream.Write(data.Span);
    }

    if (compressedStream.TryGetBuffer(out var buffer))
    { return buffer; }
    else
    { return compressedStream.ToArray(); }
}
```
The third issue, is when you get the `ArraySegment<byte>` you can't use it directly as a `byte[]`.
The length will nearly always be larger than your actual data sets usage as it is a `buffer`. Thus
using it directly will often break serializers.

1) Shouldn't be an issue in this code.
2) Shouldn't be as big of an issue with comments/examples etc.
3) Has two potential good solves.
   a) Change return type to include length, i.e. `ValueTuple(ArraySegment<byte> data, int length)`
      i) Problem is we don't have a length until we have a final set of data or its provided to us via a header etc.
      ii) Gzip's first 4 bytes are usually the int length. 
   b) Teach developers to use the `.ToArray()` method when they are ready to get the data out of it!

The `ToArray()` constructs a `byte[]` with the correct length of your actual dataset. This is that
second allocation we have avoided until now.

This is what I call a `deferred allocation`. You have fully deferred a second allocation of the memory at
least until such time the developer decides to use the data. This can be immediately or the user may
have strict memory requirements thus may be more conservative with its willy nilly allocation. This
would give them more control for that option.

#### Whole Bunch of Examples Together
This should be good enough to get you started with `GzipStream`. You can replace `GzipStream` with
 `BrotliStream` or `DeflateStream` and then you would have all 3 major internal compression libraries
implemented.

```csharp
using System;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;

public byte[] Compress(byte[] data)
{
    using var compressedStream = new MemoryStream();
    using (var gzipStream = new GZipStream(compressedStream, CompressionLevel.Optimal, false))
    {
        gzipStream.Write(data);
    }

    return compressedStream.ToArray();
}

// because the bytes are here, and the streams are built here... this async is virtually useless
// and does nothing to help with performance nor will it really ever await.
public async Task<byte[]> CompressAsync(byte[] data)
{
    using var compressedStream = new MemoryStream();
    using (var gzipStream = new GZipStream(compressedStream, CompressionLevel.Optimal, false))
    {
        await gzipStream
            .WriteAsync(data)
            .ConfigureAwait(false);
    }

    return compressedStream.ToArray();
}

// in this case, we have the input data, but we maybe waiting to write based on the stream status,
// so writeasync could block depending on what the caller is doing with the stream.
public async Task CompressAsync(Stream outputStream, byte[] data)
{
    // Add a little safety check.
    if (!outputStream.CanWrite) throw new InvalidOperationException($"{nameof(outputStream)} is not available for writing to.");

    using (var gzipStream = new GZipStream(outputStream, CompressionLevel.Optimal, false))
    {
        await gzipStream
            .WriteAsync(data)
            .ConfigureAwait(false);
    }
}

public ArraySegment<byte> CompressAlt(ReadOnlyMemory<byte> data)
{
    using var compressedStream = new MemoryStream();
    using (var gzipStream = new GZipStream(compressedStream, CompressionLevel.Optimal, false))
    {
        gzipStream.Write(data.Span);
    }

    if (compressedStream.TryGetBuffer(out var buffer))
    { return buffer; }
    else
    { return compressedStream.ToArray(); }
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

public ArraySegment<byte> DecompressAlt(byte[] compressedData)
{
    using var uncompressedStream = new MemoryStream();

    using (var compressedStream = new MemoryStream(compressedData))
    using (var gzipStream = new GZipStream(compressedStream, CompressionMode.Decompress, false))
    {
        gzipStream.CopyTo(uncompressedStream);
    }

    if (uncompressedStream.TryGetBuffer(out var buffer))
    { return buffer; }
    else
    { return uncompressedStream.ToArray(); }
}

public async Task<byte[]> DecompressAsync(byte[] compressedData)
{
    using var uncompressedStream = new MemoryStream();

    using (var compressedStream = new MemoryStream(compressedData))
    using (var gzipStream = new GZipStream(compressedStream, CompressionMode.Decompress, false))
    {
        await gzipStream
            .CopyToAsync(uncompressedStream)
            .ConfigureAwait(false);
    }

    return uncompressedStream.ToArray();
}

public async Task DecompressAsync(Stream outputStream, byte[] compressedData)
{
    if (!outputStream.CanWrite) throw new InvalidOperationException($"{nameof(outputStream)} is not available for writing to.");

    using (var compressedStream = new MemoryStream(compressedData))
    using (var gzipStream = new GZipStream(compressedStream, CompressionMode.Decompress, false))
    {
        await gzipStream
            .CopyToAsync(outputStream)
            .ConfigureAwait(false);
    }
}

// unsafe keyword means the project now needs to be marked unsafe to the compiler.
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
            
        return uncompressedStream.ToArray();
    }
}

// unsafe keyword means the project now needs to be marked unsafe to the compiler.
public unsafe ArraySegment<byte> UnsafeDecompressAlt(ReadOnlyMemory<byte> compressedData)
{
    fixed (byte* pBuffer = &compressedData.Span[0])
    {
        using var uncompressedStream = new MemoryStream();
        using (var compressedStream = new UnmanagedMemoryStream(pBuffer, compressedData.Length))
        using (var gzipStream = new GZipStream(compressedStream, CompressionMode.Decompress, false))
        {
            gzipStream.CopyTo(uncompressedStream);
        }
            
        if (uncompressedStream.TryGetBuffer(out var buffer))
        { return buffer; }
        else
        { return uncompressedStream.ToArray(); }
    }
}
```

#### Benchmarks
Pretty much just comparing the basic implementation (`Basic`) vs. the slightly more optimized deferred
allocation way. It's most noticeable on Decompress obviously.
``` ini
BenchmarkDotNet=v0.13.0, OS=Windows 10.0.19042.1110 (20H2/October2020Update)
Intel Core i9-10900KF CPU 3.70GHz, 1 CPU, 20 logical and 10 physical cores
.NET SDK=5.0.302
  [Host]   : .NET 5.0.8 (5.0.821.31504), X64 RyuJIT
  .NET 5.0 : .NET 5.0.8 (5.0.821.31504), X64 RyuJIT

Job=.NET 5.0  Runtime=.NET 5.0  

|                           Method |      Mean |     Error |    StdDev |    Median | Ratio | RatioSD |  Gen 0 |  Gen 1 | Gen 2 | Allocated |
|--------------------------------- |----------:|----------:|----------:|----------:|------:|--------:|-------:|-------:|------:|----------:|
|             BasicCompress5KBytes | 11.149 μs | 0.2209 μs | 0.5581 μs | 11.258 μs |  1.00 |    0.00 | 0.0610 |      - |     - |     664 B |
|                  Compress5KBytes | 11.156 μs | 0.2197 μs | 0.4730 μs | 11.207 μs |  1.01 |    0.06 | 0.0458 |      - |     - |     584 B |
|             Compress5KBytesAsync | 11.812 μs | 0.0829 μs | 0.0776 μs | 11.828 μs |  1.11 |    0.16 | 0.0458 |      - |     - |     584 B |
|          Compress5KBytesToStream |  9.654 μs | 0.0911 μs | 0.0852 μs |  9.666 μs |  0.90 |    0.13 | 0.0458 |      - |     - |     584 B |
|     Compress5KBytesToStreamAsync | 10.230 μs | 0.3431 μs | 0.9845 μs | 10.820 μs |  0.90 |    0.09 | 0.0458 |      - |     - |     584 B |
|           BasicDecompress5KBytes |  8.684 μs | 0.0623 μs | 0.0552 μs |  8.687 μs |  0.82 |    0.12 | 0.9918 | 0.0153 |     - |  10,472 B |
|                Decompress5KBytes |  8.584 μs | 0.0491 μs | 0.0435 μs |  8.578 μs |  0.81 |    0.12 | 0.5188 |      - |     - |   5,472 B |
|           Decompress5KBytesAsync |  8.711 μs | 0.0441 μs | 0.0369 μs |  8.719 μs |  0.82 |    0.12 | 0.5188 |      - |     - |   5,432 B |
|      Decompress5KBytesFromStream |  8.387 μs | 0.0394 μs | 0.0368 μs |  8.392 μs |  0.79 |    0.11 | 0.5188 |      - |     - |   5,448 B |
| Decompress5KBytesFromStreamAsync |  8.682 μs | 0.0355 μs | 0.0297 μs |  8.683 μs |  0.82 |    0.12 | 0.5188 |      - |     - |   5,448 B |
```

#### Benchmark Class/Methodology
```csharp
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using HouseofCat.Compression;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;

[MarkdownExporterAttribute.GitHub]
[MemoryDiagnoser]
[SimpleJob(runtimeMoniker: RuntimeMoniker.Net50 | RuntimeMoniker.NetCoreApp31)]
public class GzipBenchmark
{
    private ICompressionProvider CompressionProvider;

    private byte[] Payload1 { get; set; } = new byte[5000];
    private byte[] CompressedPayload1 { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        Enumerable.Repeat<byte>(0xFF, 1000).ToArray().CopyTo(Payload1, 0);
        Enumerable.Repeat<byte>(0xAA, 1000).ToArray().CopyTo(Payload1, 1000);
        Enumerable.Repeat<byte>(0x1A, 1000).ToArray().CopyTo(Payload1, 2000);
        Enumerable.Repeat<byte>(0xAF, 1000).ToArray().CopyTo(Payload1, 3000);
        Enumerable.Repeat<byte>(0x01, 1000).ToArray().CopyTo(Payload1, 4000);

        CompressionProvider = new GzipProvider();
        CompressedPayload1 = CompressionProvider.Compress(Payload1).ToArray();
    }

    [Benchmark(Baseline = true)]
    public void BasicCompress5KBytes()
    {
        var data = BasicCompress(Payload1);
    }

    [Benchmark]
    public void Compress5KBytes()
    {
        var data = CompressionProvider.Compress(Payload1);
    }

    [Benchmark]
    public async Task Compress5KBytesAsync()
    {
        var data = await CompressionProvider.CompressAsync(Payload1);
    }

    [Benchmark]
    public void Compress5KBytesToStream()
    {
        var stream = CompressionProvider.CompressToStream(Payload1);
    }

    [Benchmark]
    public async Task Compress5KBytesToStreamAsync()
    {
        var stream = await CompressionProvider.CompressToStreamAsync(Payload1);
    }

    [Benchmark]
    public void BasicDecompress5KBytes()
    {
        var data = BasicDecompress(CompressedPayload1);
    }

    [Benchmark]
    public void Decompress5KBytes()
    {
        var data = CompressionProvider.Decompress(CompressedPayload1);
    }

    [Benchmark]
    public async Task Decompress5KBytesAsync()
    {
        var data = await CompressionProvider.DecompressAsync(CompressedPayload1);
    }

    [Benchmark]
    public void Decompress5KBytesFromStream()
    {
        var stream = CompressionProvider.DecompressStream(new MemoryStream(CompressedPayload1));
    }

    [Benchmark]
    public async Task Decompress5KBytesFromStreamAsync()
    {
        var stream = await CompressionProvider.DecompressStreamAsync(new MemoryStream(CompressedPayload1));
    }

    public byte[] BasicCompress(byte[] data)
    {
        using var compressedStream = new MemoryStream();
        using (var gzipStream = new GZipStream(compressedStream, CompressionLevel.Optimal, false))
        {
            gzipStream.Write(data);
        }

        return compressedStream.ToArray();
    }

    public byte[] BasicDecompress(byte[] compressedData)
    {
        using var uncompressedStream = new MemoryStream();

        using (var compressedStream = new MemoryStream(compressedData))
        using (var gzipStream = new GZipStream(compressedStream, CompressionMode.Decompress, false))
        {
            gzipStream.CopyTo(uncompressedStream);
        }

        return uncompressedStream.ToArray();
    }
}
```

#### Final Thoughts
Really, just wanted to share my mental notes of playing around with Compression/Decompression, for my
open source library Tesseract. I know its a very verbose guide - so if you made it this far kudos! 
Tesseract code is just code that really helps devs build software by doing my best to have the
fundamentals clean/solid. I do also have some high performance parallelism voodoo but its nothing
really special any one could have come up with it. What makes it special is its purpose. By trying to
handle the parallelism, compression, encryption, serialization, and application metrics, you get to 
spend nearly all your focus and your energy on your core code making it even better.

At least in theory! I hope this was an interesting dive into coding or at the very least was able to
provide a working example. Nothing wrong with that if that's all you needed.

Plenty of reference links to docs and my library code will actually stay up to date.

Lastly, I just want to say, when it comes to advance topics like Memory Allocation, don't be afraid to
experiment but also take a step back at the end of the day. Ask yourself, is the code still readable?
Does it diverge too far from what you wanted it to actually do?

Also, if your end goal was lower allocations make sure that your upstream architecture accounts for it. You gain
nothing using deferred allocation if the upstream immediately allocates for example... unless its just now
future ready. There can be lasting implications that aren't readily changeable after the fact. Sometimes its
just easier to leave things alone.

Taking that final step back is valuable. You don't always get the high level view when writing libraries. You do
benefit from actual use cases, because without them you can get locked into narrow scope of an individual action 
or idea. It can completely miss the bigger picture of how the code gets used/implemented.

If you have feedback on the library code, feel free to raise issues/questions on Github,
and I will respond to them!