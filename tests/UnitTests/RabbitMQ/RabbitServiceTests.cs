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

    public RabbitServiceTests()
    {
        var options = new RabbitOptions();
        var hashingProvider = new ArgonHashingProvider();

        var hashKey = hashingProvider.GetHashKey("Sega", "Nintendo", 32);

        _rabbitService = new RabbitService(
            options,
            new JsonProvider(),
            new AesGcmEncryptionProvider(hashKey),
            new GzipProvider());
    }

    [Fact]
    public async Task ComcryptTestAsync()
    {
        var message = new Message("", "TestQueue", Encoding.UTF8.GetBytes("Hello World"));

        await _rabbitService.ComcryptAsync(message);

        Assert.True(message.Metadata.Encrypted());
        Assert.True(message.Metadata.Compressed());
    }

    [Fact]
    public async Task ComcryptDecomcryptTestAsync()
    {
        var messageAsString = "Hello World";
        var message = new Message("", "TestQueue", Encoding.UTF8.GetBytes(messageAsString));

        await _rabbitService.ComcryptAsync(message);

        Assert.True(message.Metadata.Encrypted());
        Assert.True(message.Metadata.Compressed());

        await _rabbitService.DecomcryptAsync(message);

        Assert.False(message.Metadata.Encrypted());
        Assert.False(message.Metadata.Compressed());

        var bodyAsString = Encoding.UTF8.GetString(message.Body.Span);

        Assert.Equal(messageAsString, bodyAsString);
    }
}
