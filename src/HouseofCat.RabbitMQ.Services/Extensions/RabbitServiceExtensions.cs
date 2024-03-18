using HouseofCat.Compression;
using HouseofCat.Encryption.Providers;
using HouseofCat.Hashing.Argon;
using HouseofCat.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace HouseofCat.RabbitMQ.Services.Extensions;

public static class RabbitServiceExtensions
{
    /// <summary>
    /// Sets up RabbitService with JsonProvider, GzipProvider (recyclable), and Aes256 (GCM) encryption/decryption.
    /// </summary>
    /// <param name="services"></param>
    /// <param name="config"></param>
    /// <param name="configSectionKey"></param>
    /// <param name="encryptionPassword"></param>
    /// <param name="encryptionSalt"></param>
    /// <returns></returns>
    public static async Task SetupRabbitServiceAsync(
        this IServiceCollection services,
        IConfiguration config,
        string configSectionKey,
        string encryptionPassword,
        string encryptionSalt)
    {
        using var scope = services.BuildServiceProvider().CreateScope();

        var loggerFactory = scope.ServiceProvider.GetService<ILoggerFactory>();
        var options = config.GetRabbitOptions(configSectionKey);

        var jsonProvider = new JsonProvider();

        var hashProvider = new Argon2ID_HashingProvider();
        var aes256Key = hashProvider.GetHashKey(encryptionPassword, encryptionSalt, 32);
        var aes256Provider = new AesGcmEncryptionProvider(aes256Key);

        var gzipProvider = new RecyclableGzipProvider();

        var rabbitService = await BuildRabbitServiceAsync(
            options,
            jsonProvider,
            aes256Provider,
            gzipProvider,
            loggerFactory);

        services.TryAddSingleton<IRabbitService>(rabbitService);
    }

    /// <summary>
    /// Setup RabbitService with provided providers. Automatically loads the configured LoggerFactory from IServiceCollection.
    /// </summary>
    /// <param name="services"></param>
    /// <param name="config"></param>
    /// <param name="configSectionKey"></param>
    /// <param name="serializationProvider"></param>
    /// <param name="encryptionProvider"></param>
    /// <param name="compressionProvider"></param>
    /// <returns></returns>
    public static async Task SetupRabbitServiceAsync(
        this IServiceCollection services,
        IConfiguration config,
        string configSectionKey,
        ISerializationProvider serializationProvider,
        IEncryptionProvider encryptionProvider = null,
        ICompressionProvider compressionProvider = null)
    {
        using var scope = services.BuildServiceProvider().CreateScope();

        var loggerFactory = scope.ServiceProvider.GetService<ILoggerFactory>();
        var options = config.GetRabbitOptions(configSectionKey);

        var rabbitService = await BuildRabbitServiceAsync(
            options,
            serializationProvider,
            encryptionProvider,
            compressionProvider,
            loggerFactory);

        services.TryAddSingleton<IRabbitService>(rabbitService);
    }

    public static async Task<RabbitService> BuildRabbitServiceAsync(
        this RabbitOptions options,
        ISerializationProvider serializationProvider,
        IEncryptionProvider encryptionProvider = null,
        ICompressionProvider compressionProvider = null,
        ILoggerFactory loggerFactory = null)
    {
        var rabbitService = new RabbitService(
            options: options,
            serializationProvider,
            encryptionProvider,
            compressionProvider,
            loggerFactory);

        await rabbitService.Publisher.StartAutoPublishAsync();

        return rabbitService;
    }
}
