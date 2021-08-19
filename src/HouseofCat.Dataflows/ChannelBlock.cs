using HouseofCat.Logger;
using HouseofCat.Utilities.Errors;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace HouseofCat.Dataflows
{
    public class ChannelBlock<TOut> : IPropagatorBlock<TOut, TOut>, IReceivableSourceBlock<TOut>
    {
        public Task Completion { get; }

        private readonly ILogger<ChannelBlock<TOut>> _logger;
        protected Channel<TOut> _channel;
        protected ITargetBlock<TOut> _targetBlock;
        protected ISourceBlock<TOut> _sourceFromTargetBlock;

        protected CancellationTokenSource _cts;
        protected Task _channelProcessing;

        public ChannelBlock(BoundedChannelOptions options, Func<TOut, TOut> optionalfirstStep = null)
        {
            Guard.AgainstNull(options, nameof(options));
            _logger = LogHelper.LoggerFactory.CreateLogger<ChannelBlock<TOut>>();
            _channel = Channel.CreateBounded<TOut>(options);

            _targetBlock = new TransformBlock<TOut, TOut>(optionalfirstStep ?? ((input) => input));
            _sourceFromTargetBlock = (ISourceBlock<TOut>)_targetBlock;
            Completion = _targetBlock.Completion;
        }

        public ChannelBlock(UnboundedChannelOptions options, Func<TOut, TOut> optionalfirstStep = null)
        {
            Guard.AgainstNull(options, nameof(options));
            _logger = LogHelper.LoggerFactory.CreateLogger<ChannelBlock<TOut>>();
            _channel = Channel.CreateUnbounded<TOut>(options);

            _targetBlock = new TransformBlock<TOut, TOut>(optionalfirstStep ?? ((input) => input));
            _sourceFromTargetBlock = (ISourceBlock<TOut>)_targetBlock;
            Completion = _targetBlock.Completion;
        }

        public ChannelBlock(Channel<TOut> channel, Func<TOut, TOut> optionalfirstStep = null)
        {
            Guard.AgainstNull(channel, nameof(channel));
            _logger = LogHelper.LoggerFactory.CreateLogger<ChannelBlock<TOut>>();
            _channel = channel;

            _targetBlock = new TransformBlock<TOut, TOut>(optionalfirstStep ?? ((input) => input));
            _sourceFromTargetBlock = (ISourceBlock<TOut>)_targetBlock;
            Completion = _targetBlock.Completion;
        }

        public ChannelBlock(Channel<TOut> channel, ITargetBlock<TOut> targetBlock)
        {
            Guard.AgainstNull(channel, nameof(channel));
            _logger = LogHelper.LoggerFactory.CreateLogger<ChannelBlock<TOut>>();
            _channel = channel;

            if (targetBlock is ISourceBlock<TOut> sourceBlock)
            {
                _targetBlock = targetBlock;
                _sourceFromTargetBlock = sourceBlock;
                Completion = _targetBlock.Completion;
            }
            else { throw new InvalidCastException($"{nameof(targetBlock)} doesn't implement {typeof(ISourceBlock<TOut>)}"); }
        }

        public void StartReadChannel(CancellationToken token = default)
        {
            _channelProcessing = ReadChannelAsync(
                token.Equals(default) ? (_cts = new CancellationTokenSource()).Token : token);
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

        public void Complete()
        {
            _targetBlock.Complete();
        }

        public void Fault(Exception exception)
        {
            _targetBlock.Fault(exception);
        }

        public IDisposable LinkTo(ITargetBlock<TOut> target, DataflowLinkOptions linkOptions)
        {
            return _sourceFromTargetBlock.LinkTo(target, linkOptions);
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

        public async Task<bool> SendAsync(TOut input, CancellationToken cancellationToken = default)
        {
            if (await _channel.Writer.WaitToWriteAsync())
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

        // Fast
        protected virtual async Task ReadChannelAsync(CancellationToken token = default)
        {
            try
            {
                var reader = _channel.Reader;
                while (await reader.WaitToReadAsync(token).ConfigureAwait(false))
                {
                    var message = await _channel.Reader.ReadAsync(token);
                    await _targetBlock.SendAsync(message, token).ConfigureAwait(false);

                    if (token.IsCancellationRequested) return;
                }
            }
            catch (OperationCanceledException)
            { _logger.LogDebug("Consumer task was cancelled. Disregard if this was manually invoked."); }
            catch (Exception ex)
            { _logger.LogError(ex, "Reading consumer buffer threw an exception."); }
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
}
