using System.Net;

namespace HouseofCat.Network;

public class DnsEntry
{
    public string HostName { get; set; }
    public IPAddress PrimaryAddress { get; set; }
    public IPAddress[] Addresses { get; set; }
    public int Port { get; set; }
    public EndPoint Endpoint { get; set; }
}
