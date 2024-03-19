using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using HouseofCat.Utilities;
using Microsoft.Extensions.Logging;

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
        _logger = LogHelper.GetLogger<ChannelReaderBlockEngine<TIn, TOut>>();
        _workBodyAsync = workBodyAsync ?? throw new ArgumentNullException(nameof(workBodyAsync));

        _executeOptions = new ExecutionDataflowBlockOptions
        {
            MaxDegreeOfParallelism = maxDegreeOfParallelism ?? DataflowBlockOptions.Unbounded,
            EnsureOrdered = ensureOrdered ?? false
        };

        _postWorkBodyAsync = postWorkBodyAsync;
        
        if (taskScheduler != null)
        {
            _executeOptions.TaskScheduler = taskScheduler;
        }

        _workBlock = new ActionBlock<TIn>(ExecuteWorkBodyAsync, _executeOptions);
    }

    public async ValueTask ReadChannelAsync(CancellationToken token = default) =>
        await _channelReaderBlock.ReadChannelAsync(token).ConfigureAwait(false);
    
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
}    

