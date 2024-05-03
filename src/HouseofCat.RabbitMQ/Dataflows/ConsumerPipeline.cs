using HouseofCat.Dataflows.Pipelines;
using HouseofCat.Utilities.Helpers;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace HouseofCat.RabbitMQ.Dataflows;

public interface IConsumerPipeline
{
    string ConsumerPipelineName { get; }
    ConsumerOptions ConsumerOptions { get; }
    bool Started { get; }

    Task AwaitCompletionAsync();
    Task StartAsync(bool useStream, CancellationToken token = default);
    Task StopAsync(bool immediate = false);
}

public class ConsumerPipeline<TOut> : IConsumerPipeline, IDisposable where TOut : RabbitWorkState
{
    public string ConsumerPipelineName { get; }
    public ConsumerOptions ConsumerOptions { get; }
    public bool Started { get; private set; }

    private readonly ILogger<ConsumerPipeline<TOut>> _logger;
    private IConsumer<PipeReceivedMessage> Consumer { get; }
    private IPipeline<PipeReceivedMessage, TOut> Pipeline { get; }
    private Task FeedPipelineWithDataTasks { get; set; }

    private TaskCompletionSource<bool> _completionSource;
    private CancellationTokenSource _cancellationTokenSource;

    private readonly SemaphoreSlim _cpLock = new SemaphoreSlim(1, 1);

    public ConsumerPipeline(
        IConsumer<PipeReceivedMessage> consumer,
        IPipeline<PipeReceivedMessage, TOut> pipeline)
    {
        _logger = LogHelpers.GetLogger<ConsumerPipeline<TOut>>();
        Pipeline = pipeline ?? throw new ArgumentNullException(nameof(pipeline));
        Consumer = consumer ?? throw new ArgumentNullException(nameof(consumer));
        ConsumerOptions = consumer.ConsumerOptions ?? throw new ArgumentNullException(nameof(consumer.Options));

        ConsumerPipelineName = !string.IsNullOrEmpty(consumer.ConsumerOptions.WorkflowName)
            ? consumer.ConsumerOptions.WorkflowName
            : "Unknown";
    }

    public async Task StartAsync(bool useStream, CancellationToken token = default)
    {
        await _cpLock.WaitAsync(token).ConfigureAwait(false);

        try
        {
            if (Started) return;

            _cancellationTokenSource = new CancellationTokenSource();
            _completionSource = new TaskCompletionSource<bool>();

            await Consumer
                .StartConsumerAsync()
                .ConfigureAwait(false);

            if (Consumer.Started)
            {
                if (useStream)
                {
                    FeedPipelineWithDataTasks = Task.Run(
                        () =>
                            PipelineStreamEngineAsync(
                                Pipeline,
                                ConsumerOptions.WorkflowWaitForCompletion,
                                token.Equals(default)
                                    ? _cancellationTokenSource.Token
                                    : token),
                            CancellationToken.None);
                }
                else
                {
                    FeedPipelineWithDataTasks = Task.Run(
                        () =>
                            PipelineExecutionEngineAsync(
                                Pipeline,
                                ConsumerOptions.WorkflowWaitForCompletion,
                                token.Equals(default)
                                    ? _cancellationTokenSource.Token
                                    : token),
                            CancellationToken.None);
                }

                Started = true;
            }
        }
        catch { /* SWALLOW */ }
        finally
        { _cpLock.Release(); }
    }

    public async Task StopAsync(bool immediate = false)
    {
        await _cpLock.WaitAsync().ConfigureAwait(false);

        try
        {
            if (!Started) return;

            _cancellationTokenSource.Cancel();

            await Consumer
                .StopConsumerAsync(immediate)
                .ConfigureAwait(false);

            if (FeedPipelineWithDataTasks is not null)
            {
                await FeedPipelineWithDataTasks.ConfigureAwait(false);
                FeedPipelineWithDataTasks = null;
            }
            Started = false;
            _completionSource.SetResult(true);
        }
        catch { /* SWALLOW */ }
        finally { _cpLock.Release(); }
    }

