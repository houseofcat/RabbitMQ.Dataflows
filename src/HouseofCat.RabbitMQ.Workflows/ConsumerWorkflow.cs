using HouseofCat.Compression;
using HouseofCat.Encryption;
using HouseofCat.Logger;
using HouseofCat.Metrics;
using HouseofCat.RabbitMQ.Services;
using HouseofCat.RabbitMQ.WorkState;
using HouseofCat.Serialization;
using HouseofCat.Utilities.Errors;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using static HouseofCat.Reflection.Generics;

namespace HouseofCat.RabbitMQ.Workflows
{
    public class ConsumerWorkflow<TState> : BaseWorkflow<TState> where TState : class, IRabbitWorkState, new()
    {
        public string WorkflowName { get; }

        private readonly ILogger<BaseWorkflow<TState>> _logger;
        private readonly IRabbitService _rabbitService;
        private readonly ConsumerOptions _consumerOptions;
        private readonly string _consumerName;
        private readonly int _consumerCount;

        // Main Flow - Ingestion
        private readonly List<ConsumerBlock<ReceivedData>> _consumerBlocks;
        private BufferBlock<ReceivedData> _inputBuffer;
        private TransformBlock<ReceivedData, TState> _buildStateBlock;
        private TransformBlock<TState, TState> _createSendLetter;
        protected TransformBlock<TState, TState> _decryptBlock;
        protected TransformBlock<TState, TState> _decompressBlock;

        // Main Flow - Supplied Steps
        protected BufferBlock<TState> _readyBuffer;
        protected readonly List<TransformBlock<TState, TState>> _suppliedTransforms = new List<TransformBlock<TState, TState>>();

        // Main Flow - PostProcessing
        protected BufferBlock<TState> _postProcessingBuffer;
        protected TransformBlock<TState, TState> _compressBlock;
        protected TransformBlock<TState, TState> _encryptBlock;
        protected TransformBlock<TState, TState> _sendLetterBlock;
        protected ActionBlock<TState> _finalization;

        // Error/Fault Flow
        protected BufferBlock<TState> _errorBuffer;
        protected ActionBlock<TState> _errorAction;

        public ConsumerWorkflow(
            IRabbitService rabbitService,
            string workflowName,
            string consumerName,
            int consumerCount)
        {
            Guard.AgainstNull(rabbitService, nameof(rabbitService));
            Guard.AgainstNullOrEmpty(consumerName, nameof(consumerName));

            WorkflowName = workflowName;
            _consumerCount = consumerCount;
            _consumerName = consumerName;

            _logger = LogHelper.LoggerFactory.CreateLogger<ConsumerWorkflow<TState>>();
            _rabbitService = rabbitService;
            _consumerOptions = rabbitService.GetConsumer(consumerName).ConsumerOptions;

            _linkStepOptions = new DataflowLinkOptions { PropagateCompletion = true };
            _executeStepOptions = new ExecutionDataflowBlockOptions
            {
                MaxDegreeOfParallelism = _consumerOptions.ConsumerPipelineOptions.MaxDegreesOfParallelism ?? 1,
                SingleProducerConstrained = true,
                EnsureOrdered = _consumerOptions.ConsumerPipelineOptions.EnsureOrdered ?? true
            };

            _consumerBlocks = new List<ConsumerBlock<ReceivedData>>();
        }

        public async Task StartAsync()
        {
            BuildLinkages();

            foreach (var consumerBlock in _consumerBlocks)
            {
                await consumerBlock.StartConsumingAsync().ConfigureAwait(false);
            }
        }

        public async Task StopAsync()
        {
            foreach (var consumerBlock in _consumerBlocks)
            {
                await consumerBlock.StopConsumingAsync().ConfigureAwait(false);
                consumerBlock.Complete();
            }
        }

        public ConsumerWorkflow<TState> SetSerilizationProvider(ISerializationProvider provider)
        {
            Guard.AgainstNull(provider, nameof(provider));
            _serializationProvider = provider;
            return this;
        }

