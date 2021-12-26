using HouseofCat.Logger;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace HouseofCat.Dataflows
{
    public class ChannelBlockEngine<TIn, TOut> : IDataBlockEngine<TIn>
    {
        private readonly ILogger<ChannelBlockEngine<TIn, TOut>> _logger;
        protected ChannelBlock<TIn> _channelBlock;
        protected ActionBlock<TIn> _workBlock;
        protected Func<TIn, Task<TOut>> _workBodyAsync;
        protected Func<TOut, Task> _postWorkBodyAsync;

        public ChannelBlockEngine(
            Func<TIn, Task<TOut>> workBodyAsync,
            int maxDegreeOfParallelism,
            bool ensureOrdered,
            Func<TOut, Task> postWorkBodyAsync = null,
            int boundedCapacity = 1000,
            TaskScheduler taskScheduler = null,
            CancellationToken token = default) :
            this(workBodyAsync, maxDegreeOfParallelism, ensureOrdered, boundedCapacity, taskScheduler, token)
        {
            _postWorkBodyAsync = postWorkBodyAsync;
        }

        public ChannelBlockEngine(
            Func<TIn, Task<TOut>> workBodyAsync,
            int maxDegreeOfParallelism,
            bool ensureOrdered,
            int boundedCapacity = 1000,
            TaskScheduler taskScheduler = null,
            CancellationToken token = default) :
            this(workBodyAsync, maxDegreeOfParallelism, ensureOrdered, taskScheduler)
        {
            if (boundedCapacity > 0)
            { _channelBlock = new ChannelBlock<TIn>(new BoundedChannelOptions(boundedCapacity)); }
            else
            { _channelBlock = new ChannelBlock<TIn>(new UnboundedChannelOptions()); }

            _channelBlock.LinkTo(_workBlock);
            _channelBlock.StartReadChannel(token);
        }

        public ChannelBlockEngine(
            Func<TIn, Task<TOut>> workBodyAsync,
            int maxDegreeOfParallelism,
            bool ensureOrdered,
            BoundedChannelOptions options,
            TaskScheduler taskScheduler = null,
            CancellationToken token = default) :
            this(workBodyAsync, maxDegreeOfParallelism, ensureOrdered, taskScheduler)
        {
            _channelBlock = new ChannelBlock<TIn>(options);
            _channelBlock.LinkTo(_workBlock);
            _channelBlock.StartReadChannel(token);
        }

        public ChannelBlockEngine(
            Func<TIn, Task<TOut>> workBodyAsync,
            int maxDegreeOfParallelism,
            bool ensureOrdered,
            UnboundedChannelOptions options,
            TaskScheduler taskScheduler = null,
            CancellationToken token = default) :
            this(workBodyAsync, maxDegreeOfParallelism, ensureOrdered, taskScheduler)
        {
            _channelBlock = new ChannelBlock<TIn>(options);
            _channelBlock.LinkTo(_workBlock);
            _channelBlock.StartReadChannel(token);
        }

        public ChannelBlockEngine(
            Channel<TIn> channel,
            Func<TIn, Task<TOut>> workBodyAsync,
            int maxDegreeOfParallelism,
            bool ensureOrdered,
            TaskScheduler taskScheduler = null,
            CancellationToken token = default) :
            this(workBodyAsync, maxDegreeOfParallelism, ensureOrdered, taskScheduler)
        {
            _channelBlock = new ChannelBlock<TIn>(channel);
            _channelBlock.LinkTo(_workBlock);
            _channelBlock.StartReadChannel(token);
        }

        private ChannelBlockEngine(
            Func<TIn, Task<TOut>> workBodyAsync,
            int maxDegreeOfParallelism,
            bool ensureOrdered,
            TaskScheduler taskScheduler = null)
        {
            _logger = LogHelper.GetLogger<ChannelBlockEngine<TIn, TOut>>();
            _workBodyAsync = workBodyAsync ?? throw new ArgumentNullException(nameof(workBodyAsync));

            var executeOptions = new ExecutionDataflowBlockOptions
            {
                MaxDegreeOfParallelism = maxDegreeOfParallelism,
                EnsureOrdered = ensureOrdered
            };

            if (taskScheduler != null)
            { executeOptions.TaskScheduler = taskScheduler ?? TaskScheduler.Current; }

            _workBlock = new ActionBlock<TIn>(
                ExecuteWorkBodyAsync,
                executeOptions);
        }

        protected virtual async Task ExecuteWorkBodyAsync(TIn data)
        {
            try
            {
                if (_postWorkBodyAsync != null)
                {
                    var output = await _workBodyAsync(data);
                    if (output != null)
                    {
                        await _postWorkBodyAsync(output).ConfigureAwait(false);
                    }
                }
                else
                {
                    await _workBodyAsync(data).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, Constants.Dataflows.Error);
            }
        }

        public virtual async ValueTask EnqueueWorkAsync(TIn data)
        {
            await _channelBlock
                .SendAsync(data)
                .ConfigureAwait(false);
        }
    }
}
