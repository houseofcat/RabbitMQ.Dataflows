using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace HouseofCat.RabbitMQ.Pools;

public interface IRecoveryAwareChannelHost : IChannelHost
{
    ValueTask<bool> CancelRecoveredConsumerTag(string consumerTag);
}

public class RecoveryAwareChannelHost : ChannelHost, IRecoveryAwareChannelHost
{
    public ConcurrentBag<string> RegisteredConsumerTags { get; private set; } = new();
    
    private IRecoveryAwareConnectionHost _connHost;

    public RecoveryAwareChannelHost(ulong channelId, IRecoveryAwareConnectionHost connHost, bool ackable) :
        base(channelId, connHost, ackable)
    {
        _connHost = connHost;
    }

    public async ValueTask<bool> CancelRecoveredConsumerTag(string consumerTag)
    {
        await EnterLockAsync().ConfigureAwait(false);

        try
        {
            if (!_connHost.RemoveRecoveredConsumerTag(consumerTag))
            {
                return false;
            }

            Channel.BasicCancelNoWait(consumerTag);
            return true;
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