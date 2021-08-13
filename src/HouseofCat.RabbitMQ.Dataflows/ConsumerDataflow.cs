using HouseofCat.Compression;
using HouseofCat.Dataflows;
using HouseofCat.Encryption;
using HouseofCat.Metrics;
using HouseofCat.RabbitMQ.Services;
using HouseofCat.RabbitMQ.WorkState;
using HouseofCat.Serialization;
using HouseofCat.Utilities.Errors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using static HouseofCat.Reflection.Generics;

namespace HouseofCat.RabbitMQ.Dataflows
{
    public class ConsumerDataflow<TState> : BaseDataflow<TState> where TState : class, IRabbitWorkState, new()
    {
        public string WorkflowName { get; }

        private readonly IRabbitService _rabbitService;
        private readonly ICollection<IConsumer<ReceivedData>> _consumers;
        private readonly ConsumerOptions _consumerOptions;
        private readonly TaskScheduler _taskScheduler;
        private readonly string _consumerName;
        private readonly int _consumerCount;

        // Main Flow - Ingestion
        private readonly List<ConsumerBlock<ReceivedData>> _consumerBlocks;
        private BufferBlock<ReceivedData> _inputBuffer;
        private TransformBlock<ReceivedData, TState> _buildStateBlock;
        private TransformBlock<TState, TState> _createSendLetter;
        protected TransformBlock<TState, TState> _decryptBlock;
        protected TransformBlock<TState, TState> _decompressBlock;

        // Main Flow - User Defined/Supplied Steps
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

        public ConsumerDataflow(
            IRabbitService rabbitService,
            string workflowName,
            string consumerName,
            int consumerCount,
            TaskScheduler taskScheduler = null)
        {
            Guard.AgainstNull(rabbitService, nameof(rabbitService));
            Guard.AgainstNullOrEmpty(consumerName, nameof(consumerName));

            WorkflowName = workflowName;
            _consumerCount = consumerCount;
            _consumerName = consumerName;

            _rabbitService = rabbitService;
            _consumerOptions = rabbitService.Options.GetConsumerOptions(consumerName);
            _serializationProvider = rabbitService.SerializationProvider;

            _linkStepOptions = new DataflowLinkOptions { PropagateCompletion = true };
            _taskScheduler = taskScheduler ?? TaskScheduler.Current;

            _executeStepOptions = new ExecutionDataflowBlockOptions
            {
                MaxDegreeOfParallelism = _consumerOptions.ConsumerPipelineOptions.MaxDegreesOfParallelism ?? 1,
                SingleProducerConstrained = true,
                EnsureOrdered = _consumerOptions.ConsumerPipelineOptions.EnsureOrdered ?? true,
                TaskScheduler = _taskScheduler,
            };

            _consumerBlocks = new List<ConsumerBlock<ReceivedData>>();
        }

        /// <summary>
        /// This constructor is used for when you want to supply Consumers manually, or custom Consumers without having to write a custom IRabbitService,
        /// and have global consumer pipeline options to retrieve maxDoP and ensureOrdered from.
        /// </summary>
        /// <param name="rabbitService"></param>
        /// <param name="workflowName"></param>
        /// <param name="consumers"></param>
        /// <param name="globalConsumerPipelineOptions"></param>
        /// <param name="taskScheduler"></param>
        public ConsumerDataflow(
            IRabbitService rabbitService,
            string workflowName,
            ICollection<IConsumer<ReceivedData>> consumers,
            GlobalConsumerPipelineOptions globalConsumerPipelineOptions,
            TaskScheduler taskScheduler = null) : this(
                rabbitService, 
                workflowName,
                consumers, 
                globalConsumerPipelineOptions?.MaxDegreesOfParallelism ?? 1,
                globalConsumerPipelineOptions?.EnsureOrdered ?? true,
                taskScheduler)
        {
            Guard.AgainstNull(globalConsumerPipelineOptions, nameof(globalConsumerPipelineOptions));
        }

