using HouseofCat.Compression;
using HouseofCat.Encryption;
using HouseofCat.Logger;
using HouseofCat.RabbitMQ.Pipelines;
using HouseofCat.RabbitMQ.Services;
using HouseofCat.Serialization;
using HouseofCat.Utilities.Errors;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks.Dataflow;

namespace HouseofCat.RabbitMQ.Workflows
{
    public class ConsumerWorkflow<TState> where TState : IWorkState, new()
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
        private readonly BufferBlock<ReceivedData> _inputBuffer;
        private readonly TransformBlock<ReceivedData, TState> _createState;
        private readonly TransformBlock<TState, TState> _decryptBlock;
        private readonly TransformBlock<TState, TState> _decompressBlock;
        private readonly BufferBlock<TState> _readyForProcessing;

        // Main Flow - Supplied Steps
        private readonly TransformBlock<TState, TState>[] _suppliedTransforms;

        // Main Flow - PostProcessing
        private readonly BufferBlock<TState> _postProcessing;
        private readonly TransformBlock<TState, TState> _mainCompress;
        private readonly TransformBlock<TState, TState> _mainEncrypt;
        private readonly Action<TState> _finalization;

        // Park Flow
        private readonly BufferBlock<TState> _parkBuffer;
        private readonly TransformBlock<TState, TState> _parkCompress;
        private readonly TransformBlock<TState, TState> _parkEncrypt;
        private readonly Action<TState> _parkFinalization;

        // Error/Fault Flow
        private readonly BufferBlock<TState> _errorBuffer;
        private readonly Action<TState> _exceptionProcessor;

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
