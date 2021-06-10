# Brotli - Unsafe
``` ini

BenchmarkDotNet=v0.13.0, OS=Windows 10.0.18363.1556 (1909/November2019Update/19H2)
Intel Core i7-9850H CPU 2.60GHz, 1 CPU, 12 logical and 6 physical cores
.NET SDK=5.0.203
  [Host]   : .NET 5.0.6 (5.0.621.22011), X64 RyuJIT
  .NET 5.0 : .NET 5.0.6 (5.0.621.22011), X64 RyuJIT

Job=.NET 5.0  Runtime=.NET 5.0  

```
|                 Method |         Mean |      Error |     StdDev |       Median | Ratio | RatioSD |  Gen 0 |  Gen 1 | Gen 2 | Allocated |
|----------------------- |-------------:|-----------:|-----------:|-------------:|------:|--------:|-------:|-------:|------:|----------:|
|        Compress5KBytes | 1,034.030 μs | 20.2812 μs | 29.7280 μs | 1,021.293 μs | 1.000 |    0.00 |      - |      - |     - |     601 B |
|   Compress5KBytesAsync | 1,067.396 μs | 21.9973 μs | 61.6828 μs | 1,046.704 μs | 1.029 |    0.06 |      - |      - |     - |     729 B |
|      Decompress5KBytes |     9.545 μs |  0.1770 μs |  0.1818 μs |     9.565 μs | 0.009 |    0.00 | 0.8392 | 0.0153 |     - |   5,307 B |
| Decompress5KBytesAsync |    10.567 μs |  0.2050 μs |  0.3191 μs |    10.519 μs | 0.010 |    0.00 | 1.6479 | 0.0305 |     - |  10,363 B |

Note: Ran the numbers multiple times... Brotli has performance issues it seems.



# Deflate - Unsafe
``` ini

BenchmarkDotNet=v0.13.0, OS=Windows 10.0.18363.1556 (1909/November2019Update/19H2)
Intel Core i7-9850H CPU 2.60GHz, 1 CPU, 12 logical and 6 physical cores
.NET SDK=5.0.203
  [Host]   : .NET 5.0.6 (5.0.621.22011), X64 RyuJIT
  .NET 5.0 : .NET 5.0.6 (5.0.621.22011), X64 RyuJIT

Job=.NET 5.0  Runtime=.NET 5.0  

```
|                 Method |     Mean |    Error |   StdDev |   Median | Ratio | RatioSD |  Gen 0 |  Gen 1 | Gen 2 | Allocated |
|----------------------- |---------:|---------:|---------:|---------:|------:|--------:|-------:|-------:|------:|----------:|
|        Compress5KBytes | 11.11 μs | 0.434 μs | 1.253 μs | 11.28 μs |  1.00 |    0.00 | 0.0610 |      - |     - |     552 B |
|   Compress5KBytesAsync | 11.37 μs | 0.460 μs | 1.306 μs | 11.77 μs |  1.03 |    0.14 | 0.1068 |      - |     - |     680 B |
|      Decompress5KBytes | 10.83 μs | 0.105 μs | 0.093 μs | 10.84 μs |  1.12 |    0.12 | 0.8545 | 0.0153 |     - |   5,442 B |
| Decompress5KBytesAsync | 11.46 μs | 0.225 μs | 0.211 μs | 11.38 μs |  1.17 |    0.13 | 1.6632 | 0.0305 |     - |  10,498 B |



# GZIP - Unsafe
``` ini

BenchmarkDotNet=v0.13.0, OS=Windows 10.0.18363.1556 (1909/November2019Update/19H2)
Intel Core i7-9850H CPU 2.60GHz, 1 CPU, 12 logical and 6 physical cores
.NET SDK=5.0.203
  [Host]   : .NET 5.0.6 (5.0.621.22011), X64 RyuJIT
  .NET 5.0 : .NET 5.0.6 (5.0.621.22011), X64 RyuJIT

Job=.NET 5.0  Runtime=.NET 5.0  

```
|                 Method |      Mean |     Error |    StdDev |    Median | Ratio | RatioSD |  Gen 0 |  Gen 1 | Gen 2 | Allocated |
|----------------------- |----------:|----------:|----------:|----------:|------:|--------:|-------:|-------:|------:|----------:|
|        Compress5KBytes | 11.012 μs | 0.4326 μs | 1.2481 μs | 11.440 μs |  1.00 |    0.00 | 0.0916 |      - |     - |     584 B |
|   Compress5KBytesAsync | 11.711 μs | 0.3873 μs | 1.0923 μs | 11.873 μs |  1.07 |    0.12 | 0.1068 |      - |     - |     736 B |
|      Decompress5KBytes |  9.933 μs | 0.1329 μs | 0.1243 μs |  9.923 μs |  1.01 |    0.07 | 0.8698 | 0.0153 |     - |   5,474 B |
| Decompress5KBytesAsync | 10.918 μs | 0.2117 μs | 0.5867 μs | 10.740 μs |  1.01 |    0.15 | 1.6632 | 0.0305 |     - |  10,530 B |



Going through various iterations but wanted to keep them for reference.
```csharp
// Original
public byte[] Decompress(ReadOnlyMemory<byte> data)
{
    using var uncompressedStream = new MemoryStream();

    using (var compressedStream = new MemoryStream(data.ToArray()))
    using (var bstream = new BrotliStream(compressedStream, CompressionMode.Decompress, false))
    {
        bstream.CopyTo(uncompressedStream);
    }

    return uncompressedStream.ToArray();
}

// Memory optimized version.
public unsafe byte[] Decompress(ReadOnlyMemory<byte> data)
{
    fixed (byte* pBuffer = &data.Span[0])
    {
        using var uncompressedStream = new MemoryStream();

        using (var compressedStream = new UnmanagedMemoryStream(pBuffer, data.Length))
        using (var bstream = new BrotliStream(compressedStream, CompressionMode.Decompress, false))
        {
            bstream.CopyTo(uncompressedStream);
        }

        return uncompressedStream.ToArray();
    }
}

// High Performance Toolkit Variant
public byte[] Decompress(ReadOnlyMemory<byte> data)
{
    using var uncompressedStream = new MemoryStream();

    using (var compressedStream = data.AsStream())
    using (var bstream = new BrotliStream(compressedStream, CompressionMode.Decompress, false))
    {
        bstream.CopyTo(uncompressedStream);
    }

    return uncompressedStream.ToArray();
}

// ReadOnlySpan Sync Version
public unsafe Span<byte> Decompress(ReadOnlySpan<byte> compressedData)
{
    fixed (byte* pBuffer = &compressedData[0])
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