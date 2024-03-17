using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace HouseofCat.Dataflows;

public static class DataflowExtensions
{
    private static readonly DataflowLinkOptions _options = new DataflowLinkOptions { PropagateCompletion = true };

    public static IDisposable LinkWithCompletion<T>(this ISourceBlock<T> source, ITargetBlock<T> target)
    {
        return source.LinkTo(target, _options);
    }

    public static IDisposable LinkWithCompletion<T>(this ISourceBlock<T> source, ITargetBlock<T> target, Predicate<T> predicate)
    {
        return source.LinkTo(target, _options, predicate);
    }

    public static async Task CompleteAsync<T>(this ISourceBlock<T> source, ITargetBlock<T> targetBlock)
    {
        source.Complete();
        await Task.WhenAll(source.Completion, targetBlock.Completion).ConfigureAwait(false);
    }

    public static async Task CompleteAsync<T>(this ISourceBlock<T> source, IEnumerable<ITargetBlock<T>> targetBlocks)
    {
        source.Complete();
        await Task.WhenAll(targetBlocks.Select(tb => tb.Completion)).ConfigureAwait(false);
    }

    public static async Task CompleteAfterTargetFinishAsync<T>(this ISourceBlock<T> source, ITargetBlock<T> targetBlock)
    {
        await targetBlock.Completion.ContinueWith(_ => source.Complete()).ConfigureAwait(false);
        await source.Completion.ConfigureAwait(false);
    }

    public static async Task CompleteAfterTargetsFinishAsync<T>(this ISourceBlock<T> source, IEnumerable<ITargetBlock<T>> targetBlocks)
    {
        await Task.WhenAll(targetBlocks.Select(tb => tb.Completion)).ContinueWith(_ => source.Complete()).ConfigureAwait(false);
        await source.Completion.ConfigureAwait(false);
    }

    public static IPropagatorBlock<TIn, TIn> CreateFilterBlock<TIn>(Predicate<TIn> predicate)
    {
        var outputBuffer = new BufferBlock<TIn>();
        var filterAction = new ActionBlock<TIn>(
            async (input) =>
            {
                if (predicate(input))
                {
                    await outputBuffer
                        .SendAsync(input)
                        .ConfigureAwait(false);
                }
            });

        // Link faulted status or trigger completion at time of filter ActionBlock achieving completion.
        filterAction.Completion.ContinueWith(
            task =>
            {
                if (task.IsFaulted)
                {
                    ((ITargetBlock<TIn>)outputBuffer).Fault(task.Exception?.Flatten().InnerException);
                }
                else
                { outputBuffer.Complete(); }
            });

        return DataflowBlock.Encapsulate(filterAction, outputBuffer);
    }
}
