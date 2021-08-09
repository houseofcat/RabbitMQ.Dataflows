using HouseofCat.Logger;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Channels;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace HouseofCat.Dataflows
{
    public class ChannelBlockEngine<TIn, TOut>
    {
        private readonly ILogger<ChannelBlockEngine<TIn, TOut>> _logger;
        private readonly ChannelBlock<TIn> _channelBlock;
        private readonly ActionBlock<TIn> _workBlock;
        private readonly Func<TIn, Task<TOut>> _workBodyAsync;
        private readonly Func<TOut, Task> _postWorkBodyAsync;

        public ChannelBlockEngine(
            Func<TIn, Task<TOut>> workBodyAsync,
            int maxDegreeOfParallelism,
            bool ensureOrdered,
            Func<TOut, Task> postWorkBodyAsync = null,
            int boundedCapacity = 1000,
            TaskScheduler taskScheduler = null) : this(workBodyAsync, maxDegreeOfParallelism, ensureOrdered, boundedCapacity, taskScheduler)
        {
            _postWorkBodyAsync = postWorkBodyAsync;
        }

        public ChannelBlockEngine(
            Func<TIn, Task<TOut>> workBodyAsync,
            int maxDegreeOfParallelism,
            bool ensureOrdered,
            int boundedCapacity = 1000,
            TaskScheduler taskScheduler = null) : this(workBodyAsync, maxDegreeOfParallelism, ensureOrdered, taskScheduler)
        {
            if (boundedCapacity > 0)
            { _channelBlock = new ChannelBlock<TIn>(new BoundedChannelOptions(boundedCapacity)); }
            else
            { _channelBlock = new ChannelBlock<TIn>(new UnboundedChannelOptions()); }
        }

        public ChannelBlockEngine(
            Func<TIn, Task<TOut>> workBodyAsync,
            int maxDegreeOfParallelism,
            bool ensureOrdered,
            BoundedChannelOptions options,
            TaskScheduler taskScheduler = null) : this(workBodyAsync, maxDegreeOfParallelism, ensureOrdered, taskScheduler)
        {
            _channelBlock = new ChannelBlock<TIn>(options);
        }

        public ChannelBlockEngine(
            Func<TIn, Task<TOut>> workBodyAsync,
            int maxDegreeOfParallelism,
            bool ensureOrdered,
            UnboundedChannelOptions options,
            TaskScheduler taskScheduler = null) : this(workBodyAsync, maxDegreeOfParallelism, ensureOrdered, taskScheduler)
        {
            _channelBlock = new ChannelBlock<TIn>(options);
        }

        public ChannelBlockEngine(
            Channel<TIn> channel,
            Func<TIn, Task<TOut>> workBodyAsync,
            int maxDegreeOfParallelism,
            bool ensureOrdered,
            TaskScheduler taskScheduler = null) : this(workBodyAsync, maxDegreeOfParallelism, ensureOrdered, taskScheduler)
        {
            _channelBlock = new ChannelBlock<TIn>(channel);
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

        private async Task ExecuteWorkBodyAsync(TIn data)
        {
            try
            {
                if (_postWorkBodyAsync != null)
                {
                    var output = await _workBodyAsync(data).ConfigureAwait(false);
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

        public async ValueTask EnqueueWorkAsync(TIn data)
        {
            await _channelBlock
                .SendAsync(data)
                .ConfigureAwait(false);
        }
    }
}
