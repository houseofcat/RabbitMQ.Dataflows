using System.Collections.Generic;

namespace HouseofCat.RabbitMQ
{
    public class ConsumerOptions : GlobalConsumerOptions
    {
        public bool Enabled { get; set; }
        public string GlobalSettings { get; set; }
        public string QueueName { get; set; }
        public string ConsumerName { get; set; }

        public string TargetQueueName { get; set; }
        public Dictionary<string, string> TargetQueues { get; set; } = new Dictionary<string, string>();

        public string ErrorQueueName => $"{QueueName}.{ErrorSuffix ?? "Error"}";

        public ConsumerPipelineOptions ConsumerPipelineOptions { get; set; }
    }
}
