using HouseofCat.RabbitMQ.Dataflows;
using HouseofCat.RabbitMQ.Extensions;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace HouseofCat.RabbitMQ.Services;

public interface IConsumerDataflowService<TState> where TState : class, IRabbitWorkState, new()
{
    ConsumerDataflow<TState> Dataflow { get; }
    ConsumerOptions Options { get; }

    void AddDefaultErrorHandling();
    void AddErrorHandling(Action<TState> step);
    void AddErrorHandling(Func<TState, Task> step);

    void AddDefaultFinalization();
    void AddFinalization(Action<TState> step);
    void AddFinalization(Func<TState, Task> step);

    void AddStep(string stepName, Func<TState, Task<TState>> step);
    void AddStep(string stepName, Func<TState, TState> step);

    Task StartAsync();
    Task StopAsync(bool immediate = false, bool shutdownService = false);
}

public class ConsumerDataflowService<TState> : IConsumerDataflowService<TState> where TState : class, IRabbitWorkState, new()
{
    private readonly ILogger<ConsumerDataflowService<TState>> _logger;
    private readonly IRabbitService _rabbitService;
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
        ILogger<ConsumerDataflowService<TState>> logger,
        IRabbitService rabbitService,
        string consumerName,
        TaskScheduler taskScheduler = null)
    {
        _logger = logger;
        _rabbitService = rabbitService;

        Options = rabbitService.Options.GetConsumerOptions(consumerName);
        Dataflow = rabbitService.CreateConsumerDataflow<TState>(consumerName, taskScheduler);
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

    public void AddDefaultFinalization()
    {
        Dataflow.WithDefaultFinalization();
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

    public void AddDefaultErrorHandling()
    {
        Dataflow.WithDefaultErrorHandling();
    }

    public async Task StartAsync()
    {
        await Dataflow.StartAsync();
    }

    /// <summary>
    /// Provides mechanism to stop the Dataflow gracefully and optionally shutdown RabbitService.
    /// </summary>
    /// <param name="immediate"></param>
    /// <param name="shutdownService"></param>
    /// <returns></returns>
    public async Task StopAsync(
        bool immediate = false,
        bool shutdownService = false)
    {
        await Dataflow.StopAsync(immediate, shutdownService);
    }
}
