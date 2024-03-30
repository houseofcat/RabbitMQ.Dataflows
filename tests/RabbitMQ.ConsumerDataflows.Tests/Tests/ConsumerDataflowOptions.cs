using HouseofCat.RabbitMQ;
using HouseofCat.RabbitMQ.Dataflows;
using HouseofCat.RabbitMQ.Services;

namespace RabbitMQ.ConsumerDataflows.Tests;

public sealed class ConsumerDataflowOptions
{
    public string WorkflowName { get; set; }
    public string WorkStateKey { get; set; } = "State";
    public string ConsumerName { get; set; }
    public int ConsumerCount { get; set; } = 1;
    public int MaxDegreeOfParallelism { get; set; } = 1;
    public bool EnsureOrdered { get; set; } = true;
    public int Capacity { get; set; } = 1000;
    public string ErrorQueueName { get; set; }

    public ConsumerDataflow<CustomWorkState> BuildConsumerDataflow(IRabbitService rabbitService)
    {
        return new ConsumerDataflow<CustomWorkState>(
            rabbitService,
            WorkflowName,
            ConsumerName,
            ConsumerCount)
            .WithBuildState<string>(WorkStateKey);
    }
}
