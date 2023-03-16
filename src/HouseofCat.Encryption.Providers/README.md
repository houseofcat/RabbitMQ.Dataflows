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

|                     Method |     Mean |     Error |    StdDev | Ratio | RatioSD |  Gen 0 | Gen 1 | Gen 2 | Allocated |
|--------------------------- |---------:|----------:|----------:|------:|--------:|-------:|------:|------:|----------:|
|                Encrypt_1KB | 1.411 us | 0.0047 us | 0.0044 us |  1.00 |    0.00 | 0.1144 |     - |     - |      1 KB |
|                Encrypt_2KB | 1.777 us | 0.0045 us | 0.0042 us |  1.26 |    0.01 | 0.2136 |     - |     - |      2 KB |
|                Encrypt_4KB | 2.432 us | 0.0087 us | 0.0077 us |  1.72 |    0.01 | 0.4082 |     - |     - |      4 KB |
|                Encrypt_8KB | 3.694 us | 0.0122 us | 0.0115 us |  2.62 |    0.01 | 0.8011 |     - |     - |      8 KB |
|        EncryptToStream_1KB | 1.417 us | 0.0046 us | 0.0041 us |  1.00 |    0.00 | 0.1221 |     - |     - |      1 KB |
|        EncryptToStream_2KB | 1.769 us | 0.0049 us | 0.0041 us |  1.25 |    0.01 | 0.2193 |     - |     - |      2 KB |
|        EncryptToStream_4KB | 2.465 us | 0.0115 us | 0.0107 us |  1.75 |    0.01 | 0.4158 |     - |     - |      4 KB |
|        EncryptToStream_8KB | 3.708 us | 0.0108 us | 0.0101 us |  2.63 |    0.01 | 0.8049 |     - |     - |      8 KB |
|                Decrypt_1KB | 1.214 us | 0.0039 us | 0.0037 us |  0.86 |    0.00 | 0.1125 |     - |     - |      1 KB |
|                Decrypt_2KB | 1.521 us | 0.0039 us | 0.0037 us |  1.08 |    0.00 | 0.2098 |     - |     - |      2 KB |
|                Decrypt_4KB | 2.151 us | 0.0047 us | 0.0039 us |  1.52 |    0.01 | 0.4044 |     - |     - |      4 KB |
|                Decrypt_8KB | 3.374 us | 0.0074 us | 0.0069 us |  2.39 |    0.01 | 0.7973 |     - |     - |      8 KB |
|        DecryptToStream_1KB | 1.198 us | 0.0049 us | 0.0046 us |  0.85 |    0.00 | 0.1183 |     - |     - |      1 KB |
|        DecryptToStream_2KB | 1.517 us | 0.0039 us | 0.0037 us |  1.07 |    0.00 | 0.2155 |     - |     - |      2 KB |
|        DecryptToStream_4KB | 2.146 us | 0.0100 us | 0.0094 us |  1.52 |    0.01 | 0.4120 |     - |     - |      4 KB |
|        DecryptToStream_8KB | 3.372 us | 0.0114 us | 0.0101 us |  2.39 |    0.01 | 0.8049 |     - |     - |      8 KB |
|       EncryptDecryptTo_8KB | 7.116 us | 0.0219 us | 0.0183 us |  5.04 |    0.02 | 1.5945 |     - |     - |     16 KB |
| EncryptDecryptToStream_8KB | 7.422 us | 0.0183 us | 0.0162 us |  5.26 |    0.02 | 1.6098 |     - |     - |     16 KB |
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
