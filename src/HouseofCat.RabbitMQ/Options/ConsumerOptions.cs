using HouseofCat.RabbitMQ.Dataflows;
using HouseofCat.RabbitMQ.Services;
using System.Collections.Generic;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace HouseofCat.RabbitMQ;

public sealed class ConsumerOptions
{
    public bool NoLocal { get; set; }
    public bool Exclusive { get; set; }
    public ushort BatchSize { get; set; } = 5;
    public bool AutoAck { get; set; }

    public BoundedChannelFullMode? BehaviorWhenFull { get; set; } = BoundedChannelFullMode.Wait;

    public bool Enabled { get; set; }

    public string ConsumerName { get; set; }

    public string QueueName { get; set; }
    public IDictionary<string, object> QueueArgs { get; set; }

    public string SendQueueName { get; set; }
    public IDictionary<string, object> SendQueueArgs { get; set; }

    public string ErrorQueueName { get; set; }
    public IDictionary<string, object> ErrorQueueArgs { get; set; }

    public bool BuildQueues { get; set; } = true;
    public bool BuildQueueDurable { get; set; } = true;
    public bool BuildQueueExclusive { get; set; }
    public bool BuildQueueAutoDelete { get; set; }

    public string WorkflowName { get; set; }
    public int WorkflowMaxDegreesOfParallelism { get; set; } = 1;
    public int WorkflowConsumerCount { get; set; } = 1;
    public int WorkflowBatchSize { get; set; } = 5;
    public bool WorkflowEnsureOrdered { get; set; } = true;
    public bool WorkflowWaitForCompletion { get; set; }

    public bool WorkflowSendCompressed { get; set; }
    public bool WorkflowSendEncrypted { get; set; }

    private static readonly string _dlqKey = "x-dead-letter-routing-key";
    private static readonly string _dleKey = "x-dead-letter-exchange";

    public bool RejectOnError()
    {
        if (QueueArgs is not null
            && (QueueArgs.ContainsKey(_dlqKey)
            || QueueArgs.ContainsKey(_dleKey)))
        {
            return true;
        }

        return false;
    }
}
