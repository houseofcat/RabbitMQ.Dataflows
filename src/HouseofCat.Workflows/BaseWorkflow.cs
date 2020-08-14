using HouseofCat.Compression;
using HouseofCat.Encryption;
using HouseofCat.Serialization;
using HouseofCat.Utilities.Errors;
using HouseofCat.Workflows;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace HouseofCat.RabbitMQ.Workflows
{
    public abstract class BaseWorkflow<TState> where TState : class, IWorkState, new()
    {
        protected ILogger<BaseWorkflow<TState>> _logger;

        protected ExecutionDataflowBlockOptions _executeStepOptions;
        protected DataflowLinkOptions _linkStepOptions;
        protected IEncryptionProvider _encryptionProvider;
        protected ICompressionProvider _compressProvider;
        protected ISerializationProvider _serializationProvider;

        protected TransformBlock<TState, TState> _decryptBlock;
        protected TransformBlock<TState, TState> _decompressBlock;

        // Main Flow - Supplied Steps
        protected BufferBlock<TState> _readyBuffer;
        protected readonly List<TransformBlock<TState, TState>> _suppliedTransforms = new List<TransformBlock<TState, TState>>();

        // Main Flow - PostProcessing
        protected BufferBlock<TState> _postProcessingBuffer;
        protected TransformBlock<TState, TState> _compressBlock;
        protected TransformBlock<TState, TState> _encryptBlock;
        protected ActionBlock<TState> _finalization;

        // Error/Fault Flow
        protected BufferBlock<TState> _errorBuffer;
        protected ActionBlock<TState> _errorAction;

        // Used for Simplifying Dependency
        protected ISourceBlock<TState> _currentBlock;
        public Task Completion { get; protected set; }

        public BaseWorkflow<TState> WithSerilizationProvider(ISerializationProvider provider)
        {
            Guard.AgainstNull(provider, nameof(provider));
            _serializationProvider = provider;
            return this;
        }

        public BaseWorkflow<TState> WithCompressionProvider(ICompressionProvider provider)
        {
            Guard.AgainstNull(provider, nameof(provider));
            _compressProvider = provider;
            return this;
        }

        public BaseWorkflow<TState> WithEncryptionProvider(IEncryptionProvider provider)
        {
            Guard.AgainstNull(provider, nameof(provider));
            _encryptionProvider = provider;
            return this;
        }

        public BaseWorkflow<TState> WithErrorHandling(Action<TState> action, int bufferCapacity, int? maxDoPOverride = null, bool? ensureOrdered = null)
        {
            _errorBuffer = new BufferBlock<TState>(new DataflowBlockOptions { BoundedCapacity = bufferCapacity > 0 ? bufferCapacity : 1000 });
            var executionOptions = GetExecuteStepOptions(maxDoPOverride, ensureOrdered, bufferCapacity);
            _errorAction = GetWrappedActionBlock(action, executionOptions);
            return this;
        }

        public BaseWorkflow<TState> WithErrorHandling(Func<TState, Task> action, int bufferCapacity, int? maxDoPOverride = null, bool? ensureOrdered = null)
        {
            _errorBuffer = new BufferBlock<TState>(new DataflowBlockOptions { BoundedCapacity = bufferCapacity > 0 ? bufferCapacity : 1000 });
            var executionOptions = GetExecuteStepOptions(maxDoPOverride, ensureOrdered, bufferCapacity);
            _errorAction = GetWrappedActionBlock(action, executionOptions);
            return this;
        }

        public BaseWorkflow<TState> WithReadyToProcessBuffer(int bufferCapacity)
        {
            _readyBuffer = new BufferBlock<TState>(new DataflowBlockOptions { BoundedCapacity = bufferCapacity > 0 ? bufferCapacity : 1000 });
            return this;
        }

        public BaseWorkflow<TState> AddStep(Func<TState, TState> suppliedStep, int? maxDoPOverride = null, bool? ensureOrdered = null, int? bufferSizeOverride = null)
        {
            Guard.AgainstNull(_encryptionProvider, nameof(_encryptionProvider));
            var executionOptions = GetExecuteStepOptions(maxDoPOverride, ensureOrdered, bufferSizeOverride);
            _suppliedTransforms.Add(GetWrappedTransformBlock(suppliedStep, executionOptions));
            return this;
        }

        public BaseWorkflow<TState> AddStep(Func<TState, Task<TState>> suppliedStep, int? maxDoPOverride = null, bool? ensureOrdered = null, int? bufferSizeOverride = null)
        {
            Guard.AgainstNull(_encryptionProvider, nameof(_encryptionProvider));
            var executionOptions = GetExecuteStepOptions(maxDoPOverride, ensureOrdered, bufferSizeOverride);
            _suppliedTransforms.Add(GetWrappedTransformBlock(suppliedStep, executionOptions));
            return this;
        }

        public BaseWorkflow<TState> WithPostProcessingBuffer(int bufferCapacity)
        {
            _postProcessingBuffer = new BufferBlock<TState>(new DataflowBlockOptions { BoundedCapacity = bufferCapacity > 0 ? bufferCapacity : 1000 });
            return this;
        }

        public BaseWorkflow<TState> WithFinalization(Action<TState> action, int? maxDoPOverride = null, bool? ensureOrdered = null, int? bufferSizeOverride = null)
        {
            var executionOptions = GetExecuteStepOptions(maxDoPOverride, ensureOrdered, bufferSizeOverride);
            _finalization = GetWrappedActionBlock(action, executionOptions);
            return this;
        }

        public BaseWorkflow<TState> WithFinalization(Func<TState, Task> action, int? maxDoPOverride = null, bool? ensureOrdered = null, int? bufferSizeOverride = null)
        {
            var executionOptions = GetExecuteStepOptions(maxDoPOverride, ensureOrdered, bufferSizeOverride);
            _finalization = GetWrappedActionBlock(action, executionOptions);
            return this;
        }

        protected void SetCurrentSourceBlock(IDataflowBlock block)
        {
            _currentBlock = (ISourceBlock<TState>)block;
        }

        protected ExecutionDataflowBlockOptions GetExecuteStepOptions(int? maxDoPOverride, bool? ensureOrdered, int? bufferSizeOverride)
        {
            if (maxDoPOverride.HasValue || ensureOrdered.HasValue || bufferSizeOverride.HasValue)
            {
                return new ExecutionDataflowBlockOptions
                {
                    EnsureOrdered = ensureOrdered ?? _executeStepOptions.EnsureOrdered,
                    MaxDegreeOfParallelism = maxDoPOverride ?? _executeStepOptions.MaxDegreeOfParallelism,
                    BoundedCapacity = bufferSizeOverride ?? _executeStepOptions.BoundedCapacity
                };
            }

            return _executeStepOptions;
        }

        public TransformBlock<TState, TState> GetTransformBlock(Func<TState, Task<TState>> action, ExecutionDataflowBlockOptions options)
        {
            return new TransformBlock<TState, TState>(action, options);
        }

        public TransformBlock<TState, TState> GetTransformBlock(Func<TState, TState> action, ExecutionDataflowBlockOptions options)
        {
            return new TransformBlock<TState, TState>(action, options);
        }

        public TransformBlock<TState, TState> GetWrappedTransformBlock(Func<TState, TState> action, ExecutionDataflowBlockOptions options)
        {
            TState WrapAction(TState state)
            {
                try
                {
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

        public TransformBlock<TState, TState> GetWrappedTransformBlock(Func<TState, Task<TState>> action, ExecutionDataflowBlockOptions options)
        {
            async Task<TState> WrapActionAsync(TState state)
            {
                try
                {
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

        public ActionBlock<TState> GetWrappedActionBlock(Action<TState> action, ExecutionDataflowBlockOptions options)
        {
            void WrapAction(TState state)
            {
                try
                { action(state); }
                catch
                { /* Actions are terminating block, so swallow (maybe log) */ }
            }

            return new ActionBlock<TState>(WrapAction, options);
        }

        public ActionBlock<TState> GetWrappedActionBlock(Func<TState, TState> action, ExecutionDataflowBlockOptions options)
        {
            void WrapAction(TState state)
            {
                try
                { action(state); }
                catch
                { /* Actions are terminating block, so swallow (maybe log) */ }
            }

            return new ActionBlock<TState>(WrapAction, options);
        }

        public ActionBlock<TState> GetWrappedActionBlock(Func<TState, Task> action, ExecutionDataflowBlockOptions options)
        {
            async Task WrapActionAsync(TState state)
            {
                try
                { await action(state).ConfigureAwait(false); }
                catch
                { /* Actions are terminating block, so swallow (maybe log) */ }
            }

            return new ActionBlock<TState>(WrapActionAsync, options);
        }
    }
}
