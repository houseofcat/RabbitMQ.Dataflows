using HouseofCat.Network;
using System;
using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace HouseofCat.Sockets;

public interface IQuickSocketFactory
{
    IDnsCaching DnsCaching { get; }

    ValueTask<IQuickListeningSocket> GetListeningTcpSocketAsync(string hostNameOrAddresss, int bindingPort, bool overideAsLocal = false, bool verbatimAddress = false);
    ValueTask<IQuickListeningSocket> GetListeningUdpSocketAsync(string hostNameOrAddresss, int bindingPort, bool overideAsLocal = false, bool verbatimAddress = false);
    ValueTask<IQuickSocket> GetTcpSocketAsync(string hostNameOrAddresss, int bindingPort, bool overideAsLocal = false, bool verbatimAddress = false);
    ValueTask<IQuickSocket> GetUdpSocketAsync(string hostNameOrAddresss, int bindingPort, bool overideAsLocal = false, bool verbatimAddress = false);
}

public class QuickSocketFactory : IQuickSocketFactory
{
    public IDnsCaching DnsCaching { get; }
    private ConcurrentDictionary<string, QuickSocket> Sockets { get; }
    private ConcurrentDictionary<string, QuickListeningSocket> ListeningSockets { get; }

    private const string SocketKeyFormat = "{0}{1}:{2}:{3}";
    private const int CachingExpiryInHours = 1;

    public QuickSocketFactory()
    {
        DnsCaching = new DnsCaching(TimeSpan.FromHours(CachingExpiryInHours));
        Sockets = new ConcurrentDictionary<string, QuickSocket>();
        ListeningSockets = new ConcurrentDictionary<string, QuickListeningSocket>();
    }

    private string GetSocketKey(ProtocolType protocolType, SocketType socketType, string hostNameOrAddresss, int bindingPort)
    {
        return string.Format(SocketKeyFormat, protocolType, socketType, hostNameOrAddresss, bindingPort);
    }

    public async ValueTask<IQuickSocket> GetTcpSocketAsync(string hostNameOrAddresss, int bindingPort, bool overideAsLocal = false, bool verbatimAddress = false)
    {
        var key = GetSocketKey(ProtocolType.Tcp, SocketType.Stream, hostNameOrAddresss, bindingPort);

        if (Sockets.ContainsKey(key))
        {
            return Sockets[key];
        }
        else
        {
            var dnsEntry = await DnsCaching
                .GetDnsEntryAsync(hostNameOrAddresss, bindingPort, overideAsLocal, verbatimAddress)
                .ConfigureAwait(false);

            var quickSocket = new QuickSocket
            {
                Socket = new Socket(dnsEntry.PrimaryAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp),
                DnsEntry = dnsEntry,
            };

            Sockets[key] = quickSocket;

            return quickSocket;
        }
    }

    public async ValueTask<IQuickSocket> GetUdpSocketAsync(string hostNameOrAddresss, int bindingPort, bool overideAsLocal = false, bool verbatimAddress = false)
    {
        var key = GetSocketKey(ProtocolType.Udp, SocketType.Stream, hostNameOrAddresss, bindingPort);

        if (Sockets.ContainsKey(key))
        {
            return Sockets[key];
        }
        else
        {
            var dnsEntry = await DnsCaching
                .GetDnsEntryAsync(hostNameOrAddresss, bindingPort, overideAsLocal, verbatimAddress)
                .ConfigureAwait(false);

            var quickSocket = new QuickSocket
            {
                Socket = new Socket(dnsEntry.PrimaryAddress.AddressFamily, SocketType.Stream, ProtocolType.Udp),
                DnsEntry = dnsEntry,
            };

            Sockets[key] = quickSocket;

            return quickSocket;
        }
    }

    public async ValueTask<IQuickListeningSocket> GetListeningTcpSocketAsync(string hostNameOrAddresss, int bindingPort, bool overideAsLocal = false, bool verbatimAddress = false)
    {
        var key = GetSocketKey(ProtocolType.Tcp, SocketType.Stream, hostNameOrAddresss, bindingPort);

        if (ListeningSockets.ContainsKey(key))
        {
            return ListeningSockets[key];
        }
        else
        {
            var dnsEntry = await DnsCaching
                .GetDnsEntryAsync(hostNameOrAddresss, bindingPort, overideAsLocal, verbatimAddress)
                .ConfigureAwait(false);

            var quickListeningSocket = new QuickListeningSocket
            {
                Socket = new Socket(dnsEntry.PrimaryAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp),
                DnsEntry = dnsEntry,
            };

            ListeningSockets[key] = quickListeningSocket;

            return quickListeningSocket;
        }
    }

    public async ValueTask<IQuickListeningSocket> GetListeningUdpSocketAsync(string hostNameOrAddresss, int bindingPort, bool overideAsLocal = false, bool verbatimAddress = false)
    {
        var key = GetSocketKey(ProtocolType.Udp, SocketType.Stream, hostNameOrAddresss, bindingPort);

        if (ListeningSockets.ContainsKey(key))
        {
            return ListeningSockets[key];
        }
        else
        {
            var dnsEntry = await DnsCaching
                .GetDnsEntryAsync(hostNameOrAddresss, bindingPort, overideAsLocal, verbatimAddress)
                .ConfigureAwait(false);

            var quickListeningSocket = new QuickListeningSocket
            {
                Socket = new Socket(dnsEntry.PrimaryAddress.AddressFamily, SocketType.Stream, ProtocolType.Udp),
                DnsEntry = dnsEntry,
            };

            ListeningSockets[key] = quickListeningSocket;

            return quickListeningSocket;
        }
    }
}
