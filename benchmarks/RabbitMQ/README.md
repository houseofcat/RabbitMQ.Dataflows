
### Benchmark Results

A collection of all the microbenchmarks ran for CookedRabbit.

<details><summary>Click here to see CookedRabbit.Core ConnectionPool results!</summary>
<p>

``` ini
// * Summary *

BenchmarkDotNet=v0.12.0, OS=Windows 10.0.17763.914 (1809/October2018Update/Redstone5)
Intel Core i7-8700K CPU 3.70GHz (Coffee Lake), 1 CPU, 12 logical and 6 physical cores
.NET Core SDK=3.1.100
  [Host]     : .NET Core 3.1.0 (CoreCLR 4.700.19.56402, CoreFX 4.700.19.56404), X64 RyuJIT
  Job-PRFRPX : .NET Core 3.1.0 (CoreCLR 4.700.19.56402, CoreFX 4.700.19.56404), X64 RyuJIT

Runtime=.NET Core 3.1

|                                         Method |       x |          Mean |         Error |        StdDev | Ratio | RatioSD | Completed Work Items | Lock Contentions |     Gen 0 |     Gen 1 | Gen 2 |  Allocated |
|----------------------------------------------- |-------- |--------------:|--------------:|--------------:|------:|--------:|---------------------:|-----------------:|----------:|----------:|------:|-----------:|
|                              CreateConnections |     100 | 177,825.34 us |  3,512.386 us |  5,363.784 us | 1.000 |    0.00 |             204.0000 |           0.3333 | 1333.3333 |  333.3333 |     - | 10127293 B |
|           GetConnectionFromConnectionPoolAsync |     100 |      13.42 us |      0.208 us |      0.195 us | 0.000 |    0.00 |               0.0001 |                - |         - |         - |     - |          - |
| ConcurrentGetConnectionFromConnectionPoolAsync |     100 |      32.43 us |      0.390 us |      0.365 us | 0.000 |    0.00 |               4.0001 |           0.0244 |    0.1831 |    0.0610 |     - |     1248 B |
|                                                |         |               |               |               |       |         |                      |                  |           |           |       |            |
|                              CreateConnections |     500 | 900,525.30 us | 12,094.633 us | 10,721.577 us | 1.000 |    0.00 |            1008.0000 |                - | 8000.0000 | 2000.0000 |     - | 50632000 B |
|           GetConnectionFromConnectionPoolAsync |     500 |      67.58 us |      1.127 us |      1.054 us | 0.000 |    0.00 |               0.0010 |           0.0005 |         - |         - |     - |          - |
| ConcurrentGetConnectionFromConnectionPoolAsync |     500 |     150.79 us |      0.715 us |      0.634 us | 0.000 |    0.00 |               4.0032 |           0.3318 |         - |         - |     - |     1249 B |
|                                                |         |               |               |               |       |         |                      |                  |           |           |       |            |
|           GetConnectionFromConnectionPoolAsync |    5000 |     729.04 us |     12.259 us |     11.467 us |     ? |       ? |               0.0137 |                - |         - |         - |     - |        4 B |
| ConcurrentGetConnectionFromConnectionPoolAsync |    5000 |   1,470.17 us |     28.724 us |     34.194 us |     ? |       ? |               4.0137 |           7.0078 |         - |         - |     - |     1253 B |
|                                                |         |               |               |               |       |         |                      |                  |           |           |       |            |
|           GetConnectionFromConnectionPoolAsync |  500000 |  67,422.86 us |    993.882 us |    929.678 us |     ? |       ? |               0.2500 |                - |         - |         - |     - |      251 B |
| ConcurrentGetConnectionFromConnectionPoolAsync |  500000 | 152,217.78 us |  2,508.368 us |  2,346.329 us |     ? |       ? |               4.5000 |         362.7500 |         - |         - |     - |     1394 B |
|                                                |         |               |               |               |       |         |                      |                  |           |           |       |            |
|           GetConnectionFromConnectionPoolAsync | 1000000 | 133,252.40 us |  2,015.316 us |  1,885.128 us |     ? |       ? |               1.7500 |           0.7500 |         - |         - |     - |      982 B |
| ConcurrentGetConnectionFromConnectionPoolAsync | 1000000 | 289,690.25 us |  4,803.740 us |  4,493.422 us |     ? |       ? |               6.0000 |         831.0000 |         - |         - |     - |     3368 B |
```

<details><summary>Click here to see outliers and the legend!</summary>
<p>

```ini
// * Warnings *
BaselineCustomAnalyzer
  Summary -> A question mark '?' symbol indicates that it was not possible to compute the (Ratio, RatioSD) column(s) because the baseline value is too close to zero.

// * Hints *
Outliers
  ConnectionPoolBenchmark.CreateConnections: Runtime=.NET Core 3.1                              -> 1 outlier  was  removed (195.34 ms)
  ConnectionPoolBenchmark.CreateConnections: Runtime=.NET Core 3.1                              -> 1 outlier  was  removed (944.32 ms)
  ConnectionPoolBenchmark.ConcurrentGetConnectionFromConnectionPoolAsync: Runtime=.NET Core 3.1 -> 1 outlier  was  removed (153.10 us)

// * Legends *
  x                    : Value of the 'x' parameter
  Mean                 : Arithmetic mean of all measurements
  Error                : Half of 99.9% confidence interval
  StdDev               : Standard deviation of all measurements
  Ratio                : Mean of the ratio distribution ([Current]/[Baseline])
  RatioSD              : Standard deviation of the ratio distribution ([Current]/[Baseline])
  Completed Work Items : The number of work items that have been processed in ThreadPool (per single operation)
  Lock Contentions     : The number of times there was contention upon trying to take a Monitor's lock (per single operation)
  Gen 0                : GC Generation 0 collects per 1000 operations
  Gen 1                : GC Generation 1 collects per 1000 operations
  Gen 2                : GC Generation 2 collects per 1000 operations
  Allocated            : Allocated memory per single operation (managed only, inclusive, 1KB = 1024B)
  1 us                 : 1 Microsecond (0.000001 sec)
```
</p>
</details>

<details><summary>Click here to see the detailed results!</summary>
<p>

