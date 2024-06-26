﻿using HouseofCat.Compression;
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
    /// Sets up RabbitService with JsonProvider, GzipProvider (recyclable), and optional AesGcm (256-bit) encryption/decryption.
    /// <para>Because this uses the configured LoggerFactory for internal logging, it is recommended calling this after you've setup your Logging configuration.</para>
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
        string configSectionKey = Constants.DefaultRabbitOptionsConfigKey)
    {
        using var scope = services.BuildServiceProvider().CreateScope();

        var loggerFactory = scope.ServiceProvider.GetService<ILoggerFactory>();
        var options = config.GetRabbitOptions(configSectionKey);

        var jsonProvider = new JsonProvider();
        var gzipProvider = new RecyclableGzipProvider();

        AesGcmEncryptionProvider encryptionProvider = null;
        if (!string.IsNullOrEmpty(encryptionPassword) && !string.IsNullOrEmpty(encryptionSalt))
        {
            var hashProvider = new ArgonHashingProvider();
            var aes256Key = hashProvider.GetHashKey(encryptionPassword, encryptionSalt, 32);
            encryptionProvider = new AesGcmEncryptionProvider(aes256Key);
        }

        services.TryAddSingleton<ISerializationProvider>(jsonProvider);
        services.TryAddSingleton<ICompressionProvider>(gzipProvider);
        if (encryptionProvider is not null)
        { services.TryAddSingleton<IEncryptionProvider>(encryptionProvider); }

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
        string configSectionKey = Constants.DefaultRabbitOptionsConfigKey)
    {
        using var scope = services.BuildServiceProvider().CreateScope();

        var loggerFactory = scope.ServiceProvider.GetService<ILoggerFactory>();
        var options = config.GetRabbitOptions(configSectionKey);

        var rabbitService = await options.BuildRabbitServiceAsync(
            serializationProvider,
            encryptionProvider,
            compressionProvider,
            loggerFactory);

        services.TryAddSingleton(serializationProvider);

        if (encryptionProvider is not null)
        { services.TryAddSingleton(compressionProvider); }
        if (encryptionProvider is not null)
        { services.TryAddSingleton(encryptionProvider); }

        services.TryAddSingleton<IRabbitService>(rabbitService);
    }

    /// <summary>
    /// Setup RabbitService which will look for required and optional providers in the ServiceProvider.
    /// <para>Because this uses the configured LoggerFactory for internal logging, it is recommended calling this after you've setup your Logging configuration.</para>
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
        string configSectionKey = Constants.DefaultRabbitOptionsConfigKey)
    {
        using var scope = services.BuildServiceProvider().CreateScope();

        var options = config.GetRabbitOptions(configSectionKey);

        var serializationProvider = scope.ServiceProvider.GetRequiredService<ISerializationProvider>();
        var encryptionProvider = scope.ServiceProvider.GetService<IEncryptionProvider>();
        var compressionProvider = scope.ServiceProvider.GetService<ICompressionProvider>();
        var loggerFactory = scope.ServiceProvider.GetService<ILoggerFactory>();

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

    /// <summary>
    /// CreateConsumerDataflow will create a ConsumerDataflow with the specified consumerName and TaskScheduler.
    /// <para>It will also configure SerializationProvider, CompressionProvider, and EncryptionProvider assigned inside the RabbitService.</para>
    /// <para>Conditionally, it will also configure SendMessage, SendCompressed, and SendEncrypted based off how the ConsumerOptions are configured.</para>
    /// </summary>
    /// <typeparam name="TState"></typeparam>
    /// <param name="rabbitService"></param>
    /// <param name="consumerName"></param>
    /// <param name="taskScheduler"></param>
    /// <returns></returns>
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
                dataflow.WithSendCompressedStep();
            }
            if (rabbitService.EncryptionProvider is not null && options.WorkflowSendEncrypted)
            {
                dataflow.WithSendEncryptedStep();
            }

            dataflow.WithSendMessageStep();
        }

        return dataflow;
    }
}
