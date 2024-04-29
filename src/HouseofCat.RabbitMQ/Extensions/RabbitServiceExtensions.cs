using HouseofCat.Compression;
using HouseofCat.Compression.Recyclable;
using HouseofCat.Dataflows.Pipelines;
using HouseofCat.Encryption;
using HouseofCat.Hashing;
using HouseofCat.RabbitMQ.Dataflows;
using HouseofCat.RabbitMQ.Services;
using HouseofCat.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace HouseofCat.RabbitMQ.Extensions;

public static class RabbitServiceExtensions
{
    /// <summary>
    /// Sets up RabbitService with JsonProvider, GzipProvider (recyclable), and optional Aes256 (GCM) encryption/decryption.
    /// <para>Because this uses the configured LoggerFactory for internal logging, it is recommended calling this after you've setup your Logging configuration.</para>
    /// <para>Default RabbitOptions config key is "RabbitOptions".</para>
    /// </summary>
    /// <param name="services"></param>
    /// <param name="config"></param>
    /// <param name="configSectionKey"></param>
    /// <param name="encryptionPassword"></param>
    /// <param name="encryptionSalt"></param>
    /// <returns></returns>
    public static async Task AddRabbitServiceAsync(
        this IServiceCollection services,
        IConfiguration config,
        string encryptionPassword = null,
        string encryptionSalt = null,
        string configSectionKey = "RabbitOptions")
    {
        using var scope = services.BuildServiceProvider().CreateScope();

        var loggerFactory = scope.ServiceProvider.GetService<ILoggerFactory>();
        var options = config.GetRabbitOptions(configSectionKey);

        var jsonProvider = new JsonProvider();

        var hashProvider = new ArgonHashingProvider();

        IEncryptionProvider encryptionProvider = null;
        if (!string.IsNullOrEmpty(encryptionPassword) && !string.IsNullOrEmpty(encryptionSalt))
        {
            var aes256Key = hashProvider.GetHashKey(encryptionPassword, encryptionSalt, 32);
            encryptionProvider = new AesGcmEncryptionProvider(aes256Key);
        }

        var gzipProvider = new RecyclableGzipProvider();

        var rabbitService = await options.BuildRabbitServiceAsync(
            jsonProvider,
            encryptionProvider,
            gzipProvider,
            loggerFactory);

        services.TryAddSingleton<IRabbitService>(rabbitService);
    }

    /// <summary>
    /// Setup RabbitService with supplied providers.
    /// <para>Because this uses the configured LoggerFactory for internal logging, it is recommended calling this after you've setup your Logging configuration.</para>
    /// <para>Default RabbitOptions config key is "RabbitOptions".</para>
    /// </summary>
    /// <param name="services"></param>
    /// <param name="config"></param>
    /// <param name="configSectionKey"></param>
    /// <param name="serializationProvider"></param>
    /// <param name="encryptionProvider"></param>
    /// <param name="compressionProvider"></param>
    /// <returns></returns>
    public static async Task AddRabbitServiceAsync(
        this IServiceCollection services,
        IConfiguration config,
        ISerializationProvider serializationProvider,
        IEncryptionProvider encryptionProvider = null,
        ICompressionProvider compressionProvider = null,
        string configSectionKey = "RabbitOptions")
    {
        using var scope = services.BuildServiceProvider().CreateScope();

        var loggerFactory = scope.ServiceProvider.GetService<ILoggerFactory>();
        var options = config.GetRabbitOptions(configSectionKey);

        var rabbitService = await options.BuildRabbitServiceAsync(
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

        await rabbitService.StartAsync();

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

    public static ConsumerDataflow<TState> CreateConsumerDataflow<TState>(
        this IRabbitService rabbitService,
        string consumerName,
        TaskScheduler taskScheduler = null)
        where TState : class, IRabbitWorkState, new()
    {
        var options = rabbitService.Options.GetConsumerOptions(consumerName);

        var dataflow = new ConsumerDataflow<TState>(
            rabbitService,
            options,
            taskScheduler)
            .SetSerializationProvider(rabbitService.SerializationProvider)
            .SetCompressionProvider(rabbitService.CompressionProvider)
            .SetEncryptionProvider(rabbitService.EncryptionProvider)
            .WithBuildState()
            .WithDecompressionStep()
            .WithDecryptionStep();

        if (!string.IsNullOrWhiteSpace(options.SendQueueName))
        {
            if (rabbitService.CompressionProvider is not null && options.WorkflowSendCompressed)
            {
                dataflow = dataflow.WithSendCompressedStep();
            }
            if (rabbitService.EncryptionProvider is not null && options.WorkflowSendEncrypted)
            {
                dataflow = dataflow.WithSendEncryptedStep();
            }

            dataflow = dataflow.WithSendStep();
        }

        return dataflow;
    }
}
