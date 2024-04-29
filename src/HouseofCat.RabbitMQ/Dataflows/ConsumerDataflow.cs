using HouseofCat.Compression;
using HouseofCat.Dataflows;
using HouseofCat.Dataflows.Extensions;
using HouseofCat.Encryption;
using HouseofCat.RabbitMQ.Services;
using HouseofCat.Serialization;
using HouseofCat.Utilities.Errors;
using OpenTelemetry.Trace;
using System;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using static HouseofCat.Dataflows.Extensions.WorkStateExtensions;

namespace HouseofCat.RabbitMQ.Dataflows;

public class ConsumerDataflow<TState> : BaseDataflow<TState> where TState : class, IRabbitWorkState, new()
{
    private readonly IRabbitService _rabbitService;
    private readonly ConsumerOptions _consumerOptions;
    private readonly TaskScheduler _taskScheduler;

    // Main Flow - Ingestion
    private readonly List<ConsumerBlock<IReceivedMessage>> _consumerBlocks;
    protected ITargetBlock<IReceivedMessage> _inputBuffer;
    private TransformBlock<IReceivedMessage, TState> _buildStateBlock;
    private TransformBlock<TState, TState> _createSendMessage;
    protected TransformBlock<TState, TState> _decryptBlock;
    protected TransformBlock<TState, TState> _decompressBlock;

    // Main Flow - User Defined/Supplied Steps
    protected ITargetBlock<TState> _readyBuffer;
    protected readonly List<TransformBlock<TState, TState>> _suppliedTransforms = new List<TransformBlock<TState, TState>>();

    // Main Flow - PostProcessing
    protected ITargetBlock<TState> _postProcessingBuffer;
    protected TransformBlock<TState, TState> _compressBlock;
    protected TransformBlock<TState, TState> _encryptBlock;
    protected TransformBlock<TState, TState> _sendMessageBlock;
    protected ActionBlock<TState> _finalization;

    // Error/Fault Flow
    protected ITargetBlock<TState> _errorBuffer;
    protected ActionBlock<TState> _errorAction;

    public string WorkflowName
    {
        get
        {
            return _consumerOptions?.WorkflowName;
        }
    }

    public ConsumerDataflow(
        IRabbitService rabbitService,
        ConsumerOptions consumerOptions,
        TaskScheduler taskScheduler = null)
    {
        Guard.AgainstNull(rabbitService, nameof(rabbitService));
        Guard.AgainstNull(consumerOptions, nameof(consumerOptions));

        _rabbitService = rabbitService;
        _consumerOptions = consumerOptions;
        _serializationProvider = rabbitService.SerializationProvider;

        _linkStepOptions = new DataflowLinkOptions { PropagateCompletion = true };
        _taskScheduler = taskScheduler ?? TaskScheduler.Current;

        _executeStepOptions = new ExecutionDataflowBlockOptions
        {
            MaxDegreeOfParallelism = _consumerOptions.WorkflowMaxDegreesOfParallelism < 1
                ? 1
                : _consumerOptions.WorkflowMaxDegreesOfParallelism,
            SingleProducerConstrained = true,
            EnsureOrdered = _consumerOptions.WorkflowEnsureOrdered,
            TaskScheduler = _taskScheduler,
        };

        _consumerBlocks = new List<ConsumerBlock<IReceivedMessage>>();
    }

    public virtual Task StartAsync() => StartAsync<ConsumerBlock<IReceivedMessage>>();

    protected async Task StartAsync<TConsumerBlock>() where TConsumerBlock : ConsumerBlock<IReceivedMessage>, new()
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

