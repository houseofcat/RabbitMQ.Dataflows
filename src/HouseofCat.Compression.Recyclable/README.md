# NetCore Builtin Compression Providers With Recycleable Memory Streams
Hopefully considered optimally implemented for lower allocations.

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