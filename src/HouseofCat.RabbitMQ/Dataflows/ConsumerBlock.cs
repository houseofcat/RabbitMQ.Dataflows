using HouseofCat.Utilities.Errors;
using HouseofCat.Utilities.Helpers;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace HouseofCat.RabbitMQ.Dataflows;

public class ConsumerBlock<TOut> : ISourceBlock<TOut>, IDisposable
{
    public Task Completion { get; }

    public IConsumer<TOut> Consumer { get; set; }

    private readonly ILogger<ConsumerBlock<TOut>> _logger;

    private readonly ITargetBlock<TOut> _bufferBlock;
    private readonly ISourceBlock<TOut> _sourceBufferBlock;

    private CancellationTokenSource _cts;
    private Task _bufferProcessor;
    private bool disposedValue;

    public ConsumerBlock() : this(new BufferBlock<TOut>())
    { }

    public ConsumerBlock(IConsumer<TOut> consumer) : this()
    {
        Guard.AgainstNull(consumer, nameof(consumer));
        Consumer = consumer;
    }

    protected ConsumerBlock(ITargetBlock<TOut> bufferBlock) : this(bufferBlock, (ISourceBlock<TOut>)bufferBlock)
    { }

    protected ConsumerBlock(ITargetBlock<TOut> bufferBlock, ISourceBlock<TOut> sourceBufferBlock)
    {
        _logger = LogHelpers.LoggerFactory.CreateLogger<ConsumerBlock<TOut>>();
        _bufferBlock = bufferBlock;
        _sourceBufferBlock = sourceBufferBlock;
        Completion = _bufferBlock.Completion;
    }

    public async Task StartConsumingAsync()
    {
        _cts = new CancellationTokenSource();
        await Consumer.StartConsumerAsync().ConfigureAwait(false);
        _bufferProcessor = PushToBufferBlockAsync(_cts.Token);
    }

    public async Task StopConsumingAsync(bool immediate = false)
    {
        await Consumer.StopConsumerAsync(immediate).ConfigureAwait(false);
        _cts.Cancel();
        await _bufferProcessor.ConfigureAwait(false);
    }

    public DataflowMessageStatus OfferMessage(DataflowMessageHeader messageHeader, TOut messageValue, ISourceBlock<TOut> source, bool consumeToAccept)
    {
        return _bufferBlock.OfferMessage(messageHeader, messageValue, source, consumeToAccept);
    }

    public void Complete()
    {
        _bufferBlock.Complete();
    }

    public void Fault(Exception exception)
    {
        _bufferBlock.Fault(exception);
    }

    public IDisposable LinkTo(ITargetBlock<TOut> target, DataflowLinkOptions linkOptions)
    {
        return _sourceBufferBlock.LinkTo(target, linkOptions);
    }

    public TOut ConsumeMessage(DataflowMessageHeader messageHeader, ITargetBlock<TOut> target, out bool messageConsumed)
    {
        throw new NotImplementedException();
    }

    public void ReleaseReservation(DataflowMessageHeader messageHeader, ITargetBlock<TOut> target)
    {
        throw new NotImplementedException();
    }

    public bool ReserveMessage(DataflowMessageHeader messageHeader, ITargetBlock<TOut> target)
    {
        throw new NotImplementedException();
    }

    // Fast
    protected virtual async Task PushToBufferBlockAsync(CancellationToken token = default)
    {
        try
        {
            var consumerBuffer = Consumer.GetConsumerBuffer();
            while (await consumerBuffer.WaitToReadAsync(token).ConfigureAwait(false))
            {
                while (consumerBuffer.TryRead(out var message))
                {
                    await _bufferBlock.SendAsync(message, token).ConfigureAwait(false);
                }
            }
        }
        catch (OperationCanceledException)
        { _logger.LogDebug("Consumer task was cancelled. Disregard if this was manually invoked."); }
        catch (Exception ex)
        { _logger.LogError(ex, "Reading consumer buffer threw an exception."); }
    }

    // Slow - leave for testing purposes (CPU utilization is lower here).
    protected virtual async Task StreamToBufferAsync(CancellationToken token = default)
    {
        try
        {
            await foreach (var message in Consumer.GetConsumerBuffer().ReadAllAsync(token).ConfigureAwait(false))
            {
                await _bufferBlock.SendAsync(message, token).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        { _logger.LogDebug("Consumer task was cancelled. Disregard if this was manually invoked."); }
        catch (Exception ex)
        { _logger.LogError(ex, "Reading consumer buffer threw an exception."); }
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                _cts?.Dispose();
            }

            disposedValue = true;
        }
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
