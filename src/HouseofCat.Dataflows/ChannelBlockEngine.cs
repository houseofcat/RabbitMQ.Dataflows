using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace HouseofCat.Dataflows;

public class ChannelBlockEngine<TIn, TOut> : ChannelReaderBlockEngine<TIn, TOut>, IDataflowEngine<TIn>
{
    protected ChannelBlock<TIn> _channelBlock;

    public ChannelBlockEngine(
        Func<TIn, Task<TOut>> workBodyAsync,
        int maxDegreeOfParallelism,
        bool ensureOrdered,
        int boundedCapacity = 1000,
        TaskScheduler taskScheduler = null,
        CancellationToken token = default) :
        this(workBodyAsync, maxDegreeOfParallelism, ensureOrdered, null, boundedCapacity, taskScheduler, token)
    { }

    public ChannelBlockEngine(
        Func<TIn, Task<TOut>> workBodyAsync,
        int maxDegreeOfParallelism,
        bool ensureOrdered,
        Func<TOut, Task> postWorkBodyAsync,
        int boundedCapacity = 1000,
        TaskScheduler taskScheduler = null,
        CancellationToken token = default) :
        base(workBodyAsync, maxDegreeOfParallelism, ensureOrdered, taskScheduler, postWorkBodyAsync)
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
        base(workBodyAsync, maxDegreeOfParallelism, ensureOrdered, taskScheduler)
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
        base(workBodyAsync, maxDegreeOfParallelism, ensureOrdered, taskScheduler)
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
        base(workBodyAsync, maxDegreeOfParallelism, ensureOrdered, taskScheduler)
    {
        _channelBlock = new ChannelBlock<TIn>(channel);
        _channelBlock.LinkTo(_workBlock);
        _channelBlock.StartReadChannel(token);
    }

    public virtual async ValueTask EnqueueWorkAsync(TIn data)
    {
        await _channelBlock
            .SendAsync(data)
            .ConfigureAwait(false);
    }

    public virtual Task ExecuteWorkBodyAsync(TIn data)
    {
        throw new NotImplementedException();
    }
}
