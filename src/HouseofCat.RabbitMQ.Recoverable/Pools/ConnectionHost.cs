using System;
using System.Runtime.CompilerServices;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace HouseofCat.RabbitMQ.Recoverable.Pools;

public interface IConnectionHost : HouseofCat.RabbitMQ.Pools.IConnectionHost
{
    event EventHandler<ConnectionRecoveryErrorEventArgs> ConnectionRecoveryError;
    event EventHandler<ConsumerTagChangedAfterRecoveryEventArgs> ConsumerTagChangeAfterRecovery;
    event EventHandler<RecoveringConsumerEventArgs> RecoveringConsumer;
}

public class ConnectionHost : HouseofCat.RabbitMQ.Pools.ConnectionHost, IConnectionHost
{
    private EventHandler<ConnectionRecoveryErrorEventArgs> _recordedConnectionRecoveryErrorEventHandlers;
    private EventHandler<ConsumerTagChangedAfterRecoveryEventArgs> _recordedConsumerTagChangeAfterRecoveryEventHandlers;
    private EventHandler<RecoveringConsumerEventArgs> _recordedRecoveringConsumerEventHandlers;

    public ConnectionHost(ulong connectionId, IConnection connection) : base(connectionId, connection) { }

    public event EventHandler<ConnectionRecoveryErrorEventArgs> ConnectionRecoveryError
    {
        add
        {
            EnterLock();
            try
            {
                if (Connection is not IAutorecoveringConnection recoverableConnection)
                {
                    return;
                }
                _recordedConnectionRecoveryErrorEventHandlers += value;
                recoverableConnection.ConnectionRecoveryError += value;
            }
            finally
            {
                ExitLock();
            }
        }
        remove
        {
            EnterLock();
            try
            {
                if (Connection is not IAutorecoveringConnection recoverableConnection)
                {
                    return;
                }
                _recordedConnectionRecoveryErrorEventHandlers -= value;
                recoverableConnection.ConnectionRecoveryError -= value;
            }
            finally
            {
                ExitLock();
            }
        }
    }

    public event EventHandler<ConsumerTagChangedAfterRecoveryEventArgs> ConsumerTagChangeAfterRecovery
    {
        add
        {
            EnterLock();
            try
            {
                if (Connection is not IAutorecoveringConnection recoverableConnection)
                {
                    return;
                }
                _recordedConsumerTagChangeAfterRecoveryEventHandlers += value;
                recoverableConnection.ConsumerTagChangeAfterRecovery += value;
            }
            finally
            {
                ExitLock();
            }
        }
        remove
        {
            EnterLock();
            try
            {
                if (Connection is not IAutorecoveringConnection recoverableConnection)
                {
                    return;
                }
                _recordedConsumerTagChangeAfterRecoveryEventHandlers -= value;
                recoverableConnection.ConsumerTagChangeAfterRecovery -= value;
            }
            finally
            {
                ExitLock();
            }
        }
    }

    public event EventHandler<RecoveringConsumerEventArgs> RecoveringConsumer
    {
        add
        {
            EnterLock();
            try
            {
                if (Connection is not IAutorecoveringConnection recoverableConnection)
                {
                    return;
                }
                recoverableConnection.RecoveringConsumer += value;
                _recordedRecoveringConsumerEventHandlers += value;
            }
            finally
            {
                ExitLock();
            }
        }
        remove
        {
            EnterLock();
            try
            {
                if (Connection is not IAutorecoveringConnection recoverableConnection)
                {
                    return;
                }
                recoverableConnection.RecoveringConsumer -= value;
                _recordedRecoveringConsumerEventHandlers -= value;
            }
            finally
            {
                ExitLock();
            }            
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected override void AddEventHandlers(IConnection connection)
    {
        base.AddEventHandlers(connection);
        if (Connection is not IAutorecoveringConnection recoverableConnection)
        {
            return;
        }
        recoverableConnection.ConnectionRecoveryError += _recordedConnectionRecoveryErrorEventHandlers;
        recoverableConnection.ConsumerTagChangeAfterRecovery += _recordedConsumerTagChangeAfterRecoveryEventHandlers;
        recoverableConnection.RecoveringConsumer += _recordedRecoveringConsumerEventHandlers;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected override void RemoveEventHandlers(IConnection connection)
    {
        base.RemoveEventHandlers(connection);
        if (Connection is not IAutorecoveringConnection recoverableConnection)
        {
            return;
        }
        recoverableConnection.ConnectionRecoveryError -= _recordedConnectionRecoveryErrorEventHandlers;
        recoverableConnection.ConsumerTagChangeAfterRecovery -= _recordedConsumerTagChangeAfterRecoveryEventHandlers;
        recoverableConnection.RecoveringConsumer -= _recordedRecoveringConsumerEventHandlers;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        _recordedConnectionRecoveryErrorEventHandlers = null;
        _recordedConsumerTagChangeAfterRecoveryEventHandlers = null;
        _recordedRecoveringConsumerEventHandlers = null;
    }
}