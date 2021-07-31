# HouseofCat.Data.Recyclable
These benchmarks include Serialization, Compression, and Encryption, that focus on using Recylable classes.

Payload as JSON was about ~12,000 bytes.

### RecyclableTransformer Benchmarks

 * Utf8JsonProvider
 * RecyclableAesGcmEncryptionProvider
 * RecyclableGzipProvider

I am observing that Utf8JsonProvider is making quite a few extra bytes on deserialization from Stream. Confirmed by
`Middleware_InputOutput3` test. Memory only balloons after serialization.

``` ini
// * Summary *

BenchmarkDotNet=v0.13.0, OS=Windows 10.0.19042.1110 (20H2/October2020Update)
Intel Core i9-10900KF CPU 3.70GHz, 1 CPU, 20 logical and 10 physical cores
.NET SDK=5.0.302
  [Host]   : .NET 5.0.8 (5.0.821.31504), X64 RyuJIT
  .NET 5.0 : .NET 5.0.8 (5.0.821.31504), X64 RyuJIT

Job=.NET 5.0  Runtime=.NET 5.0
```

|                        Method |   x |        Mean |    Error |   StdDev | Ratio | RatioSD |    Gen 0 |   Gen 1 | Gen 2 | Allocated |
|------------------------------ |---- |------------:|---------:|---------:|------:|--------:|---------:|--------:|------:|----------:|
|                Transform_12KB |  10 |    408.9 us |  2.28 us |  2.02 us |  1.00 |    0.00 |   1.4648 |  0.4883 |     - |     17 KB |
|                  Restore_12KB |  10 |    601.1 us |  4.23 us |  3.96 us |  1.47 |    0.01 |  24.4141 |       - |     - |    253 KB |
|         TransformRestore_12KB |  10 |  1,065.7 us |  6.15 us |  5.14 us |  2.61 |    0.02 |  25.3906 |  5.8594 |     - |    274 KB |
|        TransformToStream_12KB |  10 |    402.4 us |  2.92 us |  2.73 us |  0.98 |    0.01 |   1.4648 |       - |     - |     17 KB |
| TransformToStreamRestore_12KB |  10 |  1,072.6 us |  6.39 us |  5.98 us |  2.62 |    0.02 |  25.3906 |       - |     - |    270 KB |
|          Middleware_Serialize |  10 |    147.2 us |  0.53 us |  0.45 us |  0.36 |    0.00 |  21.2402 |       - |     - |    219 KB |
|             Middleware_Input1 |  10 |    146.2 us |  0.86 us |  0.81 us |  0.36 |    0.00 |   0.2441 |       - |     - |      4 KB |
|             Middleware_Input2 |  10 |    410.0 us |  2.82 us |  2.64 us |  1.00 |    0.01 |   1.4648 |  0.4883 |     - |     17 KB |
|             Middleware_Input3 |  10 |    409.9 us |  2.86 us |  2.54 us |  1.00 |    0.01 |   1.4648 |  0.4883 |     - |     17 KB |
|       Middleware_InputOutput1 |  10 |    430.9 us |  2.35 us |  2.08 us |  1.05 |    0.01 |   2.4414 |  0.9766 |     - |     26 KB |
|       Middleware_InputOutput2 |  10 |    572.4 us |  3.49 us |  2.91 us |  1.40 |    0.01 |   2.9297 |  0.9766 |     - |     34 KB |
|       Middleware_InputOutput3 |  10 |  1,072.0 us |  7.76 us |  7.26 us |  2.62 |    0.02 |  25.3906 |  5.8594 |     - |    269 KB |
|                               |     |             |          |          |       |         |          |         |       |           |
|                Transform_12KB | 100 |  4,115.4 us | 20.25 us | 17.95 us |  1.00 |    0.00 |  15.6250 |  7.8125 |     - |    167 KB |
|                  Restore_12KB | 100 |  6,013.4 us | 44.83 us | 39.74 us |  1.46 |    0.01 | 242.1875 | 15.6250 |     - |  2,529 KB |
|         TransformRestore_12KB | 100 | 10,748.1 us | 71.11 us | 63.04 us |  2.61 |    0.02 | 265.6250 | 62.5000 |     - |  2,737 KB |
|        TransformToStream_12KB | 100 |  4,018.5 us | 29.70 us | 27.78 us |  0.98 |    0.01 |  15.6250 |       - |     - |    167 KB |
| TransformToStreamRestore_12KB | 100 | 10,675.3 us | 84.43 us | 74.85 us |  2.59 |    0.02 | 250.0000 |       - |     - |  2,696 KB |
|          Middleware_Serialize | 100 |  1,472.7 us |  2.98 us |  2.64 us |  0.36 |    0.00 | 212.8906 |       - |     - |  2,195 KB |
|             Middleware_Input1 | 100 |  1,458.8 us |  6.15 us |  5.76 us |  0.35 |    0.00 |   3.9063 |       - |     - |     42 KB |
|             Middleware_Input2 | 100 |  4,094.3 us | 29.46 us | 26.11 us |  0.99 |    0.01 |  15.6250 |  7.8125 |     - |    167 KB |
|             Middleware_Input3 | 100 |  4,096.4 us | 27.32 us | 24.22 us |  1.00 |    0.01 |  15.6250 |  7.8125 |     - |    167 KB |
|       Middleware_InputOutput1 | 100 |  4,313.7 us | 25.63 us | 22.72 us |  1.05 |    0.01 |  23.4375 |  7.8125 |     - |    256 KB |
|       Middleware_InputOutput2 | 100 |  5,770.3 us | 35.11 us | 32.84 us |  1.40 |    0.01 |  31.2500 | 15.6250 |     - |    339 KB |
|       Middleware_InputOutput3 | 100 | 10,730.5 us | 74.49 us | 58.16 us |  2.61 |    0.02 | 250.0000 | 46.8750 |     - |  2,696 KB |