```ini
// * Detailed results *
ConnectionPoolBenchmark.CreateConnections: Job-PRFRPX(Runtime=.NET Core 3.1) [x=100]
Runtime = .NET Core 3.1.0 (CoreCLR 4.700.19.56402, CoreFX 4.700.19.56404), X64 RyuJIT; GC = Concurrent Workstation
Mean = 177.8253 ms, StdErr = 0.9634 ms (0.54%); N = 31, StdDev = 5.3638 ms
Min = 165.7774 ms, Q1 = 174.2480 ms, Median = 179.5727 ms, Q3 = 181.1805 ms, Max = 187.7291 ms
IQR = 6.9325 ms, LowerFence = 163.8493 ms, UpperFence = 191.5792 ms
ConfidenceInterval = [174.3130 ms; 181.3377 ms] (CI 99.9%), Margin = 3.5124 ms (1.98% of Mean)
Skewness = -0.81, Kurtosis = 2.97, MValue = 2
-------------------- Histogram --------------------
[164.721 ms ; 171.376 ms) | @@@@
[171.376 ms ; 175.705 ms) | @@@@
[175.705 ms ; 182.724 ms) | @@@@@@@@@@@@@@@@@@@@@
[182.724 ms ; 188.190 ms) | @@
---------------------------------------------------

ConnectionPoolBenchmark.GetConnectionFromConnectionPoolAsync: Job-PRFRPX(Runtime=.NET Core 3.1) [x=100]
Runtime = .NET Core 3.1.0 (CoreCLR 4.700.19.56402, CoreFX 4.700.19.56404), X64 RyuJIT; GC = Concurrent Workstation
Mean = 13.4208 us, StdErr = 0.0503 us (0.37%); N = 15, StdDev = 0.1947 us
Min = 13.1984 us, Q1 = 13.2470 us, Median = 13.3616 us, Q3 = 13.6076 us, Max = 13.7615 us
IQR = 0.3607 us, LowerFence = 12.7060 us, UpperFence = 14.1486 us
ConfidenceInterval = [13.2126 us; 13.6289 us] (CI 99.9%), Margin = 0.2082 us (1.55% of Mean)
Skewness = 0.59, Kurtosis = 1.77, MValue = 2
-------------------- Histogram --------------------
[13.160 us ; 13.831 us) | @@@@@@@@@@@@@@@
---------------------------------------------------

ConnectionPoolBenchmark.ConcurrentGetConnectionFromConnectionPoolAsync: Job-PRFRPX(Runtime=.NET Core 3.1) [x=100]
Runtime = .NET Core 3.1.0 (CoreCLR 4.700.19.56402, CoreFX 4.700.19.56404), X64 RyuJIT; GC = Concurrent Workstation
Mean = 32.4266 us, StdErr = 0.0942 us (0.29%); N = 15, StdDev = 0.3649 us
Min = 31.9662 us, Q1 = 32.1578 us, Median = 32.2943 us, Q3 = 32.8259 us, Max = 33.0290 us
IQR = 0.6682 us, LowerFence = 31.1555 us, UpperFence = 33.8281 us
ConfidenceInterval = [32.0365 us; 32.8166 us] (CI 99.9%), Margin = 0.3901 us (1.20% of Mean)
Skewness = 0.35, Kurtosis = 1.38, MValue = 2
-------------------- Histogram --------------------
[31.837 us ; 33.158 us) | @@@@@@@@@@@@@@@
---------------------------------------------------

ConnectionPoolBenchmark.CreateConnections: Job-PRFRPX(Runtime=.NET Core 3.1) [x=500]
Runtime = .NET Core 3.1.0 (CoreCLR 4.700.19.56402, CoreFX 4.700.19.56404), X64 RyuJIT; GC = Concurrent Workstation
Mean = 900.5253 ms, StdErr = 2.8655 ms (0.32%); N = 14, StdDev = 10.7216 ms
Min = 886.6493 ms, Q1 = 891.5272 ms, Median = 901.0943 ms, Q3 = 907.0387 ms, Max = 919.6287 ms
IQR = 15.5115 ms, LowerFence = 868.2600 ms, UpperFence = 930.3060 ms
ConfidenceInterval = [888.4307 ms; 912.6199 ms] (CI 99.9%), Margin = 12.0946 ms (1.34% of Mean)
Skewness = 0.31, Kurtosis = 1.79, MValue = 2
-------------------- Histogram --------------------
[882.757 ms ; 923.521 ms) | @@@@@@@@@@@@@@
---------------------------------------------------

ConnectionPoolBenchmark.GetConnectionFromConnectionPoolAsync: Job-PRFRPX(Runtime=.NET Core 3.1) [x=500]
Runtime = .NET Core 3.1.0 (CoreCLR 4.700.19.56402, CoreFX 4.700.19.56404), X64 RyuJIT; GC = Concurrent Workstation
Mean = 67.5791 us, StdErr = 0.2722 us (0.40%); N = 15, StdDev = 1.0543 us
Min = 66.4739 us, Q1 = 66.6254 us, Median = 67.2828 us, Q3 = 68.9056 us, Max = 68.9828 us
IQR = 2.2803 us, LowerFence = 63.2050 us, UpperFence = 72.3260 us
ConfidenceInterval = [66.4519 us; 68.7063 us] (CI 99.9%), Margin = 1.1272 us (1.67% of Mean)
Skewness = 0.38, Kurtosis = 1.26, MValue = 2
-------------------- Histogram --------------------
[66.100 us ; 69.357 us) | @@@@@@@@@@@@@@@
---------------------------------------------------

ConnectionPoolBenchmark.ConcurrentGetConnectionFromConnectionPoolAsync: Job-PRFRPX(Runtime=.NET Core 3.1) [x=500]
Runtime = .NET Core 3.1.0 (CoreCLR 4.700.19.56402, CoreFX 4.700.19.56404), X64 RyuJIT; GC = Concurrent Workstation
Mean = 150.7914 us, StdErr = 0.1695 us (0.11%); N = 14, StdDev = 0.6342 us
Min = 149.5197 us, Q1 = 150.4027 us, Median = 150.8028 us, Q3 = 151.3425 us, Max = 151.9484 us
IQR = 0.9398 us, LowerFence = 148.9930 us, UpperFence = 152.7523 us
ConfidenceInterval = [150.0759 us; 151.5069 us] (CI 99.9%), Margin = 0.7155 us (0.47% of Mean)
Skewness = -0.15, Kurtosis = 2.32, MValue = 2
-------------------- Histogram --------------------
[149.289 us ; 152.179 us) | @@@@@@@@@@@@@@
---------------------------------------------------

ConnectionPoolBenchmark.GetConnectionFromConnectionPoolAsync: Job-PRFRPX(Runtime=.NET Core 3.1) [x=5000]
Runtime = .NET Core 3.1.0 (CoreCLR 4.700.19.56402, CoreFX 4.700.19.56404), X64 RyuJIT; GC = Concurrent Workstation
Mean = 729.0414 us, StdErr = 2.9608 us (0.41%); N = 15, StdDev = 11.4670 us
Min = 710.2952 us, Q1 = 716.5209 us, Median = 729.8895 us, Q3 = 739.6325 us, Max = 743.8411 us
IQR = 23.1115 us, LowerFence = 681.8537 us, UpperFence = 774.2998 us
ConfidenceInterval = [716.7825 us; 741.3003 us] (CI 99.9%), Margin = 12.2589 us (1.68% of Mean)
Skewness = -0.3, Kurtosis = 1.44, MValue = 2
-------------------- Histogram --------------------
[706.227 us ; 732.604 us) | @@@@@@@@
[732.604 us ; 747.910 us) | @@@@@@@
---------------------------------------------------

ConnectionPoolBenchmark.ConcurrentGetConnectionFromConnectionPoolAsync: Job-PRFRPX(Runtime=.NET Core 3.1) [x=5000]
Runtime = .NET Core 3.1.0 (CoreCLR 4.700.19.56402, CoreFX 4.700.19.56404), X64 RyuJIT; GC = Concurrent Workstation
Mean = 1.4702 ms, StdErr = 0.0075 ms (0.51%); N = 21, StdDev = 0.0342 ms
Min = 1.4329 ms, Q1 = 1.4461 ms, Median = 1.4558 ms, Q3 = 1.5006 ms, Max = 1.5384 ms
IQR = 0.0545 ms, LowerFence = 1.3643 ms, UpperFence = 1.5825 ms
ConfidenceInterval = [1.4414 ms; 1.4989 ms] (CI 99.9%), Margin = 0.0287 ms (1.95% of Mean)
Skewness = 0.8, Kurtosis = 1.98, MValue = 2.14
-------------------- Histogram --------------------
[1.422 ms ; 1.464 ms) | @@@@@@@@@@@@@@
[1.464 ms ; 1.511 ms) | @@@
[1.511 ms ; 1.546 ms) | @@@@
---------------------------------------------------

ConnectionPoolBenchmark.GetConnectionFromConnectionPoolAsync: Job-PRFRPX(Runtime=.NET Core 3.1) [x=500000]
Runtime = .NET Core 3.1.0 (CoreCLR 4.700.19.56402, CoreFX 4.700.19.56404), X64 RyuJIT; GC = Concurrent Workstation
Mean = 67.4229 ms, StdErr = 0.2400 ms (0.36%); N = 15, StdDev = 0.9297 ms
Min = 66.2897 ms, Q1 = 66.7439 ms, Median = 67.1943 ms, Q3 = 67.8771 ms, Max = 69.4947 ms
IQR = 1.1332 ms, LowerFence = 65.0441 ms, UpperFence = 69.5769 ms
ConfidenceInterval = [66.4290 ms; 68.4167 ms] (CI 99.9%), Margin = 0.9939 ms (1.47% of Mean)
Skewness = 0.68, Kurtosis = 2.35, MValue = 2
-------------------- Histogram --------------------
[65.960 ms ; 68.303 ms) | @@@@@@@@@@@@
[68.303 ms ; 69.825 ms) | @@@
---------------------------------------------------

ConnectionPoolBenchmark.ConcurrentGetConnectionFromConnectionPoolAsync: Job-PRFRPX(Runtime=.NET Core 3.1) [x=500000]
Runtime = .NET Core 3.1.0 (CoreCLR 4.700.19.56402, CoreFX 4.700.19.56404), X64 RyuJIT; GC = Concurrent Workstation
Mean = 152.2178 ms, StdErr = 0.6058 ms (0.40%); N = 15, StdDev = 2.3463 ms
Min = 146.5413 ms, Q1 = 150.3571 ms, Median = 153.2018 ms, Q3 = 153.9952 ms, Max = 154.9232 ms
IQR = 3.6380 ms, LowerFence = 144.9001 ms, UpperFence = 159.4522 ms
ConfidenceInterval = [149.7094 ms; 154.7261 ms] (CI 99.9%), Margin = 2.5084 ms (1.65% of Mean)
Skewness = -0.94, Kurtosis = 2.83, MValue = 2
-------------------- Histogram --------------------
[145.709 ms ; 151.280 ms) | @@@@
[151.280 ms ; 155.351 ms) | @@@@@@@@@@@
---------------------------------------------------

ConnectionPoolBenchmark.GetConnectionFromConnectionPoolAsync: Job-PRFRPX(Runtime=.NET Core 3.1) [x=1000000]
Runtime = .NET Core 3.1.0 (CoreCLR 4.700.19.56402, CoreFX 4.700.19.56404), X64 RyuJIT; GC = Concurrent Workstation
Mean = 133.2524 ms, StdErr = 0.4867 ms (0.37%); N = 15, StdDev = 1.8851 ms
Min = 131.0821 ms, Q1 = 131.9236 ms, Median = 132.2271 ms, Q3 = 135.1997 ms, Max = 136.4339 ms
IQR = 3.2761 ms, LowerFence = 127.0095 ms, UpperFence = 140.1138 ms
ConfidenceInterval = [131.2371 ms; 135.2677 ms] (CI 99.9%), Margin = 2.0153 ms (1.51% of Mean)
Skewness = 0.54, Kurtosis = 1.56, MValue = 2
-------------------- Histogram --------------------
[130.413 ms ; 137.103 ms) | @@@@@@@@@@@@@@@
---------------------------------------------------

ConnectionPoolBenchmark.ConcurrentGetConnectionFromConnectionPoolAsync: Job-PRFRPX(Runtime=.NET Core 3.1) [x=1000000]
Runtime = .NET Core 3.1.0 (CoreCLR 4.700.19.56402, CoreFX 4.700.19.56404), X64 RyuJIT; GC = Concurrent Workstation
Mean = 289.6903 ms, StdErr = 1.1602 ms (0.40%); N = 15, StdDev = 4.4934 ms
Min = 282.8318 ms, Q1 = 286.6009 ms, Median = 289.5104 ms, Q3 = 294.4427 ms, Max = 296.9260 ms
IQR = 7.8418 ms, LowerFence = 274.8382 ms, UpperFence = 306.2054 ms
ConfidenceInterval = [284.8865 ms; 294.4940 ms] (CI 99.9%), Margin = 4.8037 ms (1.66% of Mean)
Skewness = 0.14, Kurtosis = 1.55, MValue = 2
-------------------- Histogram --------------------
[281.238 ms ; 289.200 ms) | @@@@@@@
[289.200 ms ; 298.520 ms) | @@@@@@@@
---------------------------------------------------
```

