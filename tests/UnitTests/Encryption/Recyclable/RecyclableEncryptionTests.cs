using HouseofCat.Encryption;
using HouseofCat.Hashing;
using HouseofCat.Hashing.Argon;
using System.Text;
using Xunit.Abstractions;

namespace Encryption.Recyclable;

public class RecyclableEncryptionTests
{
    private readonly ITestOutputHelper _output;
    private readonly IHashingProvider _hashingProvider;
    private const string Passphrase = "SuperNintendoHadTheBestZelda";
    private const string Salt = "SegaGenesisIsTheBestConsole";
    private static readonly byte[] _data = new byte[] { 0xFF, 0x00, 0xAA, 0xFF, 0x00, 0x00, 0xFF, 0xAA, 0x00, 0xFF, 0x00, 0xFF };

    public RecyclableEncryptionTests(ITestOutputHelper output)
    {
        _output = output;
        _hashingProvider = new Argon2ID_HashingProvider();
    }

    [Fact]
    public async Task Aes256_GCM()
    {
        var hashKey = await _hashingProvider
            .GetHashKeyAsync(Passphrase, Salt, 32);

        _output.WriteLine(Encoding.UTF8.GetString(hashKey));
        _output.WriteLine($"HashKey: {Encoding.UTF8.GetString(hashKey)}");

        var encryptionProvider = new RecyclableAesGcmEncryptionProvider(hashKey);

        var encryptedData = encryptionProvider.Encrypt(_data);
        _output.WriteLine($"Encrypted: {Encoding.UTF8.GetString(encryptedData.Span)}");

        var decryptedBytes = encryptionProvider.Decrypt(encryptedData);
        var data = Encoding.UTF8.GetString(_data);
        var decryptedData = Encoding.UTF8.GetString(decryptedBytes.Span);
        _output.WriteLine($"Data: {data}");
        _output.WriteLine($"Decrypted: {decryptedData}");

        Assert.Equal(data, decryptedData);
    }

    [Fact]
    public void Aes256_GCM_Stream()
    {
        var hashKey = _hashingProvider.GetHashKey(Passphrase, Salt, 32);

        _output.WriteLine(Encoding.UTF8.GetString(hashKey));
        _output.WriteLine($"HashKey: {Encoding.UTF8.GetString(hashKey)}");

        var encryptionProvider = new RecyclableAesGcmEncryptionProvider(hashKey);
        var encryptedStream = encryptionProvider.Encrypt(new MemoryStream(_data));
        var decryptedStream = encryptionProvider.Decrypt(encryptedStream);

        Assert.Equal(_data, decryptedStream.ToArray());
    }

    [Fact]
    public async Task Aes256_GCM_StreamAsync()
    {
        var hashKey = await _hashingProvider
            .GetHashKeyAsync(Passphrase, Salt, 32);

        _output.WriteLine(Encoding.UTF8.GetString(hashKey));
        _output.WriteLine($"HashKey: {Encoding.UTF8.GetString(hashKey)}");

        var encryptionProvider = new RecyclableAesGcmEncryptionProvider(hashKey);
        var encryptedStream = await encryptionProvider.EncryptAsync(new MemoryStream(_data));
        var decryptedStream = encryptionProvider.Decrypt(encryptedStream);

        Assert.Equal(_data, decryptedStream.ToArray());
    }

    [Fact]
    public async Task Aes192_GCM()
    {
        var hashKey = await _hashingProvider
            .GetHashKeyAsync(Passphrase, Salt, 24);

        _output.WriteLine(Encoding.UTF8.GetString(hashKey));
        _output.WriteLine($"HashKey: {Encoding.UTF8.GetString(hashKey)}");

        var encryptionProvider = new RecyclableAesGcmEncryptionProvider(hashKey);

        var encryptedData = encryptionProvider.Encrypt(_data);
        _output.WriteLine($"Encrypted: {Encoding.UTF8.GetString(encryptedData.Span)}");

        var decryptedBytes = encryptionProvider.Decrypt(encryptedData);
        var data = Encoding.UTF8.GetString(_data);
        var decryptedData = Encoding.UTF8.GetString(decryptedBytes.Span);
        _output.WriteLine($"Data: {data}");
        _output.WriteLine($"Decrypted: {decryptedData}");

        Assert.Equal(data, decryptedData);
    }

