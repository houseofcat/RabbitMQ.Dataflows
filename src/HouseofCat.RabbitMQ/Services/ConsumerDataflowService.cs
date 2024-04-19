using HouseofCat.RabbitMQ.Dataflows;
using System;
using System.Threading.Tasks;

namespace HouseofCat.RabbitMQ.Services;

public class ConsumerDataflowService<TState> where TState : class, IRabbitWorkState, new()
{
    private readonly ConsumerDataflow<TState> _workflow;
    private readonly ConsumerOptions _options;
    private readonly IRabbitService _rabbitService;

    public ConsumerDataflowService(
        IRabbitService rabbitService,
        string consumerName)
    {
        _rabbitService = rabbitService;
        _options = _rabbitService.Options.GetConsumerOptions(consumerName);

        _workflow = new ConsumerDataflow<TState>(
            _rabbitService,
            _options.WorkflowName,
            _options.ConsumerName,
            _options.WorkflowConsumerCount)
            .WithBuildState();
    }

    public void AddStep(string stepName, Func<TState, TState> step)
    {
        _workflow.AddStep(
            step,
            stepName,
            _options.WorkflowMaxDegreesOfParallelism,
            _options.WorkflowEnsureOrdered,
            _options.WorkflowBatchSize);
    }

    public void AddStep(string stepName, Func<TState, Task<TState>> step)
    {
        _workflow.AddStep(
            step,
            stepName,
            _options.WorkflowMaxDegreesOfParallelism,
            _options.WorkflowEnsureOrdered,
            _options.WorkflowBatchSize);
    }

    public void AddFinalization(Action<TState> step)
    {
        _workflow.WithFinalization(
            step,
            _options.WorkflowMaxDegreesOfParallelism,
            _options.WorkflowEnsureOrdered,
            _options.WorkflowBatchSize);
    }

    public void AddFinalization(Func<TState, Task> step)
    {
        _workflow.WithFinalization(
            step,
            _options.WorkflowMaxDegreesOfParallelism,
            _options.WorkflowEnsureOrdered,
            _options.WorkflowBatchSize);
    }

    public void AddErrorHandling(Action<TState> step)
    {
        _workflow.WithErrorHandling(
            step,
            _options.WorkflowBatchSize,
            _options.WorkflowMaxDegreesOfParallelism,
            _options.WorkflowEnsureOrdered);
    }

    public void AddErrorHandling(Func<TState, Task> step)
    {
        _workflow.WithErrorHandling(
            step,
            _options.WorkflowBatchSize,
            _options.WorkflowMaxDegreesOfParallelism,
            _options.WorkflowEnsureOrdered);
    }

    public async Task StartAsync()
    {
        await _workflow.StartAsync();
    }

    public async Task StopAsync()
    {
        await _workflow.StopAsync();
    }
}
