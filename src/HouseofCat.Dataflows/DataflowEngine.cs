using HouseofCat.Utilities.Helpers;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace HouseofCat.Dataflows;

public interface IDataflowEngine<in TIn>
{
    Task ExecuteWorkBodyAsync(TIn data);
    ValueTask EnqueueWorkAsync(TIn data);
}

public class DataflowEngine<TIn, TOut> : IDataflowEngine<TIn>
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
        _logger = LogHelpers.GetLogger<DataflowEngine<TIn, TOut>>();
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

        if (taskScheduler is not null)
        { executeOptions.TaskScheduler = taskScheduler ?? TaskScheduler.Current; }

        _workBlock = new ActionBlock<TIn>(
            ExecuteWorkBodyAsync,
            executeOptions);

        _bufferBlock.LinkTo(_workBlock);
    }

    private static readonly string _error = "Execption occurred in the Dataflow Engine when running work body steps.";

    public virtual async Task ExecuteWorkBodyAsync(TIn data)
    {
        await Task.Yield();

        try
        {
            if (_preWorkBodyAsync is not null)
            {
                data = await _preWorkBodyAsync(data).ConfigureAwait(false);
            }

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

    public virtual async ValueTask EnqueueWorkAsync(TIn data)
    {
        await _bufferBlock
            .SendAsync(data)
            .ConfigureAwait(false);
    }
}