        public ConsumerWorkflow<TState> SetCompressionProvider(ICompressionProvider provider)
        {
            Guard.AgainstNull(provider, nameof(provider));
            _compressProvider = provider;
            return this;
        }

        public ConsumerWorkflow<TState> SetEncryptionProvider(IEncryptionProvider provider)
        {
            Guard.AgainstNull(provider, nameof(provider));
            _encryptionProvider = provider;
            return this;
        }

        public ConsumerWorkflow<TState> SetMetricsProvider(IMetricsProvider provider)
        {
            _metricsProvider = provider ?? new NullMetricsProvider();
            return this;
        }

        #region Step Adders

        public ConsumerWorkflow<TState> WithErrorHandling(Action<TState> action, int boundedCapacity, int? maxDoP = null, bool? ensureOrdered = null)
        {
            Guard.AgainstNull(action, nameof(action));
            if (_errorBuffer == null)
            {
                _errorBuffer = new BufferBlock<TState>(new DataflowBlockOptions { BoundedCapacity = boundedCapacity > 0 ? boundedCapacity : 1000 });
                var executionOptions = GetExecuteStepOptions(maxDoP, ensureOrdered, boundedCapacity);
                _errorAction = GetWrappedActionBlock(action, executionOptions, $"{WorkflowName}_ErrorHandler", false);
            }
            return this;
        }

        public ConsumerWorkflow<TState> WithErrorHandling(
            Func<TState, Task> action,
            int boundedCapacity,
            int? maxDoP = null,
            bool? ensureOrdered = null)
        {
            Guard.AgainstNull(action, nameof(action));
            if (_errorBuffer == null)
            {
                _errorBuffer = new BufferBlock<TState>(new DataflowBlockOptions { BoundedCapacity = boundedCapacity > 0 ? boundedCapacity : 1000 });
                var executionOptions = GetExecuteStepOptions(maxDoP, ensureOrdered, boundedCapacity);
                _errorAction = GetWrappedActionBlock(action, executionOptions, $"{WorkflowName}_ErrorHandler", false);
            }
            return this;
        }

        public ConsumerWorkflow<TState> WithReadyToProcessBuffer(int boundedCapacity)
        {
            if (_readyBuffer == null)
            {
                _readyBuffer = new BufferBlock<TState>(new DataflowBlockOptions { BoundedCapacity = boundedCapacity > 0 ? boundedCapacity : 1000 });
            }
            return this;
        }

        public ConsumerWorkflow<TState> AddStep(
            Func<TState, TState> suppliedStep,
            string metricIdentifier,
            bool metricMicroScale = false,
            string metricDescription = null,
            int? maxDoP = null,
            bool? ensureOrdered = null,
            int? boundedCapacity = null)
        {
            Guard.AgainstNull(suppliedStep, nameof(suppliedStep));
            Guard.AgainstNull(_encryptionProvider, nameof(_encryptionProvider));
            _metricsProvider.IncrementCounter($"{WorkflowName}_Steps", metricDescription);
            var executionOptions = GetExecuteStepOptions(maxDoP, ensureOrdered, boundedCapacity);
            _suppliedTransforms.Add(GetWrappedTransformBlock(suppliedStep, executionOptions, metricIdentifier, metricMicroScale));
            return this;
        }

        public ConsumerWorkflow<TState> AddStep(
            Func<TState, Task<TState>> suppliedStep,
            string metricIdentifier,
            bool metricMicroScale = false,
            string metricDescription = null,
            int? maxDoP = null,
            bool? ensureOrdered = null,
            int? boundedCapacity = null)
        {
            Guard.AgainstNull(suppliedStep, nameof(suppliedStep));
            Guard.AgainstNull(_encryptionProvider, nameof(_encryptionProvider));
            _metricsProvider.IncrementCounter($"{WorkflowName}_Steps", metricDescription);
            var executionOptions = GetExecuteStepOptions(maxDoP, ensureOrdered, boundedCapacity);
            _suppliedTransforms.Add(GetWrappedTransformBlock(suppliedStep, executionOptions, metricIdentifier, metricMicroScale));
            return this;
        }

