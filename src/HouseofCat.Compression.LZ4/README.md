# LZ4 Compression Benchmarks

### LZ4 Pickle
``` ini

BenchmarkDotNet=v0.13.0, OS=Windows 10.0.18363.1556 (1909/November2019Update/19H2)
Intel Core i7-9850H CPU 2.60GHz, 1 CPU, 12 logical and 6 physical cores
.NET SDK=5.0.203
  [Host]   : .NET 5.0.6 (5.0.621.22011), X64 RyuJIT
  .NET 5.0 : .NET 5.0.6 (5.0.621.22011), X64 RyuJIT

Job=.NET 5.0  Runtime=.NET 5.0  

```
|            Method |     Mean |     Error |    StdDev | Ratio |  Gen 0 | Gen 1 | Gen 2 | Allocated |
|------------------ |---------:|----------:|----------:|------:|-------:|------:|------:|----------:|
|   Compress5KBytes | 2.092 μs | 0.0310 μs | 0.0290 μs |  1.00 | 0.0114 |     - |     - |      80 B |
| Decompress5KBytes | 1.092 μs | 0.0181 μs | 0.0169 μs |  0.52 | 0.7992 |     - |     - |   5,024 B |



### LZ4 Stream
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
|        Compress5KBytes | 4.728 μs | 0.0614 μs | 0.0479 μs |  1.00 |    0.00 | 10.5209 | 0.2899 |     - |     65 KB |
|   Compress5KBytesAsync | 5.037 μs | 0.0991 μs | 0.1289 μs |  1.06 |    0.03 | 10.5209 | 0.0229 |     - |     65 KB |
|      Decompress5KBytes | 4.773 μs | 0.0933 μs | 0.1506 μs |  1.02 |    0.03 | 11.2305 | 0.2289 |     - |     69 KB |
| Decompress5KBytesAsync | 6.164 μs | 0.0949 μs | 0.0888 μs |  1.30 |    0.03 | 12.1918 | 0.2441 |     - |     75 KB |
