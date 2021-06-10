# NetCore Builtin Compression Providers
Hopefully considered optimally implemented.

### Brotli
``` ini

BenchmarkDotNet=v0.13.0, OS=Windows 10.0.18363.1556 (1909/November2019Update/19H2)
Intel Core i7-9850H CPU 2.60GHz, 1 CPU, 12 logical and 6 physical cores
.NET SDK=5.0.203
  [Host]   : .NET 5.0.6 (5.0.621.22011), X64 RyuJIT
  .NET 5.0 : .NET 5.0.6 (5.0.621.22011), X64 RyuJIT

Job=.NET 5.0  Runtime=.NET 5.0  

```
|                           Method |         Mean |      Error |      StdDev |       Median | Ratio | RatioSD |  Gen 0 |  Gen 1 | Gen 2 | Allocated |
|--------------------------------- |-------------:|-----------:|------------:|-------------:|------:|--------:|-------:|-------:|------:|----------:|
|                  Compress5KBytes | 1,092.655 μs | 38.0460 μs | 110.9821 μs | 1,052.348 μs | 1.000 |    0.00 |      - |      - |     - |     729 B |
|             Compress5KBytesAsync | 1,008.077 μs | 20.0877 μs |  27.4963 μs | 1,003.506 μs | 0.964 |    0.07 |      - |      - |     - |     681 B |
|          Compress5KBytesToStream | 1,014.082 μs | 19.6544 μs |  24.8566 μs | 1,007.944 μs | 0.984 |    0.05 |      - |      - |     - |     601 B |
|     Compress5KBytesToStreamAsync | 1,000.003 μs | 16.3154 μs |  15.2614 μs |   995.087 μs | 0.972 |    0.05 |      - |      - |     - |     673 B |
|                Decompress5KBytes |     9.499 μs |  0.1020 μs |   0.0904 μs |     9.502 μs | 0.009 |    0.00 | 0.8392 | 0.0153 |     - |   5,307 B |
|           Decompress5KBytesAsync |    10.217 μs |  0.1867 μs |   0.1655 μs |    10.170 μs | 0.010 |    0.00 | 0.8392 | 0.0153 |     - |   5,347 B |
|      Decompress5KBytesFromStream |     9.259 μs |  0.0895 μs |   0.0794 μs |     9.266 μs | 0.009 |    0.00 | 0.8392 | 0.0153 |     - |   5,283 B |
| Decompress5KBytesFromStreamAsync |     9.753 μs |  0.1676 μs |   0.1486 μs |     9.747 μs | 0.009 |    0.00 | 0.8392 | 0.0153 |     - |   5,355 B |

Note: Ran the numbers multiple times... Brotli is supposed to be slower for ahead-of-use compression strategies but this looks like it has performance issues.



### Deflate
``` ini

BenchmarkDotNet=v0.13.0, OS=Windows 10.0.18363.1556 (1909/November2019Update/19H2)
Intel Core i7-9850H CPU 2.60GHz, 1 CPU, 12 logical and 6 physical cores
.NET SDK=5.0.203
  [Host]   : .NET 5.0.6 (5.0.621.22011), X64 RyuJIT
  .NET 5.0 : .NET 5.0.6 (5.0.621.22011), X64 RyuJIT

Job=.NET 5.0  Runtime=.NET 5.0  

```
|                           Method |     Mean |    Error |   StdDev |   Median | Ratio | RatioSD |  Gen 0 |  Gen 1 | Gen 2 | Allocated |
|--------------------------------- |---------:|---------:|---------:|---------:|------:|--------:|-------:|-------:|------:|----------:|
|                  Compress5KBytes | 11.83 μs | 0.432 μs | 1.225 μs | 12.03 μs |  1.00 |    0.00 | 0.0610 |      - |     - |     552 B |
|             Compress5KBytesAsync | 11.11 μs | 0.372 μs | 1.091 μs | 11.48 μs |  0.95 |    0.16 | 0.0916 |      - |     - |     632 B |
|          Compress5KBytesToStream | 10.93 μs | 0.359 μs | 1.043 μs | 11.30 μs |  0.93 |    0.11 | 0.0763 |      - |     - |     552 B |
|     Compress5KBytesToStreamAsync | 12.49 μs | 0.310 μs | 0.890 μs | 12.59 μs |  1.07 |    0.13 | 0.0916 |      - |     - |     624 B |
|                Decompress5KBytes | 10.79 μs | 0.159 μs | 0.141 μs | 10.78 μs |  1.15 |    0.21 | 0.8545 | 0.0153 |     - |   5,440 B |
|           Decompress5KBytesAsync | 10.83 μs | 0.092 μs | 0.077 μs | 10.83 μs |  1.18 |    0.21 | 0.8698 | 0.0153 |     - |   5,480 B |
|      Decompress5KBytesFromStream | 10.58 μs | 0.142 μs | 0.133 μs | 10.59 μs |  1.12 |    0.20 | 0.8545 | 0.0153 |     - |   5,416 B |
| Decompress5KBytesFromStreamAsync | 11.05 μs | 0.152 μs | 0.135 μs | 11.07 μs |  1.18 |    0.22 | 0.8698 |      - |     - |   5,488 B |



### GZIP
``` ini

BenchmarkDotNet=v0.13.0, OS=Windows 10.0.18363.1556 (1909/November2019Update/19H2)
Intel Core i7-9850H CPU 2.60GHz, 1 CPU, 12 logical and 6 physical cores
.NET SDK=5.0.203
  [Host]   : .NET 5.0.6 (5.0.621.22011), X64 RyuJIT
  .NET 5.0 : .NET 5.0.6 (5.0.621.22011), X64 RyuJIT

Job=.NET 5.0  Runtime=.NET 5.0  

```
|                           Method |      Mean |     Error |    StdDev |    Median | Ratio | RatioSD |  Gen 0 |  Gen 1 | Gen 2 | Allocated |
|--------------------------------- |----------:|----------:|----------:|----------:|------:|--------:|-------:|-------:|------:|----------:|
|                  Compress5KBytes | 11.771 μs | 0.4217 μs | 1.1473 μs | 12.025 μs |  1.00 |    0.00 | 0.0916 |      - |     - |     584 B |
|             Compress5KBytesAsync | 11.619 μs | 0.4038 μs | 1.1584 μs | 12.058 μs |  0.99 |    0.17 | 0.0916 |      - |     - |     664 B |
|          Compress5KBytesToStream | 10.724 μs | 0.4427 μs | 1.2773 μs | 11.288 μs |  0.91 |    0.09 | 0.0916 |      - |     - |     584 B |
|     Compress5KBytesToStreamAsync | 11.582 μs | 0.4320 μs | 1.2464 μs | 10.985 μs |  0.98 |    0.12 | 0.0916 |      - |     - |     656 B |
|                Decompress5KBytes | 10.934 μs | 0.4019 μs | 1.1787 μs | 10.520 μs |  0.95 |    0.14 | 0.8698 | 0.0153 |     - |   5,474 B |
|           Decompress5KBytesAsync |  9.993 μs | 0.1978 μs | 0.2117 μs |  9.927 μs |  1.00 |    0.21 | 0.8698 |      - |     - |   5,514 B |
|      Decompress5KBytesFromStream |  9.788 μs | 0.1877 μs | 0.2505 μs |  9.727 μs |  0.93 |    0.17 | 0.8545 | 0.0153 |     - |   5,450 B |
| Decompress5KBytesFromStreamAsync |  9.955 μs | 0.1482 μs | 0.1386 μs |  9.972 μs |  1.03 |    0.19 | 0.8698 | 0.0153 |     - |   5,522 B |



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