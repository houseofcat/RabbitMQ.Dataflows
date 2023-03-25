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
    public string RecoveredConsumerTag { get; private set; }
    public string RecoveringConsumerTag { get; private set; }
    public bool Recovering { get; private set; }

    public RecoverableChannelHost(ulong channelId, IConnectionHost connHost, bool ackable)
        : base(channelId, connHost, ackable)
    { }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public new async Task<bool> MakeChannelAsync(Func<ValueTask<bool>> startConsumingAsync = null)
    {
        await EnterLockAsync().ConfigureAwait(false);

        bool hasRecoveredTag;
        try
        {
            if (Recovering || !await ConnectionHostHealthyAsync().ConfigureAwait(false))
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

    public override async Task<bool> HealthyAsync() => !Recovering && await base.HealthyAsync().ConfigureAwait(false);

    protected override void AddEventHandlers(IModel channel, IConnection connection)
    {
        base.AddEventHandlers(channel, connection);
        if (channel is not IRecoverable recoverableChannel)
        {
            return;
        }
        recoverableChannel.Recovery += ChannelRecovered;
        if (connection is not IAutorecoveringConnection recoverableConnection)
        {
            return;
        }
        recoverableConnection.ConsumerTagChangeAfterRecovery += ConsumerTagChangedAfterRecovery;
        recoverableConnection.RecoveringConsumer += RecoveringConsumer;
    }

    protected override void RemoveEventHandlers(IModel channel, IConnection connection)
    {
        base.RemoveEventHandlers(channel, connection);
        if (channel is not IRecoverable recoverableChannel)
        {
            return;
        }
        recoverableChannel.Recovery -= ChannelRecovered;
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
        EnterLock();
        if (e.ConsumerArguments.TryGetValue("ChannelNumber", out var channelNumber) &&
            channelNumber.Equals(ChannelNumber))
        {
            RecoveringConsumerTag = e.ConsumerTag;
        }
        ExitLock();
    }

    protected virtual void ConsumerTagChangedAfterRecovery(object sender, ConsumerTagChangedAfterRecoveryEventArgs e)
    {
        EnterLock();
        if (e.TagBefore == RecoveringConsumerTag)
        {
            RecoveredConsumerTag = e.TagAfter;
            RecoveringConsumerTag = null;
        }
        ExitLock();
    }
}