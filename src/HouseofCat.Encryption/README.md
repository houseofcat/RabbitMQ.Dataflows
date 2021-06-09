New EncryptionProvider based on Net5.0.

```
// * Summary *

BenchmarkDotNet=v0.13.0, OS=Windows 10.0.18363.1556 (1909/November2019Update/19H2)
Intel Core i7-9850H CPU 2.60GHz, 1 CPU, 12 logical and 6 physical cores
.NET SDK=5.0.203
  [Host]     : .NET 5.0.6 (5.0.621.22011), X64 RyuJIT
  Job-ADZLQM : .NET 5.0.6 (5.0.621.22011), X64 RyuJIT
  .NET 5.0   : .NET 5.0.6 (5.0.621.22011), X64 RyuJIT

Runtime=.NET 5.0

|                  Method |        Job | IterationCount |          Mean |         Error |        StdDev |        Median | Ratio | RatioSD |     Gen 0 |    Gen 1 |    Gen 2 | Allocated |
|------------------------ |----------- |--------------- |--------------:|--------------:|--------------:|--------------:|------:|--------:|----------:|---------:|---------:|----------:|
| CreateArgonHashKeyAsync | Job-ADZLQM |             10 | 27,959.639 us | 1,587.3437 us | 1,049.9296 us | 27,847.659 us |     ? |       ? | 1000.0000 | 500.0000 | 500.0000 |  5,481 KB |
|                         |            |                |               |               |               |               |       |         |           |          |          |           |
|          Encrypt1KBytes |   .NET 5.0 |        Default |      1.512 us |     0.0298 us |     0.0398 us |      1.504 us |  1.00 |    0.00 |    0.1926 |        - |        - |      1 KB |
|          Encrypt2KBytes |   .NET 5.0 |        Default |      1.965 us |     0.0382 us |     0.0408 us |      1.951 us |  1.30 |    0.04 |    0.3548 |        - |        - |      2 KB |
|          Encrypt4kBytes |   .NET 5.0 |        Default |      2.946 us |     0.0583 us |     0.0942 us |      2.948 us |  1.96 |    0.07 |    0.6828 |        - |        - |      4 KB |
|          Encrypt8KBytes |   .NET 5.0 |        Default |      4.630 us |     0.0826 us |     0.0733 us |      4.631 us |  3.09 |    0.08 |    1.3351 |        - |        - |      8 KB |
|          Decrypt1KBytes |   .NET 5.0 |        Default |      1.234 us |     0.0247 us |     0.0338 us |      1.216 us |  0.82 |    0.03 |    0.1869 |        - |        - |      1 KB |
|          Decrypt2KBytes |   .NET 5.0 |        Default |      1.644 us |     0.0328 us |     0.0378 us |      1.630 us |  1.09 |    0.04 |    0.3510 |        - |        - |      2 KB |
|          Decrypt4kBytes |   .NET 5.0 |        Default |      2.462 us |     0.0274 us |     0.0214 us |      2.460 us |  1.64 |    0.04 |    0.6752 |        - |        - |      4 KB |
|          Decrypt8KBytes |   .NET 5.0 |        Default |      4.167 us |     0.0828 us |     0.1016 us |      4.179 us |  2.76 |    0.12 |    1.3275 |        - |        - |      8 KB |
```

BouncyCastle EncryptionProvider

```
// * Summary *

BenchmarkDotNet=v0.13.0, OS=Windows 10.0.18363.1556 (1909/November2019Update/19H2)
Intel Core i7-9850H CPU 2.60GHz, 1 CPU, 12 logical and 6 physical cores
.NET SDK=5.0.203
  [Host]   : .NET 5.0.6 (5.0.621.22011), X64 RyuJIT
  .NET 5.0 : .NET 5.0.6 (5.0.621.22011), X64 RyuJIT

Job=.NET 5.0  Runtime=.NET 5.0

|         Method |      Mean |    Error |   StdDev | Ratio | RatioSD |   Gen 0 |  Gen 1 | Gen 2 | Allocated | Completed Work Items | Lock Contentions |
|--------------- |----------:|---------:|---------:|------:|--------:|--------:|-------:|------:|----------:|---------------------:|-----------------:|
| Encrypt1KBytes |  51.70 us | 0.889 us | 0.788 us |  1.00 |    0.00 |  5.9814 | 0.4883 |     - |     37 KB |               0.0001 |                - |
| Encrypt2KBytes |  73.54 us | 0.936 us | 0.782 us |  1.42 |    0.03 |  7.4463 | 0.6104 |     - |     46 KB |               0.0002 |                - |
| Encrypt4kBytes | 118.41 us | 0.789 us | 0.699 us |  2.29 |    0.03 | 10.2539 | 0.9766 |     - |     64 KB |               0.0005 |                - |
| Encrypt8KBytes | 211.31 us | 2.984 us | 2.791 us |  4.09 |    0.05 | 16.1133 | 1.9531 |     - |    100 KB |               0.0005 |                - |
| Decrypt1KBytes |  53.72 us | 0.652 us | 0.578 us |  1.04 |    0.02 |  5.7983 | 0.5493 |     - |     36 KB |               0.0001 |                - |
| Decrypt2KBytes |  79.22 us | 1.123 us | 0.996 us |  1.53 |    0.03 |  7.0801 | 0.6104 |     - |     44 KB |               0.0002 |                - |
| Decrypt4kBytes | 130.72 us | 2.272 us | 2.125 us |  2.53 |    0.04 |  9.5215 | 0.9766 |     - |     60 KB |               0.0005 |                - |
| Decrypt8KBytes | 225.92 us | 2.618 us | 2.449 us |  4.37 |    0.08 | 14.8926 | 1.7090 |     - |     92 KB |               0.0005 |                - |
```
