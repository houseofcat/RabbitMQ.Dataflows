using HouseofCat.Compression;
using HouseofCat.Dataflows;
using HouseofCat.Dataflows.Extensions;
using HouseofCat.Encryption;
using HouseofCat.RabbitMQ.Services;
using HouseofCat.Serialization;
using HouseofCat.Utilities.Errors;
using HouseofCat.Utilities.Helpers;
using OpenTelemetry.Trace;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using static HouseofCat.Dataflows.Extensions.WorkStateExtensions;

namespace HouseofCat.RabbitMQ.Dataflows;

public class ConsumerDataflow<TState> : BaseDataflow<TState> where TState : class, IRabbitWorkState, new()
{
    public string WorkflowName { get; }

    private readonly IRabbitService _rabbitService;
    private readonly ICollection<IConsumer<ReceivedMessage>> _consumers;
    private readonly ConsumerOptions _consumerOptions;
    private readonly TaskScheduler _taskScheduler;
    private readonly string _consumerName;
    private readonly int _consumerCount;

    // Main Flow - Ingestion
    private readonly List<ConsumerBlock<ReceivedMessage>> _consumerBlocks;
    protected ITargetBlock<ReceivedMessage> _inputBuffer;
    private TransformBlock<ReceivedMessage, TState> _buildStateBlock;
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

        _consumerBlocks = new List<ConsumerBlock<ReceivedMessage>>();
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
        ICollection<IConsumer<ReceivedMessage>> consumers,
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
        ICollection<IConsumer<ReceivedMessage>> consumers,
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

