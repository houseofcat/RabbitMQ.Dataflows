using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace HouseofCat.RabbitMQ.Pools;

public interface IRecoveryAwareChannelHost : IChannelHost
{
    bool? Recovered { get; }
    void DeleteRecordedConsumerTag(string consumerTag);
    void RecordConsumerTag(string consumerTag);
    Task DeleteRecordedConsumerTagAsync(string consumerTag);
    Task RecordConsumerTagAsync(string consumerTag);
    Task<bool> RecoverChannelAsync(Func<Task<bool>> restartConsumingAsync);
}

public class RecoveryAwareChannelHost : ChannelHost, IRecoveryAwareChannelHost
{
    public bool? Recovered { get; private set; }

    private string _recordedConsumerTag;
    private string _recoveredConsumerTag;

    public RecoveryAwareChannelHost(ulong channelId, IConnectionHost connHost, bool ackable) :
        base(channelId, connHost, ackable)
    {
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public async Task<bool> RecoverChannelAsync(Func<Task<bool>> restartConsumingAsync)
    {
        await EnterLockAsync().ConfigureAwait(false);

        try
        {
            if (Recovered is false || !await ConnHost.HealthyAsync().ConfigureAwait(false))
            {
                return false;
            }

            _recordedConsumerTag = _recoveredConsumerTag;
            _recoveredConsumerTag = null;
        }
        finally
        {
            ExitLock();
        }

        if (await base.HealthyAsync().ConfigureAwait(false))
        {
            if (!string.IsNullOrEmpty(_recordedConsumerTag))
            {
                return true;
            }
        }
        else if (!await MakeChannelAsync().ConfigureAwait(false))
        {
            return false;
        }

        return await restartConsumingAsync().ConfigureAwait(false);
    }

    public override async Task<bool> HealthyAsync() => 
        Recovered is not false && await base.HealthyAsync().ConfigureAwait(false);

    public void DeleteRecordedConsumerTag(string consumerTag)
    {
        EnterLock();
        if (_recordedConsumerTag == consumerTag)
        {
            _recordedConsumerTag = null;
        }
        ExitLock();
    }

    public void RecordConsumerTag(string consumerTag)
    {
        EnterLock();
        _recordedConsumerTag = consumerTag;
        ExitLock();
    }

    public async Task DeleteRecordedConsumerTagAsync(string consumerTag)
    {
        await EnterLockAsync().ConfigureAwait(false);
        if (_recordedConsumerTag == consumerTag)
        {
            _recordedConsumerTag = null;
        }
        ExitLock();
    }

    public async Task RecordConsumerTagAsync(string consumerTag)
    {
        await EnterLockAsync().ConfigureAwait(false);
        _recordedConsumerTag = consumerTag;
        ExitLock();
    }

    protected override void AddEventHandlers()
    {
        base.AddEventHandlers();
        if (Channel is IRecoverable recoverableChannel)
        {
            recoverableChannel.Recovery += ChannelRecovered;
        }
        if (ConnHost?.Connection is IAutorecoveringConnection autoRecoveringConnection)
        {
            autoRecoveringConnection.ConsumerTagChangeAfterRecovery += ConsumerTagChangedAfterRecovery;
        }
    }

    protected override void RemoveEventHandlers()
    {
        base.RemoveEventHandlers();
        if (Channel is IRecoverable recoverableChannel)
        {
            recoverableChannel.Recovery -= ChannelRecovered;
        }
        if (ConnHost?.Connection is IAutorecoveringConnection autoRecoveringConnection)
        {
            autoRecoveringConnection.ConsumerTagChangeAfterRecovery -= ConsumerTagChangedAfterRecovery;
        }
    }

    protected override void ChannelClose(object sender, ShutdownEventArgs e)
    {
        EnterLock();
        if (Channel is IRecoverable)
        {
            Recovered = false;
            _recoveredConsumerTag = null;
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
        if (Channel is IRecoverable && (e.TagBefore == _recordedConsumerTag || e.TagAfter == _recordedConsumerTag))
        {
            _recoveredConsumerTag = e.TagAfter;
        }
        ExitLock();
    }
}