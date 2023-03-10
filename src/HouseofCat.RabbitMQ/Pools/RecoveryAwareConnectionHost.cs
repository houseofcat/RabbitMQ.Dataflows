using System;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client;

namespace HouseofCat.RabbitMQ.Pools;

public class RecoveryAwareConnectionHost : IConnectionHost, IDisposable
{
    public IConnection Connection => _connHost.Connection;
    public ulong ConnectionId => _connHost.ConnectionId;
    public bool Blocked => _connHost.Blocked;
    public bool Dead => _connHost.Dead;
    public bool Closed => _connHost.Closed;

    private readonly SemaphoreSlim _hostLock = new(1, 1);
    private IAutorecoveringConnection _connection;
    private IConnectionHost _connHost;
    private bool? _recovered;
    private bool _disposedValue;

    public RecoveryAwareConnectionHost(IConnectionHost connHost)
    {
        _connHost = connHost;
        AssignConnection(connection: null);
    }

    public void AssignConnection(IConnection connection = null)
    {
        _hostLock.Wait();

        if (_connection != null)
        {
            _connection.ConnectionShutdown -= ConnectionClosed;
            _connection.RecoverySucceeded -= ConnectionRecovered;
            _connection = null;
        }

        if (connection is not null)
        {
            _connHost.AssignConnection(connection);
        }

        if (Connection is IAutorecoveringConnection autoRecoveringConnection)
        {
            _connection = autoRecoveringConnection;
            _connection.ConnectionShutdown += ConnectionClosed;
            _connection.RecoverySucceeded += ConnectionRecovered;
        }

        _hostLock.Release();
    }

    public void Close() => _connHost.Close();

    public async Task<bool> HealthyAsync()
    {
        await _hostLock
            .WaitAsync()
            .ConfigureAwait(false);

        try
        {
            return _recovered != false && await _connHost.HealthyAsync().ConfigureAwait(false);
        }
        finally
        {
            _hostLock.Release();
        }
    }

    private void ConnectionClosed(object sender, ShutdownEventArgs e)
    {
        _hostLock.Wait();
        _recovered = false;
        _hostLock.Release();
    }

    private void ConnectionRecovered(object sender, EventArgs e)
    {
        _hostLock.Wait();
        _recovered = true;
        _hostLock.Release();
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposedValue)
        {
            return;
        }

        if (disposing)
        {
            if (_connHost is IDisposable disposableConnHost)
            {
                disposableConnHost.Dispose();
            }
            _hostLock.Dispose();
        }

        _connection = null;
        _connHost = null;
        _disposedValue = true;
    }

    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}