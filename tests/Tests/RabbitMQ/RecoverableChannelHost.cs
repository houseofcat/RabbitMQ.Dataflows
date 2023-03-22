using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using HouseofCat.RabbitMQ.Pools;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace IntegrationTests.RabbitMQ;

public interface IRecoverableChannelHost : IChannelHost
{
    string RecordedConsumerTag { get; }
    string RecoveredConsumerTag { get; }
    bool? Recovered { get; }

    void DeleteRecordedConsumerTag(string consumerTag);
    Task DeleteRecordedConsumerTagAsync(string consumerTag);

    void RecordConsumerTag(string consumerTag);
    Task RecordConsumerTagAsync(string consumerTag);
}

public class RecoverableChannelHost : ChannelHost, IRecoverableChannelHost
{
    public string RecordedConsumerTag { get; private set; }
    public string RecoveredConsumerTag { get; private set; }
    public bool? Recovered { get; private set; }

    public RecoverableChannelHost(ulong channelId, IConnectionHost connHost, bool ackable) :
        base(channelId, connHost, ackable)
    {
    }

    public void DeleteRecordedConsumerTag(string consumerTag)
    {
        EnterLock();
        if (RecordedConsumerTag == consumerTag)
        {
            RecordedConsumerTag = null;
        }
        ExitLock();
    }

    public async Task DeleteRecordedConsumerTagAsync(string consumerTag)
    {
        await EnterLockAsync().ConfigureAwait(false);
        if (RecordedConsumerTag == consumerTag)
        {
            RecordedConsumerTag = null;
        }
        ExitLock();
    }

    public void RecordConsumerTag(string consumerTag)
    {
        EnterLock();
        RecordedConsumerTag = consumerTag;
        ExitLock();
    }

    public async Task RecordConsumerTagAsync(string consumerTag)
    {
        await EnterLockAsync().ConfigureAwait(false);
        RecordedConsumerTag = consumerTag;
        ExitLock();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override async Task<bool> RecoverChannelAsync(Func<Task<bool>> startConsumingAsync)
    {
        await EnterLockAsync().ConfigureAwait(false);

        try
        {
            if (Recovered is false || !await ConnectionHostHealthyAsync().ConfigureAwait(false))
            {
                return false;
            }

            RecordedConsumerTag = RecoveredConsumerTag;
            RecoveredConsumerTag = null;
        }
        finally
        {
            ExitLock();
        }

        return await base.HealthyAsync().ConfigureAwait(false)
            ? !string.IsNullOrEmpty(RecordedConsumerTag) || await startConsumingAsync().ConfigureAwait(false)
            : await base.RecoverChannelAsync(startConsumingAsync).ConfigureAwait(false);
    }

    public override async Task<bool> HealthyAsync() =>
        Recovered is not false && await base.HealthyAsync().ConfigureAwait(false);

    protected override void AddEventHandlers(IModel channel)
    {
        base.AddEventHandlers(channel);
        if (channel is IRecoverable recoverableChannel)
        {
            recoverableChannel.Recovery += ChannelRecovered;
        }
        if (Connection is IAutorecoveringConnection recoverableConnection)
        {
            recoverableConnection.ConsumerTagChangeAfterRecovery += ConsumerTagChangedAfterRecovery;
        }
    }

    protected override void RemoveEventHandlers(IModel channel)
    {
        base.RemoveEventHandlers(channel);
        if (channel is IRecoverable recoverableChannel)
        {
            recoverableChannel.Recovery -= ChannelRecovered;
        }
        if (Connection is IAutorecoveringConnection recoverableConnection)
        {
            recoverableConnection.ConsumerTagChangeAfterRecovery -= ConsumerTagChangedAfterRecovery;
        }
    }

    protected override void ChannelClose(object sender, ShutdownEventArgs e)
    {
        EnterLock();
        if (sender is IRecoverable)
        {
            Recovered = false;
            RecoveredConsumerTag = null;
        }
        ExitLock();
        base.ChannelClose(sender, e);
    }

    protected virtual void ChannelRecovered(object sender, EventArgs e)
    {
        EnterLock();
        Recovered = true;
        ExitLock();
    }

    protected virtual void ConsumerTagChangedAfterRecovery(object sender, ConsumerTagChangedAfterRecoveryEventArgs e)
    {
        EnterLock();
        // Check TagAfter in case of race condition where the consumer registers its tag before this handler fires
        if (sender is IRecoverable && (e.TagBefore == RecordedConsumerTag || e.TagAfter == RecordedConsumerTag))
        {
            RecoveredConsumerTag = e.TagAfter;
        }
        ExitLock();
    }
}