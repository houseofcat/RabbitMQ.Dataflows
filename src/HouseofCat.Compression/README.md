# HouseofCat.Compression
Simple and RecyclableMemoryStream .NET compression as `ICompressionProvider`.

## Typical Usage
```csharp
var compressionProvider = new GzipProvider();
var compressedData = compressionProvider.Compress(myDataAsBytes);

... 
var decompressedData = compressionProvider.Decompress(compressedData);
```

You can use the constructor to also specify CompressionLevel.
```csharp
var compressionProvider = new GzipProvider();
compressionProvider.CompessionLevel = CompressionLevel.SmallestSize;

...
var compressionProvider = new GzipProvider(CompressionLevel.SmallestSize);
```

You can also use Async and Stream methods.
```csharp
var compressionProvider = new GzipProvider(CompressionLevel.SmallestSize);
var dataMemoryStream = new MemoryStream(myDataAsBytes);
var compressedMemoryStream = await compressionProvider.CompressAsync(dataMemoryStream, keepOpen: false);
```

You can also use providers that implement RecyclableMemoryStream.
```csharp
var compressionProvider = new RecyclableGzipProvider(CompressionLevel.SmallestSize);
var compressedMemoryStream = await compressionProvider.CompressToStreamAsync(myDataAsBytes);

...

var decompressedBytes = await compressionProvider.DecompressAsync(compressedMemoryStream, keepOpen: false);
```


## ICompressionProvider
The main interface for writing your own or using dependency injection.

```csharp
public interface ICompressionProvider
{
    string Type { get; }

    ReadOnlyMemory<byte> Compress(ReadOnlyMemory<byte> inputData);
    MemoryStream Compress(Stream inputStream, bool leaveStreamOpen = false);

    ValueTask<ReadOnlyMemory<byte>> CompressAsync(ReadOnlyMemory<byte> inputData);
    ValueTask<MemoryStream> CompressAsync(Stream inputStream, bool leaveStreamOpen = false);

    MemoryStream CompressToStream(ReadOnlyMemory<byte> inputData);
    ValueTask<MemoryStream> CompressToStreamAsync(ReadOnlyMemory<byte> inputData);

    ReadOnlyMemory<byte> Decompress(ReadOnlyMemory<byte> compressedData);
    MemoryStream Decompress(Stream compressedStream, bool leaveStreamOpen = false);

    ValueTask<ReadOnlyMemory<byte>> DecompressAsync(ReadOnlyMemory<byte> compressedData);
    ValueTask<MemoryStream> DecompressAsync(Stream compressedStream, bool leaveStreamOpen = false);

    MemoryStream DecompressToStream(ReadOnlyMemory<byte> compressedData);
}
```

## Benchmarks
Last tested on `NET5.0`.

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


### Recyclabe

```ini
// * Summary *

BenchmarkDotNet=v0.13.0, OS=Windows 10.0.19043.1165 (21H1/May2021Update)
AMD Ryzen 9 5950X, 1 CPU, 32 logical and 16 physical cores
.NET SDK=5.0.302
  [Host]   : .NET 5.0.8 (5.0.821.31504), X64 RyuJIT
  .NET 5.0 : .NET 5.0.8 (5.0.821.31504), X64 RyuJIT

Job=.NET 5.0  Runtime=.NET 5.0
```

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