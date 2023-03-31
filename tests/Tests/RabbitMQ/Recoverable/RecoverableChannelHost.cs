using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using HouseofCat.RabbitMQ.Pools;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace IntegrationTests.RabbitMQ.Recoverable;

public interface IRecoverableChannelHost : IChannelHost
{
    string RecoveredConsumerTag { get; }
    string RecoveringConsumerTag { get; }
    bool Recovered { get; }
    bool Recovering { get; }
}

public class RecoverableChannelHost : ChannelHost, IRecoverableChannelHost
{
    public string RecoveredConsumerTag { get; private set; }
    public string RecoveringConsumerTag { get; private set; }
    public bool Recovered { get; private set; }
    public bool Recovering { get; private set; }

    private bool _recoveringConsumer;

    public RecoverableChannelHost(ulong channelId, IConnectionHost connHost, bool ackable)
        : base(channelId, connHost, ackable)
    { }

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
    protected override void AddEventHandlers(IModel channel, IConnectionHost connHost)
    {
        base.AddEventHandlers(channel, connHost);
        if (channel is not IRecoverable recoverableChannel)
        {
            return;
        }
        recoverableChannel.Recovery += ChannelRecovered;
        if (connHost is not IRecoverableConnectionHost recoverableConnHost)
        {
            return;
        }
        recoverableConnHost.ConsumerTagChangeAfterRecovery += ConsumerTagChangedAfterRecovery;
        recoverableConnHost.RecoveringConsumer += RecoveringConsumer;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected override void RemoveEventHandlers(IModel channel, IConnectionHost connHost)
    {
        base.RemoveEventHandlers(channel, connHost);
        if (channel is not IRecoverable recoverableChannel)
        {
            return;
        }
        recoverableChannel.Recovery -= ChannelRecovered;
        if (connHost is not IRecoverableConnectionHost recoverableConnHost)
        {
            return;
        }
        recoverableConnHost.ConsumerTagChangeAfterRecovery -= ConsumerTagChangedAfterRecovery;
        recoverableConnHost.RecoveringConsumer -= RecoveringConsumer;
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
        if (e.ConsumerArguments.TryGetValue("ChannelHostId", out var channelHostId) && channelHostId.Equals(Id))
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