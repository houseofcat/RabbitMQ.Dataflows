using HouseofCat.RabbitMQ.Dataflows;
using System;
using System.Threading.Tasks;

namespace HouseofCat.RabbitMQ.Services;

public class ConsumerDataflowService<TState> where TState : class, IRabbitWorkState, new()
{
    public ConsumerDataflow<TState> Dataflow { get; }
    public ConsumerOptions Options { get; }

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
        Options = rabbitService.Options.GetConsumerOptions(consumerName);

        var dataflow = new ConsumerDataflow<TState>(
            rabbitService,
            Options,
            taskScheduler)
            .SetSerializationProvider(rabbitService.SerializationProvider)
            .SetCompressionProvider(rabbitService.CompressionProvider)
            .SetEncryptionProvider(rabbitService.EncryptionProvider)
            .WithBuildState()
            .WithDecompressionStep()
            .WithDecryptionStep();

        if (!string.IsNullOrWhiteSpace(Options.SendQueueName))
        {
            if (rabbitService.CompressionProvider is not null && Options.WorkflowSendCompressed)
            {
                dataflow = dataflow.WithSendCompressedStep();
            }
            if (rabbitService.EncryptionProvider is not null && Options.WorkflowSendEncrypted)
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
            Options.WorkflowMaxDegreesOfParallelism,
            Options.WorkflowEnsureOrdered,
            Options.WorkflowBatchSize);
    }

    public void AddStep(string stepName, Func<TState, Task<TState>> step)
    {
        Dataflow.AddStep(
            step,
            stepName,
            Options.WorkflowMaxDegreesOfParallelism,
            Options.WorkflowEnsureOrdered,
            Options.WorkflowBatchSize);
    }

    public void AddFinalization(Action<TState> step)
    {
        Dataflow.WithFinalization(
            step,
            Options.WorkflowMaxDegreesOfParallelism,
            Options.WorkflowEnsureOrdered,
            Options.WorkflowBatchSize);
    }

    public void AddFinalization(Func<TState, Task> step)
    {
        Dataflow.WithFinalization(
            step,
            Options.WorkflowMaxDegreesOfParallelism,
            Options.WorkflowEnsureOrdered,
            Options.WorkflowBatchSize);
    }

    public void AddErrorHandling(Action<TState> step)
    {
        Dataflow.WithErrorHandling(
            step,
            Options.WorkflowBatchSize,
            Options.WorkflowMaxDegreesOfParallelism,
            Options.WorkflowEnsureOrdered);
    }

    public void AddErrorHandling(Func<TState, Task> step)
    {
        Dataflow.WithErrorHandling(
            step,
            Options.WorkflowBatchSize,
            Options.WorkflowMaxDegreesOfParallelism,
            Options.WorkflowEnsureOrdered);
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
