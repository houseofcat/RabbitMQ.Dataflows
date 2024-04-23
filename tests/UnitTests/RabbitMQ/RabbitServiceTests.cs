using HouseofCat.Compression;
using HouseofCat.Encryption;
using HouseofCat.Hashing;
using HouseofCat.RabbitMQ;
using HouseofCat.RabbitMQ.Services;
using HouseofCat.Serialization;
using System.Text;

namespace RabbitMQ;

public class RabbitServiceTests
{
    private readonly IRabbitService _rabbitService;
    private readonly IEncryptionProvider _encryptionProvider;
    private readonly ICompressionProvider _compressionProvider;

    public RabbitServiceTests()
    {
        var hashingProvider = new ArgonHashingProvider();

        var hashKey = hashingProvider.GetHashKey("Sega", "Nintendo", 32);
        _encryptionProvider = new AesGcmEncryptionProvider(hashKey);
        _compressionProvider = new GzipProvider();

        _rabbitService = new RabbitService(
            new JsonProvider(),
            _encryptionProvider,
            _compressionProvider);
    }

    [Fact]
    public async Task ComcryptTestAsync()
    {
        // Arrange
        var messageAsString = "Hello World";
        var message = new Message("", "TestQueue", Encoding.UTF8.GetBytes(messageAsString));

        // Act
        await _rabbitService.ComcryptAsync(message);
        var bodyAsString = Encoding.UTF8.GetString(message.Body.Span);

        // Assert
        Assert.True(message.Metadata.Encrypted());
        Assert.True(message.Metadata.Compressed());
        Assert.Equal(_rabbitService.EncryptionProvider.Type, message.Metadata.EncryptionType());
        Assert.Equal(_rabbitService.CompressionProvider.Type, message.Metadata.CompressionType());
        Assert.NotEqual(messageAsString, bodyAsString);
    }

    [Fact]
    public async Task DecomcryptTestAsync()
    {
        // Arrange
        var compressedData = _compressionProvider.Compress(Encoding.UTF8.GetBytes("Hello World"));
        var encryptedData = _encryptionProvider.Encrypt(compressedData);
        var message = new Message("", "TestQueue", encryptedData);

        message.Metadata.Fields[HouseofCat.RabbitMQ.Constants.HeaderForCompressed] = true;
        message.Metadata.Fields[HouseofCat.RabbitMQ.Constants.HeaderForCompression] = _rabbitService.CompressionProvider.Type;
        message.Metadata.Fields[HouseofCat.RabbitMQ.Constants.HeaderForEncrypted] = true;
        message.Metadata.Fields[HouseofCat.RabbitMQ.Constants.HeaderForEncryption] = _rabbitService.EncryptionProvider.Type;

        // Act
        await _rabbitService.DecomcryptAsync(message);
        var bodyAsString = Encoding.UTF8.GetString(message.Body.Span);

        // Assert
        Assert.False(message.Metadata.Encrypted());
        Assert.False(message.Metadata.Compressed());
        Assert.False(message.Metadata.Fields.ContainsKey(HouseofCat.RabbitMQ.Constants.HeaderForEncryption));
        Assert.False(message.Metadata.Fields.ContainsKey(HouseofCat.RabbitMQ.Constants.HeaderForCompression));
        Assert.Equal("Hello World", bodyAsString);
    }

    [Fact]
    public async Task ComcryptDecomcryptTestAsync()
    {
        // Arrange
        var messageAsString = "Hello World";
        var message = new Message("", "TestQueue", Encoding.UTF8.GetBytes(messageAsString));

        // Act
        await _rabbitService.ComcryptAsync(message);
        var bodyAsString = Encoding.UTF8.GetString(message.Body.Span);

        // Assert
        Assert.True(message.Metadata.Encrypted());
        Assert.True(message.Metadata.Compressed());
        Assert.Equal(_rabbitService.EncryptionProvider.Type, message.Metadata.EncryptionType());
        Assert.Equal(_rabbitService.CompressionProvider.Type, message.Metadata.CompressionType());
        Assert.NotEqual(messageAsString, bodyAsString);

        // Re-Act
        await _rabbitService.DecomcryptAsync(message);
        bodyAsString = Encoding.UTF8.GetString(message.Body.Span);

        // Re-Assert
        Assert.False(message.Metadata.Encrypted());
        Assert.False(message.Metadata.Compressed());
        Assert.Equal(messageAsString, bodyAsString);
    }

    [Fact]
    public void EncryptDecryptTest()
    {
        // Arrange
        var messageAsString = "Hello World";
        var message = new Message("", "TestQueue", Encoding.UTF8.GetBytes(messageAsString));

        // Act
        _rabbitService.Encrypt(message);
        var bodyAsString = Encoding.UTF8.GetString(message.Body.Span);

        // Assert
        Assert.True(message.Metadata.Encrypted());
        Assert.Equal(_rabbitService.EncryptionProvider.Type, message.Metadata.EncryptionType());
        Assert.NotEqual(messageAsString, bodyAsString);

        // Re-Act
        _rabbitService.Decrypt(message);
        bodyAsString = Encoding.UTF8.GetString(message.Body.Span);

        // Re-Assert
        Assert.False(message.Metadata.Encrypted());
        Assert.Equal(messageAsString, bodyAsString);
    }

    [Fact]
    public async Task CompressDecompressTestAsync()
    {
        // Arrange
        var messageAsString = "Hello World";
        var message = new Message("", "TestQueue", Encoding.UTF8.GetBytes(messageAsString));

        // Act
        await _rabbitService.CompressAsync(message);
        var bodyAsString = Encoding.UTF8.GetString(message.Body.Span);

        // Assert
        Assert.True(message.Metadata.Compressed());
        Assert.Equal(_rabbitService.CompressionProvider.Type, message.Metadata.CompressionType());
        Assert.NotEqual(messageAsString, bodyAsString);

        // Re-Act
        await _rabbitService.DecompressAsync(message);
        bodyAsString = Encoding.UTF8.GetString(message.Body.Span);

        // Re-Assert
        Assert.False(message.Metadata.Compressed());
        Assert.Equal(messageAsString, bodyAsString);
    }
}