        public ConsumerWorkflow<TState> WithPostProcessingBuffer(int boundedCapacity)
        {
            if (_postProcessingBuffer == null)
            {
                _postProcessingBuffer = new BufferBlock<TState>(new DataflowBlockOptions { BoundedCapacity = boundedCapacity > 0 ? boundedCapacity : 1000 });
            }
            return this;
        }

        public ConsumerWorkflow<TState> WithFinalization(
            Action<TState> action,
            int? maxDoP = null,
            bool? ensureOrdered = null,
            int? boundedCapacity = null)
        {
            Guard.AgainstNull(action, nameof(action));
            if (_finalization == null)
            {
                _metricsProvider.IncrementCounter($"{WorkflowName}_Steps");
                var executionOptions = GetExecuteStepOptions(maxDoP, ensureOrdered, boundedCapacity);
                _finalization = GetWrappedActionBlock(action, executionOptions, $"{WorkflowName}_Finalization", true);
            }
            return this;
        }

        public ConsumerWorkflow<TState> WithFinalization(
            Func<TState, Task> action,
            int? maxDoP = null,
            bool? ensureOrdered = null,
            int? boundedCapacity = null)
        {
            Guard.AgainstNull(action, nameof(action));
            if (_finalization == null)
            {
                _metricsProvider.IncrementCounter($"{WorkflowName}_Steps");
                var executionOptions = GetExecuteStepOptions(maxDoP, ensureOrdered, boundedCapacity);
                _finalization = GetWrappedActionBlock(action, executionOptions, $"{WorkflowName}_Finalization", true);
            }
            return this;
        }

        public ConsumerWorkflow<TState> WithBuildState<TOut>(
            string stateKey,
            int? maxDoP = null,
            bool? ensureOrdered = null,
            int? boundedCapacity = null)
        {
            if (_buildStateBlock == null)
            {
                _metricsProvider.IncrementCounter($"{WorkflowName}_Steps");
                var executionOptions = GetExecuteStepOptions(maxDoP, ensureOrdered, boundedCapacity);
                _buildStateBlock = GetBuildStateBlock<TOut>(_serializationProvider, stateKey, executionOptions);
            }
            return this;
        }

        public ConsumerWorkflow<TState> WithDecryptionStep(int? maxDoP = null, bool? ensureOrdered = null, int? boundedCapacity = null)
        {
            Guard.AgainstNull(_encryptionProvider, nameof(_encryptionProvider));
            if (_decryptBlock == null)
            {
                _metricsProvider.IncrementCounter($"{WorkflowName}_Steps");
                var executionOptions = GetExecuteStepOptions(maxDoP, ensureOrdered, boundedCapacity);

                _decryptBlock = GetByteManipulationTransformBlock(
                    _encryptionProvider.Decrypt,
                    executionOptions,
                    false,
                    x => x.ReceivedData.Encrypted,
                    $"{WorkflowName}_Decrypt",
                    true);
            }
            return this;
        }

        public ConsumerWorkflow<TState> WithDecompressionStep(int? maxDoP = null, bool? ensureOrdered = null, int? boundedCapacity = null)
        {
            Guard.AgainstNull(_compressProvider, nameof(_compressProvider));
            if (_decompressBlock == null)
            {
                _metricsProvider.IncrementCounter($"{WorkflowName}_Steps");
                var executionOptions = GetExecuteStepOptions(maxDoP, ensureOrdered, boundedCapacity);

                _decompressBlock = GetByteManipulationTransformBlock(
                    _compressProvider.Decompress,
                    executionOptions,
                    false,
                    x => x.ReceivedData.Compressed,
                    $"{WorkflowName}_Decompress",
                    true);
            }

            return this;
        }

