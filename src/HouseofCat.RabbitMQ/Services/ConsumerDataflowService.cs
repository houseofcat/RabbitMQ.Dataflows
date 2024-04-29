using HouseofCat.RabbitMQ.Dataflows;
using System;
using System.Threading.Tasks;

namespace HouseofCat.RabbitMQ.Services;

public class ConsumerDataflowService<TState> where TState : class, IRabbitWorkState, new()
{
    public ConsumerDataflow<TState> Dataflow { get; }
    private readonly ConsumerOptions _options;

    /// <summary>
    /// This a basic implementation service class for convenience. It serves as a simple opinionated wrapper
    /// of ConsumerDataflow with accessors to a few methods and provides streamlined auto-configuration.
    /// </summary>
    /// <param name="rabbitService"></param>
    /// <param name="consumerName"></param>
    /// <param name="taskScheduler"></param>
    public ConsumerDataflowService(
        IRabbitService rabbitService,
        string consumerName,
        TaskScheduler taskScheduler = null)
    {
        _options = rabbitService.Options.GetConsumerOptions(consumerName);

        var dataflow = new ConsumerDataflow<TState>(
            rabbitService,
            _options,
            taskScheduler)
            .SetSerializationProvider(rabbitService.SerializationProvider)
            .SetCompressionProvider(rabbitService.CompressionProvider)
            .SetEncryptionProvider(rabbitService.EncryptionProvider)
            .WithBuildState()
            .WithDecompressionStep()
            .WithDecryptionStep();

        if (!string.IsNullOrWhiteSpace(_options.SendQueueName))
        {
            if (rabbitService.CompressionProvider is not null && _options.WorkflowSendCompressed)
            {
                dataflow = dataflow.WithSendCompressedStep();
            }
            if (rabbitService.EncryptionProvider is not null && _options.WorkflowSendEncrypted)
            {
                dataflow = dataflow.WithSendEncryptedStep();
            }

            dataflow = dataflow.WithSendStep();
        }

        Dataflow = dataflow;
    }

    public void AddStep(string stepName, Func<TState, TState> step)
    {
        Dataflow.AddStep(
            step,
            stepName,
            _options.WorkflowMaxDegreesOfParallelism,
            _options.WorkflowEnsureOrdered,
            _options.WorkflowBatchSize);
    }

    public void AddStep(string stepName, Func<TState, Task<TState>> step)
    {
        Dataflow.AddStep(
            step,
            stepName,
            _options.WorkflowMaxDegreesOfParallelism,
            _options.WorkflowEnsureOrdered,
            _options.WorkflowBatchSize);
    }

    public void AddFinalization(Action<TState> step)
    {
        Dataflow.WithFinalization(
            step,
            _options.WorkflowMaxDegreesOfParallelism,
            _options.WorkflowEnsureOrdered,
            _options.WorkflowBatchSize);
    }

    public void AddFinalization(Func<TState, Task> step)
    {
        Dataflow.WithFinalization(
            step,
            _options.WorkflowMaxDegreesOfParallelism,
            _options.WorkflowEnsureOrdered,
            _options.WorkflowBatchSize);
    }

    public void AddErrorHandling(Action<TState> step)
    {
        Dataflow.WithErrorHandling(
            step,
            _options.WorkflowBatchSize,
            _options.WorkflowMaxDegreesOfParallelism,
            _options.WorkflowEnsureOrdered);
    }

    public void AddErrorHandling(Func<TState, Task> step)
    {
        Dataflow.WithErrorHandling(
            step,
            _options.WorkflowBatchSize,
            _options.WorkflowMaxDegreesOfParallelism,
            _options.WorkflowEnsureOrdered);
    }

    public async Task StartAsync()
    {
        await Dataflow.StartAsync();
    }

    public async Task StopAsync()
    {
        await Dataflow.StopAsync();
    }
}