</p>
</details>

</p>
</details>

<details><summary>Click here to see CookedRabbit.Core ChannelPool results!</summary>
<p>


``` ini
// * Summary *

BenchmarkDotNet=v0.12.0, OS=Windows 10.0.17763.914 (1809/October2018Update/Redstone5)
Intel Core i7-8700K CPU 3.70GHz (Coffee Lake), 1 CPU, 12 logical and 6 physical cores
.NET Core SDK=3.1.100
  [Host]     : .NET Core 3.1.0 (CoreCLR 4.700.19.56402, CoreFX 4.700.19.56404), X64 RyuJIT
  Job-PRFRPX : .NET Core 3.1.0 (CoreCLR 4.700.19.56402, CoreFX 4.700.19.56404), X64 RyuJIT

Runtime=.NET Core 3.1

|                                              Method |       x |            Mean |         Error |        StdDev |          Median | Ratio | RatioSD | Completed Work Items | Lock Contentions |     Gen 0 |     Gen 1 | Gen 2 |  Allocated |
|---------------------------------------------------- |-------- |----------------:|--------------:|--------------:|----------------:|------:|--------:|---------------------:|-----------------:|----------:|----------:|------:|-----------:|
|                        CreateConnectionsAndChannels |     100 |   227,649.36 us |  4,493.707 us |  7,256.499 us |   226,476.45 us | 1.000 |    0.00 |             307.3333 |          18.3333 | 1666.6667 |  333.3333 |     - | 10764987 B |
| CreateChannelsWithConnectionFromConnectionPoolAsync |     100 |    38,729.22 us |  1,189.614 us |  3,470.161 us |    36,937.64 us | 0.169 |    0.01 |               1.9167 |           0.0833 |   83.3333 |         - |     - |   630047 B |
|                      GetChannelFromChannelPoolAsync |     100 |        30.83 us |      0.218 us |      0.182 us |        30.75 us | 0.000 |    0.00 |               0.0001 |                - |         - |         - |     - |          - |
|            ConcurrentGetChannelFromChannelPoolAsync |     100 |       170.30 us |      1.533 us |      1.434 us |       170.81 us | 0.001 |    0.00 |               4.2637 |           0.0386 |         - |         - |     - |     1425 B |
|                                                     |         |                 |               |               |                 |       |         |                      |                  |           |           |       |            |
|                        CreateConnectionsAndChannels |     500 | 1,120,014.29 us | 22,365.097 us | 21,965.515 us | 1,126,787.25 us | 1.000 |    0.00 |            1020.0000 |           4.0000 | 8000.0000 | 2000.0000 |     - | 53822592 B |
| CreateChannelsWithConnectionFromConnectionPoolAsync |     500 |   196,764.32 us |  6,824.841 us | 20,016.076 us |   186,114.90 us | 0.171 |    0.01 |               7.3333 |           3.3333 |  333.3333 |         - |     - |  3149840 B |
|                      GetChannelFromChannelPoolAsync |     500 |       157.37 us |      2.419 us |      2.263 us |       157.43 us | 0.000 |    0.00 |               0.0032 |           0.0010 |         - |         - |     - |        1 B |
|            ConcurrentGetChannelFromChannelPoolAsync |     500 |       746.49 us |      1.279 us |      0.999 us |       746.43 us | 0.001 |    0.00 |               6.3926 |           0.1201 |         - |         - |     - |     2794 B |
|                                                     |         |                 |               |               |                 |       |         |                      |                  |           |           |       |            |
|                      GetChannelFromChannelPoolAsync |    5000 |     1,577.04 us |     26.763 us |     23.725 us |     1,577.45 us |     ? |       ? |               0.0254 |                - |         - |         - |     - |       12 B |
|            ConcurrentGetChannelFromChannelPoolAsync |    5000 |     8,262.86 us |     57.424 us |     53.714 us |     8,246.68 us |     ? |       ? |              15.9063 |           7.3125 |         - |         - |     - |     8276 B |
|                                                     |         |                 |               |               |                 |       |         |                      |                  |           |           |       |            |
|                      GetChannelFromChannelPoolAsync |  500000 |   158,792.03 us |  2,522.553 us |  2,236.178 us |   158,764.98 us |     ? |       ? |               2.2500 |           0.7500 |         - |         - |     - |      718 B |
|            ConcurrentGetChannelFromChannelPoolAsync |  500000 |   737,580.97 us |  3,906.604 us |  3,654.240 us |   738,151.10 us |     ? |       ? |            2484.0000 |         140.0000 |         - |         - |     - |  1370176 B |
|                                                     |         |                 |               |               |                 |       |         |                      |                  |           |           |       |            |
|                      GetChannelFromChannelPoolAsync | 1000000 |   308,008.75 us |  2,026.544 us |  1,796.479 us |   307,123.75 us |     ? |       ? |               2.0000 |                - |         - |         - |     - |       88 B |
|            ConcurrentGetChannelFromChannelPoolAsync | 1000000 | 1,510,215.91 us | 20,422.986 us | 19,103.673 us | 1,505,807.70 us |     ? |       ? |            3312.0000 |         274.0000 |         - |         - |     - |  1824960 B |
```

