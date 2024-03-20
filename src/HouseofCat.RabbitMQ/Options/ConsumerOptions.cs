using System.Collections.Generic;

namespace HouseofCat.RabbitMQ;

public class ConsumerOptions : GlobalConsumerOptions
{
    public bool Enabled { get; set; }
    public string GlobalSettings { get; set; }

    public string ConsumerName { get; set; }

    public string QueueName { get; set; }
    public IDictionary<string, object> QueueArgs { get; set; }

    public string TargetQueueName { get; set; }
    public IDictionary<string, object> TargetQueueArgs { get; set; }
    public Dictionary<string, string> TargetQueues { get; set; } = new Dictionary<string, string>();

    public string ErrorQueueName => $"{QueueName}.{ErrorSuffix ?? "Error"}";
    public IDictionary<string, object> ErrorQueueArgs { get; set; }
    public string AltQueueName => $"{QueueName}.{AltSuffix ?? "Alt"}";
    public IDictionary<string, object> AltQueueArgs { get; set; }

    public ConsumerPipelineOptions ConsumerPipelineOptions { get; set; }

    public void ApplyGlobalOptions(GlobalConsumerOptions globalConsumerOptions)
    {
        NoLocal = globalConsumerOptions.NoLocal ?? NoLocal;
        Exclusive = globalConsumerOptions.Exclusive ?? Exclusive;
        BatchSize = globalConsumerOptions.BatchSize ?? BatchSize;

        AutoAck = globalConsumerOptions.AutoAck ?? AutoAck;
        ErrorSuffix = globalConsumerOptions.ErrorSuffix ?? ErrorSuffix;
        AltSuffix = globalConsumerOptions.AltSuffix ?? AltSuffix;
        BehaviorWhenFull = globalConsumerOptions.BehaviorWhenFull ?? BehaviorWhenFull;

        if (globalConsumerOptions.GlobalConsumerPipelineOptions != null)
        {
            ConsumerPipelineOptions ??= new ConsumerPipelineOptions();

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
