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
        public string AltQueueName => $"{QueueName}.{AltSuffix ?? "Alt"}";

        public ConsumerPipelineOptions ConsumerPipelineOptions { get; set; }


        public void ApplyGlobalOptions(GlobalConsumerOptions globalConsumerOptions)
        {
            NoLocal = globalConsumerOptions.NoLocal ?? NoLocal;
            Exclusive = globalConsumerOptions.Exclusive ?? Exclusive;
            BatchSize = globalConsumerOptions.BatchSize ?? BatchSize;

            AutoAck = globalConsumerOptions.AutoAck ?? AutoAck;
            UseTransientChannels = globalConsumerOptions.UseTransientChannels ?? UseTransientChannels;
            ErrorSuffix = globalConsumerOptions.ErrorSuffix ?? ErrorSuffix;
            AltSuffix = globalConsumerOptions.AltSuffix ?? AltSuffix;
            BehaviorWhenFull = globalConsumerOptions.BehaviorWhenFull ?? BehaviorWhenFull;

            if (globalConsumerOptions.GlobalConsumerPipelineOptions != null)
            {
                if (ConsumerPipelineOptions == null)
                { ConsumerPipelineOptions = new ConsumerPipelineOptions(); }

                ConsumerPipelineOptions.WaitForCompletion =
                    globalConsumerOptions.GlobalConsumerPipelineOptions.WaitForCompletion
                    ?? ConsumerPipelineOptions.WaitForCompletion;

                ConsumerPipelineOptions.MaxDegreesOfParallelism =
                    globalConsumerOptions.GlobalConsumerPipelineOptions.MaxDegreesOfParallelism
                    ?? ConsumerPipelineOptions.MaxDegreesOfParallelism;

                ConsumerPipelineOptions.EnsureOrdered =
                    globalConsumerOptions.GlobalConsumerPipelineOptions.EnsureOrdered
                    ?? ConsumerPipelineOptions.EnsureOrdered;
            }
        }
    }
}
