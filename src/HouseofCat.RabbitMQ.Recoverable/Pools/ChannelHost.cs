using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using HouseofCat.Utilities;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace HouseofCat.RabbitMQ.Recoverable.Pools;

public interface IChannelHost : HouseofCat.RabbitMQ.Pools.IChannelHost
{
    string RecoveryId { get; }
    string RecoveredConsumerTag { get; }
    string RecoveringConsumerTag { get; }
    bool Recovered { get; }
    bool Recovering { get; }
    ValueTask<bool> RecoverChannelAsync(Func<ValueTask<bool>> startConsumingAsync);
}

public class ChannelHost : HouseofCat.RabbitMQ.Pools.ChannelHost, IChannelHost
{
    public string RecoveryId { get; } = Guid.NewGuid().ConvertToBase64Url();
    public string RecoveredConsumerTag { get; private set; }
    public string RecoveringConsumerTag { get; private set; }
    public bool Recovered { get; private set; }
    public bool Recovering { get; private set; }

    private bool _recoveringConsumer;

    public ChannelHost(ulong channelId, IConnectionHost connHost, bool ackable) : base(channelId, connHost, ackable) { }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public new async Task<bool> MakeChannelAsync()
    {
        await EnterLockAsync().ConfigureAwait(false);
        try
        {
            if (Recovering)
            {
                return false;
            }
            if (Recovered)
            {
                return await base.HealthyAsync().ConfigureAwait(false);
            }
        }
        finally
        {
            ExitLock();
        }
        return await base.MakeChannelAsync().ConfigureAwait(false);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public async ValueTask<bool> RecoverChannelAsync(Func<ValueTask<bool>> startConsumingAsync = null)
    {
        _recoveringConsumer = startConsumingAsync is not null;
        if (!await MakeChannelAsync().ConfigureAwait(false))
        {
            return false;
        }
        if (startConsumingAsync is null)
        {
            return true;
        }
        await EnterLockAsync().ConfigureAwait(false);
        try
        {
            if (Recovered && !string.IsNullOrEmpty(RecoveredConsumerTag))
            {
                return true;
            }
        }
        finally
        {
            ExitLock();
        }
        return await startConsumingAsync().ConfigureAwait(false);
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
        if (e.ConsumerArguments.TryGetValue("ChanHostRecoveryId", out var recoveryId) && recoveryId.Equals(RecoveryId))
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