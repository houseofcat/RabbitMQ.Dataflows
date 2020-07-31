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
        public const string HeaderForEncrypt = "X-CR-ENCRYPTED";
        public const string HeaderValueForArgonAesEncrypt = "ARGON2ID-AES256";
        public const string HeaderForEncryptDate = "X-CR-ENCRYPTDATE";
        public const string HeaderForCompress = "X-CR-COMPRESSED";
        public const string HeaderValueForGzipCompress = "GZIP";

        public const string RangeErrorMessage = "Value for {0} must be between {1} and {2}.";

        // Pipeline
        public const string DefaultPipelineName = "NoNameProvided";
    }

    public static class ExceptionMessages
    {
        // AutoPublisher
        public readonly static string AutoPublisherNotStartedError = "AutoPublisher has not been started.";

        // General
        public readonly static string QueueChannelError = "Can't queue a letter to a closed Threading.Channel.";

        public readonly static string ChannelReadErrorMessage = "Can't use reader on a closed Threading.Channel.";
        public readonly static string NoConsumerSettingsMessage = "Consumer {0} not found in Consumers dictionary.";
        public readonly static string NoConsumerPipelineSettingsMessage = "ConsumerPipeline {0} not found in ConsumerPipelineSettings dictionary.";

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
        public static class ChannelHost
        {
            public readonly static string FlowControlled = "Flow control detected on ChannelHost {0}";
            public readonly static string FlowControlFinished = "Flow control is finished on ChannelHost {0}";
        }

        public static class ConnectionPool
        {
            public readonly static string CreateConnections = "ConnectionPool creating Connections...";
            public readonly static string CreateConnectionsComplete = "ConnectionPool initialized.";
            public readonly static string CreateConnectionException = "Connection () failed to be created.";
            public readonly static string Shutdown = "ConnectionPool shutdown was called.";
            public readonly static string ShutdownComplete = "ConnectionPool shutdown complete.";
        }

        public static class ChannelPool
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

        public static class Publisher
        {
            public readonly static string PublishFailed = "Publish to route ({0}) failed, flagging channel host. Error: {1}";
            public readonly static string PublishLetterFailed = "Publish to route ({0}) failed [LetterId: {1}] flagging channel host. Error: {2}";
            public readonly static string PublishBatchFailed = "Batch publish failed, flagging channel host. Error: {0}";
        }

        public static class AutoPublisher
        {
            public readonly static string LetterQueued = "AutoPublisher queued letter [LetterId:{0} InternalId:{1}].";
            public readonly static string LetterPublished = "AutoPublisher published letter [LetterId:{0} InternalId:{1}]. Listen for receipt to indicate success...";
        }

        public static class Consumer
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

            public readonly static string ConsumerExecution = "Consumer ({0}) execution engine executing unit of work (ReceivedData:DT:{1}).";
            public readonly static string ConsumerExecutionSuccess = "Consumer ({0}) execution engine executing unit of work (ReceivedData:DT:{1}) was successful... acking.";
            public readonly static string ConsumerExecutionFailure = "Consumer ({0}) execution engine executing unit of work (ReceivedData:DT:{1}) was unsuccesful... nacking.";
            public readonly static string ConsumerExecutionError = "Consumer ({0}) execution engine executing unit of work (ReceivedData:DT:{1}) triggered an error. Error: {2}";

            public readonly static string ConsumerParallelExecution = "Consumer ({0}) parallel executing unit of work (ReceivedData:DT:{1}).";
            public readonly static string ConsumerParallelExecutionSuccess = "Consumer ({0}) parallel executing unit of work (ReceivedData:DT:{1}) was successful... acking.";
            public readonly static string ConsumerParallelExecutionFailure = "Consumer ({0}) parallel executing unit of work (ReceivedData:DT:{1}) was unsuccesful... nacking.";
            public readonly static string ConsumerParallelExecutionError = "Consumer ({0}) parallel executing unit of work (ReceivedData:DT:{1}) triggered an error. Error: {2}";

            public readonly static string ConsumerDataflowActionCancelled = "Consumer ({0}) dataflow engine actions were cancelled.";
            public readonly static string ConsumerDataflowError = "Consumer ({0}) dataflow engine encountered an error. Error: {1}";
            public readonly static string ConsumerDataflowQueueing = "Consumer ({0}) dataflow engine queueing unit of work (ReceivedData:DT:{1}).";
            public readonly static string ConsumerPipelineQueueing = "Consumer ({0}) pipeline engine queueing unit of work (ReceivedData:DT:{1}).";
            public readonly static string ConsumerPipelineWaiting = "Consumer ({0}) pipeline engine waiting on completion of unit of work (ReceivedData:DT:{1})...";
            public readonly static string ConsumerPipelineWaitingDone = "Consumer ({0}) pipeline engine waiting on completed unit of work (ReceivedData:DT:{1}).";

            public readonly static string ConsumerPipelineActionCancelled = "Consumer ({0}) pipeline engine actions were cancelled.";
            public readonly static string ConsumerPipelineError = "Consumer ({0}) pipeline engine encountered an error. Error: {1}";
        }

        public static class DataflowEngine
        {
            public readonly static string Execution = "Dataflow execution engine executing unit of work (DT:{0}).";
            public readonly static string ExecutionSuccess = "Dataflow execution engine executing unit of work (DT:{0}) was successful... acking.";
            public readonly static string ExecutionFailure = "Dataflow execution engine executing unit of work (DT:{0}) was unsuccesful... nacking.";
            public readonly static string ExecutionError = "Dataflow execution engine executing unit of work (DT:{0}) triggered an error. Error: {1}";
            public readonly static string QueueError = "Dataflow execution engine queueing unit of work (DT:{0}) triggered an error. Error: {1}";
        }

        public static class LetterDataflowEngine
        {
            public readonly static string Execution = "Dataflow execution engine executing unit of work (LetterId:{0} DT:{1}).";
            public readonly static string ExecutionSuccess = "Dataflow execution engine executing unit of work (LetterId:{0} DT:{1}) was successful... acking.";
            public readonly static string ExecutionFailure = "Dataflow execution engine executing unit of work (LetterId:{0} DT:{1}) was unsuccesful... nacking.";
            public readonly static string ExecutionError = "Dataflow execution engine executing unit of work (LetterId:{0} DT:{1}) triggered an error. Error: {1}";
            public readonly static string QueueError = "Dataflow execution engine queueing unit of work (LetterId:{0} DT:{1}) triggered an error. Error: {1}";
        }

        public static class Pipeline
        {
            public readonly static string Healthy = "Pipeline ({0}) appears healthy.";
            public readonly static string Faulted = "Pipeline ({0}) has faulted. Replace/rebuild Pipeline or restart Application...";
            public readonly static string AwaitsCompletion = "Pipeline ({0}) awaits completion.";
            public readonly static string Queued = "Pipeline ({0}) queued item for execution.";
        }
    }
}
