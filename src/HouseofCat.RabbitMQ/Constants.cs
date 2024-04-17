namespace HouseofCat.RabbitMQ;

public static class Constants
{
    // Publisher
    public static string HeaderPrefix { get; set; } = "X-";

    // Consumer
    public static string HeaderForContentType { get; set; } = "ContentType";
    public const string HeaderValueForContentTypeBinary = "application/octet-stream";
    public const string HeaderValueForContentTypePlainText = "text/plain";
    public const string HeaderValueForContentTypeJson = "application/json";
    public const string HeaderValueForContentTypeMessagePack = "application/msgpack";
    public static string HeaderForObjectType { get; set; } = "X-RD-OBJECTTYPE";
    public static string HeaderValueForMessageObjectType { get; set; } = "IMESSAGE";

    public static string HeaderValueForUnknownObjectType { get; set; } = "UNK";
    public static string HeaderForEncrypted { get; set; } = "X-RD-ENCRYPTED";
    public static string HeaderForEncryption { get; set; } = "X-RD-ENCRYPTION";
    public static string HeaderForEncryptDate { get; set; } = "X-RD-ENCRYPTDATE";
    public static string HeaderForCompressed { get; set; } = "X-RD-COMPRESSED";
    public static string HeaderForCompression { get; set; } = "X-RD-COMPRESSION";

    public static string HeaderForTraceParent { get; set; } = "traceparent";

    public const string RangeErrorMessage = "Value for {0} must be between {1} and {2}.";

    public static string MessagingSystemKey { get; set; } = "messaging.system";
    public static string MessagingSystemValue { get; set; } = "rabbitmq";

    public static string MessagingOperationKey { get; set; } = "messaging.operation";
    public static string MessagingOperationPublishValue { get; set; } = "publish";
    public static string MessagingOperationConsumeValue { get; set; } = "receive";
    public static string MessagingOperationProcessValue { get; set; } = "process";

    public static string MessagingDestinationNameKey { get; set; } = "messaging.destination.name";
    public static string MessagingConsumerNameKey { get; set; } = "messaging.rabbitmq.consumer.name";
    public static string MessagingMessageMessageIdKey { get; set; } = "messaging.message.id";

    public static string MessagingMessageBodySizeKey { get; set; } = "messaging.rabbitmq.message.body.size";
    public static string MessagingMessageEnvelopeSizeKey { get; set; } = "messaging.rabbitmq.message.envelope.size";

    public static string MessagingMessageRoutingKeyKey { get; set; } = "messaging.rabbitmq.message.routing_key";
    public static string MessagingMessageDeliveryTagIdKey { get; set; } = "messaging.rabbitmq.message.delivery_tag";

    public static string MessagingMessageDeliveryModeKey { get; set; } = "messaging.rabbitmq.message.delivery_mode";
    public static string MessagingMessagePriorityKey { get; set; } = "messaging.rabbitmq.message.priority";
    public static string MessagingMessageContentTypeKey { get; set; } = "messaging.rabbitmq.message.content_type";
    public static string MessagingMessageMandatoryKey { get; set; } = "messaging.rabbitmq.message.mandatory";

    public static string MessagingBatchProcessValue { get; set; } = "messaging.batch.message_count";

    public static string MessagingMessagePayloadIdKey { get; set; } = "messaging.rabbitmq.message.payload_id";
    public static string MessagingMessageEncryptedKey { get; set; } = "messaging.rabbitmq.message.encrypted";
    public static string MessagingMessageEncryptedDateKey { get; set; } = "messaging.rabbitmq.message.encrypted_date";
    public static string MessagingMessageEncryptionKey { get; set; } = "messaging.rabbitmq.message.encryption";
    public static string MessagingMessageCompressedKey { get; set; } = "messaging.rabbitmq.message.compressed";
    public static string MessagingMessageCompressionKey { get; set; } = "messaging.rabbitmq.message.compression";
}

