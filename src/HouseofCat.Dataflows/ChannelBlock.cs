using HouseofCat.Utilities.Errors;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace HouseofCat.Dataflows;

public class ChannelBlock<TOut> : ChannelReaderBlock<TOut>, IPropagatorBlock<TOut, TOut>, IReceivableSourceBlock<TOut>
{
    protected readonly Channel<TOut> _channel;

    public ChannelBlock(BoundedChannelOptions options, Func<TOut, TOut> optionalfirstStep = null) : 
        this(Channel.CreateBounded<TOut>(options), new TransformBlock<TOut, TOut>(optionalfirstStep ?? (input => input)))
    {
        Guard.AgainstNull(options, nameof(options));
    }

    public ChannelBlock(UnboundedChannelOptions options, Func<TOut, TOut> optionalfirstStep = null) :
        this(Channel.CreateUnbounded<TOut>(options), new TransformBlock<TOut, TOut>(optionalfirstStep ?? (input => input)))
    {
        Guard.AgainstNull(options, nameof(options));
    }

    public ChannelBlock(Channel<TOut> channel, Func<TOut, TOut> optionalfirstStep = null) : 
        this(channel, new TransformBlock<TOut, TOut>(optionalfirstStep ?? (input => input)))
    { }

    public ChannelBlock(Channel<TOut> channel, ITargetBlock<TOut> targetBlock) : base(channel?.Reader, targetBlock)
    {
        _channel = channel;
    }

    protected CancellationTokenSource _cts;
    protected Task _channelProcessing;

    public void StartReadChannel(CancellationToken token = default)
    {
        if (!token.Equals(default))
        {
            _channelProcessing = ReadChannelAsync(token);
        }

        _cts = new CancellationTokenSource();
        _channelProcessing = ReadChannelAsync(_cts.Token);
    }

    public async Task StopChannelAsync()
    {
        _channel.Writer.Complete();
        _cts?.Cancel();
        await _channelProcessing.ConfigureAwait(false);
    }

    public DataflowMessageStatus OfferMessage(DataflowMessageHeader messageHeader, TOut messageValue, ISourceBlock<TOut> source, bool consumeToAccept)
    {
        return _targetBlock.OfferMessage(messageHeader, messageValue, source, consumeToAccept);
    }

    public async Task<bool> SendAsync(TOut input, CancellationToken cancellationToken = default)
    {
        if (await _channel.Writer.WaitToWriteAsync(cancellationToken))
        {
            await _channel.Writer.WriteAsync(input, cancellationToken);
            return true;
        }

        return false;
    }

    public bool TryReceiveAll(out IList<TOut> items)
    {
        throw new NotImplementedException();
    }

    public bool TryReceive(Predicate<TOut> filter, out TOut item)
    {
        throw new NotImplementedException();
    }

    // Slow - leave for testing purposes (CPU utilization, overall, is lower here).
    protected virtual async Task StreamOutBufferAsync(CancellationToken token = default)
    {
        try
        {
            var reader = _channel.Reader;
            await foreach (var message in reader.ReadAllAsync(token).ConfigureAwait(false))
            {
                await _targetBlock.SendAsync(message, token).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        { _logger.LogDebug("Consumer task was cancelled. Disregard if this was manually invoked."); }
        catch (Exception ex)
        { _logger.LogError(ex, "Reading consumer buffer threw an exception."); }
    }
}