    [Fact]
    public void Aes192_GCM_Stream()
    {
        var hashKey = _hashingProvider
            .GetHashKey(Passphrase, Salt, 24);

        _output.WriteLine(Encoding.UTF8.GetString(hashKey));
        _output.WriteLine($"HashKey: {Encoding.UTF8.GetString(hashKey)}");

        var encryptionProvider = new RecyclableAesGcmEncryptionProvider(hashKey);
        var encryptedStream = encryptionProvider.Encrypt(new MemoryStream(_data));
        var decryptedStream = encryptionProvider.Decrypt(encryptedStream);

        Assert.Equal(_data, decryptedStream.ToArray());
    }

    [Fact]
    public async Task Aes192_GCM_StreamAsync()
    {
        var hashKey = await _hashingProvider
            .GetHashKeyAsync(Passphrase, Salt, 24);

        _output.WriteLine(Encoding.UTF8.GetString(hashKey));
        _output.WriteLine($"HashKey: {Encoding.UTF8.GetString(hashKey)}");

        var encryptionProvider = new RecyclableAesGcmEncryptionProvider(hashKey);
        var encryptedStream = await encryptionProvider.EncryptAsync(new MemoryStream(_data));
        var decryptedStream = encryptionProvider.Decrypt(encryptedStream);

        Assert.Equal(_data, decryptedStream.ToArray());
    }

    [Fact]
    public async Task Aes128_GCM()
    {
        var hashKey = await _hashingProvider
            .GetHashKeyAsync(Passphrase, Salt, 16);

        _output.WriteLine(Encoding.UTF8.GetString(hashKey));
        _output.WriteLine($"HashKey: {Encoding.UTF8.GetString(hashKey)}");

        var encryptionProvider = new RecyclableAesGcmEncryptionProvider(hashKey);

        var encryptedData = encryptionProvider.Encrypt(_data);
        _output.WriteLine($"Encrypted: {Encoding.UTF8.GetString(encryptedData.Span)}");

        var decryptedBytes = encryptionProvider.Decrypt(encryptedData);
        var data = Encoding.UTF8.GetString(_data);
        var decryptedData = Encoding.UTF8.GetString(decryptedBytes.Span);
        _output.WriteLine($"Data: {data}");
        _output.WriteLine($"Decrypted: {decryptedData}");

        Assert.Equal(data, decryptedData);
    }

    [Fact]
    public void Aes128_GCM_Stream()
    {
        var hashKey = _hashingProvider.GetHashKey(Passphrase, Salt, 16);

        _output.WriteLine(Encoding.UTF8.GetString(hashKey));
        _output.WriteLine($"HashKey: {Encoding.UTF8.GetString(hashKey)}");

        var encryptionProvider = new RecyclableAesGcmEncryptionProvider(hashKey);
        var encryptedStream = encryptionProvider.Encrypt(new MemoryStream(_data));
        var decryptedStream = encryptionProvider.Decrypt(encryptedStream);

        Assert.Equal(_data, decryptedStream.ToArray());
    }

    [Fact]
    public async Task Aes128_GCM_StreamAsync()
    {
        var hashKey = await _hashingProvider
            .GetHashKeyAsync(Passphrase, Salt, 16);

        _output.WriteLine(Encoding.UTF8.GetString(hashKey));
        _output.WriteLine($"HashKey: {Encoding.UTF8.GetString(hashKey)}");

        var encryptionProvider = new RecyclableAesGcmEncryptionProvider(hashKey);
        var encryptedStream = await encryptionProvider.EncryptAsync(new MemoryStream(_data));
        var decryptedStream = encryptionProvider.Decrypt(encryptedStream);

        Assert.Equal(_data, decryptedStream.ToArray());
    }
}