public static class ExceptionMessages
{
    // AutoPublisher
    public const string AutoPublisherNotStartedError = "AutoPublisher has not been started.";

    // General
    public const string QueueChannelError = "Can't queue a message to a closed Threading.Channel.";

    public const string ChannelReadErrorMessage = "Can't use reader on a closed Threading.Channel.";
    public const string NoConsumerOptionsMessage = "Consumer {0} not found in Consumers dictionary.";
    public const string NoConsumerPipelineOptionsMessage = "ConsumerPipeline {0} not found in ConsumerPipelineOptions dictionary.";

    public const string ValidationMessage = "ConnectionPool is not initialized or is shutdown.";
    public const string ShutdownValidationMessage = "ConnectionPool is not initialized. Can't be Shutdown.";
    public const string GetConnectionErrorMessage = "Threading.Channel buffer used for reading RabbitMQ connections has been closed.";

    public const string ChannelPoolNotInitializedMessage = "ChannelPool is not usable until it has been initialized.";
    public const string EncrypConfigErrorMessage = "Encryption can't be enabled without a HashKey (32-byte length).";

    // ChannelPool
    public const string ChannelPoolValidationMessage = "ChannelPool is not initialized or is shutdown.";
    public const string ChannelPoolShutdownValidationMessage = "ChannelPool is not initialized. Can't be Shutdown.";
    public const string ChannelPoolBadOptionMaxChannelError = "Check your PoolOptions, MaxChannels was less than 1.";
    public const string ChannelPoolBadOptionMaxAckChannelError = "Check your PoolOptions, MaxAckableChannels was less than 1.";
    public const string ChannelPoolGetChannelError = "Threading.Channel used for reading RabbitMQ channels has been closed.";

    // Pipeline Messages
    public const string NotFinalized = "Pipeline is not ready for receiving work as it has not been finalized yet.";
    public const string AlreadyFinalized = "Pipeline is already finalized and ready for use.";
    public const string CantFinalize = "Pipeline can't finalize as no steps have been added.";
    public const string InvalidAddError = "Pipeline is already finalized and you can no longer add steps.";

    public const string ChainingImpossible = "Pipelines can't be chained together as one, or both, pipelines have been finalized.";
    public const string ChainingNotMatched = "Pipelines can't be chained together as the last step function and the first step function don't align with type input or asynchronicity.";
    public const string NothingToChain = "Pipelines can't be chained together as one, or both, pipelines have no steps.";

    public const string InvalidStepFound = "Pipeline can't chain the last step to this new step. Unexpected type found on the previous step.";
}

public static class LogMessages
{
    public static class ChannelHosts
    {
        public const string FlowControlled = "ChannelHost [Id: {0}] - Flow control event has triggered.";
        public const string FlowControlFinished = "ChannelHost [Id: {0}] - Flow control event has resolved itself.";
        public const string ConsumerStartedConsumer = "ChannelHost [Id: {0}] - Starting consuming. ConsumerTag: [{1}]";
        public const string ConsumerStopConsumer = "ChannelHost [Id: {0}] - Stopping consuming using ConsumerTag: [{1}]";
        public const string ConsumerStopConsumerError = "ChannelHost [Id: {0}] - Error stopping consuming using ConsumerTag: [{1}]";
    }

    public static class ConnectionPools
    {
        public const string CreateConnections = "ConnectionPool creating Connections...";
        public const string CreateConnectionsComplete = "ConnectionPool initialized.";
        public const string CreateConnectionException = "Connection [{0}] failed to be created.";
        public const string Shutdown = "ConnectionPool shutdown was called.";
        public const string ShutdownComplete = "ConnectionPool shutdown complete.";
    }

