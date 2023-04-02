using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using HouseofCat.Utilities;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace HouseofCat.RabbitMQ.Recoverable.Pools;

public interface IChannelHost : HouseofCat.RabbitMQ.Pools.IChannelHost
{
    string Id { get; }
    string RecoveredConsumerTag { get; }
    string RecoveringConsumerTag { get; }
    bool Recovered { get; }
    bool Recovering { get; }
}

public class ChannelHost : HouseofCat.RabbitMQ.Pools.ChannelHost, IChannelHost
{
    public string Id { get; } = Guid.NewGuid().ConvertToBase64Url();
    public string RecoveredConsumerTag { get; private set; }
    public string RecoveringConsumerTag { get; private set; }
    public bool Recovered { get; private set; }
    public bool Recovering { get; private set; }

    private bool _recoveringConsumer;

    public ChannelHost(ulong channelId, IConnectionHost connHost, bool ackable) : base(channelId, connHost, ackable) { }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public new async Task<bool> MakeChannelAsync(Func<ValueTask<bool>> startConsumingAsync = null)
    {
        _recoveringConsumer = startConsumingAsync is not null;
        await EnterLockAsync().ConfigureAwait(false);

        bool hasRecovered, hasRecoveredTag;
        try
        {
            if (Recovering || (Recovered && !await base.HealthyAsync().ConfigureAwait(false)))
            {
                return false;
            }
            hasRecovered = Recovered;
            hasRecoveredTag = !string.IsNullOrEmpty(RecoveredConsumerTag);
        }
        finally
        {
            ExitLock();
        }

        return
            hasRecovered
                ? hasRecoveredTag || startConsumingAsync is null || await startConsumingAsync().ConfigureAwait(false)
                : await base.MakeChannelAsync(startConsumingAsync).ConfigureAwait(false);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override async Task<bool> HealthyAsync() => !Recovering && await base.HealthyAsync().ConfigureAwait(false);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected override void AddEventHandlers(IModel channel, HouseofCat.RabbitMQ.Pools.IConnectionHost connHost)
    {
        base.AddEventHandlers(channel, connHost);
        if (channel is not IRecoverable recoverableChannel)
        {
            return;
        }
        recoverableChannel.Recovery += ChannelRecovered;
        if (connHost is not IConnectionHost recoverableConnectionHost)
        {
            return;
        }
        recoverableConnectionHost.ConsumerTagChangeAfterRecovery += ConsumerTagChangedAfterRecovery;
        recoverableConnectionHost.RecoveringConsumer += RecoveringConsumer;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected override void RemoveEventHandlers(IModel channel, HouseofCat.RabbitMQ.Pools.IConnectionHost connHost)
    {
        base.RemoveEventHandlers(channel, connHost);
        if (channel is not IRecoverable recoverableChannel)
        {
            return;
        }
        recoverableChannel.Recovery -= ChannelRecovered;
        if (connHost is not IConnectionHost recoverableConnectionHost)
        {
            return;
        }
        recoverableConnectionHost.ConsumerTagChangeAfterRecovery -= ConsumerTagChangedAfterRecovery;
        recoverableConnectionHost.RecoveringConsumer -= RecoveringConsumer;
    }

    protected override void ChannelClose(object sender, ShutdownEventArgs e)
    {
        EnterLock();
        if (sender is IRecoverable)
        {
            RecoveredConsumerTag = null;
            Recovered = false;
            Recovering = true;
        }
        ExitLock();
        base.ChannelClose(sender, e);
    }

    protected virtual void ChannelRecovered(object sender, EventArgs e)
    {
        EnterLock();
        Recovered = true;
        Recovering = false;
        ExitLock();
    }

    protected virtual void RecoveringConsumer(object sender, RecoveringConsumerEventArgs e)
    {
        if (!_recoveringConsumer)
        {
            return;
        }
        EnterLock();
        if (e.ConsumerArguments.TryGetValue("RecoverableChanHostId", out var channelHostId) && channelHostId.Equals(Id))
        {
            RecoveringConsumerTag = e.ConsumerTag;
        }
        ExitLock();
    }

    protected virtual void ConsumerTagChangedAfterRecovery(object sender, ConsumerTagChangedAfterRecoveryEventArgs e)
    {
        if (!_recoveringConsumer)
        {
            return;
        }
        EnterLock();
        if (e.TagBefore == RecoveringConsumerTag)
        {
            RecoveredConsumerTag = e.TagAfter;
            RecoveringConsumerTag = null;
        }
        ExitLock();
    }
}