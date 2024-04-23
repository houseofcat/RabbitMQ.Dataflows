using HouseofCat.RabbitMQ.Dataflows;
using System;
using System.Threading.Tasks;

namespace HouseofCat.RabbitMQ.Services;

public class ConsumerDataflowService<TState> where TState : class, IRabbitWorkState, new()
{
    private readonly ConsumerDataflow<TState> _dataflow;
    private readonly ConsumerOptions _options;

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

        if (!string.IsNullOrWhiteSpace(_options.TargetQueueName))
        {
            dataflow = dataflow
                .WithEncryption()
                .WithCompression()
                .WithSendStep();
        }

        _dataflow = dataflow;
    }

    public void AddStep(string stepName, Func<TState, TState> step)
    {
        _dataflow.AddStep(
            step,
            stepName,
            _options.WorkflowMaxDegreesOfParallelism,
            _options.WorkflowEnsureOrdered,
            _options.WorkflowBatchSize);
    }

    public void AddStep(string stepName, Func<TState, Task<TState>> step)
    {
        _dataflow.AddStep(
            step,
            stepName,
            _options.WorkflowMaxDegreesOfParallelism,
            _options.WorkflowEnsureOrdered,
            _options.WorkflowBatchSize);
    }

    public void AddFinalization(Action<TState> step)
    {
        _dataflow.WithFinalization(
            step,
            _options.WorkflowMaxDegreesOfParallelism,
            _options.WorkflowEnsureOrdered,
            _options.WorkflowBatchSize);
    }

    public void AddFinalization(Func<TState, Task> step)
    {
        _dataflow.WithFinalization(
            step,
            _options.WorkflowMaxDegreesOfParallelism,
            _options.WorkflowEnsureOrdered,
            _options.WorkflowBatchSize);
    }

    public void AddErrorHandling(Action<TState> step)
    {
        _dataflow.WithErrorHandling(
            step,
            _options.WorkflowBatchSize,
            _options.WorkflowMaxDegreesOfParallelism,
            _options.WorkflowEnsureOrdered);
    }

    public void AddErrorHandling(Func<TState, Task> step)
    {
        _dataflow.WithErrorHandling(
            step,
            _options.WorkflowBatchSize,
            _options.WorkflowMaxDegreesOfParallelism,
            _options.WorkflowEnsureOrdered);
    }

    public async Task StartAsync()
    {
        await _dataflow.StartAsync();
    }

    public async Task StopAsync()
    {
        await _dataflow.StopAsync();
    }
}