    public static class ChannelPools
    {
        public const string Initialization = "ChannelPool initialize call was made.";
        public const string InitializationComplete = "ChannelPool initialized.";
        public const string ChannelHasIssues = "ChannelHost [Id: {0}] was detected to have issues. Attempting to repair...";
        public const string CreateChannel = "ChannelHost [Id: {0}] create loop is executing an iteration...";
        public const string CreateChannelFailedConnection = "The ChannelHost [Id: {0}] failed because Connection is unhealthy.";
        public const string CreateChannelFailedConstruction = "The ChannelHost [Id: {0}] failed because ChannelHost construction threw exception.";
        public const string CreateChannelSleep = "The ChannelHost [Id: {0}] create loop iteration failed. Sleeping...";
        public const string CreateChannelSuccess = "The ChannelHost [Id: {0}] create loop finished. Channel restored and flags removed.";
        public const string ReturningChannel = "The ChannelHost [Id: {0}] was returned to the pool. Flagged? {1}";
        public const string Shutdown = "ChannelPool shutdown was called.";
        public const string ShutdownComplete = "ChannelPool shutdown complete.";
    }

    public static class Publishers
    {
        public const string PublishFailed = "Publish to route [{0}] failed, flagging channel host. Error: {1}";
        public const string PublishMessageFailed = "Publish to route [{0}] failed [MessageId: {1}] flagging channel host. Error: {2}";
        public const string PublishBatchFailed = "Batch publish failed, flagging channel host. Error: {0}";
    }

    public static class AutoPublishers
    {
        public const string MessageQueued = "AutoPublisher queued message [MessageId:{0} InternalId:{1}].";
        public const string MessagePublished = "AutoPublisher published message [MessageId:{0} InternalId:{1}]. Listen for receipt to indicate success...";
    }

    public static class Consumers
    {
        public const string StartingConsumer = "Consumer [{0}] starting...";
        public const string StartingConsumerError = "Exception creating internal RabbitMQ consumer. Retrying...";
        public const string StartedConsumer = "Consumer [{0}] started.";
        public const string StartingConsumerLoop = "Consumer [{0}] startup loop executing...";
        public const string Started = "Consumer [{0}] started.";
        public const string StopConsumer = "Consumer [{0}] stop consuming called...";
        public const string StoppedConsumer = "Consumer [{0}] stopped consuming.";
        public const string GettingTransientChannelHost = "Consumer [{0}] getting a transient channel.";
        public const string GettingChannelHost = "Consumer [{0}] getting a channel.";
        public const string GettingAckChannelHost = "Consumer [{0}] getting a ackable channel host.";
        public const string ChannelEstablished = "Consumer [{0}] channel host [{1}] assigned.";
        public const string ChannelNotEstablished = "Consumer [{0}] channel host could not be assigned.";
        public const string ConsumerMessageReceived = "Consumer [{0}] message received (DT:{1}]. Adding to buffer...";
        public const string ConsumerAsyncMessageReceived = "Consumer [{0}] async message received (DT:{1}]. Adding to buffer...";
        public const string ConsumerShutdownEvent = "Consumer [{0}] recoverable shutdown event has occurred on Channel [Id: {1}]. Reason: {2}. Attempting to restart consuming...";
        public const string ConsumerShutdownEventFinished = "Consumer [{0}] shutdown event has finished on Channel [Id: {1}].";
        public const string ConsumerChannelReplacedEvent = "Consumer [{0}] recoverable shutdown event is ongoing. Connection is healthy. Channel appears to be dead and replacing it...";
        public const string ConsumerMessageWriteToBufferError = "Consumer [{0}] was unable to write to channel buffer. Error: {1}";

        public const string ConsumerDataflowActionCancelled = "Consumer [{0}] dataflow engine actions were cancelled.";
        public const string ConsumerDataflowError = "Consumer [{0}] dataflow engine encountered an error. Error: {1}";
        public const string ConsumerDataflowQueueing = "Consumer [{0}] dataflow engine queueing unit of work [receivedMessage:DT:{1}].";
    }
}
