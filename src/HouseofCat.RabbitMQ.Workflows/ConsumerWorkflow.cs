using HouseofCat.Logger;
using HouseofCat.RabbitMQ.Pipelines;
using HouseofCat.RabbitMQ.Services;
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
        public string ConsumerWorkflowName { get; }
        public string ConsumerName { get; }
        public int ConsumerCount;

        private readonly IRabbitService _rabbitService;
        private readonly ConsumerOptions _consumerOptions;

        private readonly List<ConsumerBlock<ReceivedData>> _consumerBlocks; // Doubles as a BufferBlock.
        private BufferBlock<ReceivedData> _inputBuffer;
        private TransformBlock<ReceivedData, TState> _buildStateBlock;
        private TransformBlock<TState, TState> _createSendLetter;
        private TransformBlock<TState, TState> _sendLetterBlock;

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

        public ConsumerWorkflow<TState> WithBuildState<TOut>(string key, int? maxDoPOverride = null, bool? ensureOrdered = null, int? bufferSizeOverride = null)
        {
            var executionOptions = GetExecuteStepOptions(maxDoPOverride, ensureOrdered, bufferSizeOverride);
            _buildStateBlock = GetBuildStateBlock<TOut>(_serializationProvider, key, executionOptions);
            return this;
        }

        public ConsumerWorkflow<TState> WithDecryptionStep(int? maxDoPOverride = null, bool? ensureOrdered = null, int? bufferSizeOverride = null)
        {
            Guard.AgainstNull(_encryptionProvider, nameof(_encryptionProvider));
            var executionOptions = GetExecuteStepOptions(maxDoPOverride, ensureOrdered, bufferSizeOverride);
            _decryptBlock = GetByteManipulationTransformBlock(_encryptionProvider.Decrypt, _serializationProvider, executionOptions, false, x => x.ReceivedData.Encrypted);
            return this;
        }

        public ConsumerWorkflow<TState> WithDecompressionStep(int? maxDoPOverride = null, bool? ensureOrdered = null, int? bufferSizeOverride = null)
        {
            Guard.AgainstNull(_compressProvider, nameof(_compressProvider));
            var executionOptions = GetExecuteStepOptions(maxDoPOverride, ensureOrdered, bufferSizeOverride);
            _decompressBlock = GetByteManipulationTransformBlock(_compressProvider.Decompress, _serializationProvider, executionOptions, false, x => x.ReceivedData.Compressed);
            return this;
        }

        public ConsumerWorkflow<TState> WithCreateSendLetter(Func<TState, Task<TState>> createLetter, int? maxDoPOverride = null, bool? ensureOrdered = null, int? bufferSizeOverride = null)
        {
            Guard.AgainstNull(_compressProvider, nameof(_compressProvider));
            var executionOptions = GetExecuteStepOptions(maxDoPOverride, ensureOrdered, bufferSizeOverride);
            _createSendLetter = GetWrappedTransformBlock(createLetter, executionOptions);
            return this;
        }

        public ConsumerWorkflow<TState> WithCompression(int? maxDoPOverride = null, bool? ensureOrdered = null, int? bufferSizeOverride = null)
        {
            Guard.AgainstNull(_compressProvider, nameof(_compressProvider));
            var executionOptions = GetExecuteStepOptions(maxDoPOverride, ensureOrdered, bufferSizeOverride);
            _compressBlock = GetByteManipulationTransformBlock(_compressProvider.CompressAsync, null, executionOptions, true, x => !x.ReceivedData.Compressed);
            return this;
        }

        public ConsumerWorkflow<TState> WithEncryption(int? maxDoPOverride = null, bool? ensureOrdered = null, int? bufferSizeOverride = null)
        {
            Guard.AgainstNull(_encryptionProvider, nameof(_encryptionProvider));
            var executionOptions = GetExecuteStepOptions(maxDoPOverride, ensureOrdered, bufferSizeOverride);
            _encryptBlock = GetByteManipulationTransformBlock(_encryptionProvider.Encrypt, null, executionOptions, true, x => !x.ReceivedData.Encrypted);
            return this;
        }

        public ConsumerWorkflow<TState> WithSendStep(int? maxDoPOverride = null, bool? ensureOrdered = null, int? bufferSizeOverride = null)
        {
            var executionOptions = GetExecuteStepOptions(maxDoPOverride, ensureOrdered, bufferSizeOverride);
            _sendLetterBlock = GetWrappedPublishTransformBlock(_rabbitService, executionOptions);
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
                var consumer = new Consumer(_rabbitService.ChannelPool, ConsumerName);
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

        private TState BuildState<TOut>(ReceivedData data, ISerializationProvider provider, string key)
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
                { return BuildState<TOut>(data, provider, key); }
                catch
                { return null; }
            }

            return new TransformBlock<ReceivedData, TState>(BuildStateWrap, options);
        }

        public TransformBlock<TState, TState> GetByteManipulationTransformBlock(
            Func<ReadOnlyMemory<byte>, byte[]> action,
            ISerializationProvider serializationProvider,
            ExecutionDataflowBlockOptions options,
            bool outbound,
            Predicate<TState> predicate)
        {
            TState WrapAction(TState state)
            {
                try
                {
                    if (outbound)
                    {
                        if (state.SendData?.Length > 0)
                        { state.SendData = action(state.SendData); }
                        else if (state.SendLetter.Body?.Length > 0)
                        { state.SendLetter.Body = action(state.SendLetter.Body); }
                    }
                    else if (predicate(state))
                    {
                        if (state.ReceivedData.ContentType == Constants.HeaderValueForLetter)
                        {
                            if (state.ReceivedData.Letter == null)
                            { state.ReceivedData.Letter = serializationProvider.Deserialize<Letter>(state.ReceivedData.Data); }

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
            ISerializationProvider serializationProvider,
            ExecutionDataflowBlockOptions options,
            bool outbound, Predicate<TState> predicate)
        {
            async Task<TState> WrapActionAsync(TState state)
            {
                try
                {
                    if (outbound)
                    {
                        if (state.SendData?.Length > 0)
                        { state.SendData = await action(state.SendData).ConfigureAwait(false); }
                        else if (state.SendLetter.Body?.Length > 0)
                        { state.SendLetter.Body = await action(state.SendLetter.Body).ConfigureAwait(false); }
                    }
                    else if (predicate(state))
                    {
                        if (state.ReceivedData.ContentType == Constants.HeaderValueForLetter)
                        {
                            if (state.ReceivedData.Letter == null)
                            { state.ReceivedData.Letter = serializationProvider.Deserialize<Letter>(state.ReceivedData.Data); }

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

        public TransformBlock<TState, TState> GetWrappedPublishTransformBlock(IRabbitService service, ExecutionDataflowBlockOptions options)
        {
            async Task<TState> WrapPublishAsync(TState state)
            {
                try
                {
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
    }
}
