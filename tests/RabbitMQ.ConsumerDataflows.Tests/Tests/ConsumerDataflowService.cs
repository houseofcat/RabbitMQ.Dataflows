using HouseofCat.RabbitMQ.Dataflows;
using HouseofCat.RabbitMQ.Services;

namespace RabbitMQ.ConsumerDataflows.Tests;

public class ConsumerDataflowService
{
    private readonly ConsumerDataflowOptions _consumerWorkflowOptions;
    private readonly ConsumerDataflow<CustomWorkState> _workflow;

    public ConsumerDataflowService(IRabbitService rabbitService)
    {
        _consumerWorkflowOptions = new ConsumerDataflowOptions
        {
            WorkflowName = Shared.ConsumerWorkflowName,
            ConsumerName = Shared.ConsumerName,
            ConsumerCount = 1,
            MaxDegreeOfParallelism = 1,
            EnsureOrdered = true,
            Capacity = 1000,
            ErrorQueueName = Shared.ErrorQueue
        };

        _workflow = _consumerWorkflowOptions.BuildConsumerDataflow(rabbitService);
    }

    public void AddStep(string stepName, Func<CustomWorkState, CustomWorkState> step)
    {
        _workflow.AddStep(
            step,
            stepName,
            _consumerWorkflowOptions.MaxDegreeOfParallelism,
            _consumerWorkflowOptions.EnsureOrdered,
            _consumerWorkflowOptions.Capacity);
    }

    public void AddStep(string stepName, Func<CustomWorkState, Task<CustomWorkState>> step)
    {
        _workflow.AddStep(
            step,
            stepName,
            _consumerWorkflowOptions.MaxDegreeOfParallelism,
            _consumerWorkflowOptions.EnsureOrdered,
            _consumerWorkflowOptions.Capacity);
    }

    public void AddFinalization(Action<CustomWorkState> step)
    {
        _workflow.WithFinalization(
            step,
            _consumerWorkflowOptions.MaxDegreeOfParallelism,
            _consumerWorkflowOptions.EnsureOrdered,
            _consumerWorkflowOptions.Capacity);
    }

    public void AddFinalization(Func<CustomWorkState, Task> step)
    {
        _workflow.WithFinalization(
            step,
            _consumerWorkflowOptions.MaxDegreeOfParallelism,
            _consumerWorkflowOptions.EnsureOrdered,
            _consumerWorkflowOptions.Capacity);
    }

    public void AddErrorHandling(Action<CustomWorkState> step)
    {
        _workflow.WithErrorHandling(
            step,
            _consumerWorkflowOptions.Capacity,
            _consumerWorkflowOptions.MaxDegreeOfParallelism,
            _consumerWorkflowOptions.EnsureOrdered);
    }

    public void AddErrorHandling(Func<CustomWorkState, Task> step)
    {
        _workflow.WithErrorHandling(
            step,
            _consumerWorkflowOptions.Capacity,
            _consumerWorkflowOptions.MaxDegreeOfParallelism,
            _consumerWorkflowOptions.EnsureOrdered);
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
