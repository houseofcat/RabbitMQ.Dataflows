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
        public ConsumerOptions ConsumerOptions { get; }

        private readonly ILogger<ConsumerWorkflow<TState>> _logger;
        private readonly IRabbitService _rabbitService;
        private readonly IConsumer<ReceivedData> _consumer;
        private readonly ExecutionDataflowBlockOptions _executeStepOptions;
        private readonly DataflowLinkOptions _linkStepOptions;
        private IEncryptionProvider _encryptionProvider;
        private ICompressionProvider _compressProvider;
        private SerializationProvider _serializationProvider;

        // Main Flow - PreProcessing
        private ConsumerBlock<ReceivedData> _consumerBlock;
        private BufferBlock<ReceivedData> _inputBuffer;
        private TransformBlock<ReceivedData, TState> _deserializeBlock;
        private TransformBlock<TState, TState> _decryptBlock;
        private TransformBlock<TState, TState> _decompressBlock;

        // Main Flow - Supplied Steps
        private BufferBlock<TState> _readyBuffer;
        private List<TransformBlock<TState, TState>> _suppliedTransforms = new List<TransformBlock<TState, TState>>();

        // Main Flow - PostProcessing
        private BufferBlock<TState> _postProcessingBuffer;
        private TransformBlock<TState, TState> _createLetterBlock;
        private TransformBlock<TState, TState> _compressBlock;
        private TransformBlock<TState, TState> _encryptBlock;
        private ActionBlock<TState> _finalization;

        // Park Flow
        //private BufferBlock<TState> _parkBuffer;
        //private TransformBlock<TState, TState> _parkCompress;
        //private TransformBlock<TState, TState> _parkEncrypt;
        //private ActionBlock<TState> _parkFinalization;

        // Error/Fault Flow
        private BufferBlock<TState> _errorBuffer;
        private ActionBlock<TState> _errorAction;

        public ConsumerWorkflow(
            IRabbitService rabbitService,
            string consumerName)
        {
            Guard.AgainstNull(rabbitService, nameof(rabbitService));
            Guard.AgainstNullOrEmpty(consumerName, nameof(consumerName));

            _logger = LogHelper.LoggerFactory.CreateLogger<ConsumerWorkflow<TState>>();
            _rabbitService = rabbitService;
            _consumer = rabbitService.GetConsumer(consumerName);

            _linkStepOptions = new DataflowLinkOptions { PropagateCompletion = true };
            _executeStepOptions = new ExecutionDataflowBlockOptions
            {
                MaxDegreeOfParallelism = _consumer.ConsumerOptions.ConsumerPipelineOptions.MaxDegreesOfParallelism ?? 1,
                SingleProducerConstrained = true,
            };

            _executeStepOptions.EnsureOrdered = _consumer.ConsumerOptions.ConsumerPipelineOptions.EnsureOrdered ?? true;
        }

        public ConsumerWorkflow<TState> WithSerilizationProvider(SerializationProvider provider)
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

        public ConsumerWorkflow<TState> WithInputBuffer(int bufferCapacity)
        {
            _inputBuffer = new BufferBlock<ReceivedData>(new DataflowBlockOptions { BoundedCapacity = bufferCapacity > 0 ? bufferCapacity : 1000 });
            return this;
        }

        public ConsumerWorkflow<TState> WithErrorHandling(Action<TState> action, int bufferCapacity, int? maxDoPOverride = null, bool? ensureOrdered = null)
        {
            _errorBuffer = new BufferBlock<TState>(new DataflowBlockOptions { BoundedCapacity = bufferCapacity > 0 ? bufferCapacity : 1000 });
            var executionOptions = GetExecuteStepOptions(maxDoPOverride, ensureOrdered, bufferCapacity);
            _errorAction = BlockBuilders.GetWrappedActionBlock(action, executionOptions);
            return this;
        }

        public ConsumerWorkflow<TState> WithDeserializeStep(Func<ReceivedData, TState> createState, int? maxDoPOverride = null, bool? ensureOrdered = null, int? bufferSizeOverride = null)
        {
            Guard.AgainstNull(createState, nameof(createState));
            var executionOptions = GetExecuteStepOptions(maxDoPOverride, ensureOrdered, bufferSizeOverride);
            _deserializeBlock = BlockBuilders.GetStateTransformBlock(createState, executionOptions);
            return this;
        }

        public ConsumerWorkflow<TState> WithDecryptionStep(int? maxDoPOverride = null, bool? ensureOrdered = null, int? bufferSizeOverride = null)
        {
            Guard.AgainstNull(_encryptionProvider, nameof(_encryptionProvider));
            var executionOptions = GetExecuteStepOptions(maxDoPOverride, ensureOrdered, bufferSizeOverride);
            _decryptBlock = BlockBuilders.GetByteManipulationTransformBlock<TState>(_encryptionProvider.Decrypt, executionOptions, false);
            return this;
        }

        public ConsumerWorkflow<TState> WithDecompressionStep(int? maxDoPOverride = null, bool? ensureOrdered = null, int? bufferSizeOverride = null)
        {
            Guard.AgainstNull(_compressProvider, nameof(_compressProvider));
            var executionOptions = GetExecuteStepOptions(maxDoPOverride, ensureOrdered, bufferSizeOverride);
            _decompressBlock = BlockBuilders.GetByteManipulationTransformBlock<TState>(_compressProvider.DecompressAsync, executionOptions, false);
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

        public ConsumerWorkflow<TState> WithCreateLetter(int? maxDoPOverride = null, bool? ensureOrdered = null, int? bufferSizeOverride = null)
        {
            Guard.AgainstNull(_compressProvider, nameof(_compressProvider));
            var executionOptions = GetExecuteStepOptions(maxDoPOverride, ensureOrdered, bufferSizeOverride);
            // TODO:
            return this;
        }

        public ConsumerWorkflow<TState> WithCompression(int? maxDoPOverride = null, bool? ensureOrdered = null, int? bufferSizeOverride = null)
        {
            Guard.AgainstNull(_compressProvider, nameof(_compressProvider));
            var executionOptions = GetExecuteStepOptions(maxDoPOverride, ensureOrdered, bufferSizeOverride);
            _compressBlock = BlockBuilders.GetByteManipulationTransformBlock<TState>(_compressProvider.CompressAsync, executionOptions, true);
            return this;
        }

        public ConsumerWorkflow<TState> WithEncryption(int? maxDoPOverride = null, bool? ensureOrdered = null, int? bufferSizeOverride = null)
        {
            Guard.AgainstNull(_encryptionProvider, nameof(_encryptionProvider));
            var executionOptions = GetExecuteStepOptions(maxDoPOverride, ensureOrdered, bufferSizeOverride);
            _encryptBlock = BlockBuilders.GetByteManipulationTransformBlock<TState>(_encryptionProvider.Encrypt, executionOptions, true);
            return this;
        }

        public ConsumerWorkflow<TState> WithFinalization(Action<TState> action, int? maxDoPOverride = null, bool? ensureOrdered = null, int? bufferSizeOverride = null)
        {
            Guard.AgainstNull(_encryptionProvider, nameof(_encryptionProvider));
            var executionOptions = GetExecuteStepOptions(maxDoPOverride, ensureOrdered, bufferSizeOverride);
            _finalization = BlockBuilders.GetWrappedActionBlock<TState>(action, executionOptions);
            return this;
        }

        public ConsumerWorkflow<TState> BuildLinkages(DataflowLinkOptions overrideOptions = null)
        {
            Guard.AgainstNull(_deserializeBlock, nameof(_deserializeBlock));
            Guard.AgainstNull(_errorBuffer, nameof(_errorBuffer));
            Guard.AgainstNull(_finalization, nameof(_finalization));

            _consumerBlock = new ConsumerBlock<ReceivedData>(_consumer);

            if (_inputBuffer == null)
            { _inputBuffer = new BufferBlock<ReceivedData>(); }

            if (_readyBuffer == null)
            { _readyBuffer = new BufferBlock<TState>(); }

            if (_postProcessingBuffer == null)
            { _postProcessingBuffer = new BufferBlock<TState>(); }

            _consumerBlock.LinkTo(_inputBuffer, overrideOptions ?? _linkStepOptions);
            _inputBuffer.LinkTo(_deserializeBlock, overrideOptions ?? _linkStepOptions);
            _deserializeBlock.LinkTo(_errorBuffer, overrideOptions ?? _linkStepOptions, x => x == null);

            // Link Deserialize to DecryptBlock with predicate if its not null.
            if (_decryptBlock != null)
            {
                _deserializeBlock.LinkTo(_decryptBlock, overrideOptions ?? _linkStepOptions, x => x != null);
                _decryptBlock.LinkTo(_errorBuffer, overrideOptions ?? _linkStepOptions, x => x.IsFaulted);  // Decrypt Fault Linkage

                if (_decompressBlock != null)
                {
                    _decryptBlock.LinkTo(_decompressBlock, overrideOptions ?? _linkStepOptions, x => !x.IsFaulted);
                    _decompressBlock.LinkTo(_errorBuffer, overrideOptions ?? _linkStepOptions, x => x.IsFaulted);  // Decompress Fault Linkage
                }
            }
            else if (_decompressBlock != null)
            {
                _deserializeBlock.LinkTo(_decompressBlock, overrideOptions ?? _linkStepOptions, x => !x.IsFaulted);
                _decompressBlock.LinkTo(_errorBuffer, overrideOptions ?? _linkStepOptions, x => x.IsFaulted);  // Decompress Fault Linkage
            }

            // If Decrypt and Decompress are both null, attach CreateStateBlock to ReadyForProcessingBuffer.
            if (_decryptBlock == null && _decompressBlock == null)
            { _deserializeBlock.LinkTo(_readyBuffer, overrideOptions ?? _linkStepOptions, x => !x.IsFaulted); }
            // If Decrypt exists and Decompress does not, attach DecryptBlock to ReadyForProcessingBuffer.
            else if (_decryptBlock != null && _decompressBlock == null)
            { _decryptBlock.LinkTo(_readyBuffer, overrideOptions ?? _linkStepOptions, x => !x.IsFaulted); }
            // If Decrypt & Decompress (or just Decompress) exists, attach Decompress to ReadyForProcessingBuffer.
            else
            { _decompressBlock.LinkTo(_readyBuffer, overrideOptions ?? _linkStepOptions, x => !x.IsFaulted); }

            // Link all user steps.
            if (_suppliedTransforms?.Count > 0)
            {
                for (int i = 0; i < _suppliedTransforms.Count; i++)
                {
                    if (i == 0)
                    {
                        _readyBuffer.LinkTo(_suppliedTransforms[i], overrideOptions ?? _linkStepOptions);
                        _suppliedTransforms[i].LinkTo(_errorBuffer, overrideOptions ?? _linkStepOptions, x => x.IsFaulted); // Step Fault Linkage

                    }
                    else // Link Previous Step, To Next Step
                    {
                        _suppliedTransforms[i - 1].LinkTo(_suppliedTransforms[i], overrideOptions ?? _linkStepOptions, x => !x.IsFaulted);
                        _suppliedTransforms[i].LinkTo(_errorBuffer, overrideOptions ?? _linkStepOptions, x => x.IsFaulted); // Step Fault Linkage
                    }
                }

                // Link the last user step to PostProcessingBuffer.
                _suppliedTransforms[^1].LinkTo(_postProcessingBuffer, overrideOptions ?? _linkStepOptions, x => !x.IsFaulted);
            }

            if (_compressBlock != null)
            {
                _postProcessingBuffer.LinkTo(_compressBlock, overrideOptions ?? _linkStepOptions, x => !x.IsFaulted);
                _compressBlock.LinkTo(_errorBuffer, overrideOptions ?? _linkStepOptions, x => x.IsFaulted); // Compress Fault Linkage

                if (_encryptBlock != null)
                {
                    _compressBlock.LinkTo(_encryptBlock, overrideOptions ?? _linkStepOptions, x => !x.IsFaulted);
                    _encryptBlock.LinkTo(_errorBuffer, overrideOptions ?? _linkStepOptions, x => x.IsFaulted); // Encrypt Fault Linkage
                }
            }
            else if (_encryptBlock != null)
            {
                _postProcessingBuffer.LinkTo(_encryptBlock, overrideOptions ?? _linkStepOptions, x => !x.IsFaulted);
                _encryptBlock.LinkTo(_errorBuffer, overrideOptions ?? _linkStepOptions, x => x.IsFaulted); // Encrypt Fault Linkage
            }

            // If Compress and Encrypt are both null, attach to PostProcessingBuffer.
            if (_compressBlock == null && _encryptBlock == null)
            { _postProcessingBuffer.LinkTo(_finalization, overrideOptions ?? _linkStepOptions, x => !x.IsFaulted); }
            // If Compress exists and Encrypt does not, attach Finalization to Compression.
            else if (_compressBlock != null && _encryptBlock == null)
            { _compressBlock.LinkTo(_finalization, overrideOptions ?? _linkStepOptions, x => !x.IsFaulted); }
            // If Compress & Encrypt (or just Encrypt) exists, attach Finalization to EncryptBlock.
            else
            { _encryptBlock.LinkTo(_finalization, overrideOptions ?? _linkStepOptions, x => !x.IsFaulted); }

            _errorBuffer.LinkTo(_errorAction, overrideOptions ?? _linkStepOptions);

            return this;
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
    }
}