        /// <summary>
        /// This constructor is used for when you want to supply Consumers manually, or custom Consumers without having to write a custom IRabbitService,
        /// and want a custom maxDoP and/or ensureOrdered.
        /// </summary>
        /// <param name="rabbitService"></param>
        /// <param name="workflowName"></param>
        /// <param name="consumers"></param>
        /// <param name="maxDoP"></param>
        /// <param name="ensureOrdered"></param>
        /// <param name="taskScheduler"></param>
        public ConsumerDataflow(
            IRabbitService rabbitService,
            string workflowName,
            ICollection<IConsumer<ReceivedData>> consumers,
            int maxDoP,
            bool ensureOrdered,
            TaskScheduler taskScheduler = null)
        {
            Guard.AgainstNull(rabbitService, nameof(rabbitService));
            Guard.AgainstNullOrEmpty(consumers, nameof(consumers));

            WorkflowName = workflowName;
            _consumers = consumers;
            
            _rabbitService = rabbitService;
            _serializationProvider = rabbitService.SerializationProvider;

            _linkStepOptions = new DataflowLinkOptions { PropagateCompletion = true };
            _taskScheduler = taskScheduler ?? TaskScheduler.Current;

            _executeStepOptions = new ExecutionDataflowBlockOptions
            {
                MaxDegreeOfParallelism = maxDoP,
                SingleProducerConstrained = true,
                EnsureOrdered = ensureOrdered,
                TaskScheduler = _taskScheduler,
            };

            _consumerBlocks = new List<ConsumerBlock<ReceivedData>>();
        }

        public virtual Task StartAsync() => StartAsync<ConsumerBlock<ReceivedData>>();

        protected async Task StartAsync<TConsumerBlock>() where TConsumerBlock : ConsumerBlock<ReceivedData>
        {
            BuildLinkages<TConsumerBlock>();

            foreach (var consumerBlock in _consumerBlocks)
            {
                await consumerBlock.StartConsumingAsync().ConfigureAwait(false);
            }
        }

        public async Task StopAsync(bool immediate = false)
        {
            foreach (var consumerBlock in _consumerBlocks)
            {
                await consumerBlock.StopConsumingAsync(immediate).ConfigureAwait(false);
                consumerBlock.Complete();
            }
        }

        /// <summary>
        /// Allows you to set the consumers serialization provider. This will be used to convert your bytes into an object.
        /// <para>By default, the serialization provider will auto-assign the same serialization provider as the one RabbitService uses. This is the most common use case.</para>
        /// </summary>
        /// <param name="provider"></param>
        /// <returns></returns>
        public ConsumerDataflow<TState> SetSerializationProvider(ISerializationProvider provider)
        {
            Guard.AgainstNull(provider, nameof(provider));
            _serializationProvider = provider;
            return this;
        }

        /// <summary>
        /// Allows you to unset the consumers serialization provider. This will be used when you are not using any serialization on your inner byte payloads.
        /// <para>By default, the serialization provider will auto-assign the same serialization provider (in the Constructor) as the one RabbitService uses.</para>
        /// <para>This is a more exotic scenario where you may be moving plain bytes around.</para>
        /// <para>ex.) You are transferring data from queue to database (or other queue) and don't need to deserialize the bytes.</para>
        /// </summary>
        /// <param name="provider"></param>
        /// <returns></returns>
        public ConsumerDataflow<TState> UnsetSerializationProvider()
        {
            _serializationProvider = null;
            return this;
        }

        public ConsumerDataflow<TState> SetCompressionProvider(ICompressionProvider provider)
        {
            Guard.AgainstNull(provider, nameof(provider));
            _compressProvider = provider;
            return this;
        }

        public ConsumerDataflow<TState> SetEncryptionProvider(IEncryptionProvider provider)
        {
            Guard.AgainstNull(provider, nameof(provider));
            _encryptionProvider = provider;
            return this;
        }

        public ConsumerDataflow<TState> SetMetricsProvider(IMetricsProvider provider)
        {
            _metricsProvider = provider ?? new NullMetricsProvider();
            return this;
        }

        #region Step Adders

