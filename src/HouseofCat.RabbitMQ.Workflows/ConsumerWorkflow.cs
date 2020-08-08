using HouseofCat.Compression;
using HouseofCat.Encryption;
using HouseofCat.Logger;
using HouseofCat.RabbitMQ.Pipelines;
using HouseofCat.RabbitMQ.Services;
using HouseofCat.Serialization;
using HouseofCat.Utilities.Errors;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace HouseofCat.RabbitMQ.Workflows
{
    public class ConsumerWorkflow<TState> where TState : class, IWorkState, new()
    {
        public string ConsumerWorkflowName { get; }
        public string ConsumerName { get; }
        public int ConsumerCount;

        private readonly ILogger<ConsumerWorkflow<TState>> _logger;
        private readonly IRabbitService _rabbitService;
        private readonly ConsumerOptions _consumerOptions;
        private readonly ExecutionDataflowBlockOptions _executeStepOptions;
        private readonly DataflowLinkOptions _linkStepOptions;
        private IEncryptionProvider _encryptionProvider;
        private ICompressionProvider _compressProvider;
        private ISerializationProvider _serializationProvider;

        // Main Flow - PreProcessing
        private List<ConsumerBlock<ReceivedData>> _consumerBlocks; // Doubles as a BufferBlock.
        private BufferBlock<ReceivedData> _inputBuffer;
        private TransformBlock<ReceivedData, TState> _buildStateBlock;
        private TransformBlock<TState, TState> _decryptBlock;
        private TransformBlock<TState, TState> _decompressBlock;

        // Main Flow - Supplied Steps
        private BufferBlock<TState> _readyBuffer;
        private List<TransformBlock<TState, TState>> _suppliedTransforms = new List<TransformBlock<TState, TState>>();

        // Main Flow - PostProcessing
        private BufferBlock<TState> _postProcessingBuffer;
        private TransformBlock<TState, TState> _createSendLetter;
        private TransformBlock<TState, TState> _compressBlock;
        private TransformBlock<TState, TState> _encryptBlock;
        private TransformBlock<TState, TState> _sendLetterBlock;
        private ActionBlock<TState> _finalization;

        // Error/Fault Flow
        private BufferBlock<TState> _errorBuffer;
        private ActionBlock<TState> _errorAction;

        // Used for Simplifying Dependency
        private ISourceBlock<TState> _currentBlock;
        public Task Completion { get; private set; }

        public ConsumerWorkflow(
            IRabbitService rabbitService,
            string consumerWorkflowName,
            string consumerName,
            int consumerCount)
        {
            Guard.AgainstNull(rabbitService, nameof(rabbitService));
            Guard.AgainstNullOrEmpty(consumerName, nameof(consumerName));

            ConsumerWorkflowName = consumerWorkflowName;
            ConsumerCount = consumerCount;
            ConsumerName = consumerName;

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

        public ConsumerWorkflow<TState> WithSerilizationProvider(ISerializationProvider provider)
        {
            Guard.AgainstNull(provider, nameof(provider));
            _serializationProvider = provider;
            return this;
        }

        public ConsumerWorkflow<TState> WithCompressionProvider(ICompressionProvider provider)
        {
            Guard.AgainstNull(provider, nameof(provider));
            _compressProvider = provider;
            return this;
        }

        public ConsumerWorkflow<TState> WithEncryptionProvider(IEncryptionProvider provider)
        {
            Guard.AgainstNull(provider, nameof(provider));
            _encryptionProvider = provider;
            return this;
        }

        public ConsumerWorkflow<TState> WithErrorHandling(Action<TState> action, int bufferCapacity, int? maxDoPOverride = null, bool? ensureOrdered = null)
        {
            _errorBuffer = new BufferBlock<TState>(new DataflowBlockOptions { BoundedCapacity = bufferCapacity > 0 ? bufferCapacity : 1000 });
            var executionOptions = GetExecuteStepOptions(maxDoPOverride, ensureOrdered, bufferCapacity);
            _errorAction = BlockBuilders.GetWrappedActionBlock(action, executionOptions);
            return this;
        }

        public ConsumerWorkflow<TState> WithErrorHandling(Func<TState, Task> action, int bufferCapacity, int? maxDoPOverride = null, bool? ensureOrdered = null)
        {
            _errorBuffer = new BufferBlock<TState>(new DataflowBlockOptions { BoundedCapacity = bufferCapacity > 0 ? bufferCapacity : 1000 });
            var executionOptions = GetExecuteStepOptions(maxDoPOverride, ensureOrdered, bufferCapacity);
            _errorAction = BlockBuilders.GetWrappedActionBlock(action, executionOptions);
            return this;
        }

        public ConsumerWorkflow<TState> WithBuildState<TOut>(int? maxDoPOverride = null, bool? ensureOrdered = null, int? bufferSizeOverride = null)
        {
            var executionOptions = GetExecuteStepOptions(maxDoPOverride, ensureOrdered, bufferSizeOverride);
            _buildStateBlock = BlockBuilders.GetBuildStateBlock<TState, TOut>(_serializationProvider, executionOptions);
            return this;
        }

        public ConsumerWorkflow<TState> WithDecryptionStep(int? maxDoPOverride = null, bool? ensureOrdered = null, int? bufferSizeOverride = null)
        {
            Guard.AgainstNull(_encryptionProvider, nameof(_encryptionProvider));
            var executionOptions = GetExecuteStepOptions(maxDoPOverride, ensureOrdered, bufferSizeOverride);
            _decryptBlock = BlockBuilders.GetByteManipulationTransformBlock<TState>(_encryptionProvider.Decrypt, executionOptions, false, x => x.ReceivedData.Encrypted);
            return this;
        }

        public ConsumerWorkflow<TState> WithDecompressionStep(int? maxDoPOverride = null, bool? ensureOrdered = null, int? bufferSizeOverride = null)
        {
            Guard.AgainstNull(_compressProvider, nameof(_compressProvider));
            var executionOptions = GetExecuteStepOptions(maxDoPOverride, ensureOrdered, bufferSizeOverride);
            _decompressBlock = BlockBuilders.GetByteManipulationTransformBlock<TState>(_compressProvider.DecompressAsync, executionOptions, false, x => x.ReceivedData.Compressed);
            return this;
        }

        public ConsumerWorkflow<TState> WithReadyToProcessBuffer(int bufferCapacity)
        {
            _readyBuffer = new BufferBlock<TState>(new DataflowBlockOptions { BoundedCapacity = bufferCapacity > 0 ? bufferCapacity : 1000 });
            return this;
        }

        public ConsumerWorkflow<TState> AddStep(Func<TState, TState> suppliedStep, int? maxDoPOverride = null, bool? ensureOrdered = null, int? bufferSizeOverride = null)
        {
            Guard.AgainstNull(_encryptionProvider, nameof(_encryptionProvider));
            var executionOptions = GetExecuteStepOptions(maxDoPOverride, ensureOrdered, bufferSizeOverride);
            _suppliedTransforms.Add(BlockBuilders.GetWrappedTransformBlock<TState>(suppliedStep, executionOptions));
            return this;
        }

        public ConsumerWorkflow<TState> AddStep(Func<TState, Task<TState>> suppliedStep, int? maxDoPOverride = null, bool? ensureOrdered = null, int? bufferSizeOverride = null)
        {
            Guard.AgainstNull(_encryptionProvider, nameof(_encryptionProvider));
            var executionOptions = GetExecuteStepOptions(maxDoPOverride, ensureOrdered, bufferSizeOverride);
            _suppliedTransforms.Add(BlockBuilders.GetWrappedTransformBlock<TState>(suppliedStep, executionOptions));
            return this;
        }

        public ConsumerWorkflow<TState> WithPostProcessingBuffer(int bufferCapacity)
        {
            _postProcessingBuffer = new BufferBlock<TState>(new DataflowBlockOptions { BoundedCapacity = bufferCapacity > 0 ? bufferCapacity : 1000 });
            return this;
        }

        public ConsumerWorkflow<TState> WithCreateSendLetter(Func<TState, Task<TState>> createLetter, int? maxDoPOverride = null, bool? ensureOrdered = null, int? bufferSizeOverride = null)
        {
            Guard.AgainstNull(_compressProvider, nameof(_compressProvider));
            var executionOptions = GetExecuteStepOptions(maxDoPOverride, ensureOrdered, bufferSizeOverride);
            _createSendLetter = BlockBuilders.GetWrappedTransformBlock<TState>(createLetter, executionOptions);
            return this;
        }

        public ConsumerWorkflow<TState> WithCompression(int? maxDoPOverride = null, bool? ensureOrdered = null, int? bufferSizeOverride = null)
        {
            Guard.AgainstNull(_compressProvider, nameof(_compressProvider));
            var executionOptions = GetExecuteStepOptions(maxDoPOverride, ensureOrdered, bufferSizeOverride);
            _compressBlock = BlockBuilders.GetByteManipulationTransformBlock<TState>(_compressProvider.CompressAsync, executionOptions, true, x => !x.ReceivedData.Compressed);
            return this;
        }

        public ConsumerWorkflow<TState> WithEncryption(int? maxDoPOverride = null, bool? ensureOrdered = null, int? bufferSizeOverride = null)
        {
            Guard.AgainstNull(_encryptionProvider, nameof(_encryptionProvider));
            var executionOptions = GetExecuteStepOptions(maxDoPOverride, ensureOrdered, bufferSizeOverride);
            _encryptBlock = BlockBuilders.GetByteManipulationTransformBlock<TState>(_encryptionProvider.Encrypt, executionOptions, true, x => !x.ReceivedData.Encrypted);
            return this;
        }

        public ConsumerWorkflow<TState> WithSendStep(int? maxDoPOverride = null, bool? ensureOrdered = null, int? bufferSizeOverride = null)
        {
            var executionOptions = GetExecuteStepOptions(maxDoPOverride, ensureOrdered, bufferSizeOverride);
            _sendLetterBlock = BlockBuilders.GetWrappedPublishTransformBlock<TState>(_rabbitService, executionOptions);
            return this;
        }

        public ConsumerWorkflow<TState> WithFinalization(Action<TState> action, int? maxDoPOverride = null, bool? ensureOrdered = null, int? bufferSizeOverride = null)
        {
            var executionOptions = GetExecuteStepOptions(maxDoPOverride, ensureOrdered, bufferSizeOverride);
            _finalization = BlockBuilders.GetWrappedActionBlock<TState>(action, executionOptions);
            return this;
        }

        public ConsumerWorkflow<TState> WithFinalization(Func<TState, Task> action, int? maxDoPOverride = null, bool? ensureOrdered = null, int? bufferSizeOverride = null)
        {
            var executionOptions = GetExecuteStepOptions(maxDoPOverride, ensureOrdered, bufferSizeOverride);
            _finalization = BlockBuilders.GetWrappedActionBlock<TState>(action, executionOptions);
            return this;
        }

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

            for (int i = 0; i < ConsumerCount; i++)
            {
                var consumer = new Consumer(_rabbitService.ChannelPool, ConsumerName, null);
                _consumerBlocks.Add(new ConsumerBlock<ReceivedData>(consumer));
                _consumerBlocks[i].LinkTo(_inputBuffer, overrideOptions ?? _linkStepOptions);
            }

            _inputBuffer.LinkTo(_buildStateBlock, overrideOptions ?? _linkStepOptions);
            _buildStateBlock.LinkTo(_errorBuffer, overrideOptions ?? _linkStepOptions, x => x.IsFaulted);
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

        // Simplifies node dependency logic by keeping track internally where we are in the graph.
        private void SetCurrentSourceBlock(IDataflowBlock block)
        {
            _currentBlock = (ISourceBlock<TState>)block;
        }

        private ExecutionDataflowBlockOptions GetExecuteStepOptions(int? maxDoPOverride, bool? ensureOrdered, int? bufferSizeOverride)
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

        public async Task StartAsync()
        {
            BuildLinkages();

            foreach (var consumerBlock in _consumerBlocks)
            {
                await consumerBlock.StartConsumingAsync();
            }
        }

        public async Task StopAsync()
        {
            // Signal stop consuming and completion.
            foreach (var consumerBlock in _consumerBlocks)
            {
                await consumerBlock.StopConsumingAsync();
                consumerBlock.Complete(); // Set complete at the top level.
            }
        }
    }
}
