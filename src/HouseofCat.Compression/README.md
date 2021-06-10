# Brotli - Unsafe
``` ini

BenchmarkDotNet=v0.13.0, OS=Windows 10.0.18363.1556 (1909/November2019Update/19H2)
Intel Core i7-9850H CPU 2.60GHz, 1 CPU, 12 logical and 6 physical cores
.NET SDK=5.0.203
  [Host]   : .NET 5.0.6 (5.0.621.22011), X64 RyuJIT
  .NET 5.0 : .NET 5.0.6 (5.0.621.22011), X64 RyuJIT

Job=.NET 5.0  Runtime=.NET 5.0  

```
|                 Method |         Mean |      Error |     StdDev | Ratio | RatioSD |  Gen 0 |  Gen 1 | Gen 2 | Allocated |
|----------------------- |-------------:|-----------:|-----------:|------:|--------:|-------:|-------:|------:|----------:|
|        Compress5KBytes |   998.137 μs | 15.6529 μs | 14.6417 μs | 1.000 |    0.00 |      - |      - |     - |     529 B |
|   Compress5KBytesAsync | 1,011.136 μs | 10.3212 μs |  8.6187 μs | 1.014 |    0.02 |      - |      - |     - |     601 B |
|      Decompress5KBytes |     9.895 μs |  0.0640 μs |  0.0500 μs | 0.010 |    0.00 | 1.6327 | 0.0305 |     - |  10,328 B |
| Decompress5KBytesAsync |    10.471 μs |  0.2032 μs |  0.1802 μs | 0.010 |    0.00 | 1.6479 | 0.0305 |     - |  10,432 B |

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
|        Compress5KBytes | 10.84 μs | 0.361 μs | 1.060 μs | 11.34 μs |  1.00 |    0.00 | 0.0916 |      - |     - |     608 B |
|   Compress5KBytesAsync | 11.02 μs | 0.469 μs | 1.345 μs | 11.45 μs |  1.02 |    0.11 | 0.1068 |      - |     - |     680 B |
|      Decompress5KBytes | 11.39 μs | 0.140 μs | 0.117 μs | 11.40 μs |  1.21 |    0.12 | 1.6632 | 0.0305 |     - |  10,464 B |
| Decompress5KBytesAsync | 11.69 μs | 0.223 μs | 0.219 μs | 11.66 μs |  1.23 |    0.14 | 1.6785 | 0.0305 |     - |  10,568 B |



# GZIP - Unsafe
``` ini

BenchmarkDotNet=v0.13.0, OS=Windows 10.0.18363.1556 (1909/November2019Update/19H2)
Intel Core i7-9850H CPU 2.60GHz, 1 CPU, 12 logical and 6 physical cores
.NET SDK=5.0.203
  [Host]   : .NET 5.0.6 (5.0.621.22011), X64 RyuJIT
  .NET 5.0 : .NET 5.0.6 (5.0.621.22011), X64 RyuJIT

Job=.NET 5.0  Runtime=.NET 5.0  

```
|                 Method |     Mean |    Error |   StdDev | Ratio | RatioSD |  Gen 0 |  Gen 1 | Gen 2 | Allocated |
|----------------------- |---------:|---------:|---------:|------:|--------:|-------:|-------:|------:|----------:|
|        Compress5KBytes | 10.36 μs | 0.207 μs | 0.408 μs |  1.00 |    0.00 | 0.0916 |      - |     - |     664 B |
|   Compress5KBytesAsync | 12.71 μs | 0.178 μs | 0.149 μs |  1.25 |    0.11 | 0.1068 |      - |     - |     736 B |
|      Decompress5KBytes | 10.46 μs | 0.186 μs | 0.207 μs |  1.02 |    0.07 | 1.6632 | 0.0305 |     - |  10,496 B |
| Decompress5KBytesAsync | 10.49 μs | 0.155 μs | 0.145 μs |  1.03 |    0.07 | 1.6785 | 0.0305 |     - |  10,624 B |



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