        public ConsumerWorkflow<TState> WithCreateSendLetter(
            Func<TState, Task<TState>> createLetter,
            int? maxDoP = null,
            bool? ensureOrdered = null,
            int? boundedCapacity = null)
        {
            Guard.AgainstNull(_compressProvider, nameof(_compressProvider));
            if (_createSendLetter == null)
            {
                _metricsProvider.IncrementCounter($"{WorkflowName}_Steps");
                var executionOptions = GetExecuteStepOptions(maxDoP, ensureOrdered, boundedCapacity);
                _createSendLetter = GetWrappedTransformBlock(createLetter, executionOptions, $"{WorkflowName}_CreateSendLetter");
            }
            return this;
        }

        public ConsumerWorkflow<TState> WithCompression(int? maxDoP = null, bool? ensureOrdered = null, int? boundedCapacity = null)
        {
            Guard.AgainstNull(_compressProvider, nameof(_compressProvider));
            if (_compressBlock == null)
            {
                _metricsProvider.IncrementCounter($"{WorkflowName}_Steps");
                var executionOptions = GetExecuteStepOptions(maxDoP, ensureOrdered, boundedCapacity);

                _compressBlock = GetByteManipulationTransformBlock(
                    _compressProvider.Compress,
                    executionOptions,
                    true,
                    x => !x.ReceivedData.Compressed,
                    $"{WorkflowName}_Compress",
                    true);
            }
            return this;
        }

        public ConsumerWorkflow<TState> WithEncryption(int? maxDoP = null, bool? ensureOrdered = null, int? boundedCapacity = null)
        {
            Guard.AgainstNull(_encryptionProvider, nameof(_encryptionProvider));
            if (_encryptBlock == null)
            {
                _metricsProvider.IncrementCounter($"{WorkflowName}_Steps");
                var executionOptions = GetExecuteStepOptions(maxDoP, ensureOrdered, boundedCapacity);

                _encryptBlock = GetByteManipulationTransformBlock(
                    _encryptionProvider.Encrypt,
                    executionOptions,
                    true,
                    x => !x.ReceivedData.Encrypted,
                    $"{WorkflowName}_Encrypt",
                    true);
            }
            return this;
        }

        public ConsumerWorkflow<TState> WithSendStep(int? maxDoP = null, bool? ensureOrdered = null, int? boundedCapacity = null)
        {
            _metricsProvider.IncrementCounter($"{WorkflowName}_Steps");
            if (_sendLetterBlock == null)
            {
                var executionOptions = GetExecuteStepOptions(maxDoP, ensureOrdered, boundedCapacity);
                _sendLetterBlock = GetWrappedPublishTransformBlock(_rabbitService, executionOptions);
            }
            return this;
        }

        #endregion

        #region Step Linking

        private void BuildLinkages(DataflowLinkOptions overrideOptions = null)
        {
            Guard.AgainstNull(_buildStateBlock, nameof(_buildStateBlock)); // Create State Is Mandatory
            Guard.AgainstNull(_finalization, nameof(_finalization)); // Leaving The Workflow Is Mandatory
            Guard.AgainstNull(_errorAction, nameof(_errorAction)); // Processing Errors Is Mandatory

            if (_inputBuffer == null)
            { _inputBuffer = new BufferBlock<ReceivedData>(); }

            if (_readyBuffer == null)
            { _readyBuffer = new BufferBlock<TState>(); }

            if (_postProcessingBuffer == null)
            { _postProcessingBuffer = new BufferBlock<TState>(); }

            for (int i = 0; i < _consumerCount; i++)
            {
                var consumer = new Consumer(_rabbitService.ChannelPool, _consumerName);
                _consumerBlocks.Add(new ConsumerBlock<ReceivedData>(consumer));
                _consumerBlocks[i].LinkTo(_inputBuffer, overrideOptions ?? _linkStepOptions);
            }

            _inputBuffer.LinkTo(_buildStateBlock, overrideOptions ?? _linkStepOptions);
            _buildStateBlock.LinkTo(_errorBuffer, overrideOptions ?? _linkStepOptions, x => x == null);
            SetCurrentSourceBlock(_buildStateBlock);

            LinkPreProcessing(overrideOptions);
            LinkSuppliedSteps(overrideOptions);
            LinkPostProcessing(overrideOptions);

            _errorBuffer.LinkTo(_errorAction, overrideOptions ?? _linkStepOptions);
            Completion = _currentBlock.Completion;
        }

