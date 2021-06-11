# New EncryptionProvider based on Net5.0.

Byte structure is straight forward if you need to use alternative decryption mechanism.
```
  12 bytes       16 bytes    n bytes up to int.IntMax - 28 bytes.
[ Nonce / IV ][ Tag / MAC ][               Ciphertext           ]
```

### Benchmark
```ini
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

### Older Reference Code
#### Encrypt Byte Allocation Version

```csharp
var nonce = new byte[AesGcm.NonceByteSizes.MaxSize]; // MaxSize = 12
_rng.GetNonZeroBytes(nonce);

var encryptedBytes = new byte[data.Length];
var tag = new byte[AesGcm.TagByteSizes.MaxSize]; // MaxSize = 16

aes.Encrypt(nonce, data, encryptedBytes, tag);

var encryptedData = new byte[nonce.Length + tag.Length + enryptedBytes.Length];
Buffer.BlockCopy(nonce, 0, encryptedData, 0, nonce.Length);
Buffer.BlockCopy(tag, 0, encryptedData, nonce.Length, tag.Length);
Buffer.BlockCopy(encryptedBytes, 0, encryptedData, nonce.Length + tag.Length, encryptedBytes.Length);
```

#### Decrypt Byte Allocation Version
```csharp
var encryptedBytes = encryptedData.ToArray();
var nonce = new byte[AesGcm.NonceByteSizes.MaxSize]; // MaxSize = 12
var tag = new byte[AesGcm.TagByteSizes.MaxSize]; // MaxSize = 16
var ciphertext = new byte[encryptedData.Length - AesGcm.NonceByteSizes.MaxSize - AesGcm.TagByteSizes.MaxSize];
var decryptedBytes = new byte[ciphertext.Length];

// Isolate nonce and tag from ciphertext.
Buffer.BlockCopy(encryptedBytes, 0, nonce, 0, nonce.Length);
Buffer.BlockCopy(encryptedBytes, nonce.Length, tag, 0, tag.Length);
Buffer.BlockCopy(encryptedBytes, nonce.Length + tag.Length, ciphertext, 0, ciphertext.Length);
```
