using System;
using System.Globalization;

namespace HouseofCat.RabbitMQ
{
    public static class Constants
    {
        // RabbitService 
        public const int EncryptionKeySize = 32; // AES256

        // Publisher
        public const string HeaderPrefix = "X-";

        // Consumer
        public const string HeaderForObjectType = "X-CR-OBJECTTYPE";
        public const string HeaderValueForMessage = "MESSAGE";
        public const string HeaderValueForLetter = "LETTER";
        public const string HeaderValueForUnknown = "UNKNOWN";
        public const string HeaderForEncrypted = "X-CR-ENCRYPTED";
        public const string HeaderForEncryption = "X-CR-ENCRYPTION";
        public const string HeaderForEncryptDate = "X-CR-ENCRYPTDATE";
        public const string HeaderForCompressed = "X-CR-COMPRESSED";
        public const string HeaderForCompression = "X-CR-COMPRESSION";

        public const string RangeErrorMessage = "Value for {0} must be between {1} and {2}.";

        // Pipeline
        public const string DefaultPipelineName = "NoNameProvided";
    }

    public static class ExceptionMessages
    {
        // AutoPublisher
        public readonly static string AutoPublisherNotStartedError = "AutoPublisher has not been started.";

        // General
        public readonly static string QueueChannelError = "Can't queue a message to a closed Threading.Channel.";

        public readonly static string ChannelReadErrorMessage = "Can't use reader on a closed Threading.Channel.";
        public readonly static string NoConsumerOptionsMessage = "Consumer {0} not found in Consumers dictionary.";
        public readonly static string NoConsumerPipelineOptionsMessage = "ConsumerPipeline {0} not found in ConsumerPipelineOptions dictionary.";

        public readonly static string ValidationMessage = "ConnectionPool is not initialized or is shutdown.";
        public readonly static string ShutdownValidationMessage = "ConnectionPool is not initialized. Can't be Shutdown.";
        public readonly static string GetConnectionErrorMessage = "Threading.Channel used for reading RabbitMQ connections has been closed.";

        public readonly static string ChannelPoolNotInitializedMessage = "ChannelPool is not usable until it has been initialized.";
        public readonly static string EncrypConfigErrorMessage = "Encryption can't be enabled without a HashKey (32-byte length).";

        // ChannelPool
        public readonly static string ChannelPoolValidationMessage = "ChannelPool is not initialized or is shutdown.";
        public readonly static string ChannelPoolShutdownValidationMessage = "ChannelPool is not initialized. Can't be Shutdown.";
        public readonly static string ChannelPoolGetChannelError = "Threading.Channel used for reading RabbitMQ channels has been closed.";

        // Pipeline Messages
        public readonly static string NotFinalized = "Pipeline is not ready for receiving work as it has not been finalized yet.";
        public readonly static string AlreadyFinalized = "Pipeline is already finalized and ready for use.";
        public readonly static string CantFinalize = "Pipeline can't finalize as no steps have been added.";
        public readonly static string InvalidAddError = "Pipeline is already finalized and you can no longer add steps.";

        public readonly static string ChainingImpossible = "Pipelines can't be chained together as one, or both, pipelines have been finalized.";
        public readonly static string ChainingNotMatched = "Pipelines can't be chained together as the last step function and the first step function don't align with type input or asynchronicity.";
        public readonly static string NothingToChain = "Pipelines can't be chained together as one, or both, pipelines have no steps.";

        public readonly static string InvalidStepFound = "Pipeline can't chain the last step to this new step. Unexpected type found on the previous step.";
    }

    public static class LogMessages
    {
        public static class ChannelHosts
        {
            public readonly static string FlowControlled = "Flow control detected on ChannelHost {0}";
            public readonly static string FlowControlFinished = "Flow control is finished on ChannelHost {0}";
        }

