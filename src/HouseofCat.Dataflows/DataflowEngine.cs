using HouseofCat.Utilities;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace HouseofCat.Dataflows;

public class DataflowEngine<TIn, TOut> : IDataBlockEngine<TIn>
{
    private readonly ILogger<DataflowEngine<TIn, TOut>> _logger;
    protected BufferBlock<TIn> _bufferBlock;
    protected ActionBlock<TIn> _workBlock;
    protected Func<TIn, Task<TIn>> _preWorkBodyAsync;
    protected Func<TIn, Task<TOut>> _workBodyAsync;
    protected Func<TOut, Task> _postWorkBodyAsync;

    public DataflowEngine(
        Func<TIn, Task<TOut>> workBodyAsync,
        int maxDegreeOfParallelism,
        bool ensureOrdered,
        Func<TIn, Task<TIn>> preWorkBodyAsync = null,
        Func<TOut, Task> postWorkBodyAsync = null,
        int boundedCapacity = 1000,
        TaskScheduler taskScheduler = null) : this(workBodyAsync, maxDegreeOfParallelism, ensureOrdered, boundedCapacity, taskScheduler)
    {
        _preWorkBodyAsync = preWorkBodyAsync;
        _postWorkBodyAsync = postWorkBodyAsync;
    }

    public DataflowEngine(
        Func<TIn, Task<TOut>> workBodyAsync,
        int maxDegreeOfParallelism,
        bool ensureOrdered,
        int boundedCapacity = 1000,
        TaskScheduler taskScheduler = null)
    {
        _logger = LogHelper.GetLogger<DataflowEngine<TIn, TOut>>();
        _workBodyAsync = workBodyAsync ?? throw new ArgumentNullException(nameof(workBodyAsync));

        _bufferBlock = new BufferBlock<TIn>(
            new DataflowBlockOptions
            {
                BoundedCapacity = boundedCapacity,
                TaskScheduler = taskScheduler ?? TaskScheduler.Current
            });

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

        _bufferBlock.LinkTo(_workBlock);
    }

    private static readonly string _error = "Execption occurred in the Dataflow Engine when running work body steps.";

    protected virtual async Task ExecuteWorkBodyAsync(TIn data)
    {
        try
        {
            if (_preWorkBodyAsync != null)
            {
                data = await _preWorkBodyAsync(data).ConfigureAwait(false);
            }

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
            _logger.LogError(ex, _error);
        }
    }

    public virtual async ValueTask EnqueueWorkAsync(TIn data)
    {
        await _bufferBlock
            .SendAsync(data)
            .ConfigureAwait(false);
    }
}
