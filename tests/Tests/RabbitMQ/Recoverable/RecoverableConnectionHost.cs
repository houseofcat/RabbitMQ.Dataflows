using System;
using HouseofCat.RabbitMQ.Pools;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace IntegrationTests.RabbitMQ.Recoverable;

public interface IRecoverableConnectionHost : IConnectionHost
{
    event EventHandler<ConnectionRecoveryErrorEventArgs> ConnectionRecoveryError;
    event EventHandler<ConsumerTagChangedAfterRecoveryEventArgs> ConsumerTagChangeAfterRecovery;
    event EventHandler<RecoveringConsumerEventArgs> RecoveringConsumer;
}

public class RecoverableConnectionHost : ConnectionHost, IRecoverableConnectionHost
{
    public RecoverableConnectionHost(ulong connectionId, IConnection connection) : base(connectionId, connection) { }

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