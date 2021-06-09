# LZ4 Pickle
``` ini

BenchmarkDotNet=v0.13.0, OS=Windows 10.0.18363.1556 (1909/November2019Update/19H2)
Intel Core i7-9850H CPU 2.60GHz, 1 CPU, 12 logical and 6 physical cores
.NET SDK=5.0.203
  [Host]   : .NET 5.0.6 (5.0.621.22011), X64 RyuJIT
  .NET 5.0 : .NET 5.0.6 (5.0.621.22011), X64 RyuJIT

Job=.NET 5.0  Runtime=.NET 5.0  

```
|                 Method |     Mean |     Error |    StdDev | Ratio | RatioSD |  Gen 0 | Gen 1 | Gen 2 | Allocated |
|----------------------- |---------:|----------:|----------:|------:|--------:|-------:|------:|------:|----------:|
|        Compress5KBytes | 1.966 μs | 0.0201 μs | 0.0188 μs |  1.00 |    0.00 | 0.0114 |     - |     - |      80 B |
|   Compress5KBytesAsync | 2.103 μs | 0.0410 μs | 0.0403 μs |  1.07 |    0.02 | 0.0229 |     - |     - |     152 B |
|      Decompress5KBytes | 1.113 μs | 0.0222 μs | 0.0272 μs |  0.57 |    0.02 | 0.7992 |     - |     - |   5,024 B |
| Decompress5KBytesAsync | 1.159 μs | 0.0230 μs | 0.0322 μs |  0.59 |    0.02 | 0.8106 |     - |     - |   5,096 B |



# LZ4 Stream
``` ini

BenchmarkDotNet=v0.13.0, OS=Windows 10.0.18363.1556 (1909/November2019Update/19H2)
Intel Core i7-9850H CPU 2.60GHz, 1 CPU, 12 logical and 6 physical cores
.NET SDK=5.0.203
  [Host]   : .NET 5.0.6 (5.0.621.22011), X64 RyuJIT
  .NET 5.0 : .NET 5.0.6 (5.0.621.22011), X64 RyuJIT

Job=.NET 5.0  Runtime=.NET 5.0  

```
|                 Method |     Mean |     Error |    StdDev | Ratio | RatioSD |   Gen 0 |  Gen 1 | Gen 2 | Allocated |
|----------------------- |---------:|----------:|----------:|------:|--------:|--------:|-------:|------:|----------:|
|        Compress5KBytes | 4.983 μs | 0.0968 μs | 0.2481 μs |  1.00 |    0.00 | 10.5209 | 0.6561 |     - |     65 KB |
|   Compress5KBytesAsync | 5.338 μs | 0.1063 μs | 0.2355 μs |  1.07 |    0.06 | 10.5209 | 0.0229 |     - |     65 KB |
|      Decompress5KBytes | 5.254 μs | 0.0939 μs | 0.1118 μs |  1.04 |    0.06 | 12.0468 | 0.2365 |     - |     74 KB |
| Decompress5KBytesAsync | 6.318 μs | 0.1237 μs | 0.1735 μs |  1.26 |    0.08 | 12.1918 | 0.2365 |     - |     75 KB |
