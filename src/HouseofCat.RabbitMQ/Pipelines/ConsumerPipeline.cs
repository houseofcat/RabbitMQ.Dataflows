using HouseofCat.Dataflows.Pipelines;
using HouseofCat.RabbitMQ.Dataflows;
using HouseofCat.Utilities.Helpers;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using static HouseofCat.RabbitMQ.Pipelines.Constants;

namespace HouseofCat.RabbitMQ.Pipelines;

public interface IConsumerPipeline<TOut> where TOut : RabbitWorkState
{
    string ConsumerPipelineName { get; }
    ConsumerOptions ConsumerOptions { get; }
    bool Started { get; }

    Task AwaitCompletionAsync();
    Task StartAsync(bool useStream, CancellationToken cancellationToken = default);
    Task StopAsync(bool immediate = false);
}

public class ConsumerPipeline<TOut> : IConsumerPipeline<TOut>, IDisposable where TOut : RabbitWorkState
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
    private bool _disposedValue;
    private readonly SemaphoreSlim _cpLock = new SemaphoreSlim(1, 1);
    private readonly SemaphoreSlim _pipeExecLock = new SemaphoreSlim(1, 1);

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
            if (!Started)
            {
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
            if (Started)
            {
                _cancellationTokenSource.Cancel();

                await Consumer
                    .StopConsumerAsync(immediate)
                    .ConfigureAwait(false);

                if (FeedPipelineWithDataTasks != null)
                {
                    await FeedPipelineWithDataTasks.ConfigureAwait(false);
                    FeedPipelineWithDataTasks = null;
                }
                Started = false;
                _completionSource.SetResult(true);
            }
        }
        catch { /* SWALLOW */ }
        finally { _cpLock.Release(); }
    }

    public async Task PipelineStreamEngineAsync(
        IPipeline<PipeReceivedMessage, TOut> pipeline,
        bool waitForCompletion,
        CancellationToken token = default)
    {
        await _pipeExecLock
            .WaitAsync(2000, token)
            .ConfigureAwait(false);

        try
        {
            await foreach (var receivedMessage in Consumer.GetConsumerBuffer().ReadAllAsync(token))
            {
                if (receivedMessage == null) { continue; }

                _logger.LogDebug(
                    ConsumerPipelines.ConsumerPipelineQueueing,
                    ConsumerOptions.ConsumerName,
                    receivedMessage.DeliveryTag);

                await pipeline
                    .QueueForExecutionAsync(receivedMessage)
                    .ConfigureAwait(false);

                if (waitForCompletion)
                {
                    _logger.LogTrace(
                        ConsumerPipelines.ConsumerPipelineWaiting,
                        ConsumerOptions.ConsumerName,
                        receivedMessage.DeliveryTag);

                    await receivedMessage
                        .Completion
                        .ConfigureAwait(false);

                    _logger.LogTrace(
                        ConsumerPipelines.ConsumerPipelineWaitingDone,
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
                ConsumerPipelines.ConsumerPipelineActionCancelled,
                ConsumerOptions.ConsumerName);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ConsumerPipelines.ConsumerPipelineError,
                ConsumerOptions.ConsumerName,
                ex.Message);
        }
        finally { _pipeExecLock.Release(); }
    }

    public async Task PipelineExecutionEngineAsync(
        IPipeline<PipeReceivedMessage, TOut> pipeline,
        bool waitForCompletion,
        CancellationToken token = default)
    {
        await _pipeExecLock
            .WaitAsync(2000, token)
            .ConfigureAwait(false);

        try
        {
            while (await Consumer.GetConsumerBuffer().WaitToReadAsync(token).ConfigureAwait(false))
            {
                while (Consumer.GetConsumerBuffer().TryRead(out var receivedMessage))
                {
                    if (receivedMessage == null) { continue; }

                    _logger.LogDebug(
                        ConsumerPipelines.ConsumerPipelineQueueing,
                        ConsumerOptions.ConsumerName,
                        receivedMessage.DeliveryTag);

                    await pipeline
                        .QueueForExecutionAsync(receivedMessage)
                        .ConfigureAwait(false);

                    if (waitForCompletion)
                    {
                        _logger.LogTrace(
                            ConsumerPipelines.ConsumerPipelineWaiting,
                            ConsumerOptions.ConsumerName,
                            receivedMessage.DeliveryTag);

                        await receivedMessage
                            .Completion
                            .ConfigureAwait(false);

                        _logger.LogTrace(
                            ConsumerPipelines.ConsumerPipelineWaitingDone,
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
                ConsumerPipelines.ConsumerPipelineActionCancelled,
                ConsumerOptions.ConsumerName);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ConsumerPipelines.ConsumerPipelineError,
                ConsumerOptions.ConsumerName,
                ex.Message);
        }
        finally { _pipeExecLock.Release(); }
    }

    public async Task AwaitCompletionAsync()
    {
        await _completionSource.Task.ConfigureAwait(false);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposedValue)
        {
            if (disposing)
            {
                _cpLock.Dispose();
                _pipeExecLock.Dispose();
                _cancellationTokenSource?.Dispose();
            }

            _disposedValue = true;
        }
    }

    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