    private static readonly string _consumerPipelineQueueing = "Consumer ({0}) pipeline engine queueing unit of work (receivedMessage:DT:{1}).";
    private static readonly string _consumerPipelineWaiting = "Consumer ({0}) pipeline engine waiting on completion of unit of work (receivedMessage:DT:{1})...";
    private static readonly string _consumerPipelineWaitingDone = "Consumer ({0}) pipeline engine waiting on completed unit of work (receivedMessage:DT:{1}).";
    private static readonly string _consumerPipelineActionCancelled = "Consumer ({0}) pipeline engine actions were cancelled.";
    private static readonly string _consumerPipelineError = "Consumer ({0}) pipeline engine encountered an error. Error: {1}";

    public async Task PipelineStreamEngineAsync(
        IPipeline<PipeReceivedMessage, TOut> pipeline,
        bool waitForCompletion,
        CancellationToken token = default)
    {
        try
        {
            await foreach (var receivedMessage in Consumer.GetConsumerBuffer().ReadAllAsync(token))
            {
                if (receivedMessage is null) { continue; }

                _logger.LogDebug(
                    _consumerPipelineQueueing,
                    ConsumerOptions.ConsumerName,
                    receivedMessage.DeliveryTag);

                await pipeline
                    .QueueForExecutionAsync(receivedMessage)
                    .ConfigureAwait(false);

                if (waitForCompletion)
                {
                    _logger.LogTrace(
                        _consumerPipelineWaiting,
                        ConsumerOptions.ConsumerName,
                        receivedMessage.DeliveryTag);

                    await receivedMessage
                        .Completion
                        .ConfigureAwait(false);

                    _logger.LogTrace(
                        _consumerPipelineWaitingDone,
                        ConsumerOptions.ConsumerName,
                        receivedMessage.DeliveryTag);
                }

                if (token.IsCancellationRequested)
                { return; }
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning(
                _consumerPipelineActionCancelled,
                ConsumerOptions.ConsumerName);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                _consumerPipelineError,
                ConsumerOptions.ConsumerName,
                ex.Message);
        }
    }

    public async Task PipelineExecutionEngineAsync(
        IPipeline<PipeReceivedMessage, TOut> pipeline,
        bool waitForCompletion,
        CancellationToken token = default)
    {
        try
        {
            while (await Consumer.GetConsumerBuffer().WaitToReadAsync(token).ConfigureAwait(false))
            {
                while (Consumer.GetConsumerBuffer().TryRead(out var receivedMessage))
                {
                    if (receivedMessage is null) { continue; }

                    _logger.LogDebug(
                        _consumerPipelineQueueing,
                        ConsumerOptions.ConsumerName,
                        receivedMessage.DeliveryTag);

                    await pipeline
                        .QueueForExecutionAsync(receivedMessage)
                        .ConfigureAwait(false);

                    if (waitForCompletion)
                    {
                        _logger.LogTrace(
                            _consumerPipelineWaiting,
                            ConsumerOptions.ConsumerName,
                            receivedMessage.DeliveryTag);

                        await receivedMessage
                            .Completion
                            .ConfigureAwait(false);

                        _logger.LogTrace(
                            _consumerPipelineWaitingDone,
                            ConsumerOptions.ConsumerName,
                            receivedMessage.DeliveryTag);
                    }

                    if (token.IsCancellationRequested)
                    { return; }
                }
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation(
                _consumerPipelineActionCancelled,
                ConsumerOptions.ConsumerName);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                _consumerPipelineError,
                ConsumerOptions.ConsumerName,
                ex.Message);
        }
    }

    public async Task AwaitCompletionAsync()
    {
        await _completionSource.Task.ConfigureAwait(false);
    }

    private bool _disposedValue;

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposedValue)
        {
            if (disposing)
            {
                _cpLock.Dispose();
                _cancellationTokenSource?.Dispose();
            }

            _disposedValue = true;
        }
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
