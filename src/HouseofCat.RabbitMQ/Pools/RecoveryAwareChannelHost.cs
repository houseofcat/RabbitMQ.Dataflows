using System;
using System.Collections.Concurrent;
using RabbitMQ.Client;

namespace HouseofCat.RabbitMQ.Pools;

public interface IRecoveryAwareChannelHost : IChannelHost
{
    ConcurrentBag<string> RegisteredConsumerTags { get; }
}

public class RecoveryAwareChannelHost : ChannelHost, IRecoveryAwareChannelHost
{
    public ConcurrentBag<string> RegisteredConsumerTags { get; private set; } = new();
    
    private IRecoveryAwareConnectionHost _connHost;

    public RecoveryAwareChannelHost(IRecoveryAwareConnectionHost connHost, bool ackable) : base(0, connHost, ackable)
    {
        _connHost = connHost;
    }
    
    protected override void AddEventHandlers()
    {
        base.AddEventHandlers();
        if (Channel is IRecoverable recoverableChannel)
        {
            recoverableChannel.Recovery += ChannelRecovered;
        }
    }

    protected override void RemoveEventHandlers()
    {
        base.RemoveEventHandlers();
        if (Channel is IRecoverable recoverableChannel)
        {
            recoverableChannel.Recovery -= ChannelRecovered;
        }
    }

    protected virtual void ChannelRecovered(object sender, EventArgs e)
    {
        EnterLock();
        try
        {
            while (RegisteredConsumerTags?.TryTake(out var consumerTag) ?? false)
            {
                if (_connHost.RecoveredConsumerTags.TryRemove(consumerTag, out var newConsumerTag))
                {
                    Channel.BasicCancel(newConsumerTag);
                }
            }
        }
        finally
        {
            ExitLock();
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (DisposedValue)
        {
            return;
        }

        _connHost = null;
        RegisteredConsumerTags = null;
        base.Dispose(disposing);
    }
}