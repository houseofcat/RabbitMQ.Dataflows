using System;
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
    public ConnectionHost(ulong connectionId, IConnection connection) : base(connectionId, connection) { }

    public event EventHandler<ConnectionRecoveryErrorEventArgs> ConnectionRecoveryError
    {
        add
        {
            EnterLock();
            try
            {
                if (Connection is IAutorecoveringConnection recoverableConnection)
                {
                    recoverableConnection.ConnectionRecoveryError += value;
                }
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
                if (Connection is IAutorecoveringConnection recoverableConnection)
                {
                    recoverableConnection.ConnectionRecoveryError -= value;
                }
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
                if (Connection is IAutorecoveringConnection recoverableConnection)
                {
                    recoverableConnection.ConsumerTagChangeAfterRecovery += value;
                }
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
                if (Connection is IAutorecoveringConnection recoverableConnection)
                {
                    recoverableConnection.ConsumerTagChangeAfterRecovery -= value;
                }
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
                if (Connection is IAutorecoveringConnection recoverableConnection)
                {
                    recoverableConnection.RecoveringConsumer += value;
                }
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
                if (Connection is IAutorecoveringConnection recoverableConnection)
                {
                    recoverableConnection.RecoveringConsumer -= value;
                }
            }
            finally
            {
                ExitLock();
            }            
        }
    }
}