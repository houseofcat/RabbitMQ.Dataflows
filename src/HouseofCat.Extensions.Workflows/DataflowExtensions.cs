using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace HouseofCat.Extensions.Workflows
{
    public static class DataflowExtensions
    {
        private static DataflowLinkOptions _options = new DataflowLinkOptions { PropagateCompletion = true };

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
            await Task.WhenAll(source.Completion, targetBlock.Completion);
        }

        public static async Task CompleteAsync<T>(this ISourceBlock<T> source, IEnumerable<ITargetBlock<T>> targetBlocks)
        {
            source.Complete();
            await Task.WhenAll(targetBlocks.Select(tb => tb.Completion));
        }

        public static async Task CompleteAfterTargetFinishAsync<T>(this ISourceBlock<T> source, ITargetBlock<T> targetBlock)
        {
            await targetBlock.Completion.ContinueWith(_ => source.Complete());
            await source.Completion;
        }

        public static async Task CompleteAfterTargetsFinishAsync<T>(this ISourceBlock<T> source, IEnumerable<ITargetBlock<T>> targetBlocks)
        {
            await Task.WhenAll(targetBlocks.Select(tb => tb.Completion)).ContinueWith(_ => source.Complete());
            await source.Completion;
        }

        public static IPropagatorBlock<TIn, TIn> CreateFilterBlock<TIn>(Predicate<TIn> predicate)
        {
            var output = new BufferBlock<TIn>();
            var filterAction = new ActionBlock<TIn>(
                async (input) =>
                {
                    if (predicate(input))
                    {
                        await output.SendAsync(input).ConfigureAwait(false);
                    }
                });

            // Link faulted status or trigger completion at time of filter ActionBlock achieving completion.
            filterAction.Completion.ContinueWith(
                task =>
                {
                    if (task.IsFaulted)
                    {
                        ((ITargetBlock<TIn>)output).Fault(task.Exception?.Flatten().InnerException);
                    }
                    else
                    { output.Complete(); }
                });

            return DataflowBlock.Encapsulate(filterAction, output);
        }
    }
}
