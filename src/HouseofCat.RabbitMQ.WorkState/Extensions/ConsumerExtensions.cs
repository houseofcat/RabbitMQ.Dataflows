using System;
using System.Threading;
using System.Threading.Tasks;
using HouseofCat.Dataflows;

namespace HouseofCat.RabbitMQ.WorkState.Extensions;

public static class ConsumerExtensions
{
    public static ValueTask DirectChannelExecutionEngineAsync(
        this IConsumer<ReceivedData> consumer,
        Func<ReceivedData, Task<IRabbitWorkState>> workBodyAsync,
        Func<IRabbitWorkState, Task> postWorkBodyAsync = null,
        TaskScheduler taskScheduler = null,
        CancellationToken cancellationToken = default)
    {
        var channelReaderBlockEngine = new ChannelReaderBlockEngine<ReceivedData, IRabbitWorkState>(
            consumer.GetConsumerBuffer(),
            workBodyAsync,
            consumer.ConsumerOptions.ConsumerPipelineOptions.MaxDegreesOfParallelism,
            consumer.ConsumerOptions.ConsumerPipelineOptions.EnsureOrdered,
            postWorkBodyAsync,
            taskScheduler);

        return channelReaderBlockEngine.ReadChannelAsync(cancellationToken);
    }
}    
