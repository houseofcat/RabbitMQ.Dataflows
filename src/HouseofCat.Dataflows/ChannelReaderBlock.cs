using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using HouseofCat.Logger;
using HouseofCat.Utilities.Errors;
using Microsoft.Extensions.Logging;

namespace HouseofCat.Dataflows
{
    public class ChannelReaderBlock<TOut> : ISourceBlock<TOut>
    {
        public Task Completion { get; }

        protected readonly Channel<TOut> _channel;
        protected readonly ITargetBlock<TOut> _targetBlock;

        private readonly ILogger<ChannelReaderBlock<TOut>> _logger;
        private readonly ChannelReader<TOut> _channelReader;
        private readonly ISourceBlock<TOut> _sourceFromTargetBlock;

        public ChannelReaderBlock(ChannelReader<TOut> channelReader, ExecutionDataflowBlockOptions executeOptions) :
            this(channelReader, new TransformBlock<TOut, TOut>(input => input, executeOptions))
        {
        }

        protected ChannelReaderBlock(Channel<TOut> channel, ITargetBlock<TOut> targetBlock) : 
            this(channel?.Reader, targetBlock)
        {
            _channel = channel;
        }

        private ChannelReaderBlock(ChannelReader<TOut> channelReader, ITargetBlock<TOut> targetBlock)
        {
            Guard.AgainstNull(channelReader, nameof(channelReader));
            _logger = LogHelper.LoggerFactory.CreateLogger<ChannelReaderBlock<TOut>>();
            _channelReader = channelReader;

            if (targetBlock is ISourceBlock<TOut> sourceBlock)
            {
                _targetBlock = targetBlock;
                _sourceFromTargetBlock = sourceBlock;
                Completion = _targetBlock.Completion;
            }
            else
            {
                throw new InvalidCastException($"{nameof(targetBlock)} doesn't implement {typeof(ISourceBlock<TOut>)}");
            }
        }

        public async ValueTask ReadChannelAsync(CancellationToken token = default)
        {
            try
            {
                while (await _channelReader.WaitToReadAsync(token).ConfigureAwait(false))
                {
                    var message = await _channelReader.ReadAsync(token).ConfigureAwait(false);
                    if (message != null)
                    {
                        await _targetBlock.SendAsync(message, token).ConfigureAwait(false);
                    }
                    
                    if (token.IsCancellationRequested) return;
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogDebug("Consumer task was cancelled. Disregard if this was manually invoked.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Reading consumer buffer threw an exception");
            }
        }

        public void Complete() => _targetBlock.Complete();

        public void Fault(Exception exception) => _targetBlock.Fault(exception);

        public IDisposable LinkTo(ITargetBlock<TOut> target, DataflowLinkOptions linkOptions) =>
            _sourceFromTargetBlock.LinkTo(target, linkOptions);

        public TOut ConsumeMessage(
            DataflowMessageHeader messageHeader, ITargetBlock<TOut> target, out bool messageConsumed) =>
            throw new NotImplementedException();

        public void ReleaseReservation(DataflowMessageHeader messageHeader, ITargetBlock<TOut> target) =>
            throw new NotImplementedException();

        public bool ReserveMessage(DataflowMessageHeader messageHeader, ITargetBlock<TOut> target) =>
            throw new NotImplementedException();
    }
}