        public static class ConnectionPools
        {
            public readonly static string CreateConnections = "ConnectionPool creating Connections...";
            public readonly static string CreateConnectionsComplete = "ConnectionPool initialized.";
            public readonly static string CreateConnectionException = "Connection () failed to be created.";
            public readonly static string Shutdown = "ConnectionPool shutdown was called.";
            public readonly static string ShutdownComplete = "ConnectionPool shutdown complete.";
        }

        public static class ChannelPools
        {
            public readonly static string Initialization = "ChannelPool initialize call was made.";
            public readonly static string InitializationComplete = "ChannelPool initialized.";
            public readonly static string DeadChannel = "A dead channel ({0}) was detected... attempting to repair indefinitely.";
            public readonly static string CreateChannel = "The channel host ({0}) create loop is executing an iteration...";
            public readonly static string CreateChannelFailedConnection = "The channel host ({0}) failed because Connection is unhealthy.";
            public readonly static string CreateChannelFailedConstruction = "The channel host ({0}) failed because ChannelHost construction threw exception.";
            public readonly static string CreateChannelSleep = "The channel host ({0}) create loop iteration failed. Sleeping...";
            public readonly static string CreateChannelSuccess = "The channel host ({0}) create loop finished. Channel restored and flags removed.";
            public readonly static string ReturningChannel = "The channel host ({0}) was returned to the pool. Flagged? {1}";
            public readonly static string Shutdown = "ChannelPool shutdown was called.";
            public readonly static string ShutdownComplete = "ChannelPool shutdown complete.";
        }

        public static class Publishers
        {
            public readonly static string PublishFailed = "Publish to route ({0}) failed, flagging channel host. Error: {1}";
            public readonly static string PublishMessageFailed = "Publish to route ({0}) failed [MessageId: {1}] flagging channel host. Error: {2}";
            public readonly static string PublishBatchFailed = "Batch publish failed, flagging channel host. Error: {0}";
        }

        public static class AutoPublishers
        {
            public readonly static string MessageQueued = "AutoPublisher queued message [MessageId:{0} InternalId:{1}].";
            public readonly static string MessagePublished = "AutoPublisher published message [MessageId:{0} InternalId:{1}]. Listen for receipt to indicate success...";
        }

        public static class Consumers
        {
            public readonly static string StartingConsumer = "Consumer ({0}) starting...";
            public readonly static string StartedConsumer = "Consumer ({0}) started.";
            public readonly static string StartingConsumerLoop = "Consumer ({0}) startup loop executing...";
            public readonly static string Started = "Consumer ({0}) started.";
            public readonly static string StopConsumer = "Consumer ({0}) stop consuming called...";
            public readonly static string StoppedConsumer = "Consumer ({0}) stopped consuming.";
            public readonly static string GettingTransientChannelHost = "Consumer ({0}) getting a transient channel.";
            public readonly static string GettingChannelHost = "Consumer ({0}) getting a channel.";
            public readonly static string GettingAckChannelHost = "Consumer ({0}) getting a ackable channel host.";
            public readonly static string ChannelEstablished = "Consumer ({0}) channel host ({1}) assigned.";
            public readonly static string ChannelNotEstablished = "Consumer ({0}) channel host could not be assigned.";
            public readonly static string ConsumerMessageReceived = "Consumer ({0}) message received (DT:{1}). Adding to buffer...";
            public readonly static string ConsumerAsyncMessageReceived = "Consumer ({0}) async message received (DT:{1}). Adding to buffer...";
            public readonly static string ConsumerShutdownEvent = "Consumer ({0}) shutdown event has occurred. Reason: {1}. Attempting to restart consuming...";

            public readonly static string ConsumerDataflowActionCancelled = "Consumer ({0}) dataflow engine actions were cancelled.";
            public readonly static string ConsumerDataflowError = "Consumer ({0}) dataflow engine encountered an error. Error: {1}";
            public readonly static string ConsumerDataflowQueueing = "Consumer ({0}) dataflow engine queueing unit of work (ReceivedData:DT:{1}).";
        }
    }
}
