# New EncryptionProvider based on Net5.0.

Byte structure is straight forward if you need to use alternative decryption mechanism.
```
  12 bytes       16 bytes    n bytes up to int.IntMax - 28 bytes.
[ Nonce / IV ][ Tag / MAC ][               Ciphertext           ]
```

### Benchmark
```ini
// * Summary *

BenchmarkDotNet=v0.13.0, OS=Windows 10.0.19042.1110 (20H2/October2020Update)
Intel Core i9-10900KF CPU 3.70GHz, 1 CPU, 20 logical and 10 physical cores
.NET SDK=5.0.302
  [Host]   : .NET 5.0.8 (5.0.821.31504), X64 RyuJIT
  .NET 5.0 : .NET 5.0.8 (5.0.821.31504), X64 RyuJIT

Job=.NET 5.0  Runtime=.NET 5.0

|                     Method |     Mean |     Error |    StdDev | Ratio | RatioSD |  Gen 0 |  Gen 1 |  Gen 2 | Allocated |
|--------------------------- |---------:|----------:|----------:|------:|--------:|-------:|-------:|-------:|----------:|
|                Encrypt_1KB | 1.416 us | 0.0049 us | 0.0043 us |  1.00 |    0.00 | 0.1144 |      - |      - |      1 KB |
|                Encrypt_2KB | 1.766 us | 0.0063 us | 0.0059 us |  1.25 |    0.01 | 0.2136 |      - |      - |      2 KB |
|                Encrypt_4KB | 2.448 us | 0.0117 us | 0.0110 us |  1.73 |    0.01 | 0.4082 |      - |      - |      4 KB |
|                Encrypt_8KB | 3.682 us | 0.0113 us | 0.0106 us |  2.60 |    0.01 | 0.8011 |      - |      - |      8 KB |
|        EncryptToStream_1KB | 2.128 us | 0.0400 us | 0.0428 us |  1.50 |    0.04 | 0.1526 | 0.0725 | 0.0038 |      2 KB |
|        EncryptToStream_2KB | 2.506 us | 0.0501 us | 0.0557 us |  1.75 |    0.03 | 0.2480 | 0.1221 |      - |      3 KB |
|        EncryptToStream_4KB | 3.276 us | 0.0134 us | 0.0118 us |  2.31 |    0.01 | 0.4463 | 0.2213 |      - |      5 KB |
|        EncryptToStream_8KB | 4.730 us | 0.0907 us | 0.0848 us |  3.34 |    0.06 | 0.8392 | 0.4120 |      - |      9 KB |
|                Decrypt_1KB | 1.205 us | 0.0035 us | 0.0033 us |  0.85 |    0.00 | 0.1125 |      - |      - |      1 KB |
|                Decrypt_2KB | 1.520 us | 0.0059 us | 0.0049 us |  1.07 |    0.00 | 0.2098 |      - |      - |      2 KB |
|                Decrypt_4KB | 2.151 us | 0.0063 us | 0.0059 us |  1.52 |    0.01 | 0.4044 |      - |      - |      4 KB |
|                Decrypt_8KB | 3.383 us | 0.0082 us | 0.0076 us |  2.39 |    0.01 | 0.7973 |      - |      - |      8 KB |
|        DecryptToStream_1KB | 1.894 us | 0.0372 us | 0.0470 us |  1.35 |    0.04 | 0.1488 | 0.0725 |      - |      2 KB |
|        DecryptToStream_2KB | 2.294 us | 0.0421 us | 0.0374 us |  1.62 |    0.03 | 0.2480 | 0.1183 |      - |      3 KB |
|        DecryptToStream_4KB | 2.983 us | 0.0099 us | 0.0077 us |  2.11 |    0.01 | 0.4425 | 0.2213 |      - |      5 KB |
|        DecryptToStream_8KB | 4.347 us | 0.0417 us | 0.0326 us |  3.07 |    0.03 | 0.8392 | 0.4196 |      - |      9 KB |
|       EncryptDecryptTo_8KB | 7.082 us | 0.0234 us | 0.0219 us |  5.00 |    0.02 | 1.5945 |      - |      - |     16 KB |
| EncryptDecryptToStream_8KB | 9.144 us | 0.0543 us | 0.0481 us |  6.46 |    0.04 | 1.6785 | 0.5493 |      - |     17 KB |
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
