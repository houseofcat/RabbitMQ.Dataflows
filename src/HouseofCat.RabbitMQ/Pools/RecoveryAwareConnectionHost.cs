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
    ConcurrentDictionary<string, string> RecoveredConsumerTags { get; }
}

public class RecoveryAwareConnectionHost : ConnectionHost, IRecoveryAwareConnectionHost
{
    public bool? Recovered { get; private set; }
    public bool Recovering { get; private set; }
    public ConcurrentDictionary<string, string> RecoveredConsumerTags { get; private set; } = new();

    public RecoveryAwareConnectionHost(ulong connectionId, IConnection connection) : base(connectionId, connection)
    {
    }

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
        RecoveredConsumerTags.Clear();
    }

    protected virtual void ConsumerTagChangedAfterRecovery(object sender, ConsumerTagChangedAfterRecoveryEventArgs e)
    {
        EnterLock();
        Recovering = true;
        ExitLock();
        RecoveredConsumerTags.TryAdd(e.TagBefore, e.TagAfter);
    }

    protected override void Dispose(bool disposing)
    {
        if (DisposedValue)
        {
            return;
        }

        RecoveredConsumerTags = null;
        base.Dispose(disposing);
    }
}