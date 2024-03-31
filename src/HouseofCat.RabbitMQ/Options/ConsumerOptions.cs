using System.Collections.Generic;
using System.Threading.Channels;

namespace HouseofCat.RabbitMQ;

public class ConsumerOptions
{
    public bool NoLocal { get; set; }
    public bool Exclusive { get; set; }
    public ushort BatchSize { get; set; } = 5;
    public bool AutoAck { get; set; }

    public string ErrorSuffix { get; set; }
    public string AltSuffix { get; set; }

    public BoundedChannelFullMode? BehaviorWhenFull { get; set; } = BoundedChannelFullMode.Wait;

    public bool Enabled { get; set; }

    public string ConsumerName { get; set; }

    public string QueueName { get; set; }
    public IDictionary<string, object> QueueArgs { get; set; }

    public string TargetQueueName { get; set; }
    public IDictionary<string, object> TargetQueueArgs { get; set; }
    public Dictionary<string, string> TargetQueues { get; set; } = new Dictionary<string, string>();

    public string ErrorQueueName => $"{QueueName}.{ErrorSuffix ?? "Error"}";
    public IDictionary<string, object> ErrorQueueArgs { get; set; }

    public string WorkflowName { get; set; }
    public int WorkflowMaxDegreesOfParallelism { get; set; } = 1;
    public int WorkflowConsumerCount { get; set; } = 1;
    public int WorkflowBatchSize { get; set; } = 5;
    public bool WorkflowEnsureOrdered { get; set; } = true;
    public bool WorkflowWaitForCompletion { get; set; }
}
