using HouseofCat.Compression;
using HouseofCat.Compression.Recyclable;
using HouseofCat.Dataflows.Pipelines;
using HouseofCat.Encryption;
using HouseofCat.Hashing;
using HouseofCat.RabbitMQ.Dataflows;
using HouseofCat.RabbitMQ.Pipelines;
using HouseofCat.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using System;
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

        var hashProvider = new ArgonHashingProvider();
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

    public static IConsumerPipeline CreateConsumerPipeline<TOut>(
        this IRabbitService rabbitService,
        string consumerName,
        int maxDoP,
        int batchSize,
        bool? ensureOrdered,
        Func<int, int, bool?, IPipeline<PipeReceivedMessage, TOut>> pipelineBuilder)
        where TOut : RabbitWorkState
    {
        var consumer = rabbitService.GetConsumer(consumerName);
        var pipeline = pipelineBuilder.Invoke(maxDoP, batchSize, ensureOrdered);

        return new ConsumerPipeline<TOut>((IConsumer<PipeReceivedMessage>)consumer, pipeline);
    }

    public static IConsumerPipeline CreateConsumerPipeline<TOut>(
        this IRabbitService rabbitService,
        string consumerName,
        Func<int, int, bool?, IPipeline<PipeReceivedMessage, TOut>> pipelineBuilder)
        where TOut : RabbitWorkState
    {
        if (rabbitService.ConsumerOptions.TryGetValue(consumerName, out var options))
        {
            return rabbitService.CreateConsumerPipeline(
                consumerName,
                options.WorkflowMaxDegreesOfParallelism,
                options.WorkflowBatchSize,
                options.WorkflowEnsureOrdered,
                pipelineBuilder);
        }

        throw new InvalidOperationException($"ConsumerOptions for {consumerName} not found.");
    }

    public static IConsumerPipeline CreateConsumerPipeline<TOut>(
        this IRabbitService rabbitService,
        string consumerName,
        IPipeline<PipeReceivedMessage, TOut> pipeline)
        where TOut : RabbitWorkState
    {
        var consumer = rabbitService.GetConsumer(consumerName);

        return new ConsumerPipeline<TOut>((IConsumer<PipeReceivedMessage>)consumer, pipeline);
    }

    public static ConsumerDataflow<TState> BuildConsumerDataflow<TState>(
        this IRabbitService rabbitService,
        string consumerName,
        TaskScheduler taskScheduler = null)
        where TState : class, IRabbitWorkState, new()
    {
        if (rabbitService.ConsumerOptions.TryGetValue(consumerName, out var options))
        {
            return new ConsumerDataflow<TState>(
                rabbitService,
                options.WorkflowName,
                options.ConsumerName,
                options.WorkflowConsumerCount,
                taskScheduler);
        }

        throw new InvalidOperationException($"ConsumerOptions for {consumerName} not found.");
    }
}
