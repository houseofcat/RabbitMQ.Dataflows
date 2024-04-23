using HouseofCat.Utilities.Helpers;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;

namespace HouseofCat.RabbitMQ.Pools;

public interface IConnectionHost
{
    IConnection Connection { get; }
    ulong ConnectionId { get; }

    bool Blocked { get; }
    bool Dead { get; }
    bool Closed { get; }

    void AssignConnection(IConnection connection);
    void Close();
    bool Healthy();
}

public class ConnectionHost : IConnectionHost, IDisposable
{
    public IConnection Connection { get; private set; }
    public ulong ConnectionId { get; }

    public bool Blocked { get; private set; }
    public bool Dead { get; private set; }
    public bool Closed { get; private set; }

    private readonly ILogger<ConnectionHost> _logger;

    public ConnectionHost(ulong connectionId, IConnection connection)
    {
        _logger = LogHelpers.GetLogger<ConnectionHost>();
        ConnectionId = connectionId;

        AssignConnection(connection);
    }

    public void AssignConnection(IConnection connection)
    {
        if (Connection != null)
        {
            Connection.ConnectionBlocked -= ConnectionBlocked;
            Connection.ConnectionUnblocked -= ConnectionUnblocked;
            Connection.ConnectionShutdown -= ConnectionClosed;

            try
            { Close(); }
            catch { /* SWALLOW */ }

            Connection = null;
        }

        Connection = connection;

        Connection.ConnectionBlocked += ConnectionBlocked;
        Connection.ConnectionUnblocked += ConnectionUnblocked;
        Connection.ConnectionShutdown += ConnectionClosed;
    }

    protected virtual void ConnectionClosed(object sender, ShutdownEventArgs e)
    {
        _logger.LogWarning(e.ReplyText);
        Closed = true;
    }

    protected virtual void ConnectionBlocked(object sender, ConnectionBlockedEventArgs e)
    {
        _logger.LogWarning(e.Reason);
        Blocked = true;
    }

    protected virtual void ConnectionUnblocked(object sender, EventArgs e)
    {
        _logger.LogInformation("Connection unblocked!");
        Blocked = false;
    }

    private const int CloseCode = 200;
    private const string CloseMessage = "HouseofCat.RabbitMQ manual close initiated.";

    public void Close() => Connection.Close(CloseCode, CloseMessage);

    /// <summary>
    /// Due to the complexity of the RabbitMQ Dotnet Client there are a few odd scenarios.
    /// Just casually check Health() when looping through Connections, skip when not Healthy.
    /// <para>AutoRecovery = False yields results like Closed, Dead, and IsOpen will be true, true, false or false, false, true.</para>
    /// <para>AutoRecovery = True, yields difficult results like Closed, Dead, And IsOpen will be false, false, false or true, true, true (and other variations).</para>
    /// </summary>
    public bool Healthy()
    {
        var connectionOpen = (Connection?.IsOpen ?? false);
        if (Closed && connectionOpen)
        { Closed = false; } // Means a Recovery took place.
        else if (Dead && connectionOpen)
        { Dead = false; } // Means a Miracle took place.

        return connectionOpen && !Blocked; // TODO: See if we can incorporate Dead/Closed observations.
    }

    private bool _disposedValue;

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposedValue)
        {
            Connection = null;
            _disposedValue = true;
        }
    }

    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