<details><summary>Click here to see the outliers and legend!</summary>
<p>

```ini
// * Warnings *
BaselineCustomAnalyzer
  Summary -> A question mark '?' symbol indicates that it was not possible to compute the (Ratio, RatioSD) column(s) because the baseline value is too close to zero.

// * Hints *
Outliers
  ChannelPoolBenchmark.CreateConnectionsAndChannels: Runtime=.NET Core 3.1                        -> 1 outlier  was  removed (246.95 ms)
  ChannelPoolBenchmark.CreateChannelsWithConnectionFromConnectionPoolAsync: Runtime=.NET Core 3.1 -> 2 outliers were removed (50.62 ms, 51.00 ms)
  ChannelPoolBenchmark.GetChannelFromChannelPoolAsync: Runtime=.NET Core 3.1                      -> 2 outliers were removed (31.74 us, 31.80 us)
  ChannelPoolBenchmark.CreateChannelsWithConnectionFromConnectionPoolAsync: Runtime=.NET Core 3.1 -> 1 outlier  was  removed (269.58 ms)
  ChannelPoolBenchmark.ConcurrentGetChannelFromChannelPoolAsync: Runtime=.NET Core 3.1            -> 3 outliers were removed (755.21 us..758.01 us)
  ChannelPoolBenchmark.GetChannelFromChannelPoolAsync: Runtime=.NET Core 3.1                      -> 1 outlier  was  removed (1.79 ms)
  ChannelPoolBenchmark.GetChannelFromChannelPoolAsync: Runtime=.NET Core 3.1                      -> 1 outlier  was  removed (165.96 ms)
  ChannelPoolBenchmark.ConcurrentGetChannelFromChannelPoolAsync: Runtime=.NET Core 3.1            -> 1 outlier  was  detected (730.21 ms)
  ChannelPoolBenchmark.GetChannelFromChannelPoolAsync: Runtime=.NET Core 3.1                      -> 1 outlier  was  removed (316.83 ms)

// * Legends *
  x                    : Value of the 'x' parameter
  Mean                 : Arithmetic mean of all measurements
  Error                : Half of 99.9% confidence interval
  StdDev               : Standard deviation of all measurements
  Median               : Value separating the higher half of all measurements (50th percentile)
  Ratio                : Mean of the ratio distribution ([Current]/[Baseline])
  RatioSD              : Standard deviation of the ratio distribution ([Current]/[Baseline])
  Completed Work Items : The number of work items that have been processed in ThreadPool (per single operation)
  Lock Contentions     : The number of times there was contention upon trying to take a Monitor's lock (per single operation)
  Gen 0                : GC Generation 0 collects per 1000 operations
  Gen 1                : GC Generation 1 collects per 1000 operations
  Gen 2                : GC Generation 2 collects per 1000 operations
  Allocated            : Allocated memory per single operation (managed only, inclusive, 1KB = 1024B)
  1 us                 : 1 Microsecond (0.000001 sec)
```

