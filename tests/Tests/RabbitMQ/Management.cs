using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EasyNetQ.Management.Client;
using EasyNetQ.Management.Client.Model;
using HouseofCat.RabbitMQ;
using Utilities;
using Xunit.Abstractions;

namespace IntegrationTests.RabbitMQ;

public class Management
{
    private readonly ManagementClient _client;
    private readonly string _vhost;
    private readonly ITestOutputHelper _output;

    public Management(FactoryOptions factoryOptions, ITestOutputHelper output)
    {
        if (factoryOptions.Uri is not null)
        {
            var managementUrl = $"http://{factoryOptions.Uri.Host}:{factoryOptions.Uri.Port + 10000}";
            if (!Uri.TryCreate(managementUrl, UriKind.Absolute, out var managementUri))
                throw new InvalidOperationException($"Could not parse rabbit URI \'{managementUrl}\'");
            var userInfo = factoryOptions.Uri.UserInfo.Split(':');
            _client = new ManagementClient(managementUri, userInfo[0], userInfo[1]);
            _vhost = 
                factoryOptions.Uri.AbsolutePath == "/"
                    ? factoryOptions.Uri.AbsolutePath
                    : factoryOptions.Uri.AbsolutePath[1..];
        }
        else
        {
            var managementUrl = $"http://{factoryOptions.HostName}:{factoryOptions.Port + 10000}";
            if (!Uri.TryCreate(managementUrl, UriKind.Absolute, out var managementUri))
                throw new InvalidOperationException($"Could not parse rabbit URI \'{managementUrl}\'");
            _client = new ManagementClient(managementUri, factoryOptions.UserName, factoryOptions.Password);
            _vhost = factoryOptions.VirtualHost;
        }

        _output = output;
    }

    private Task CloseConnection(Connection connection) => _client.CloseConnectionAsync(connection);

    private async Task<ICollection<ChannelDetail>> GetActiveChannelDetails(string queueName)
    {
        try
        {
            var queue = await GetQueue(queueName);
            return 
                queue.ConsumerDetails?.Count > 0
                    ? queue.ConsumerDetails
                        .Where(d => d.ChannelDetails is not null).Select(d => d.ChannelDetails).ToArray()
                    : Array.Empty<ChannelDetail>();

        }
        catch
        {
            return Array.Empty<ChannelDetail>();
        }
    }

    private async Task<IReadOnlyCollection<Connection>> GetActiveConnections(string queueName)
    {
        var allConnections = await GetConnections();
        if (allConnections.Count == 0)
        {
            return allConnections;
        }

        var activeChannels = await GetActiveChannelDetails(queueName);
        if (activeChannels.Count == 0)
        {
            return Array.Empty<Connection>();
        }

        var connectionsByPeer = allConnections.ToDictionary(c => $"{c.PeerHost}:{c.PeerPort}", c => c);
        return activeChannels
            .Select(ch => $"{ch.PeerHost}:{ch.PeerPort}")
            .Distinct()
            .Select(peer => connectionsByPeer.TryGetValue(peer, out var connection) ? connection : null)
            .Where(connection => connection is not null)
            .ToArray();
    }

    private async Task<IReadOnlyCollection<Connection>> GetConnections()
    {
        try
        {
            var connections = await _client.GetConnectionsAsync().ConfigureAwait(false);
            return connections.Where(c => c.Vhost == _vhost).ToArray();
        }
        catch
        {
            return Array.Empty<Connection>();
        }
    }

    private Task<Queue> GetQueue(string queueName) => _client.GetQueueAsync(_vhost, queueName);

    public async ValueTask CloseActiveConnections(string queueName, IEnumerable<Connection> activeConnections)
    {
        await Task.WhenAll(activeConnections.Select(CloseConnection));
        await new Wait(TimeSpan.FromSeconds(10), TimeSpan.FromMilliseconds(50)).UntilAsync(
            async () => (await GetActiveConnections(queueName)).Count, 0, "active connection(s)", _output);
    }

    public async Task<IReadOnlyCollection<Channel>> GetChannels(bool throwOnError = false)
    {
        try
        {
            var channels = await _client.GetChannelsAsync().ConfigureAwait(false);
            return channels.Where(c => c.Vhost == _vhost).ToArray();
        }
        catch
        {
            if (throwOnError)
            {
                throw;
            }

            return Array.Empty<Channel>();
        }
    }

    public Task ClearQueue(string queueName) => _client.PurgeAsync(_vhost, queueName);

    public async ValueTask<IReadOnlyCollection<Connection>> WaitForActiveConnections(string queueName)
    {
        IReadOnlyCollection<Connection> activeConnections = null;
        await new Wait(TimeSpan.FromSeconds(20), TimeSpan.FromMilliseconds(100)).UntilAsync(
            async () =>
            {
                activeConnections = await GetActiveConnections(queueName);
                return activeConnections.Count;
            }, 1, "active connection(s)", _output, false);
        return activeConnections;
    }

    public ValueTask<bool> WaitForQueueToHaveConsumers(
        string queueName, int expectedConsumerCount, bool throwOnTimeout = true) =>
        new Wait(TimeSpan.FromSeconds(15), TimeSpan.FromMilliseconds(50)).UntilAsync(
            async () =>
            {
                try
                {
                    var queue = await GetQueue(queueName);
                    return Convert.ToInt32(queue.Consumers);
                }
                catch
                {
                    return 0;
                }
            },
            expectedConsumerCount, $"consumer(s) on {queueName}", _output, throwOnTimeout);

    public ValueTask<bool> WaitForQueueToHaveNoConsumers(string queueName) => WaitForQueueToHaveConsumers(queueName, 0);

    public ValueTask<bool> WaitForQueueToHaveNoMessages(string queueName, double timeout, double interval) =>
        new Wait(TimeSpan.FromSeconds(timeout), TimeSpan.FromMilliseconds(interval)).UntilAsync(
            async () =>
            {
                try
                {
                    var queue = await GetQueue(queueName);
                    return Convert.ToInt32(queue.Messages);
                }
                catch
                {
                    return 0;
                }
            },
            0, $"messages on {queueName}", _output);

    public ValueTask<bool> WaitForQueueToHaveNoUnacknowledgedMessages(string queueName, bool throwOnTimeout = true) =>
        WaitForQueueToHaveUnacknowledgedMessages(queueName, 0, 15, 50, throwOnTimeout);

    public ValueTask<bool> WaitForQueueToHaveUnacknowledgedMessages(
        string queueName, int unacknowledgedCount, double timeout, double interval, bool throwOnTimeout = true) =>
        new Wait(TimeSpan.FromSeconds(timeout), TimeSpan.FromMilliseconds(interval)).UntilAsync(
            async () => Convert.ToInt32((await GetQueue(queueName)).MessagesUnacknowledged),
            unacknowledgedCount, $"unacknowledged on {queueName}", _output, throwOnTimeout);
}