        public ConsumerDataflow<TState> WithErrorHandling(
            Action<TState> action,
            int boundedCapacity,
            int? maxDoP = null,
            bool? ensureOrdered = null,
            TaskScheduler taskScheduler = null)
        {
            Guard.AgainstNull(action, nameof(action));
            if (_errorBuffer == null)
            {
                _errorBuffer = new BufferBlock<TState>(new DataflowBlockOptions { BoundedCapacity = boundedCapacity > 0 ? boundedCapacity : 1000 });
                var executionOptions = GetExecuteStepOptions(maxDoP, ensureOrdered, boundedCapacity, taskScheduler ?? _taskScheduler);
                _errorAction = GetWrappedActionBlock(action, executionOptions, $"{WorkflowName}_ErrorHandler", false);
            }
            return this;
        }

        public ConsumerDataflow<TState> WithErrorHandling(
            Func<TState, Task> action,
            int boundedCapacity,
            int? maxDoP = null,
            bool? ensureOrdered = null,
            TaskScheduler taskScheduler = null)
        {
            Guard.AgainstNull(action, nameof(action));
            if (_errorBuffer == null)
            {
                _errorBuffer = new BufferBlock<TState>(new DataflowBlockOptions { BoundedCapacity = boundedCapacity > 0 ? boundedCapacity : 1000 });
                var executionOptions = GetExecuteStepOptions(maxDoP, ensureOrdered, boundedCapacity, taskScheduler ?? _taskScheduler);
                _errorAction = GetWrappedActionBlock(action, executionOptions, $"{WorkflowName}_ErrorHandler", false);
            }
            return this;
        }

        public ConsumerDataflow<TState> WithReadyToProcessBuffer(
            int boundedCapacity,
            TaskScheduler taskScheduler = null)
        {
            if (_readyBuffer == null)
            {
                _readyBuffer = new BufferBlock<TState>(
                    new DataflowBlockOptions
                    {
                        BoundedCapacity = boundedCapacity > 0 ? boundedCapacity : 1000,
                        TaskScheduler = taskScheduler ?? _taskScheduler
                    });
            }
            return this;
        }

        public ConsumerDataflow<TState> AddStep(
            Func<TState, TState> suppliedStep,
            string metricIdentifier,
            bool metricMicroScale = false,
            string metricDescription = null,
            int? maxDoP = null,
            bool? ensureOrdered = null,
            int? boundedCapacity = null,
            TaskScheduler taskScheduler = null)
        {
            Guard.AgainstNull(suppliedStep, nameof(suppliedStep));
            _metricsProvider.IncrementCounter($"{WorkflowName}_Steps", metricDescription);
            var executionOptions = GetExecuteStepOptions(maxDoP, ensureOrdered, boundedCapacity, taskScheduler ?? _taskScheduler);
            _suppliedTransforms.Add(GetWrappedTransformBlock(suppliedStep, executionOptions, metricIdentifier, metricMicroScale));
            return this;
        }

        public ConsumerDataflow<TState> AddStep(
            Func<TState, Task<TState>> suppliedStep,
            string metricIdentifier,
            bool metricMicroScale = false,
            string metricDescription = null,
            int? maxDoP = null,
            bool? ensureOrdered = null,
            int? boundedCapacity = null,
            TaskScheduler taskScheduler = null)
        {
            Guard.AgainstNull(suppliedStep, nameof(suppliedStep));
            _metricsProvider.IncrementCounter($"{WorkflowName}_Steps", metricDescription);
            var executionOptions = GetExecuteStepOptions(maxDoP, ensureOrdered, boundedCapacity, taskScheduler ?? _taskScheduler);
            _suppliedTransforms.Add(GetWrappedTransformBlock(suppliedStep, executionOptions, metricIdentifier, metricMicroScale));
            return this;
        }

        public ConsumerDataflow<TState> WithPostProcessingBuffer(
            int boundedCapacity,
            TaskScheduler taskScheduler = null)
        {
            if (_postProcessingBuffer == null)
            {
                _postProcessingBuffer = new BufferBlock<TState>(
                    new DataflowBlockOptions
                    {
                        BoundedCapacity = boundedCapacity > 0 ? boundedCapacity : 1000,
                        TaskScheduler = taskScheduler ?? _taskScheduler
                    });
            }
            return this;
        }