</p>
</details>

<details><summary>Click here to see the Detailed results!</summary>
<p>

```ini
// * Detailed results *
ChannelPoolBenchmark.CreateConnectionsAndChannels: Job-PRFRPX(Runtime=.NET Core 3.1) [x=100]
Runtime = .NET Core 3.1.0 (CoreCLR 4.700.19.56402, CoreFX 4.700.19.56404), X64 RyuJIT; GC = Concurrent Workstation
Mean = 227.6494 ms, StdErr = 1.2445 ms (0.55%); N = 34, StdDev = 7.2565 ms
Min = 215.2687 ms, Q1 = 223.8478 ms, Median = 226.4764 ms, Q3 = 232.1172 ms, Max = 242.0588 ms
IQR = 8.2694 ms, LowerFence = 211.4437 ms, UpperFence = 244.5213 ms
ConfidenceInterval = [223.1557 ms; 232.1431 ms] (CI 99.9%), Margin = 4.4937 ms (1.97% of Mean)
Skewness = 0.34, Kurtosis = 2.39, MValue = 2
-------------------- Histogram --------------------
[213.309 ms ; 217.755 ms) | @@@
[217.755 ms ; 222.980 ms) | @@@@@
[222.980 ms ; 228.342 ms) | @@@@@@@@@@
[228.342 ms ; 236.868 ms) | @@@@@@@@@@@@
[236.868 ms ; 244.019 ms) | @@@@
---------------------------------------------------

ChannelPoolBenchmark.CreateChannelsWithConnectionFromConnectionPoolAsync: Job-PRFRPX(Runtime=.NET Core 3.1) [x=100]
Runtime = .NET Core 3.1.0 (CoreCLR 4.700.19.56402, CoreFX 4.700.19.56404), X64 RyuJIT; GC = Concurrent Workstation
Mean = 38.7292 ms, StdErr = 0.3505 ms (0.91%); N = 98, StdDev = 3.4702 ms
Min = 34.8618 ms, Q1 = 36.1532 ms, Median = 36.9376 ms, Q3 = 41.0575 ms, Max = 48.5377 ms
IQR = 4.9043 ms, LowerFence = 28.7967 ms, UpperFence = 48.4140 ms
ConfidenceInterval = [37.5396 ms; 39.9188 ms] (CI 99.9%), Margin = 1.1896 ms (3.07% of Mean)
Skewness = 1.08, Kurtosis = 2.85, MValue = 2.33
-------------------- Histogram --------------------
[34.203 ms ; 35.724 ms) | @@
[35.724 ms ; 37.041 ms) | @@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@
[37.041 ms ; 38.435 ms) | @@@@@@@@@@@
[38.435 ms ; 39.971 ms) | @@@@@@
[39.971 ms ; 41.288 ms) | @@@@@@@@
[41.288 ms ; 43.031 ms) | @@@@@@
[43.031 ms ; 44.810 ms) | @@@@@@@@@
[44.810 ms ; 45.723 ms) |
[45.723 ms ; 47.879 ms) | @@@@@@
[47.879 ms ; 49.196 ms) | @
---------------------------------------------------

ChannelPoolBenchmark.GetChannelFromChannelPoolAsync: Job-PRFRPX(Runtime=.NET Core 3.1) [x=100]
Runtime = .NET Core 3.1.0 (CoreCLR 4.700.19.56402, CoreFX 4.700.19.56404), X64 RyuJIT; GC = Concurrent Workstation
Mean = 30.8329 us, StdErr = 0.0504 us (0.16%); N = 13, StdDev = 0.1818 us
Min = 30.6252 us, Q1 = 30.7292 us, Median = 30.7530 us, Q3 = 30.8972 us, Max = 31.2911 us
IQR = 0.1680 us, LowerFence = 30.4773 us, UpperFence = 31.1492 us
ConfidenceInterval = [30.6151 us; 31.0506 us] (CI 99.9%), Margin = 0.2178 us (0.71% of Mean)
Skewness = 1.29, Kurtosis = 3.65, MValue = 2
-------------------- Histogram --------------------
[30.557 us ; 31.359 us) | @@@@@@@@@@@@@
---------------------------------------------------

ChannelPoolBenchmark.ConcurrentGetChannelFromChannelPoolAsync: Job-PRFRPX(Runtime=.NET Core 3.1) [x=100]
Runtime = .NET Core 3.1.0 (CoreCLR 4.700.19.56402, CoreFX 4.700.19.56404), X64 RyuJIT; GC = Concurrent Workstation
Mean = 170.2969 us, StdErr = 0.3702 us (0.22%); N = 15, StdDev = 1.4339 us
Min = 168.6783 us, Q1 = 168.8755 us, Median = 170.8119 us, Q3 = 171.1829 us, Max = 172.5438 us
IQR = 2.3074 us, LowerFence = 165.4143 us, UpperFence = 174.6441 us
ConfidenceInterval = [168.7640 us; 171.8299 us] (CI 99.9%), Margin = 1.5329 us (0.90% of Mean)
Skewness = 0.2, Kurtosis = 1.32, MValue = 2
-------------------- Histogram --------------------
[168.170 us ; 173.053 us) | @@@@@@@@@@@@@@@
---------------------------------------------------

ChannelPoolBenchmark.CreateConnectionsAndChannels: Job-PRFRPX(Runtime=.NET Core 3.1) [x=500]
Runtime = .NET Core 3.1.0 (CoreCLR 4.700.19.56402, CoreFX 4.700.19.56404), X64 RyuJIT; GC = Concurrent Workstation
Mean = 1.1200 s, StdErr = 0.0055 s (0.49%); N = 16, StdDev = 0.0220 s
Min = 1.0856 s, Q1 = 1.0963 s, Median = 1.1268 s, Q3 = 1.1375 s, Max = 1.1492 s
IQR = 0.0412 s, LowerFence = 1.0346 s, UpperFence = 1.1992 s
ConfidenceInterval = [1.0976 s; 1.1424 s] (CI 99.9%), Margin = 0.0224 s (2.00% of Mean)
Skewness = -0.31, Kurtosis = 1.46, MValue = 2
-------------------- Histogram --------------------
[1.081 s ; 1.118 s) | @@@@@@
[1.118 s ; 1.157 s) | @@@@@@@@@@
---------------------------------------------------

ChannelPoolBenchmark.CreateChannelsWithConnectionFromConnectionPoolAsync: Job-PRFRPX(Runtime=.NET Core 3.1) [x=500]
Runtime = .NET Core 3.1.0 (CoreCLR 4.700.19.56402, CoreFX 4.700.19.56404), X64 RyuJIT; GC = Concurrent Workstation
Mean = 196.7643 ms, StdErr = 2.0117 ms (1.02%); N = 99, StdDev = 20.0161 ms
Min = 176.7790 ms, Q1 = 180.0581 ms, Median = 186.1149 ms, Q3 = 214.1987 ms, Max = 255.4361 ms
IQR = 34.1407 ms, LowerFence = 128.8471 ms, UpperFence = 265.4097 ms
ConfidenceInterval = [189.9395 ms; 203.5892 ms] (CI 99.9%), Margin = 6.8248 ms (3.47% of Mean)
Skewness = 0.85, Kurtosis = 2.48, MValue = 2.64
-------------------- Histogram --------------------
[172.993 ms ; 177.866 ms) | @@
[177.866 ms ; 185.438 ms) | @@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@
[185.438 ms ; 193.504 ms) | @@@@@@@@@
[193.504 ms ; 197.918 ms) |
[197.918 ms ; 205.490 ms) | @@@@@@@@@@@
[205.490 ms ; 216.836 ms) | @@@@@@@@@
[216.836 ms ; 227.700 ms) | @@@@@@@@@@@@
[227.700 ms ; 235.423 ms) | @@@@@@
[235.423 ms ; 245.910 ms) | @@
[245.910 ms ; 251.650 ms) |
[251.650 ms ; 259.222 ms) | @
---------------------------------------------------

ChannelPoolBenchmark.GetChannelFromChannelPoolAsync: Job-PRFRPX(Runtime=.NET Core 3.1) [x=500]
Runtime = .NET Core 3.1.0 (CoreCLR 4.700.19.56402, CoreFX 4.700.19.56404), X64 RyuJIT; GC = Concurrent Workstation
Mean = 157.3706 us, StdErr = 0.5843 us (0.37%); N = 15, StdDev = 2.2631 us
Min = 154.1693 us, Q1 = 155.0606 us, Median = 157.4287 us, Q3 = 159.9232 us, Max = 160.2313 us
IQR = 4.8626 us, LowerFence = 147.7668 us, UpperFence = 167.2171 us
ConfidenceInterval = [154.9513 us; 159.7900 us] (CI 99.9%), Margin = 2.4194 us (1.54% of Mean)
Skewness = -0.02, Kurtosis = 1.32, MValue = 2
-------------------- Histogram --------------------
[153.366 us ; 161.034 us) | @@@@@@@@@@@@@@@
---------------------------------------------------

ChannelPoolBenchmark.ConcurrentGetChannelFromChannelPoolAsync: Job-PRFRPX(Runtime=.NET Core 3.1) [x=500]
Runtime = .NET Core 3.1.0 (CoreCLR 4.700.19.56402, CoreFX 4.700.19.56404), X64 RyuJIT; GC = Concurrent Workstation
Mean = 746.4872 us, StdErr = 0.2883 us (0.04%); N = 12, StdDev = 0.9988 us
Min = 745.4849 us, Q1 = 745.6996 us, Median = 746.4302 us, Q3 = 746.7267 us, Max = 749.0396 us
IQR = 1.0271 us, LowerFence = 744.1590 us, UpperFence = 748.2672 us
ConfidenceInterval = [745.2079 us; 747.7665 us] (CI 99.9%), Margin = 1.2793 us (0.17% of Mean)
Skewness = 1.19, Kurtosis = 3.87, MValue = 2
-------------------- Histogram --------------------
[745.103 us ; 749.421 us) | @@@@@@@@@@@@
---------------------------------------------------

ChannelPoolBenchmark.GetChannelFromChannelPoolAsync: Job-PRFRPX(Runtime=.NET Core 3.1) [x=5000]
Runtime = .NET Core 3.1.0 (CoreCLR 4.700.19.56402, CoreFX 4.700.19.56404), X64 RyuJIT; GC = Concurrent Workstation
Mean = 1.5770 ms, StdErr = 0.0063 ms (0.40%); N = 14, StdDev = 0.0237 ms
Min = 1.5424 ms, Q1 = 1.5594 ms, Median = 1.5775 ms, Q3 = 1.5921 ms, Max = 1.6124 ms
IQR = 0.0327 ms, LowerFence = 1.5104 ms, UpperFence = 1.6411 ms
ConfidenceInterval = [1.5503 ms; 1.6038 ms] (CI 99.9%), Margin = 0.0268 ms (1.70% of Mean)
Skewness = 0.11, Kurtosis = 1.62, MValue = 2
-------------------- Histogram --------------------
[1.534 ms ; 1.579 ms) | @@@@@@@
[1.579 ms ; 1.621 ms) | @@@@@@@
---------------------------------------------------

ChannelPoolBenchmark.ConcurrentGetChannelFromChannelPoolAsync: Job-PRFRPX(Runtime=.NET Core 3.1) [x=5000]
Runtime = .NET Core 3.1.0 (CoreCLR 4.700.19.56402, CoreFX 4.700.19.56404), X64 RyuJIT; GC = Concurrent Workstation
Mean = 8.2629 ms, StdErr = 0.0139 ms (0.17%); N = 15, StdDev = 0.0537 ms
Min = 8.1932 ms, Q1 = 8.2189 ms, Median = 8.2467 ms, Q3 = 8.3122 ms, Max = 8.3557 ms
IQR = 0.0933 ms, LowerFence = 8.0790 ms, UpperFence = 8.4521 ms
ConfidenceInterval = [8.2054 ms; 8.3203 ms] (CI 99.9%), Margin = 0.0574 ms (0.69% of Mean)
Skewness = 0.31, Kurtosis = 1.54, MValue = 2
-------------------- Histogram --------------------
[8.174 ms ; 8.375 ms) | @@@@@@@@@@@@@@@
---------------------------------------------------

ChannelPoolBenchmark.GetChannelFromChannelPoolAsync: Job-PRFRPX(Runtime=.NET Core 3.1) [x=500000]
Runtime = .NET Core 3.1.0 (CoreCLR 4.700.19.56402, CoreFX 4.700.19.56404), X64 RyuJIT; GC = Concurrent Workstation
Mean = 158.7920 ms, StdErr = 0.5976 ms (0.38%); N = 14, StdDev = 2.2362 ms
Min = 155.0506 ms, Q1 = 157.1224 ms, Median = 158.7650 ms, Q3 = 159.6354 ms, Max = 163.9811 ms
IQR = 2.5130 ms, LowerFence = 153.3529 ms, UpperFence = 163.4050 ms
ConfidenceInterval = [156.2695 ms; 161.3146 ms] (CI 99.9%), Margin = 2.5226 ms (1.59% of Mean)
Skewness = 0.5, Kurtosis = 2.97, MValue = 2
-------------------- Histogram --------------------
[154.239 ms ; 160.512 ms) | @@@@@@@@@@@@
[160.512 ms ; 164.793 ms) | @@
---------------------------------------------------

ChannelPoolBenchmark.ConcurrentGetChannelFromChannelPoolAsync: Job-PRFRPX(Runtime=.NET Core 3.1) [x=500000]
Runtime = .NET Core 3.1.0 (CoreCLR 4.700.19.56402, CoreFX 4.700.19.56404), X64 RyuJIT; GC = Concurrent Workstation
Mean = 737.5810 ms, StdErr = 0.9435 ms (0.13%); N = 15, StdDev = 3.6542 ms
Min = 730.2129 ms, Q1 = 735.9001 ms, Median = 738.1511 ms, Q3 = 739.4721 ms, Max = 744.7653 ms
IQR = 3.5720 ms, LowerFence = 730.5421 ms, UpperFence = 744.8301 ms
ConfidenceInterval = [733.6744 ms; 741.4876 ms] (CI 99.9%), Margin = 3.9066 ms (0.53% of Mean)
Skewness = -0.27, Kurtosis = 2.68, MValue = 2
-------------------- Histogram --------------------
[728.916 ms ; 745.629 ms) | @@@@@@@@@@@@@@@
---------------------------------------------------

ChannelPoolBenchmark.GetChannelFromChannelPoolAsync: Job-PRFRPX(Runtime=.NET Core 3.1) [x=1000000]
Runtime = .NET Core 3.1.0 (CoreCLR 4.700.19.56402, CoreFX 4.700.19.56404), X64 RyuJIT; GC = Concurrent Workstation
Mean = 308.0088 ms, StdErr = 0.4801 ms (0.16%); N = 14, StdDev = 1.7965 ms
Min = 306.5162 ms, Q1 = 306.5895 ms, Median = 307.1237 ms, Q3 = 309.7579 ms, Max = 311.9153 ms
IQR = 3.1684 ms, LowerFence = 301.8369 ms, UpperFence = 314.5105 ms
ConfidenceInterval = [305.9822 ms; 310.0353 ms] (CI 99.9%), Margin = 2.0265 ms (0.66% of Mean)
Skewness = 0.88, Kurtosis = 2.22, MValue = 2
-------------------- Histogram --------------------
[305.864 ms ; 312.568 ms) | @@@@@@@@@@@@@@
---------------------------------------------------

ChannelPoolBenchmark.ConcurrentGetChannelFromChannelPoolAsync: Job-PRFRPX(Runtime=.NET Core 3.1) [x=1000000]
Runtime = .NET Core 3.1.0 (CoreCLR 4.700.19.56402, CoreFX 4.700.19.56404), X64 RyuJIT; GC = Concurrent Workstation
Mean = 1.5102 s, StdErr = 0.0049 s (0.33%); N = 15, StdDev = 0.0191 s
Min = 1.4769 s, Q1 = 1.4934 s, Median = 1.5058 s, Q3 = 1.5276 s, Max = 1.5365 s
IQR = 0.0342 s, LowerFence = 1.4421 s, UpperFence = 1.5789 s
ConfidenceInterval = [1.4898 s; 1.5306 s] (CI 99.9%), Margin = 0.0204 s (1.35% of Mean)
Skewness = -0.1, Kurtosis = 1.35, MValue = 2
-------------------- Histogram --------------------
[1.470 s ; 1.511 s) | @@@@@@@@
[1.511 s ; 1.543 s) | @@@@@@@
---------------------------------------------------
```

