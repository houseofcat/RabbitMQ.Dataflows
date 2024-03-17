using HouseofCat.Network;
using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace HouseofCat.Sockets;

public interface IQuickSocket
{
    bool Connected { get; }
    DnsEntry DnsEntry { get; set; }
    Socket Socket { get; set; }

    Task ConnectToAddressesAsync();
    Task ConnectToPrimaryAddressAsync();
    Task ShutdownAsync();
}

public class QuickSocket : IQuickSocket
{
    public DnsEntry DnsEntry { get; set; }
    public Socket Socket { get; set; }
    private SemaphoreSlim SockLock { get; } = new SemaphoreSlim(1, 1);

    public bool Connected { get; private set; }

    private const string SocketNullErrorMessage = "Can't complete request because the Socket is null.";

    public async Task ConnectToPrimaryAddressAsync()
    {
        if (Socket == null) throw new InvalidOperationException(SocketNullErrorMessage);

        await SockLock.WaitAsync().ConfigureAwait(false);
        if (!Connected)
        {
            await Socket
                .ConnectAsync(DnsEntry.PrimaryAddress, DnsEntry.Port)
                .ConfigureAwait(false);

            Socket.NoDelay = true;

            Connected = true;
        }
        SockLock.Release();
    }

    public async Task ConnectToAddressesAsync()
    {
        if (Socket == null) throw new InvalidOperationException(SocketNullErrorMessage);

        await SockLock.WaitAsync().ConfigureAwait(false);
        if (!Connected)
        {
            await Socket
                .ConnectAsync(DnsEntry.Addresses, DnsEntry.Port)
                .ConfigureAwait(false);

            Socket.NoDelay = true;

            Connected = true;
        }
        SockLock.Release();
    }

    public async Task ShutdownAsync()
    {
        await SockLock.WaitAsync().ConfigureAwait(false);
        if (Connected)
        {
            Socket.Shutdown(SocketShutdown.Send);
            Socket.Close();
            Connected = false;
        }
        SockLock.Release();
    }
}
