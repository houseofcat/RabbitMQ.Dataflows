using HouseofCat.Compression;
using HouseofCat.Dataflows.Extensions;
using HouseofCat.Encryption;
using HouseofCat.Serialization;
using OpenTelemetry.Trace;
using System;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace HouseofCat.Dataflows;

public abstract class BaseDataflow<TState> where TState : class, IWorkState, new()
{
    protected ExecutionDataflowBlockOptions _executeStepOptions;
    protected DataflowLinkOptions _linkStepOptions;

    protected ISerializationProvider _serializationProvider;
    protected IEncryptionProvider _encryptionProvider;
    protected ICompressionProvider _compressProvider;

    protected ISourceBlock<TState> _currentBlock;
    public Task Completion { get; protected set; }

    protected void SetCurrentSourceBlock(IDataflowBlock block)
    {
        _currentBlock = (ISourceBlock<TState>)block;
    }

    protected ExecutionDataflowBlockOptions GetExecuteStepOptions(int? maxDoP, bool? ensureOrdered, int? boundedCapacity, TaskScheduler taskScheduler = null)
    {
        if (maxDoP.HasValue || ensureOrdered.HasValue || boundedCapacity.HasValue)
        {
            return new ExecutionDataflowBlockOptions
            {
                BoundedCapacity = boundedCapacity ?? _executeStepOptions.BoundedCapacity,
                EnsureOrdered = ensureOrdered ?? _executeStepOptions.EnsureOrdered,
                MaxDegreeOfParallelism = maxDoP ?? _executeStepOptions.MaxDegreeOfParallelism,
                TaskScheduler = taskScheduler ?? TaskScheduler.Current,
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
        string spanName)
    {
        TState WrapAction(TState state)
        {
            using var childSpan = state.CreateActiveSpan(spanName, SpanKind.Internal);
            try
            {

                return action(state);
            }
            catch (Exception ex)
            {
                childSpan?.SetStatus(Status.Error.WithDescription(ex.Message));
                childSpan?.RecordException(ex);
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
        string spanName)
    {
        async Task<TState> WrapActionAsync(TState state)
        {
            using var childSpan = state.CreateActiveSpan(spanName, SpanKind.Internal);
            try
            {
                return await action(state).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                childSpan?.SetStatus(Status.Error.WithDescription(ex.Message));
                childSpan?.RecordException(ex);
                state.IsFaulted = true;
                state.EDI = ExceptionDispatchInfo.Capture(ex);
                return state;
            }
        }

        return new TransformBlock<TState, TState>(WrapActionAsync, options);
    }

    public ActionBlock<TState> GetLastWrappedActionBlock(
        Action<TState> action,
        ExecutionDataflowBlockOptions options,
        string spanName)
    {
        void WrapAction(TState state)
        {
            var childSpan = state.CreateActiveSpan(spanName, SpanKind.Internal);
            try
            {
                action(state);
            }
            catch (Exception ex)
            {
                childSpan?.SetStatus(Status.Error.WithDescription(ex.Message));
                childSpan?.RecordException(ex);
            }

            childSpan?.Dispose();
            state.EndRootSpan();
        }

        return new ActionBlock<TState>(WrapAction, options);
    }

    public ActionBlock<TState> GetLastWrappedActionBlock(
        Func<TState, TState> action,
        ExecutionDataflowBlockOptions options,
        string spanName)
    {
        void WrapAction(TState state)
        {
            var childSpan = state.CreateActiveSpan(spanName, SpanKind.Internal);
            try
            {
                action(state);
            }
            catch (Exception ex)
            {
                childSpan?.SetStatus(Status.Error.WithDescription(ex.Message));
                childSpan?.RecordException(ex);
            }

            childSpan?.Dispose();
            state.EndRootSpan();
        }

        return new ActionBlock<TState>(WrapAction, options);
    }

    public ActionBlock<TState> GetLastWrappedActionBlock(
        Func<TState, Task> action,
        ExecutionDataflowBlockOptions options,
        string spanName)
    {
        async Task WrapActionAsync(TState state)
        {
            var childSpan = state.CreateActiveSpan(spanName, SpanKind.Internal);
            try
            {
                await action(state).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                childSpan?.SetStatus(Status.Error.WithDescription(ex.Message));
                childSpan?.RecordException(ex);
            }

            childSpan?.Dispose();
            state.EndRootSpan();
        }

        return new ActionBlock<TState>(WrapActionAsync, options);
    }
}
