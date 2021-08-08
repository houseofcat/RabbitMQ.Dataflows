using HouseofCat.Logger;
using HouseofCat.Utilities.Errors;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace HouseofCat.RabbitMQ.Dataflows
{
    public class ConsumerBlock<TOut> : ISourceBlock<TOut>
    {
        public Task Completion { get; }

        private readonly ILogger<ConsumerBlock<TOut>> _logger;
        protected readonly IConsumer<TOut> _consumer;
        protected readonly ITargetBlock<TOut> _bufferBlock;
        protected readonly ISourceBlock<TOut> _sourceBufferBlock;

        private CancellationTokenSource _cts;
        private Task _bufferProcessor;

        public ConsumerBlock(IConsumer<TOut> consumer)
        {
            Guard.AgainstNull(consumer, nameof(consumer));
            _logger = LogHelper.LoggerFactory.CreateLogger<ConsumerBlock<TOut>>();
            _consumer = consumer;

            _bufferBlock = new BufferBlock<TOut>();
            _sourceBufferBlock = (ISourceBlock<TOut>)_bufferBlock;
            Completion = _bufferBlock.Completion;
        }

        public async Task StartConsumingAsync()
        {
            _cts = new CancellationTokenSource();
            await _consumer.StartConsumerAsync().ConfigureAwait(false);
            _bufferProcessor = PushToBufferAsync(_cts.Token);
        }

        public async Task StopConsumingAsync(bool immediate = false)
        {
            await _consumer.StopConsumerAsync(immediate).ConfigureAwait(false);
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
        protected virtual async Task PushToBufferAsync(CancellationToken token = default)
        {
            try
            {
                while (await _consumer.GetConsumerBuffer().WaitToReadAsync(token).ConfigureAwait(false))
                {
                    while (_consumer.GetConsumerBuffer().TryRead(out var message))
                    {
                        await _bufferBlock.SendAsync(message).ConfigureAwait(false);
                    }

                    if (token.IsCancellationRequested) return;
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
                await foreach (var message in _consumer.GetConsumerBuffer().ReadAllAsync(token).ConfigureAwait(false))
                {
                    await _bufferBlock.SendAsync(message).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException)
            { _logger.LogDebug("Consumer task was cancelled. Disregard if this was manually invoked."); }
            catch (Exception ex)
            { _logger.LogError(ex, "Reading consumer buffer threw an exception."); }
        }
    }
}
