using System;
using System.Collections.Generic;
using System.Collections.Immutable;
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
            var queue = await GetQueue(queueName).ConfigureAwait(false);
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

    private async Task<ImmutableArray<Connection>> GetActiveConnections(string queueName)
    {
        var allConnections = await GetConnections().ConfigureAwait(false);
        if (allConnections.Length == 0)
        {
            return allConnections;
        }

        var activeChannels = await GetActiveChannelDetails(queueName).ConfigureAwait(false);
        if (activeChannels.Count == 0)
        {
            return ImmutableArray<Connection>.Empty;
        }

        var connectionsByPeer = allConnections.ToDictionary(c => $"{c.PeerHost}:{c.PeerPort}", c => c);
        return activeChannels
            .Select(ch => $"{ch.PeerHost}:{ch.PeerPort}")
            .Distinct()
            .Select(peer => connectionsByPeer.TryGetValue(peer, out var connection) ? connection : null)
            .Where(connection => connection is not null)
            .ToImmutableArray();
    }

    private async Task<ImmutableArray<Connection>> GetConnections()
    {
        try
        {
            var connections = await _client.GetConnectionsAsync().ConfigureAwait(false);
            return connections.Where(c => c.Vhost == _vhost).ToImmutableArray();
        }
        catch
        {
            return ImmutableArray<Connection>.Empty;
        }
    }

    private Task<Queue> GetQueue(string queueName) => _client.GetQueueAsync(_vhost, queueName);
    public Task ClearQueue(string queueName) => _client.PurgeAsync(_vhost, queueName);

    public async ValueTask CloseActiveConnections(string queueName, IEnumerable<Connection> activeConnections)
    {
        await Task.WhenAll(activeConnections.Select(CloseConnection)).ConfigureAwait(false);
        await new Wait(TimeSpan.FromSeconds(10)).UntilAsync(
            async () => (await GetActiveConnections(queueName).ConfigureAwait(false)).Length,
            0, "active connection(s)", _output).ConfigureAwait(false);
    }

    public async ValueTask<ImmutableArray<Connection>> RecoverConnectionsAndConsumers(
        string queueName, ImmutableArray<Connection> activeConnections, int consumerCount, bool clearQueue = false)
    {
        await CloseActiveConnections(queueName, activeConnections).ConfigureAwait(false);
        await WaitForQueueToHaveNoConsumers(queueName).ConfigureAwait(false);
        if (clearQueue)
        {
            await ClearQueue(queueName).ConfigureAwait(false);
            await WaitForQueueToHaveNoMessages(queueName).ConfigureAwait(false);
        }
        return await WaitForConnectionsAndConsumers(queueName, consumerCount).ConfigureAwait(false);
    }

    public async ValueTask<ImmutableArray<Connection>> WaitForActiveConnections(string queueName)
    {
        var activeConnections = ImmutableArray<Connection>.Empty;
        await new Wait().UntilAsync(
            async () =>
            {
                activeConnections = await GetActiveConnections(queueName).ConfigureAwait(false);
                return activeConnections.Length;
            }, 1, "active connection(s)", _output);
        return activeConnections;
    }

    public async ValueTask<ImmutableArray<Connection>> WaitForConnectionsAndConsumers(
        string queueName, int consumerCount)
    {
        var connections = await WaitForActiveConnections(queueName).ConfigureAwait(false);
        if (connections.Length == 0)
        {
            return connections;
        }
        await WaitForQueueToHaveConsumers(queueName, consumerCount).ConfigureAwait(false);
        return connections;
    }

    public ValueTask<bool> WaitForQueueToHaveConsumers(
        string queueName, int expectedConsumerCount, bool throwOnTimeout = true) =>
        new Wait().UntilAsync(
            async () =>
            {
                try
                {
                    var queue = await GetQueue(queueName).ConfigureAwait(false);
                    return Convert.ToInt32(queue.Consumers);
                }
                catch
                {
                    return 0;
                }
            },
            expectedConsumerCount, $"consumer(s) on {queueName}", _output, throwOnTimeout);

    public ValueTask<bool> WaitForQueueToHaveNoConsumers(string queueName) => WaitForQueueToHaveConsumers(queueName, 0);

    public ValueTask<bool> WaitForQueueToHaveNoMessages(string queueName, bool throwOnTimeout = true) =>
        new Wait().UntilAsync(
            async () =>
            {
                try
                {
                    var queue = await GetQueue(queueName).ConfigureAwait(false);
                    return Convert.ToInt32(queue.Messages);
                }
                catch
                {
                    return 0;
                }
            },
            0, $"messages on {queueName}", _output, throwOnTimeout);

    public ValueTask<bool> WaitForQueueToHaveUnacknowledgedMessages(
        string queueName, int unacknowledgedCount, bool throwOnTimeout = true) =>
        new Wait().UntilAsync(
            async () => Convert.ToInt32((await GetQueue(queueName).ConfigureAwait(false)).MessagesUnacknowledged),
            unacknowledgedCount, $"unacknowledged on {queueName}", _output, throwOnTimeout);
}