        public ConsumerDataflow<TState> WithFinalization(
            Action<TState> action,
            int? maxDoP = null,
            bool? ensureOrdered = null,
            int? boundedCapacity = null,
            TaskScheduler taskScheduler = null)
        {
            Guard.AgainstNull(action, nameof(action));
            if (_finalization == null)
            {
                _metricsProvider.IncrementCounter($"{WorkflowName}_Steps");
                var executionOptions = GetExecuteStepOptions(maxDoP, ensureOrdered, boundedCapacity, taskScheduler ?? _taskScheduler);
                _finalization = GetWrappedActionBlock(action, executionOptions, $"{WorkflowName}_Finalization", true);
            }
            return this;
        }

        public ConsumerDataflow<TState> WithFinalization(
            Func<TState, Task> action,
            int? maxDoP = null,
            bool? ensureOrdered = null,
            int? boundedCapacity = null,
            TaskScheduler taskScheduler = null)
        {
            Guard.AgainstNull(action, nameof(action));
            if (_finalization == null)
            {
                _metricsProvider.IncrementCounter($"{WorkflowName}_Steps");
                var executionOptions = GetExecuteStepOptions(maxDoP, ensureOrdered, boundedCapacity, taskScheduler ?? _taskScheduler);
                _finalization = GetWrappedActionBlock(action, executionOptions, $"{WorkflowName}_Finalization", true);
            }
            return this;
        }

        public ConsumerDataflow<TState> WithBuildState<TOut>(
            string stateKey,
            int? maxDoP = null,
            bool? ensureOrdered = null,
            int? boundedCapacity = null,
            TaskScheduler taskScheduler = null)
        {
            if (_buildStateBlock == null)
            {
                _metricsProvider.IncrementCounter($"{WorkflowName}_Steps");
                var executionOptions = GetExecuteStepOptions(maxDoP, ensureOrdered, boundedCapacity, taskScheduler ?? _taskScheduler);
                _buildStateBlock = GetBuildStateBlock<TOut>(_serializationProvider, stateKey, executionOptions);
            }
            return this;
        }