        private void LinkPreProcessing(DataflowLinkOptions overrideOptions = null)
        {
            // Link Deserialize to DecryptBlock with predicate if its not null.
            if (_decryptBlock != null)
            { LinkWithFaultRoute(_currentBlock, _decryptBlock, x => x.IsFaulted, overrideOptions ?? _linkStepOptions); }

            if (_decompressBlock != null)
            { LinkWithFaultRoute(_currentBlock, _decompressBlock, x => x.IsFaulted, overrideOptions ?? _linkStepOptions); }

            _currentBlock.LinkTo(_readyBuffer, overrideOptions ?? _linkStepOptions, x => !x.IsFaulted);
            SetCurrentSourceBlock(_readyBuffer); // Not Neeeded
        }

        private void LinkSuppliedSteps(DataflowLinkOptions overrideOptions = null)
        {
            // Link all user steps.
            if (_suppliedTransforms?.Count > 0)
            {
                for (int i = 0; i < _suppliedTransforms.Count; i++)
                {
                    if (i == 0)
                    { LinkWithFaultRoute(_currentBlock, _suppliedTransforms[i], x => x.IsFaulted, overrideOptions ?? _linkStepOptions); }
                    else // Link Previous Step, To Next Step
                    { LinkWithFaultRoute(_suppliedTransforms[i - 1], _suppliedTransforms[i], x => x.IsFaulted, overrideOptions ?? _linkStepOptions); }
                }

                // Link the last user step to PostProcessingBuffer/CreateSendLetter.
                if (_createSendLetter != null)
                {
                    LinkWithFaultRoute(_suppliedTransforms[^1], _createSendLetter, x => x.IsFaulted, overrideOptions ?? _linkStepOptions);
                    _createSendLetter.LinkTo(_postProcessingBuffer, overrideOptions ?? _linkStepOptions);
                    SetCurrentSourceBlock(_postProcessingBuffer);
                }
                else
                {
                    _suppliedTransforms[^1].LinkTo(_postProcessingBuffer, overrideOptions ?? _linkStepOptions);
                    SetCurrentSourceBlock(_postProcessingBuffer);
                }
            }
        }

        private void LinkPostProcessing(DataflowLinkOptions overrideOptions = null)
        {
            if (_compressBlock != null)
            { LinkWithFaultRoute(_currentBlock, _compressBlock, x => x.IsFaulted, overrideOptions); }

            if (_encryptBlock != null)
            { LinkWithFaultRoute(_currentBlock, _encryptBlock, x => x.IsFaulted, overrideOptions); }

            if (_sendLetterBlock != null)
            { LinkWithFaultRoute(_currentBlock, _sendLetterBlock, x => x.IsFaulted, overrideOptions); }

            _currentBlock.LinkTo(_finalization, overrideOptions ?? _linkStepOptions); // Last Action
        }

        private void LinkWithFaultRoute(ISourceBlock<TState> source, IPropagatorBlock<TState, TState> target, Predicate<TState> faultPredicate, DataflowLinkOptions overrideOptions = null)
        {
            source.LinkTo(target, overrideOptions ?? _linkStepOptions);
            target.LinkTo(_errorBuffer, overrideOptions ?? _linkStepOptions, faultPredicate); // Fault Linkage
            SetCurrentSourceBlock(target);
        }

        #endregion

        #region Step Wrappers

        private string StateIdentifier => $"{WorkflowName}_StateBuild";
        private TState BuildState<TOut>(ISerializationProvider provider, string key, ReceivedData data)
        {
            var state = New<TState>.Instance.Invoke();
            state.ReceivedData = data;
            state.Data = new Dictionary<string, object>
            {
                { key, provider.Deserialize<TOut>(data.Data) },
            };
            return state;
        }

