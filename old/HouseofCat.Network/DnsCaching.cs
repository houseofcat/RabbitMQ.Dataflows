using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using System;
using System.Net;
using System.Threading.Tasks;

namespace HouseofCat.Network;

public interface IDnsCaching
{
    ValueTask<DnsEntry> GetDnsEntryAsync(string hostNameOrAddresss, int bindingPort, bool overideAsLocal, bool verbatimAddress);
    void RemoveDnsEntry(string hostNameOrAddresss, int bindingPort);
}

public class DnsCaching : IDnsCaching
{
    private readonly TimeSpan _expiration;
    private readonly MemoryCache _memoryCache = new MemoryCache(Options.Create(new MemoryCacheOptions()));
    private const string DnsKeyFormat = "{0}:{1}";

    public DnsCaching(TimeSpan expiration)
    {
        _expiration = expiration;
    }

    private string GetDnsKey(string hostName, int bindingPort)
    {
        return string.Format(DnsKeyFormat, hostName, bindingPort);
    }

    public async ValueTask<DnsEntry> GetDnsEntryAsync(string hostNameOrAddresss, int bindingPort, bool overideAsLocal, bool verbatimAddress)
    {
        // See if we already have a cached DnsEntry
        var key = GetDnsKey(hostNameOrAddresss, bindingPort);

        var dnsEntry = _memoryCache.Get<DnsEntry>(key);
        if (dnsEntry != null)
        {
            return dnsEntry;
        }
        else // Else build one, cache it, and return.
        {
            dnsEntry = new DnsEntry
            {
                HostName = hostNameOrAddresss,
                Port = bindingPort,
                Addresses = await Dns.GetHostAddressesAsync(hostNameOrAddresss).ConfigureAwait(false),
            };

            if (overideAsLocal)
            {
                dnsEntry.PrimaryAddress = IPAddress.Loopback;
                dnsEntry.Endpoint = new IPEndPoint(IPAddress.Loopback, bindingPort);
            }
            else if (verbatimAddress)
            {
                // Find verbatim IP address match based on the hostname or address.
                for (int i = 0; i < dnsEntry.Addresses.Length; i++)
                {
                    if (dnsEntry.Addresses[i].ToString() == hostNameOrAddresss)
                    {
                        dnsEntry.Endpoint = new IPEndPoint(dnsEntry.Addresses[i], bindingPort);
                        break;
                    }
                }
            }
            else
            {
                // Find first non-Loopback address for PrimaryAddress.
                for (int i = 0; i < dnsEntry.Addresses.Length; i++)
                {
                    if (!IPAddress.IsLoopback(dnsEntry.Addresses[i]))
                    {
                        dnsEntry.Endpoint = new IPEndPoint(dnsEntry.Addresses[i], bindingPort);
                        break;
                    }
                }
            }

            _memoryCache.Set(key, dnsEntry, new MemoryCacheEntryOptions { AbsoluteExpirationRelativeToNow = _expiration });

            return dnsEntry;
        }
    }

    public void RemoveDnsEntry(string hostNameOrAddresss, int bindingPort)
    {
        // See if we already have a cached DnsEntry
        var key = GetDnsKey(hostNameOrAddresss, bindingPort);

        _memoryCache.Remove(key);
    }
}