</p>
</details>

</p>
</details>

<details><summary>Click here to Utils.XorShift Results!</summary>
<p>

```ini
// * Summary *

BenchmarkDotNet=v0.12.0, OS=Windows 10.0.17763.914 (1809/October2018Update/Redstone5)
Intel Core i7-8700K CPU 3.70GHz (Coffee Lake), 1 CPU, 12 logical and 6 physical cores
.NET Core SDK=3.1.100
  [Host]     : .NET Core 3.1.0 (CoreCLR 4.700.19.56402, CoreFX 4.700.19.56404), X64 RyuJIT
  Job-MUNGBO : .NET Core 3.1.0 (CoreCLR 4.700.19.56402, CoreFX 4.700.19.56404), X64 RyuJIT

Runtime=.NET Core 3.1

|                         Method |      x |          Mean |        Error |       StdDev | Ratio |   Gen 0 |   Gen 1 |   Gen 2 | Allocated | Completed Work Items | Lock Contentions |
|------------------------------- |------- |--------------:|-------------:|-------------:|------:|--------:|--------:|--------:|----------:|---------------------:|-----------------:|
|          CreateRandomByteArray |    100 |   1,934.62 ns |     7.628 ns |     6.370 ns |  1.00 |  0.0648 |       - |       - |     408 B |               0.0000 |                - |
|       CreateXorRandomByteArray |    100 |      78.82 ns |     1.576 ns |     1.475 ns |  0.04 |  0.0204 |       - |       - |     128 B |               0.0000 |                - |
| CreateUnsafeXorRandomByteArray |    100 |      31.75 ns |     0.347 ns |     0.325 ns |  0.02 |  0.0204 |       - |       - |     128 B |               0.0000 |                - |
|                                |        |               |              |              |       |         |         |         |           |                      |                  |
|          CreateRandomByteArray |    500 |   4,302.83 ns |    41.711 ns |    34.831 ns |  1.00 |  0.1221 |       - |       - |     808 B |               0.0000 |                - |
|       CreateXorRandomByteArray |    500 |     393.81 ns |     7.621 ns |     7.485 ns |  0.09 |  0.0839 |       - |       - |     528 B |               0.0000 |                - |
| CreateUnsafeXorRandomByteArray |    500 |     137.54 ns |     2.087 ns |     1.952 ns |  0.03 |  0.0842 |       - |       - |     528 B |               0.0000 |                - |
|                                |        |               |              |              |       |         |         |         |           |                      |                  |
|          CreateRandomByteArray |   1000 |   7,308.81 ns |    64.291 ns |    60.138 ns |  1.00 |  0.2060 |       - |       - |    1304 B |               0.0000 |                - |
|          CreateRandomByteArray |   1000 |   7,264.11 ns |    83.620 ns |    78.218 ns |  0.99 |  0.2060 |       - |       - |    1304 B |               0.0000 |                - |
|       CreateXorRandomByteArray |   1000 |     791.60 ns |    15.847 ns |    27.755 ns |  0.11 |  0.1631 |       - |       - |    1024 B |               0.0000 |                - |
|       CreateXorRandomByteArray |   1000 |     774.75 ns |    13.572 ns |    12.695 ns |  0.11 |  0.1631 |       - |       - |    1024 B |               0.0000 |                - |
| CreateUnsafeXorRandomByteArray |   1000 |     276.47 ns |     5.035 ns |     4.710 ns |  0.04 |  0.1631 |       - |       - |    1024 B |               0.0000 |                - |
| CreateUnsafeXorRandomByteArray |   1000 |     275.98 ns |     3.834 ns |     3.586 ns |  0.04 |  0.1631 |       - |       - |    1024 B |               0.0000 |                - |
|                                |        |               |              |              |       |         |         |         |           |                      |                  |
|          CreateRandomByteArray | 100000 | 590,970.86 ns | 1,959.599 ns | 1,636.354 ns |  1.00 | 30.2734 | 30.2734 | 30.2734 |  100305 B |               0.0020 |                - |
|       CreateXorRandomByteArray | 100000 |  91,365.62 ns |   930.069 ns |   824.482 ns |  0.15 | 31.1279 | 31.1279 | 31.1279 |  100024 B |               0.0002 |                - |
| CreateUnsafeXorRandomByteArray | 100000 |  42,577.86 ns |   614.341 ns |   574.655 ns |  0.07 | 31.1890 | 31.1890 | 31.1890 |  100025 B |               0.0001 |                - |
```

</p>
</details>
