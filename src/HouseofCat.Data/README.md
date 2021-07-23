# TransformMiddlewareBenchmarks
These benchmarks include Serialization, Compression, and Encryption.

Target payload before anything was about 7,058 bytes and serialized to around ~338 bytes.

### Gzip - Net5.0 AesGCM - Utf8Json
``` ini

BenchmarkDotNet=v0.13.0, OS=Windows 10.0.18363.1556 (1909/November2019Update/19H2)
Intel Core i7-9850H CPU 2.60GHz, 1 CPU, 12 logical and 6 physical cores
.NET SDK=5.0.203
  [Host]   : .NET 5.0.6 (5.0.621.22011), X64 RyuJIT
  .NET 5.0 : .NET 5.0.6 (5.0.621.22011), X64 RyuJIT

Job=.NET 5.0  Runtime=.NET 5.0  

```
|               Method |     Mean |    Error |   StdDev | Ratio | RatioSD |  Gen 0 |  Gen 1 | Gen 2 | Allocated |
|--------------------- |---------:|---------:|---------:|------:|--------:|-------:|-------:|------:|----------:|
|        Serialize_7KB | 36.26 μs | 0.383 μs | 0.339 μs |  1.00 |    0.00 | 3.4790 |      - |     - |     22 KB |
|      Deserialize_7KB | 54.36 μs | 0.479 μs | 0.400 μs |  1.50 |    0.02 | 5.9204 | 0.1831 |     - |     37 KB |
|   SerializeAsync_7KB | 38.46 μs | 0.505 μs | 0.473 μs |  1.06 |    0.02 | 3.6621 | 0.0610 |     - |     22 KB |
| DeserializeAsync_7KB | 54.68 μs | 0.891 μs | 0.914 μs |  1.51 |    0.03 | 4.9438 | 0.1221 |     - |     30 KB |



### Gzip - Bouncy AesGCM - Utfj8Json
``` ini

BenchmarkDotNet=v0.13.0, OS=Windows 10.0.18363.1556 (1909/November2019Update/19H2)
Intel Core i7-9850H CPU 2.60GHz, 1 CPU, 12 logical and 6 physical cores
.NET SDK=5.0.203
  [Host]   : .NET 5.0.6 (5.0.621.22011), X64 RyuJIT
  .NET 5.0 : .NET 5.0.6 (5.0.621.22011), X64 RyuJIT

Job=.NET 5.0  Runtime=.NET 5.0  

```
|               Method |     Mean |    Error |   StdDev | Ratio | RatioSD |   Gen 0 |  Gen 1 | Gen 2 | Allocated |
|--------------------- |---------:|---------:|---------:|------:|--------:|--------:|-------:|------:|----------:|
|        Serialize_7KB | 74.42 μs | 0.927 μs | 0.822 μs |  1.00 |    0.00 |  8.4229 | 0.6104 |     - |     52 KB |
|      Deserialize_7KB | 88.11 μs | 0.712 μs | 0.631 μs |  1.18 |    0.01 | 10.7422 | 0.6104 |     - |     66 KB |
|   SerializeAsync_7KB | 74.65 μs | 1.150 μs | 1.020 μs |  1.00 |    0.02 |  8.3008 | 0.6104 |     - |     52 KB |
| DeserializeAsync_7KB | 88.87 μs | 1.377 μs | 1.288 μs |  1.20 |    0.02 |  9.7656 | 0.4883 |     - |     60 KB |
