using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Threading.Tasks;

namespace HouseofCat.RabbitMQ;

public interface ITaskComplete
{
    void Complete();
    Task Completion { get; }
}

public sealed class PipeReceivedMessage : ReceivedMessage, ITaskComplete, IDisposable
{
    private readonly TaskCompletionSource _completionSource = new TaskCompletionSource();
    public Task Completion => _completionSource.Task;

    public PipeReceivedMessage(
        IModel channel,
        BasicGetResult result,
        bool ackable) : base(channel, result, ackable)
    { }

    public PipeReceivedMessage(
        IModel channel,
        BasicDeliverEventArgs args,
        bool ackable) : base(channel, args, ackable)
    { }

    /// <summary>
    /// A way to indicate this message is fully finished with.
    /// </summary>
    public void Complete()
    {
        if (_completionSource.Task.Status < TaskStatus.RanToCompletion)
        {
            _completionSource.SetResult();
        }
    }
}
