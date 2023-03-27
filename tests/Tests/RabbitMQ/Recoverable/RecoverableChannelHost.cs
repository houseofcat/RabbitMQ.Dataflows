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
    bool Recovering { get; }
}

public class RecoverableChannelHost : ChannelHost, IRecoverableChannelHost
{
    private bool _recoveringConsumer;

    public string RecoveredConsumerTag { get; private set; }
    public string RecoveringConsumerTag { get; private set; }
    public bool Recovering { get; private set; }

    public RecoverableChannelHost(ulong channelId, IConnectionHost connHost, bool ackable)
        : base(channelId, connHost, ackable)
    { }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public new async Task<bool> MakeChannelAsync(Func<ValueTask<bool>> startConsumingAsync = null)
    {
        _recoveringConsumer = startConsumingAsync is not null;
        await EnterLockAsync().ConfigureAwait(false);

        bool hasRecoveredTag;
        try
        {
            if (Recovering || !await ConnectionHealthyAsync().ConfigureAwait(false))
            {
                return false;
            }
        }
        finally
        {
            hasRecoveredTag = !string.IsNullOrEmpty(RecoveredConsumerTag);
            ExitLock();
        }

        return
            await base.HealthyAsync().ConfigureAwait(false)
                ? hasRecoveredTag || startConsumingAsync is null || await startConsumingAsync().ConfigureAwait(false)
                : await base.MakeChannelAsync(startConsumingAsync).ConfigureAwait(false);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override async Task<bool> HealthyAsync() => !Recovering && await base.HealthyAsync().ConfigureAwait(false);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected override void AddChannelEventHandlers(IModel channel)
    {
        base.AddChannelEventHandlers(channel);
        if (channel is not IRecoverable recoverableChannel)
        {
            return;
        }
        recoverableChannel.Recovery += ChannelRecovered;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected override void AddConnectionEventHandlers(IConnection connection)
    {
        base.AddConnectionEventHandlers(connection);
        if (connection is not IAutorecoveringConnection recoverableConnection)
        {
            return;
        }
        recoverableConnection.ConsumerTagChangeAfterRecovery += ConsumerTagChangedAfterRecovery;
        recoverableConnection.RecoveringConsumer += RecoveringConsumer;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected override void RemoveChannelEventHandlers(IModel channel)
    {
        base.RemoveChannelEventHandlers(channel);
        if (channel is not IRecoverable recoverableChannel)
        {
            return;
        }
        recoverableChannel.Recovery -= ChannelRecovered;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected override void RemoveConnectionEventHandlers(IConnection connection)
    {
        base.RemoveConnectionEventHandlers(connection);
        if (connection is not IAutorecoveringConnection recoverableConnection)
        {
            return;
        }
        recoverableConnection.ConsumerTagChangeAfterRecovery -= ConsumerTagChangedAfterRecovery;
        recoverableConnection.RecoveringConsumer -= RecoveringConsumer;
    }

    protected override void ChannelClose(object sender, ShutdownEventArgs e)
    {
        EnterLock();
        if (sender is IRecoverable)
        {
            RecoveredConsumerTag = null;
            Recovering = true;
        }
        ExitLock();
        base.ChannelClose(sender, e);
    }

    protected virtual void ChannelRecovered(object sender, EventArgs e)
    {
        EnterLock();
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