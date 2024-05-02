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

    void AddDefaultFinalization(bool log = true);
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

    public void AddDefaultFinalization(bool log = true)
    {
        _logFinalizationMessage = log;

        Dataflow.WithFinalization(
            DefaultFinalization,
            Options.WorkflowMaxDegreesOfParallelism,
            Options.WorkflowEnsureOrdered,
            Options.WorkflowBatchSize);
    }

    protected static readonly string _defaultFinalizationMessage = "Message [{0}] finished processing. Acking message.";
    private bool _logFinalizationMessage;

    protected void DefaultFinalization(TState state)
    {
        if (_logFinalizationMessage)
        {
            _logger.LogInformation(_defaultFinalizationMessage, state?.ReceivedMessage?.Message?.MessageId);
        }

        state?.ReceivedMessage?.AckMessage();
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
        Dataflow.WithErrorHandling(
            DefaultErrorHandlerAsync,
            Options.WorkflowBatchSize,
            Options.WorkflowMaxDegreesOfParallelism,
            Options.WorkflowEnsureOrdered);
    }

    // First, check if DLQ is configured in QueueArgs.
    // Second, check if ErrorQueue is set in Options.
    // Lastly, decide if you want to Nack with requeue, or anything else.
    protected async Task DefaultErrorHandlerAsync(TState state)
    {
        _logger.LogError(state?.EDI?.SourceException, "Error Handler: {0}", state?.EDI?.SourceException?.Message);

        if (Options.RejectOnError())
        {
            state.ReceivedMessage?.RejectMessage(requeue: false);
        }
        else if (!string.IsNullOrEmpty(Options.ErrorQueueName))
        {
            // If type is currently an IMessage, republish with new RoutingKey.
            if (state.ReceivedMessage.Message is not null)
            {
                state.ReceivedMessage.Message.RoutingKey = Options.ErrorQueueName;
                await _rabbitService.Publisher.QueueMessageAsync(state.ReceivedMessage.Message);
            }
            else
            {
                await _rabbitService.Publisher.PublishAsync(
                    exchangeName: "",
                    routingKey: Options.ErrorQueueName,
                    body: state.ReceivedMessage.Body,
                    headers: state.ReceivedMessage.Properties.Headers,
                    messageId: Guid.NewGuid().ToString(),
                    deliveryMode: 2,
                    mandatory: false);
            }

            // Don't forget to Ack the original message when sending it to a different Queue.
            state.ReceivedMessage?.AckMessage();
        }
        else
        {
            state.ReceivedMessage?.NackMessage(requeue: true);
        }
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