        await _rabbitService.ShutdownAsync(false);
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
        _compressionProvider = provider;
        return this;
    }

    public ConsumerDataflow<TState> SetEncryptionProvider(IEncryptionProvider provider)
    {
        Guard.AgainstNull(provider, nameof(provider));
        _encryptionProvider = provider;
        return this;
    }

    #region Step Adders

    protected static readonly string _defaultSpanNameFormat = "{0}.{1}";
    protected static readonly string _defaultStepSpanNameFormat = "{0}.{1}.{2}";

    protected string GetSpanName(string stepName)
    {
        return string.Format(_defaultSpanNameFormat, WorkflowName, stepName);
    }

    protected string GetStepSpanName(string stepName)
    {
        return string.Format(_defaultStepSpanNameFormat, WorkflowName, _suppliedTransforms.Count, stepName);
    }

    protected virtual ITargetBlock<TState> CreateTargetBlock(
        int boundedCapacity,
        TaskScheduler taskScheduler = null) =>
        new BufferBlock<TState>(
            new DataflowBlockOptions
            {
                BoundedCapacity = boundedCapacity > 0 ? boundedCapacity : 1000,
                TaskScheduler = taskScheduler ?? _taskScheduler
            });

    public ConsumerDataflow<TState> WithErrorHandling(
        Action<TState> action,
        int boundedCapacity,
        int? maxDoP = null,
        bool? ensureOrdered = null,
        TaskScheduler taskScheduler = null)
    {
        Guard.AgainstNull(action, nameof(action));
        if (_errorBuffer is null)
        {
            _errorBuffer = CreateTargetBlock(boundedCapacity, taskScheduler);
            var executionOptions = GetExecuteStepOptions(maxDoP, ensureOrdered, boundedCapacity, taskScheduler ?? _taskScheduler);
            _errorAction = GetLastWrappedActionBlock(action, executionOptions, GetSpanName("error_handler"));
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
        if (_errorBuffer is null)
        {
            _errorBuffer = CreateTargetBlock(boundedCapacity, taskScheduler);
            var executionOptions = GetExecuteStepOptions(maxDoP, ensureOrdered, boundedCapacity, taskScheduler ?? _taskScheduler);
            _errorAction = GetLastWrappedActionBlock(action, executionOptions, GetSpanName("error_handler"));
        }
        return this;
    }

    public ConsumerDataflow<TState> WithReadyToProcessBuffer(int boundedCapacity, TaskScheduler taskScheduler = null)
    {
        _readyBuffer ??= CreateTargetBlock(boundedCapacity, taskScheduler);
        return this;
    }

    public ConsumerDataflow<TState> AddStep(
        Func<TState, TState> suppliedStep,
        string stepName,
        int? maxDoP = null,
        bool? ensureOrdered = null,
        int? boundedCapacity = null,
        TaskScheduler taskScheduler = null)
    {
        Guard.AgainstNull(suppliedStep, nameof(suppliedStep));

        var executionOptions = GetExecuteStepOptions(maxDoP, ensureOrdered, boundedCapacity, taskScheduler ?? _taskScheduler);
        _suppliedTransforms.Add(GetWrappedTransformBlock(suppliedStep, executionOptions, GetStepSpanName(stepName)));
        return this;
    }

    public ConsumerDataflow<TState> AddStep(
        Func<TState, Task<TState>> suppliedStep,
        string stepName,
        int? maxDoP = null,
        bool? ensureOrdered = null,
        int? boundedCapacity = null,
        TaskScheduler taskScheduler = null)
    {
        Guard.AgainstNull(suppliedStep, nameof(suppliedStep));

        var executionOptions = GetExecuteStepOptions(maxDoP, ensureOrdered, boundedCapacity, taskScheduler ?? _taskScheduler);
        _suppliedTransforms.Add(GetWrappedTransformBlock(suppliedStep, executionOptions, GetStepSpanName(stepName)));
        return this;
    }

    public ConsumerDataflow<TState> WithPostProcessingBuffer(
        int boundedCapacity, TaskScheduler taskScheduler = null)
    {
        _postProcessingBuffer ??= CreateTargetBlock(boundedCapacity, taskScheduler);
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
        if (_finalization is null)
        {
            var executionOptions = GetExecuteStepOptions(maxDoP, ensureOrdered, boundedCapacity, taskScheduler ?? _taskScheduler);
            _finalization = GetLastWrappedActionBlock(action, executionOptions, GetSpanName("finalization"));
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
        if (_finalization is null)
        {
            var executionOptions = GetExecuteStepOptions(maxDoP, ensureOrdered, boundedCapacity, taskScheduler ?? _taskScheduler);
            _finalization = GetLastWrappedActionBlock(action, executionOptions, GetSpanName("finalization"));
        }
        return this;
    }

    public ConsumerDataflow<TState> WithBuildState(
        int? maxDoP = null,
        bool? ensureOrdered = null,
        int? boundedCapacity = null,
        TaskScheduler taskScheduler = null)
    {
        if (_buildStateBlock is null)
        {
            var executionOptions = GetExecuteStepOptions(maxDoP, ensureOrdered, boundedCapacity, taskScheduler ?? _taskScheduler);
            _buildStateBlock = GetBuildStateBlock(executionOptions);
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
        if (_decryptBlock is null)
        {
            var executionOptions = GetExecuteStepOptions(maxDoP, ensureOrdered, boundedCapacity, taskScheduler ?? _taskScheduler);

            _decryptBlock = GetByteManipulationTransformBlock(
                _encryptionProvider.Decrypt,
                executionOptions,
                false,
                x => x.ReceivedMessage.Encrypted,
                GetSpanName("decrypt"));
        }
        return this;
    }

    public ConsumerDataflow<TState> WithDecompressionStep(
        int? maxDoP = null,
        bool? ensureOrdered = null,
        int? boundedCapacity = null,
        TaskScheduler taskScheduler = null)
    {
        Guard.AgainstNull(_compressionProvider, nameof(_compressionProvider));
        if (_decompressBlock is null)
        {
            var executionOptions = GetExecuteStepOptions(maxDoP, ensureOrdered, boundedCapacity, taskScheduler ?? _taskScheduler);

            _decompressBlock = GetByteManipulationTransformBlock(
                _compressionProvider.Decompress,
                executionOptions,
                false,
                x => x.ReceivedMessage.Compressed,
                GetSpanName("decompress"));
        }

        return this;
    }

    public ConsumerDataflow<TState> WithCreateSendMessage(
        Func<TState, Task<TState>> createMessage,
        int? maxDoP = null,
        bool? ensureOrdered = null,
        int? boundedCapacity = null,
        TaskScheduler taskScheduler = null)
    {
        if (_createSendMessage is null)
        {
            var executionOptions = GetExecuteStepOptions(maxDoP, ensureOrdered, boundedCapacity, taskScheduler ?? _taskScheduler);
            _createSendMessage = GetWrappedTransformBlock(createMessage, executionOptions, GetSpanName("create_send_message"));
        }
        return this;
    }

    public ConsumerDataflow<TState> WithSendCompressedStep(
        int? maxDoP = null,
        bool? ensureOrdered = null,
        int? boundedCapacity = null,
        TaskScheduler taskScheduler = null)
    {
        Guard.AgainstNull(_compressionProvider, nameof(_compressionProvider));
        if (_compressBlock is null)
        {
            var executionOptions = GetExecuteStepOptions(maxDoP, ensureOrdered, boundedCapacity, taskScheduler ?? _taskScheduler);

            _compressBlock = GetByteManipulationTransformBlock(
                _compressionProvider.Compress,
                executionOptions,
                true,
                x => !x.ReceivedMessage.Compressed,
                GetSpanName("compress"));
        }
        return this;
    }

    public ConsumerDataflow<TState> WithSendEncryptedStep(
        int? maxDoP = null,
        bool? ensureOrdered = null,
        int? boundedCapacity = null,
        TaskScheduler taskScheduler = null)
    {
        Guard.AgainstNull(_encryptionProvider, nameof(_encryptionProvider));
        if (_encryptBlock is null)
        {
            var executionOptions = GetExecuteStepOptions(maxDoP, ensureOrdered, boundedCapacity, taskScheduler ?? _taskScheduler);

            _encryptBlock = GetByteManipulationTransformBlock(
                _encryptionProvider.Encrypt,
                executionOptions,
                true,
                x => !x.ReceivedMessage.Encrypted,
                GetSpanName("encrypt"));
        }
        return this;
    }

    public ConsumerDataflow<TState> WithSendStep(
        int? maxDoP = null,
        bool? ensureOrdered = null,
        int? boundedCapacity = null,
        TaskScheduler taskScheduler = null)
    {
        if (_sendMessageBlock is null)
        {
            var executionOptions = GetExecuteStepOptions(maxDoP, ensureOrdered, boundedCapacity, taskScheduler ?? _taskScheduler);
            _sendMessageBlock = GetWrappedPublishTransformBlock(_rabbitService, executionOptions);
        }
        return this;
    }

    #endregion

    #region Step Linking

    protected virtual void BuildLinkages<TConsumerBlock>(DataflowLinkOptions overrideOptions = null)
        where TConsumerBlock : ConsumerBlock<IReceivedMessage>, new()
    {
        Guard.AgainstNull(_buildStateBlock, nameof(_buildStateBlock)); // Create State Is Mandatory
        Guard.AgainstNull(_finalization, nameof(_finalization)); // Leaving The Workflow Is Mandatory
        Guard.AgainstNull(_errorAction, nameof(_errorAction)); // Processing Errors Is Mandatory

        _inputBuffer ??= new BufferBlock<IReceivedMessage>();
        _readyBuffer ??= new BufferBlock<TState>();
        _postProcessingBuffer ??= new BufferBlock<TState>();

        for (var i = 0; i < _consumerOptions.WorkflowConsumerCount; i++)
        {
            var consumerBlock = new TConsumerBlock
            {
                Consumer = new Consumer(_rabbitService.ChannelPool, _consumerOptions.ConsumerName)
            };
            _consumerBlocks.Add(consumerBlock);
            _consumerBlocks[i].LinkTo(_inputBuffer, overrideOptions ?? _linkStepOptions);
        }

        ((ISourceBlock<IReceivedMessage>)_inputBuffer).LinkTo(_buildStateBlock, overrideOptions ?? _linkStepOptions);
        _buildStateBlock.LinkTo(_errorBuffer, overrideOptions ?? _linkStepOptions, x => x is null);
        SetCurrentSourceBlock(_buildStateBlock);

        LinkPreProcessing(overrideOptions);
        LinkSuppliedSteps(overrideOptions);
        LinkPostProcessing(overrideOptions);

        ((ISourceBlock<TState>)_errorBuffer).LinkTo(_errorAction, overrideOptions ?? _linkStepOptions);
    }

    private void LinkPreProcessing(DataflowLinkOptions overrideOptions = null)
    {
        // Link Deserialize to DecryptBlock with predicate if its not null.
        if (_decryptBlock is not null)
        { LinkWithFaultRoute(_currentBlock, _decryptBlock, x => x.IsFaulted, overrideOptions ?? _linkStepOptions); }

        if (_decompressBlock is not null)
        { LinkWithFaultRoute(_currentBlock, _decompressBlock, x => x.IsFaulted, overrideOptions ?? _linkStepOptions); }

        _currentBlock.LinkTo(_readyBuffer, overrideOptions ?? _linkStepOptions, x => !x.IsFaulted);
        SetCurrentSourceBlock(_readyBuffer);
    }

    private void LinkSuppliedSteps(DataflowLinkOptions overrideOptions = null)
    {
        // Link all user steps.
        if (_suppliedTransforms?.Count > 0)
        {
            for (var i = 0; i < _suppliedTransforms.Count; i++)
            {
                if (i == 0)
                { LinkWithFaultRoute(_currentBlock, _suppliedTransforms[i], x => x.IsFaulted, overrideOptions ?? _linkStepOptions); }
                else // Link Previous Step, To Next Step
                { LinkWithFaultRoute(_suppliedTransforms[i - 1], _suppliedTransforms[i], x => x.IsFaulted, overrideOptions ?? _linkStepOptions); }
            }

            // Link the last user step to PostProcessingBuffer/CreateSendMessage.
            if (_createSendMessage is not null)
            {
                LinkWithFaultRoute(_suppliedTransforms[^1], _createSendMessage, x => x.IsFaulted, overrideOptions ?? _linkStepOptions);
                _createSendMessage.LinkTo(_postProcessingBuffer, overrideOptions ?? _linkStepOptions);
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
        if (_compressBlock is not null)
        { LinkWithFaultRoute(_currentBlock, _compressBlock, x => x.IsFaulted, overrideOptions); }

        if (_encryptBlock is not null)
        { LinkWithFaultRoute(_currentBlock, _encryptBlock, x => x.IsFaulted, overrideOptions); }

        if (_sendMessageBlock is not null)
        { LinkWithFaultRoute(_currentBlock, _sendMessageBlock, x => x.IsFaulted, overrideOptions); }

        _currentBlock.LinkTo(_finalization, overrideOptions ?? _linkStepOptions); // Last Action
    }

    private void LinkWithFaultRoute(
        ISourceBlock<TState> source,
        IPropagatorBlock<TState, TState> target,
        Predicate<TState> faultPredicate,
        DataflowLinkOptions overrideOptions = null)
    {
        source.LinkTo(target, overrideOptions ?? _linkStepOptions);
        target.LinkTo(_errorBuffer, overrideOptions ?? _linkStepOptions, faultPredicate); // Fault Linkage
        SetCurrentSourceBlock(target);
    }

    #endregion

    #region Step Wrappers

    public virtual TState BuildState(IReceivedMessage receivedMessage)
    {
        var state = new TState
        {
            ReceivedMessage = receivedMessage,
            Data = new Dictionary<string, object>()
        };

        var attributes = GetSpanAttributes(state, receivedMessage);

        state.StartWorkflowSpan(
            WorkflowName,
            spanKind: SpanKind.Internal,
            suppliedAttributes: attributes,
            parentSpanContext: receivedMessage.ParentSpanContext);

        return state;
    }

    protected virtual List<KeyValuePair<string, string>> GetSpanAttributes(TState state, IReceivedMessage receivedMessage)
    {
        var attributes = new List<KeyValuePair<string, string>>
        {
            KeyValuePair.Create(Constants.MessagingConsumerNameKey, _consumerOptions.ConsumerName),
            KeyValuePair.Create(Constants.MessagingOperationKey, Constants.MessagingOperationProcessValue)
        };

        if (state.ReceivedMessage?.Message?.MessageId is not null)
        {
            attributes.Add(KeyValuePair.Create(Constants.MessagingMessageMessageIdKey, state.ReceivedMessage.Message.MessageId));
        }
        if (state.ReceivedMessage?.Message?.Metadata?.PayloadId is not null)
        {
            attributes.Add(KeyValuePair.Create(Constants.MessagingMessagePayloadIdKey, state.ReceivedMessage.Message.Metadata.PayloadId));
        }
        if (state.ReceivedMessage?.DeliveryTag is not null)
        {
            attributes.Add(KeyValuePair.Create(Constants.MessagingMessageDeliveryTagIdKey, state.ReceivedMessage.DeliveryTag.ToString()));
        }

        return attributes;
    }

    public TransformBlock<IReceivedMessage, TState> GetBuildStateBlock(
        ExecutionDataflowBlockOptions options)
    {
        TState BuildStateWrap(IReceivedMessage data)
        {
            try
            { return BuildState(data); }
            catch
            { return null; }
        }

        return new TransformBlock<IReceivedMessage, TState>(BuildStateWrap, options);
    }

    public TransformBlock<TState, TState> GetByteManipulationTransformBlock(
        Func<ReadOnlyMemory<byte>, ReadOnlyMemory<byte>> action,
        ExecutionDataflowBlockOptions options,
        bool outbound,
        Predicate<TState> predicate,
        string spanName)
    {
        TState WrapAction(TState state)
        {
            using var childSpan = state.CreateActiveChildSpan(spanName, state.WorkflowSpan.Context, SpanKind.Internal);
            try
            {
                if (outbound)
                {
                    if (state.SendData.Length > 0)
                    { state.SendData = action(state.SendData); }
                    else if (state.SendMessage.Body.Length > 0)
                    { state.SendMessage.Body = action(state.SendMessage.Body); }
                }
                else if (predicate.Invoke(state))
                {
                    if (state.ReceivedMessage.ObjectType == Constants.HeaderValueForMessageObjectType)
                    {
                        if (state.ReceivedMessage.Message is null)
                        { state.ReceivedMessage.Message = _serializationProvider.Deserialize<Message>(state.ReceivedMessage.Body); }

                        state.ReceivedMessage.Message.Body = action(state.ReceivedMessage.Message.Body);
                    }
                    else
                    { state.ReceivedMessage.Body = action(state.ReceivedMessage.Body); }
                }
            }
            catch (Exception ex)
            {
                childSpan?.SetStatus(Status.Error);
                childSpan?.RecordException(ex);
                state.IsFaulted = true;
                state.EDI = ExceptionDispatchInfo.Capture(ex);
            }

            childSpan.End();
            return state;
        }

        return new TransformBlock<TState, TState>(WrapAction, options);
    }

    public TransformBlock<TState, TState> GetByteManipulationTransformBlock(
        Func<ReadOnlyMemory<byte>, Task<byte[]>> action,
        ExecutionDataflowBlockOptions options,
        bool outbound,
        Predicate<TState> predicate,
        string spanName)
    {
        async Task<TState> WrapActionAsync(TState state)
        {
            using var childSpan = state.CreateActiveChildSpan(spanName, state.WorkflowSpan.Context, SpanKind.Internal);
            try
            {

                if (outbound)
                {
                    if (state.SendData.Length > 0)
                    { state.SendData = await action(state.SendData).ConfigureAwait(false); }
                    else if (state.SendMessage.Body.Length > 0)
                    { state.SendMessage.Body = await action(state.SendMessage.Body).ConfigureAwait(false); }
                }
                else if (predicate.Invoke(state))
                {
                    if (state.ReceivedMessage.ObjectType == Constants.HeaderValueForMessageObjectType)
                    {
                        if (state.ReceivedMessage.Message is null)
                        { state.ReceivedMessage.Message = _serializationProvider.Deserialize<Message>(state.ReceivedMessage.Body); }

                        state.ReceivedMessage.Message.Body = await action(state.ReceivedMessage.Message.Body).ConfigureAwait(false);
                    }
                    else
                    { state.ReceivedMessage.Body = await action(state.ReceivedMessage.Body).ConfigureAwait(false); }
                }
            }
            catch (Exception ex)
            {
                childSpan?.SetStatus(Status.Error);
                childSpan?.RecordException(ex);
                state.IsFaulted = true;
                state.EDI = ExceptionDispatchInfo.Capture(ex);
            }

            childSpan.End();
            return state;
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
            using var childSpan = state.CreateActiveChildSpan(PublishStepIdentifier, state.WorkflowSpan.Context, SpanKind.Producer);
            try
            {
                await service.Publisher.PublishAsync(state.SendMessage, true, true).ConfigureAwait(false);
                state.SendMessageSent = true;
            }
            catch (Exception ex)
            {
                childSpan?.SetStatus(Status.Error);
                childSpan?.RecordException(ex);
                state.IsFaulted = true;
                state.EDI = ExceptionDispatchInfo.Capture(ex);
            }

            childSpan.End();
            return state;
        }

        return new TransformBlock<TState, TState>(WrapPublishAsync, options);
    }

    #endregion
}
