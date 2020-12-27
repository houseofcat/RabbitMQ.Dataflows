using HouseofCat.Compression;
using HouseofCat.Encryption;
using HouseofCat.Metrics;
using HouseofCat.Serialization;
using System;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace HouseofCat.Dataflows
{
    public abstract class BaseDataflow<TState> where TState : class, IWorkState, new()
    {
        protected ExecutionDataflowBlockOptions _executeStepOptions;
        protected DataflowLinkOptions _linkStepOptions;

        protected ISerializationProvider _serializationProvider;
        protected IEncryptionProvider _encryptionProvider;
        protected ICompressionProvider _compressProvider;
        protected IMetricsProvider _metricsProvider;

        protected ISourceBlock<TState> _currentBlock;
        public Task Completion { get; protected set; }

        protected void SetCurrentSourceBlock(IDataflowBlock block)
        {
            _currentBlock = (ISourceBlock<TState>)block;
        }

        protected ExecutionDataflowBlockOptions GetExecuteStepOptions(int? maxDoP, bool? ensureOrdered, int? boundedCapacity)
        {
            if (maxDoP.HasValue || ensureOrdered.HasValue || boundedCapacity.HasValue)
            {
                return new ExecutionDataflowBlockOptions
                {
                    BoundedCapacity = boundedCapacity ?? _executeStepOptions.BoundedCapacity,
                    EnsureOrdered = ensureOrdered ?? _executeStepOptions.EnsureOrdered,
                    MaxDegreeOfParallelism = maxDoP ?? _executeStepOptions.MaxDegreeOfParallelism
                };
            }

            return _executeStepOptions;
        }

        public TransformBlock<TState, TState> GetTransformBlock(
            Func<TState, Task<TState>> action,
            ExecutionDataflowBlockOptions options)
        {
            return new TransformBlock<TState, TState>(action, options);
        }

        public TransformBlock<TState, TState> GetTransformBlock(
            Func<TState, TState> action,
            ExecutionDataflowBlockOptions options)
        {
            return new TransformBlock<TState, TState>(action, options);
        }

        public TransformBlock<TState, TState> GetWrappedTransformBlock(
            Func<TState, TState> action,
            ExecutionDataflowBlockOptions options,
            string metricIdentifier,
            bool metricMicroScale = false,
            string metricDescription = null)
        {
            TState WrapAction(TState state)
            {
                try
                {
                    using var multiDispose = _metricsProvider.TrackAndDuration(metricIdentifier, metricMicroScale, metricDescription);
                    return action(state);
                }
                catch (Exception ex)
                {
                    state.IsFaulted = true;
                    state.EDI = ExceptionDispatchInfo.Capture(ex);
                    return state;
                }
            }

            return new TransformBlock<TState, TState>(WrapAction, options);
        }

        public TransformBlock<TState, TState> GetWrappedTransformBlock(
            Func<TState, Task<TState>> action,
            ExecutionDataflowBlockOptions options,
            string metricIdentifier,
            bool metricMicroScale = false,
            string metricDescription = null)
        {
            async Task<TState> WrapActionAsync(TState state)
            {
                try
                {
                    using var multiDispose = _metricsProvider.TrackAndDuration(metricIdentifier, metricMicroScale, metricDescription);
                    return await action(state).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    state.IsFaulted = true;
                    state.EDI = ExceptionDispatchInfo.Capture(ex);
                    return state;
                }
            }

            return new TransformBlock<TState, TState>(WrapActionAsync, options);
        }

        public ActionBlock<TState> GetWrappedActionBlock(
            Action<TState> action,
            ExecutionDataflowBlockOptions options,
            string metricIdentifier,
            bool metricMicroScale = false,
            string metricDescription = null)
        {
            void WrapAction(TState state)
            {
                try
                {
                    using var multiDispose = _metricsProvider.TrackAndDuration(metricIdentifier, metricMicroScale, metricDescription);
                    action(state);
                }
                catch
                { /* Actions are terminating block, so swallow (maybe log) */ }
            }

            return new ActionBlock<TState>(WrapAction, options);
        }

        public ActionBlock<TState> GetWrappedActionBlock(
            Func<TState, TState> action,
            ExecutionDataflowBlockOptions options,
            string metricIdentifier,
            bool metricMicroScale = false,
            string metricDescription = null)
        {
            void WrapAction(TState state)
            {
                try
                {
                    using var multiDispose = _metricsProvider.TrackAndDuration(metricIdentifier, metricMicroScale, metricDescription);
                    action(state);
                }
                catch
                { /* Actions are terminating block, so swallow (maybe log) */ }
            }

            return new ActionBlock<TState>(WrapAction, options);
        }

        public ActionBlock<TState> GetWrappedActionBlock(
            Func<TState, Task> action,
            ExecutionDataflowBlockOptions options,
            string metricIdentifier,
            bool metricMicroScale = false,
            string metricDescription = null)
        {
            async Task WrapActionAsync(TState state)
            {
                try
                {
                    using var multiDispose = _metricsProvider.TrackAndDuration(metricIdentifier, metricMicroScale, metricDescription);
                    await action(state).ConfigureAwait(false);
                }
                catch
                { /* Actions are terminating block, so swallow (maybe log) */ }
            }

            return new ActionBlock<TState>(WrapActionAsync, options);
        }
    }
}
