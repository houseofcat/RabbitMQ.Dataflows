using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace HouseofCat.RabbitMQ.Pools;

public interface IRecoveryAwareConnectionHost : IConnectionHost
{
    bool? Recovered { get; }
    bool Recovering { get; }
    bool RemoveRecoveredConsumerTag(string consumerTag);
}

public class RecoveryAwareConnectionHost : ConnectionHost, IRecoveryAwareConnectionHost
{
    public bool? Recovered { get; private set; }
    public bool Recovering { get; private set; }

    private ConcurrentDictionary<string, bool> _recoveredConsumerTags = new();

    public RecoveryAwareConnectionHost(ulong connectionId, IConnection connection) : base(connectionId, connection)
    {
    }

    public override async Task<bool> HealthyAsync()
    {
        await EnterLockAsync().ConfigureAwait(false);

        try
        {
            if (Recovered is false)
            {
                return false;
            }
        }
        finally
        {
            ExitLock();
        }

        return await base.HealthyAsync().ConfigureAwait(false);
    }

    public bool RemoveRecoveredConsumerTag(string consumerTag) =>
        _recoveredConsumerTags?.TryRemove(consumerTag, out _) ?? false;
    
    protected override void AddEventHandlers()
    {
        base.AddEventHandlers();
        if (Connection is not IAutorecoveringConnection autoRecoveringConnection)
        {
            return;
        }
        autoRecoveringConnection.RecoverySucceeded += ConnectionRecovered;
        autoRecoveringConnection.ConsumerTagChangeAfterRecovery += ConsumerTagChangedAfterRecovery;
    }

    protected override void RemoveEventHandlers()
    {
        base.RemoveEventHandlers();
        if (Connection is not IAutorecoveringConnection autoRecoveringConnection)
        {
            return;
        }
        autoRecoveringConnection.RecoverySucceeded -= ConnectionRecovered;
        autoRecoveringConnection.ConsumerTagChangeAfterRecovery -= ConsumerTagChangedAfterRecovery;
    }

    protected override void ConnectionClosed(object sender, ShutdownEventArgs e)
    {
        base.ConnectionClosed(sender, e);
        EnterLock();
        Recovered = false;
        Recovering = false;
        ExitLock();
    }

    protected virtual void ConnectionRecovered(object sender, EventArgs e)
    {
        EnterLock();
        Recovered = true;
        Recovering = false;
        ExitLock();
    }

    protected virtual void ConsumerTagChangedAfterRecovery(object sender, ConsumerTagChangedAfterRecoveryEventArgs e)
    {
        EnterLock();
        Recovering = true;
        ExitLock();
        _recoveredConsumerTags.TryAdd(e.TagAfter, true);
    }

    protected override void Dispose(bool disposing)
    {
        if (DisposedValue)
        {
            return;
        }

        _recoveredConsumerTags = null;
        base.Dispose(disposing);
    }
}