        _consumerBlocks = new List<ConsumerBlock<ReceivedMessage>>();
    }

    public virtual Task StartAsync() => StartAsync<ConsumerBlock<ReceivedMessage>>();

    protected async Task StartAsync<TConsumerBlock>() where TConsumerBlock : ConsumerBlock<ReceivedMessage>, new()
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

    #region Step Adders

    protected virtual ITargetBlock<TState> CreateTargetBlock(
        int boundedCapacity, TaskScheduler taskScheduler = null) =>
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
        if (_errorBuffer == null)
        {
            _errorBuffer = CreateTargetBlock(boundedCapacity, taskScheduler);
            var executionOptions = GetExecuteStepOptions(maxDoP, ensureOrdered, boundedCapacity, taskScheduler ?? _taskScheduler);
            _errorAction = GetLastWrappedActionBlock(action, executionOptions, $"{WorkflowName}.ErrorHandler");
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
            _errorBuffer = CreateTargetBlock(boundedCapacity, taskScheduler);
            var executionOptions = GetExecuteStepOptions(maxDoP, ensureOrdered, boundedCapacity, taskScheduler ?? _taskScheduler);
            _errorAction = GetLastWrappedActionBlock(action, executionOptions, $"{WorkflowName}.ErrorHandler");
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
        string spanName,
        int? maxDoP = null,
        bool? ensureOrdered = null,
        int? boundedCapacity = null,
        TaskScheduler taskScheduler = null)
    {
        Guard.AgainstNull(suppliedStep, nameof(suppliedStep));

        var executionOptions = GetExecuteStepOptions(maxDoP, ensureOrdered, boundedCapacity, taskScheduler ?? _taskScheduler);
        _suppliedTransforms.Add(GetWrappedTransformBlock(suppliedStep, executionOptions, spanName));
        return this;
    }

    public ConsumerDataflow<TState> AddStep(
        Func<TState, Task<TState>> suppliedStep,
        string spanName,
        int? maxDoP = null,
        bool? ensureOrdered = null,
        int? boundedCapacity = null,
        TaskScheduler taskScheduler = null)
    {
        Guard.AgainstNull(suppliedStep, nameof(suppliedStep));

        var executionOptions = GetExecuteStepOptions(maxDoP, ensureOrdered, boundedCapacity, taskScheduler ?? _taskScheduler);
        _suppliedTransforms.Add(GetWrappedTransformBlock(suppliedStep, executionOptions, spanName));
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
        if (_finalization == null)
        {
            var executionOptions = GetExecuteStepOptions(maxDoP, ensureOrdered, boundedCapacity, taskScheduler ?? _taskScheduler);
            _finalization = GetLastWrappedActionBlock(action, executionOptions, $"{WorkflowName}.Finalization");
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
            var executionOptions = GetExecuteStepOptions(maxDoP, ensureOrdered, boundedCapacity, taskScheduler ?? _taskScheduler);
            _finalization = GetLastWrappedActionBlock(action, executionOptions, $"{WorkflowName}.Finalization");
        }
        return this;
    }

    public ConsumerDataflow<TState> WithBuildState(
        int? maxDoP = null,
        bool? ensureOrdered = null,
        int? boundedCapacity = null,
        TaskScheduler taskScheduler = null)
    {
        if (_buildStateBlock == null)
        {
            var executionOptions = GetExecuteStepOptions(maxDoP, ensureOrdered, boundedCapacity, taskScheduler ?? _taskScheduler);
            _buildStateBlock = GetBuildStateBlock(executionOptions);
        }
        return this;
    }

    public ConsumerDataflow<TState> WithBuildStateAndPayload<TOut>(
        string stateKey,
        int? maxDoP = null,
        bool? ensureOrdered = null,
        int? boundedCapacity = null,
        TaskScheduler taskScheduler = null)
    {
        Guard.AgainstNullOrEmpty(stateKey, nameof(stateKey));

        if (_buildStateBlock == null)
        {
            var executionOptions = GetExecuteStepOptions(maxDoP, ensureOrdered, boundedCapacity, taskScheduler ?? _taskScheduler);
            _buildStateBlock = GetBuildStateWithPayloadBlock<TOut>(stateKey, executionOptions);
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
            var executionOptions = GetExecuteStepOptions(maxDoP, ensureOrdered, boundedCapacity, taskScheduler ?? _taskScheduler);

            _decryptBlock = GetByteManipulationTransformBlock(
                _encryptionProvider.Decrypt,
                executionOptions,
                false,
                x => x.ReceivedMessage.Encrypted,
                $"{WorkflowName}_Decrypt");
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
            var executionOptions = GetExecuteStepOptions(maxDoP, ensureOrdered, boundedCapacity, taskScheduler ?? _taskScheduler);

            _decompressBlock = GetByteManipulationTransformBlock(
                _compressProvider.Decompress,
                executionOptions,
                false,
                x => x.ReceivedMessage.Compressed,
                $"{WorkflowName}_Decompress");
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
        if (_createSendMessage == null)
        {
            var executionOptions = GetExecuteStepOptions(maxDoP, ensureOrdered, boundedCapacity, taskScheduler ?? _taskScheduler);
            _createSendMessage = GetWrappedTransformBlock(createMessage, executionOptions, $"{WorkflowName}_CreateSendMessage");
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
            var executionOptions = GetExecuteStepOptions(maxDoP, ensureOrdered, boundedCapacity, taskScheduler ?? _taskScheduler);

            _compressBlock = GetByteManipulationTransformBlock(
                _compressProvider.Compress,
                executionOptions,
                true,
                x => !x.ReceivedMessage.Compressed,
                $"{WorkflowName}_Compress");
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
            var executionOptions = GetExecuteStepOptions(maxDoP, ensureOrdered, boundedCapacity, taskScheduler ?? _taskScheduler);

            _encryptBlock = GetByteManipulationTransformBlock(
                _encryptionProvider.Encrypt,
                executionOptions,
                true,
                x => !x.ReceivedMessage.Encrypted,
                $"{WorkflowName}_Encrypt");
        }
        return this;
    }

    public ConsumerDataflow<TState> WithSendStep(
        int? maxDoP = null,
        bool? ensureOrdered = null,
        int? boundedCapacity = null,
        TaskScheduler taskScheduler = null)
    {
        if (_sendMessageBlock == null)
        {
            var executionOptions = GetExecuteStepOptions(maxDoP, ensureOrdered, boundedCapacity, taskScheduler ?? _taskScheduler);
            _sendMessageBlock = GetWrappedPublishTransformBlock(_rabbitService, executionOptions);
        }
        return this;
    }

    #endregion

    #region Step Linking

    protected virtual void BuildLinkages<TConsumerBlock>(DataflowLinkOptions overrideOptions = null)
        where TConsumerBlock : ConsumerBlock<ReceivedMessage>, new()
    {
        Guard.AgainstNull(_buildStateBlock, nameof(_buildStateBlock)); // Create State Is Mandatory
        Guard.AgainstNull(_finalization, nameof(_finalization)); // Leaving The Workflow Is Mandatory
        Guard.AgainstNull(_errorAction, nameof(_errorAction)); // Processing Errors Is Mandatory

        _inputBuffer ??= new BufferBlock<ReceivedMessage>();

        _readyBuffer ??= new BufferBlock<TState>();

        _postProcessingBuffer ??= new BufferBlock<TState>();

        if (_consumers == null)
        {
            for (var i = 0; i < _consumerCount; i++)
            {
                var consumerBlock = new TConsumerBlock
                {
                    Consumer = new Consumer(_rabbitService.ChannelPool, _consumerName)
                };
                _consumerBlocks.Add(consumerBlock);
                _consumerBlocks[i].LinkTo(_inputBuffer, overrideOptions ?? _linkStepOptions);
            }
        }
        else
        {
            for (var i = 0; i < _consumers.Count; i++)
            {
                var consumerBlock = new TConsumerBlock
                {
                    Consumer = _consumers.ElementAt(i)
                };

                _consumerBlocks.Add(consumerBlock);
                _consumerBlocks[i].LinkTo(_inputBuffer, overrideOptions ?? _linkStepOptions);
            }
        }

        ((ISourceBlock<ReceivedMessage>)_inputBuffer).LinkTo(_buildStateBlock, overrideOptions ?? _linkStepOptions);
        _buildStateBlock.LinkTo(_errorBuffer, overrideOptions ?? _linkStepOptions, x => x == null);
        SetCurrentSourceBlock(_buildStateBlock);

        LinkPreProcessing(overrideOptions);
        LinkSuppliedSteps(overrideOptions);
        LinkPostProcessing(overrideOptions);

        ((ISourceBlock<TState>)_errorBuffer).LinkTo(_errorAction, overrideOptions ?? _linkStepOptions);
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
            for (var i = 0; i < _suppliedTransforms.Count; i++)
            {
                if (i == 0)
                { LinkWithFaultRoute(_currentBlock, _suppliedTransforms[i], x => x.IsFaulted, overrideOptions ?? _linkStepOptions); }
                else // Link Previous Step, To Next Step
                { LinkWithFaultRoute(_suppliedTransforms[i - 1], _suppliedTransforms[i], x => x.IsFaulted, overrideOptions ?? _linkStepOptions); }
            }

            // Link the last user step to PostProcessingBuffer/CreateSendMessage.
            if (_createSendMessage != null)
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
        if (_compressBlock != null)
        { LinkWithFaultRoute(_currentBlock, _compressBlock, x => x.IsFaulted, overrideOptions); }

        if (_encryptBlock != null)
        { LinkWithFaultRoute(_currentBlock, _encryptBlock, x => x.IsFaulted, overrideOptions); }

        if (_sendMessageBlock != null)
        { LinkWithFaultRoute(_currentBlock, _sendMessageBlock, x => x.IsFaulted, overrideOptions); }

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

    public virtual TState BuildState(ReceivedMessage data)
    {
        var state = new TState
        {
            ReceivedMessage = data,
            Data = new Dictionary<string, object>()
        };

        var attributes = new List<KeyValuePair<string, string>>()
        {
            KeyValuePair.Create(nameof(_consumerOptions.ConsumerName), _consumerOptions.ConsumerName)
        };

        if (state.ReceivedMessage?.Message?.MessageId is not null)
        {
            attributes.Add(KeyValuePair.Create(nameof(state.ReceivedMessage.Message.MessageId), state.ReceivedMessage.Message.MessageId));
        }
        if (state.ReceivedMessage?.Message?.Metadata?.PayloadId is not null)
        {
            attributes.Add(KeyValuePair.Create(nameof(state.ReceivedMessage.Message.Metadata.PayloadId), state.ReceivedMessage.Message.Metadata.PayloadId));
        }

        state.StartWorkflowSpan(
            WorkflowName,
            spanKind: SpanKind.Consumer,
            suppliedAttributes: attributes,
            traceHeader: data.TraceParentHeader);

        return state;
    }

    public virtual TState BuildStateAndPayload<TOut>(string key, ReceivedMessage data)
    {
        var state = new TState
        {
            ReceivedMessage = data,
            Data = new Dictionary<string, object>()
        };

        var attributes = new List<KeyValuePair<string, string>>()
        {
            KeyValuePair.Create(nameof(_consumerOptions.ConsumerName), _consumerOptions.ConsumerName)
        };

        if (state.ReceivedMessage?.Message?.MessageId is not null)
        {
            attributes.Add(KeyValuePair.Create(nameof(state.ReceivedMessage.Message.MessageId), state.ReceivedMessage.Message.MessageId));
        }
        if (state.ReceivedMessage?.Message?.Metadata?.PayloadId is not null)
        {
            attributes.Add(KeyValuePair.Create(nameof(state.ReceivedMessage.Message.Metadata.PayloadId), state.ReceivedMessage.Message.Metadata.PayloadId));
        }

        state.StartWorkflowSpan(
            WorkflowName,
            spanKind: SpanKind.Consumer,
            suppliedAttributes: attributes,
            traceHeader: data.TraceParentHeader);

        if (_serializationProvider != null
            && data.ObjectType != Constants.HeaderValueForUnknownObjectType)
        {
            try
            { state.Data[key] = _serializationProvider.Deserialize<TOut>(data.Data); }
            catch { }
        }

        return state;
    }

    public TransformBlock<ReceivedMessage, TState> GetBuildStateWithPayloadBlock<TOut>(
        string key,
        ExecutionDataflowBlockOptions options)
    {
        TState BuildStateWrap(ReceivedMessage data)
        {
            try
            { return BuildStateAndPayload<TOut>(key, data); }
            catch
            { return null; }
        }

        return new TransformBlock<ReceivedMessage, TState>(BuildStateWrap, options);
    }

    public TransformBlock<ReceivedMessage, TState> GetBuildStateBlock(
        ExecutionDataflowBlockOptions options)
    {
        TState BuildStateWrap(ReceivedMessage data)
        {
            try
            { return BuildState(data); }
            catch
            { return null; }
        }

        return new TransformBlock<ReceivedMessage, TState>(BuildStateWrap, options);
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
            using var childSpan = state.CreateActiveChildSpan(spanName, state.WorkflowSpan.Context, SpanKind.Consumer);
            try
            {
                if (outbound)
                {
                    if (state.SendData?.Length > 0)
                    { state.SendData = action(state.SendData.AsMemory()).ToArray(); }
                    else if (state.SendMessage.Body.Length > 0)
                    { state.SendMessage.Body = action(state.SendMessage.Body).ToArray(); }
                }
                else if (predicate.Invoke(state))
                {
                    if (state.ReceivedMessage.ObjectType == Constants.HeaderValueForMessageObjectType)
                    {
                        if (state.ReceivedMessage.Message == null)
                        { state.ReceivedMessage.Message = _serializationProvider.Deserialize<Message>(state.ReceivedMessage.Data); }

                        state.ReceivedMessage.Message.Body = action(state.ReceivedMessage.Message.Body).ToArray();
                    }
                    else
                    { state.ReceivedMessage.Data = action(state.ReceivedMessage.Data).ToArray(); }
                }

                return state;
            }
            catch (Exception ex)
            {
                childSpan?.SetStatus(Status.Error);
                childSpan?.RecordException(ex);
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
        string spanName)
    {
        async Task<TState> WrapActionAsync(TState state)
        {
            using var childSpan = state.CreateActiveChildSpan(spanName, state.WorkflowSpan.Context, SpanKind.Consumer);
            try
            {

                if (outbound)
                {
                    if (state.SendData?.Length > 0)
                    { state.SendData = await action(state.SendData).ConfigureAwait(false); }
                    else if (state.SendMessage.Body.Length > 0)
                    { state.SendMessage.Body = await action(state.SendMessage.Body).ConfigureAwait(false); }
                }
                else if (predicate.Invoke(state))
                {
                    if (state.ReceivedMessage.ObjectType == Constants.HeaderValueForMessageObjectType)
                    {
                        if (state.ReceivedMessage.Message == null)
                        { state.ReceivedMessage.Message = _serializationProvider.Deserialize<Message>(state.ReceivedMessage.Data); }

                        state.ReceivedMessage.Message.Body = await action(state.ReceivedMessage.Message.Body).ConfigureAwait(false);
                    }
                    else
                    { state.ReceivedMessage.Data = await action(state.ReceivedMessage.Data).ConfigureAwait(false); }
                }
                return state;
            }
            catch (Exception ex)
            {
                childSpan?.SetStatus(Status.Error);
                childSpan?.RecordException(ex);
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
            using var childSpan = state.CreateActiveChildSpan(PublishStepIdentifier, state.WorkflowSpan.Context, SpanKind.Producer);
            try
            {
                await service.Publisher.PublishAsync(state.SendMessage, true, true).ConfigureAwait(false);
                state.SendMessageSent = true;

                return state;
            }
            catch (Exception ex)
            {
                childSpan?.SetStatus(Status.Error);
                childSpan?.RecordException(ex);
                state.IsFaulted = true;
                state.EDI = ExceptionDispatchInfo.Capture(ex);
                return state;
            }
        }

        return new TransformBlock<TState, TState>(WrapPublishAsync, options);
    }

    #endregion
}
