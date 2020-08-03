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
        private BufferBlock<ReceivedData> _inputBuffer;
        private TransformBlock<ReceivedData, TState> _deserializeBlock;
        private TransformBlock<TState, TState> _decryptBlock;
        private TransformBlock<TState, TState> _decompressBlock;
        private BufferBlock<TState> _readyForProcessingBuffer;

        // Main Flow - Supplied Steps
        private List<TransformBlock<TState, TState>> _suppliedTransforms = new List<TransformBlock<TState, TState>>();

        // Main Flow - PostProcessing
        private BufferBlock<TState> _postProcessingBuffer;
        private TransformBlock<TState, TState> _postCompressBlock;
        private TransformBlock<TState, TState> _postEncryptBlock;
        private ActionBlock<TState> _finalization;

        // Park Flow
        //private BufferBlock<TState> _parkBuffer;
        //private TransformBlock<TState, TState> _parkCompress;
        //private TransformBlock<TState, TState> _parkEncrypt;
        //private ActionBlock<TState> _parkFinalization;

        // Error/Fault Flow
        private BufferBlock<TState> _errorBuffer;
        private ActionBlock<TState> _errorFinalization;

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
            _errorFinalization = BlockBuilders.GetWrappedActionBlock(action, executionOptions);
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
            _decryptBlock = BlockBuilders.GetByteManipulationTransformBlock<TState>(_encryptionProvider.Decrypt, executionOptions);
            return this;
        }

        public ConsumerWorkflow<TState> WithDecompressionStep(int? maxDoPOverride = null, bool? ensureOrdered = null, int? bufferSizeOverride = null)
        {
            Guard.AgainstNull(_compressProvider, nameof(_compressProvider));
            var executionOptions = GetExecuteStepOptions(maxDoPOverride, ensureOrdered, bufferSizeOverride);
            _decompressBlock = BlockBuilders.GetByteManipulationTransformBlock<TState>(_compressProvider.DecompressAsync, executionOptions);
            return this;
        }

        public ConsumerWorkflow<TState> WithReadyToProcessBuffer(int bufferCapacity)
        {
            _readyForProcessingBuffer = new BufferBlock<TState>(new DataflowBlockOptions { BoundedCapacity = bufferCapacity > 0 ? bufferCapacity : 1000 });
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

        public ConsumerWorkflow<TState> WithPostProcessingCompression(int? maxDoPOverride = null, bool? ensureOrdered = null, int? bufferSizeOverride = null)
        {
            Guard.AgainstNull(_compressProvider, nameof(_compressProvider));
            var executionOptions = GetExecuteStepOptions(maxDoPOverride, ensureOrdered, bufferSizeOverride);
            _postCompressBlock = BlockBuilders.GetByteManipulationTransformBlock<TState>(_compressProvider.CompressAsync, executionOptions);
            return this;
        }

        public ConsumerWorkflow<TState> WithPostProcessingEncryption(int? maxDoPOverride = null, bool? ensureOrdered = null, int? bufferSizeOverride = null)
        {
            Guard.AgainstNull(_encryptionProvider, nameof(_encryptionProvider));
            var executionOptions = GetExecuteStepOptions(maxDoPOverride, ensureOrdered, bufferSizeOverride);
            _postEncryptBlock = BlockBuilders.GetByteManipulationTransformBlock<TState>(_encryptionProvider.Encrypt, executionOptions);
            return this;
        }

        public ConsumerWorkflow<TState> WithFinalization(Action<TState> action, int? maxDoPOverride = null, bool? ensureOrdered = null, int? bufferSizeOverride = null)
        {
            Guard.AgainstNull(_encryptionProvider, nameof(_encryptionProvider));
            var executionOptions = GetExecuteStepOptions(maxDoPOverride, ensureOrdered, bufferSizeOverride);
            _finalization = BlockBuilders.GetWrappedActionBlock<TState>(action, executionOptions);
            return this;
        }

        // Visualization
        // 1.) InputBuffer -> Deserialize
        //
        // 2.) Deserialize -> ErrorBuffer
        // 2.) Deserialize -> Decrypt
        // 2.) Deserialize -> Decrypt -> Decompress
        // 2.) Deserialize -> Decompress
        // 2.) Deserialize -> ReadyForProcessing
        //
        // 3.) Decrypt -> ErrorBuffer
        // 3.) Decrypt -> Decompress (if not null) -> ReadForProcessing
        // 3.) Decrypt -> ReadyForProcessing
        //
        // 4.) Decompress -> Error
        // 4.) Decompress -> ReadyForProcessing
        //
        // Supplied Steps
        // 5.) ReadyForProcessing -> Step[0]
        // 5.) Step[0] -> Error
        // 5.) For n : Step(n) link to Step(n-1)
        // 5.) For n : Step(n) link to ErrorBuffer
        // 5.) Step(n) => PostProcessingBuffer
        //
        // 6.) PostProcessing -> PostCompression -> PostEncryption -> Finalization
        // 6.) PostProcessing -> PostEncryption -> Finalization
        // 6.) PostProcessing -> PostCompression -> Finalization
        // 6.) PostProcessing -> Finalization
        //
        // 7.) PostCompression -> ErrorBuffer
        // 7.) PostEncryption -> ErrorBuffer
        // 7.) ErrorBuffer -> ErrorFinilization
        public ConsumerWorkflow<TState> BuildLinkages(DataflowLinkOptions overrideOptions = null)
        {
            Guard.AgainstNull(_deserializeBlock, nameof(_deserializeBlock));
            Guard.AgainstNull(_errorBuffer, nameof(_errorBuffer));
            Guard.AgainstNull(_finalization, nameof(_finalization));

            if (_inputBuffer == null)
            { _inputBuffer = new BufferBlock<ReceivedData>(); }

            if (_readyForProcessingBuffer == null)
            { _readyForProcessingBuffer = new BufferBlock<TState>(); }

            if (_postProcessingBuffer == null)
            { _postProcessingBuffer = new BufferBlock<TState>(); }

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
            { _deserializeBlock.LinkTo(_readyForProcessingBuffer, overrideOptions ?? _linkStepOptions, x => !x.IsFaulted); }
            // If Decrypt exists and Decompress does not, attach DecryptBlock to ReadyForProcessingBuffer.
            else if (_decryptBlock != null && _decompressBlock == null)
            { _decryptBlock.LinkTo(_readyForProcessingBuffer, overrideOptions ?? _linkStepOptions, x => !x.IsFaulted); }
            // If Decrypt & Decompress (or just Decompress) exists, attach Decompress to ReadyForProcessingBuffer.
            else
            { _decompressBlock.LinkTo(_readyForProcessingBuffer, overrideOptions ?? _linkStepOptions, x => !x.IsFaulted); }

            // Link all user steps.
            if (_suppliedTransforms?.Count > 0)
            {
                for (int i = 0; i < _suppliedTransforms.Count; i++)
                {
                    if (i == 0)
                    {
                        _readyForProcessingBuffer.LinkTo(_suppliedTransforms[i], overrideOptions ?? _linkStepOptions);
                        _suppliedTransforms[i].LinkTo(_errorBuffer, overrideOptions ?? _linkStepOptions, x => x.IsFaulted); // Step Fault Linkage

                    }
                    else // Link Previous Step, To Next Step
                    {
                        _suppliedTransforms[i - 1].LinkTo(_suppliedTransforms[i], overrideOptions ?? _linkStepOptions, x => !x.IsFaulted);
                        _suppliedTransforms[i].LinkTo(_errorBuffer, overrideOptions ?? _linkStepOptions, x => x.IsFaulted); // Step Fault Linkage
                    }
                }

                _suppliedTransforms[^1].LinkTo(_postProcessingBuffer, overrideOptions ?? _linkStepOptions, x => !x.IsFaulted);
            }

            if (_postCompressBlock != null)
            {
                _postProcessingBuffer.LinkTo(_postCompressBlock, overrideOptions ?? _linkStepOptions, x => !x.IsFaulted);
                _postCompressBlock.LinkTo(_errorBuffer, overrideOptions ?? _linkStepOptions, x => x.IsFaulted); // Compress Fault Linkage

                if (_postEncryptBlock != null)
                {
                    _postCompressBlock.LinkTo(_postEncryptBlock, overrideOptions ?? _linkStepOptions, x => !x.IsFaulted);
                    _postEncryptBlock.LinkTo(_errorBuffer, overrideOptions ?? _linkStepOptions, x => x.IsFaulted); // Encrypt Fault Linkage
                }
            }
            else if (_postEncryptBlock != null)
            {
                _postProcessingBuffer.LinkTo(_postEncryptBlock, overrideOptions ?? _linkStepOptions, x => !x.IsFaulted);
                _postEncryptBlock.LinkTo(_errorBuffer, overrideOptions ?? _linkStepOptions, x => x.IsFaulted); // Encrypt Fault Linkage
            }

            // If Compress and Encrypt are both null, attach to PostProcessingBuffer.
            if (_postCompressBlock == null && _postEncryptBlock == null)
            { _postProcessingBuffer.LinkTo(_finalization, overrideOptions ?? _linkStepOptions, x => !x.IsFaulted); }
            // If Compress exists and Encrypt does not, attach Finalization to Compression.
            else if (_postCompressBlock != null && _postEncryptBlock == null)
            { _postCompressBlock.LinkTo(_finalization, overrideOptions ?? _linkStepOptions, x => !x.IsFaulted); }
            // If Compress & Encrypt (or just Encrypt) exists, attach Finalization to EncryptBlock.
            else
            { _postEncryptBlock.LinkTo(_finalization, overrideOptions ?? _linkStepOptions, x => !x.IsFaulted); }

            _errorBuffer.LinkTo(_errorFinalization, overrideOptions ?? _linkStepOptions);

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
