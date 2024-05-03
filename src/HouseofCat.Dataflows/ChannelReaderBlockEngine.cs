using HouseofCat.Utilities.Helpers;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace HouseofCat.Dataflows;

public class ChannelReaderBlockEngine<TIn, TOut>
{
    protected readonly ActionBlock<TIn> _workBlock;

    private readonly ILogger<ChannelReaderBlockEngine<TIn, TOut>> _logger;
    private readonly ExecutionDataflowBlockOptions _executeOptions;
    private readonly ChannelReaderBlock<TIn> _channelReaderBlock;
    private readonly Func<TIn, Task<TOut>> _workBodyAsync;
    private readonly Func<TOut, Task> _postWorkBodyAsync;

    public ChannelReaderBlockEngine(
        ChannelReader<TIn> channelReader,
        Func<TIn, Task<TOut>> workBodyAsync,
        int? maxDegreeOfParallelism,
        bool? ensureOrdered,
        Func<TOut, Task> postWorkBodyAsync = null,
        TaskScheduler taskScheduler = null) :
        this(workBodyAsync, maxDegreeOfParallelism, ensureOrdered, taskScheduler, postWorkBodyAsync)
    {
        _channelReaderBlock = new ChannelReaderBlock<TIn>(channelReader, _executeOptions);
        _channelReaderBlock.LinkTo(_workBlock);
    }

    protected ChannelReaderBlockEngine(
        Func<TIn, Task<TOut>> workBodyAsync,
        int? maxDegreeOfParallelism,
        bool? ensureOrdered,
        TaskScheduler taskScheduler,
        Func<TOut, Task> postWorkBodyAsync = null)
    {
        _logger = LogHelpers.GetLogger<ChannelReaderBlockEngine<TIn, TOut>>();
        _workBodyAsync = workBodyAsync ?? throw new ArgumentNullException(nameof(workBodyAsync));

        _executeOptions = new ExecutionDataflowBlockOptions
        {
            MaxDegreeOfParallelism = maxDegreeOfParallelism ?? DataflowBlockOptions.Unbounded,
            EnsureOrdered = ensureOrdered ?? false
        };

        _postWorkBodyAsync = postWorkBodyAsync;

        if (taskScheduler is not null)
        {
            _executeOptions.TaskScheduler = taskScheduler;
        }

        _workBlock = new ActionBlock<TIn>(ExecuteWorkBodyAsync, _executeOptions);
    }

    public async ValueTask ReadChannelAsync(CancellationToken token = default) =>
        await _channelReaderBlock.ReadChannelAsync(token).ConfigureAwait(false);

    private static readonly string _error = "Execption occurred in the Dataflow Engine when running work body steps.";

    private async Task ExecuteWorkBodyAsync(TIn data)
    {
        try
        {
            var output = await _workBodyAsync(data).ConfigureAwait(false);
            if (_postWorkBodyAsync is not null
                && !EqualityComparer<TIn>.Default.Equals(data, default))
            {
                await _postWorkBodyAsync(output).ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, _error);
        }
    }
}