        public TransformBlock<ReceivedData, TState> GetBuildStateBlock<TOut>(
            ISerializationProvider provider,
            string key,
            ExecutionDataflowBlockOptions options)
        {
            TState BuildStateWrap(ReceivedData data)
            {
                try
                {
                    using var multiDispose = _metricsProvider.TrackAndDuration(StateIdentifier, true);
                    return BuildState<TOut>(provider, key, data);
                }
                catch
                { return null; }
            }

            return new TransformBlock<ReceivedData, TState>(BuildStateWrap, options);
        }

        public TransformBlock<TState, TState> GetByteManipulationTransformBlock(
            Func<ReadOnlyMemory<byte>, byte[]> action,
            ExecutionDataflowBlockOptions options,
            bool outbound,
            Predicate<TState> predicate,
            string metricIdentifier,
            bool metricMicroScale = false,
            string metricDescription = null)
        {
            TState WrapAction(TState state)
            {
                try
                {
                    using var multiDispose = _metricsProvider.TrackAndDuration(metricIdentifier, metricMicroScale, metricDescription);

                    if (outbound)
                    {
                        if (state.SendData?.Length > 0)
                        { state.SendData = action(state.SendData); }
                        else if (state.SendLetter.Body?.Length > 0)
                        { state.SendLetter.Body = action(state.SendLetter.Body); }
                    }
                    else if (predicate.Invoke(state))
                    {
                        if (state.ReceivedData.ContentType == Constants.HeaderValueForLetter)
                        {
                            if (state.ReceivedData.Letter == null)
                            { state.ReceivedData.Letter = _serializationProvider.Deserialize<Letter>(state.ReceivedData.Data); }

                            state.ReceivedData.Letter.Body = action(state.ReceivedData.Letter.Body);
                        }
                        else
                        { state.ReceivedData.Data = action(state.ReceivedData.Data); }
                    }

                    return state;
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

        public TransformBlock<TState, TState> GetByteManipulationTransformBlock(
            Func<ReadOnlyMemory<byte>, Task<byte[]>> action,
            ExecutionDataflowBlockOptions options,
            bool outbound,
            Predicate<TState> predicate,
            string metricIdentifier,
            bool metricMicroScale = false,
            string metricDescription = null)
        {
            async Task<TState> WrapActionAsync(TState state)
            {
                try
                {
                    using var multiDispose = _metricsProvider.TrackAndDuration(metricIdentifier, metricMicroScale, metricDescription);

                    if (outbound)
                    {
                        if (state.SendData?.Length > 0)
                        { state.SendData = await action(state.SendData).ConfigureAwait(false); }
                        else if (state.SendLetter.Body?.Length > 0)
                        { state.SendLetter.Body = await action(state.SendLetter.Body).ConfigureAwait(false); }
                    }
                    else if (predicate.Invoke(state))
                    {
                        if (state.ReceivedData.ContentType == Constants.HeaderValueForLetter)
                        {
                            if (state.ReceivedData.Letter == null)
                            { state.ReceivedData.Letter = _serializationProvider.Deserialize<Letter>(state.ReceivedData.Data); }

                            state.ReceivedData.Letter.Body = await action(state.ReceivedData.Letter.Body).ConfigureAwait(false);
                        }
                        else
                        { state.ReceivedData.Data = await action(state.ReceivedData.Data).ConfigureAwait(false); }
                    }
                    return state;
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

        private string PublishStepIdentifier => $"{WorkflowName}_Publish";
        public TransformBlock<TState, TState> GetWrappedPublishTransformBlock(
            IRabbitService service,
            ExecutionDataflowBlockOptions options)
        {
            async Task<TState> WrapPublishAsync(TState state)
            {
                try
                {
                    using var multiDispose = _metricsProvider.TrackAndDuration(PublishStepIdentifier, true);

                    await service.Publisher.PublishAsync(state.SendLetter, true, true).ConfigureAwait(false);
                    state.SendLetterSent = true;

                    return state;
                }
                catch (Exception ex)
                {
                    state.IsFaulted = true;
                    state.EDI = ExceptionDispatchInfo.Capture(ex);
                    return state;
                }
            }

            return new TransformBlock<TState, TState>(WrapPublishAsync, options);
        }

        #endregion
    }
}
