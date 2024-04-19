namespace HouseofCat.RabbitMQ.Pipelines;

public static class Constants
{
    public static class ConsumerPipelines
    {
        public readonly static string ConsumerPipelineQueueing = "Consumer ({0}) pipeline engine queueing unit of work (receivedMessage:DT:{1}).";
        public readonly static string ConsumerPipelineWaiting = "Consumer ({0}) pipeline engine waiting on completion of unit of work (receivedMessage:DT:{1})...";
        public readonly static string ConsumerPipelineWaitingDone = "Consumer ({0}) pipeline engine waiting on completed unit of work (receivedMessage:DT:{1}).";
        public readonly static string ConsumerPipelineActionCancelled = "Consumer ({0}) pipeline engine actions were cancelled.";
        public readonly static string ConsumerPipelineError = "Consumer ({0}) pipeline engine encountered an error. Error: {1}";
    }
}
