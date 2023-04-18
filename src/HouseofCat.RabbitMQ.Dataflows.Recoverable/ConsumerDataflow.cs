using System.Collections.Generic;
using System.Threading.Tasks;
using HouseofCat.RabbitMQ.Pools;
using HouseofCat.RabbitMQ.Services;
using HouseofCat.RabbitMQ.WorkState;

namespace HouseofCat.RabbitMQ.Dataflows.Recoverable;

public class ConsumerDataflow<TState> : RabbitMQ.Dataflows.ConsumerDataflow<TState>
    where TState : class, IRabbitWorkState, new()
{
    public ConsumerDataflow(
        IRabbitService rabbitService,
        string workflowName,
        string consumerName,
        int consumerCount,
        TaskScheduler taskScheduler = null)
        : base(rabbitService, workflowName, consumerName, consumerCount, taskScheduler) { }

    /// <summary>
    /// This constructor is used for when you want to supply Consumers manually, or custom Consumers without having to write a custom IRabbitService,
    /// and have global consumer pipeline options to retrieve maxDoP and ensureOrdered from.
    /// </summary>
    /// <param name="rabbitService"></param>
    /// <param name="workflowName"></param>
    /// <param name="consumers"></param>
    /// <param name="globalConsumerPipelineOptions"></param>
    /// <param name="taskScheduler"></param>
    public ConsumerDataflow(
        IRabbitService rabbitService,
        string workflowName,
        ICollection<IConsumer<ReceivedData>> consumers,
        GlobalConsumerPipelineOptions globalConsumerPipelineOptions,
        TaskScheduler taskScheduler = null)
        : base(rabbitService, workflowName, consumers, globalConsumerPipelineOptions, taskScheduler) { }

    /// <summary>
    /// This constructor is used for when you want to supply Consumers manually, or custom Consumers without having to write a custom IRabbitService,
    /// and want a custom maxDoP and/or ensureOrdered.
    /// </summary>
    /// <param name="rabbitService"></param>
    /// <param name="workflowName"></param>
    /// <param name="consumers"></param>
    /// <param name="maxDoP"></param>
    /// <param name="ensureOrdered"></param>
    /// <param name="taskScheduler"></param>
    public ConsumerDataflow(
        IRabbitService rabbitService,
        string workflowName,
        ICollection<IConsumer<ReceivedData>> consumers,
        int maxDoP,
        bool ensureOrdered,
        TaskScheduler taskScheduler = null)
        : base(rabbitService, workflowName, consumers, maxDoP, ensureOrdered, taskScheduler) { }

    protected override Consumer CreateConsumer(IChannelPool channelPool, string consumerName) =>
        new HouseofCat.RabbitMQ.Recoverable.Consumer(channelPool, consumerName);
}