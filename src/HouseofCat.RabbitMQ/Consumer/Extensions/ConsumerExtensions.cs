using System;
using System.Threading;
using System.Threading.Tasks;
using HouseofCat.Dataflows;
using HouseofCat.RabbitMQ.Dataflows;

namespace HouseofCat.RabbitMQ;

public static class ConsumerExtensions
{
    public static ValueTask DirectChannelExecutionEngineAsync(
        this IConsumer<ReceivedMessage> consumer,
        Func<ReceivedMessage, Task<IRabbitWorkState>> workBodyAsync,
        Func<IRabbitWorkState, Task> postWorkBodyAsync = null,
        TaskScheduler taskScheduler = null,
        CancellationToken cancellationToken = default)
    {
        var channelReaderBlockEngine = new ChannelReaderBlockEngine<ReceivedMessage, IRabbitWorkState>(
            consumer.GetConsumerBuffer(),
            workBodyAsync,
            consumer.ConsumerOptions.ConsumerPipelineOptions.MaxDegreesOfParallelism,
            consumer.ConsumerOptions.ConsumerPipelineOptions.EnsureOrdered,
            postWorkBodyAsync,
            taskScheduler);

        return channelReaderBlockEngine.ReadChannelAsync(cancellationToken);
    }
}