        public ConsumerDataflow<TState> WithDecryptionStep(
            int? maxDoP = null,
            bool? ensureOrdered = null,
            int? boundedCapacity = null,
            TaskScheduler taskScheduler = null)
        {
            Guard.AgainstNull(_encryptionProvider, nameof(_encryptionProvider));
            if (_decryptBlock == null)
            {
                _metricsProvider.IncrementCounter($"{WorkflowName}_Steps");
                var executionOptions = GetExecuteStepOptions(maxDoP, ensureOrdered, boundedCapacity, taskScheduler ?? _taskScheduler);

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

        public ConsumerDataflow<TState> WithDecompressionStep(
            int? maxDoP = null,
            bool? ensureOrdered = null,
            int? boundedCapacity = null,
            TaskScheduler taskScheduler = null)
        {
            Guard.AgainstNull(_compressProvider, nameof(_compressProvider));
            if (_decompressBlock == null)
            {
                _metricsProvider.IncrementCounter($"{WorkflowName}_Steps");
                var executionOptions = GetExecuteStepOptions(maxDoP, ensureOrdered, boundedCapacity, taskScheduler ?? _taskScheduler);

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

        public ConsumerDataflow<TState> WithCreateSendLetter(
            Func<TState, Task<TState>> createLetter,
            int? maxDoP = null,
            bool? ensureOrdered = null,
            int? boundedCapacity = null,
            TaskScheduler taskScheduler = null)
        {
            if (_createSendLetter == null)
            {
                _metricsProvider.IncrementCounter($"{WorkflowName}_Steps");
                var executionOptions = GetExecuteStepOptions(maxDoP, ensureOrdered, boundedCapacity, taskScheduler ?? _taskScheduler);
                _createSendLetter = GetWrappedTransformBlock(createLetter, executionOptions, $"{WorkflowName}_CreateSendLetter");
            }
            return this;
        }

        public ConsumerDataflow<TState> WithCompression(
            int? maxDoP = null,
            bool? ensureOrdered = null,
            int? boundedCapacity = null,
            TaskScheduler taskScheduler = null)
        {
            Guard.AgainstNull(_compressProvider, nameof(_compressProvider));
            if (_compressBlock == null)
            {
                _metricsProvider.IncrementCounter($"{WorkflowName}_Steps");
                var executionOptions = GetExecuteStepOptions(maxDoP, ensureOrdered, boundedCapacity, taskScheduler ?? _taskScheduler);

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

        public ConsumerDataflow<TState> WithEncryption(
            int? maxDoP = null,
            bool? ensureOrdered = null,
            int? boundedCapacity = null,
            TaskScheduler taskScheduler = null)
        {
            Guard.AgainstNull(_encryptionProvider, nameof(_encryptionProvider));
            if (_encryptBlock == null)
            {
                _metricsProvider.IncrementCounter($"{WorkflowName}_Steps");
                var executionOptions = GetExecuteStepOptions(maxDoP, ensureOrdered, boundedCapacity, taskScheduler ?? _taskScheduler);

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

        public ConsumerDataflow<TState> WithSendStep(
            int? maxDoP = null,
            bool? ensureOrdered = null,
            int? boundedCapacity = null,
            TaskScheduler taskScheduler = null)
        {
            _metricsProvider.IncrementCounter($"{WorkflowName}_Steps");
            if (_sendLetterBlock == null)
            {
                var executionOptions = GetExecuteStepOptions(maxDoP, ensureOrdered, boundedCapacity, taskScheduler ?? _taskScheduler);
                _sendLetterBlock = GetWrappedPublishTransformBlock(_rabbitService, executionOptions);
            }
            return this;
        }

        #endregion

        #region Step Linking

        private void BuildLinkages<TConsumerBlock>(DataflowLinkOptions overrideOptions = null)
            where TConsumerBlock : ConsumerBlock<ReceivedData>
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

            if (_consumers == null)
            {
                for (var i = 0; i < _consumerCount; i++)
                {
                    var consumerBlock = New<TConsumerBlock>.Instance.Invoke();
                    consumerBlock.Consumer = new Consumer(_rabbitService.ChannelPool, _consumerName);
                    _consumerBlocks.Add(consumerBlock);
                    _consumerBlocks[i].LinkTo(_inputBuffer, overrideOptions ?? _linkStepOptions);
                }
            }
            else
            {
                for (var i = 0; i < _consumers.Count; i++)
                {
                    var consumerBlock = New<TConsumerBlock>.Instance.Invoke();
                    consumerBlock.Consumer = _consumers.ElementAt(i);
                    _consumerBlocks.Add(consumerBlock);
                    _consumerBlocks[i].LinkTo(_inputBuffer, overrideOptions ?? _linkStepOptions);
                }
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
        public virtual TState BuildState<TOut>(ISerializationProvider provider, string key, ReceivedData data)
        {
            var state = New<TState>.Instance.Invoke();
            state.ReceivedData = data;
            state.Data = new Dictionary<string, object>();

            // If the SerializationProvider was assigned, use it, else it's raw bytes.
            if (provider != null)
            { state.Data[key] = provider.Deserialize<TOut>(data.Data); }
            else
            { state.Data[key] = data.Data; }

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
            Func<ReadOnlyMemory<byte>, ArraySegment<byte>> action,
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
                        { state.SendData = action(state.SendData.AsMemory()).ToArray(); }
                        else if (state.SendMessage.Body?.Length > 0)
                        { state.SendMessage.Body = action(state.SendMessage.Body.AsMemory()).ToArray(); }
                    }
                    else if (predicate.Invoke(state))
                    {
                        if (state.ReceivedData.ContentType == Constants.HeaderValueForLetter)
                        {
                            if (state.ReceivedData.Letter == null)
                            { state.ReceivedData.Letter = _serializationProvider.Deserialize<Letter>(state.ReceivedData.Data); }

                            state.ReceivedData.Letter.Body = action(state.ReceivedData.Letter.Body.AsMemory()).ToArray();
                        }
                        else
                        { state.ReceivedData.Data = action(state.ReceivedData.Data.AsMemory()).ToArray(); }
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
                        else if (state.SendMessage.Body?.Length > 0)
                        { state.SendMessage.Body = await action(state.SendMessage.Body).ConfigureAwait(false); }
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

                    await service.Publisher.PublishAsync(state.SendMessage, true, true).ConfigureAwait(false);
                    state.SendMessageSent = true;

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
