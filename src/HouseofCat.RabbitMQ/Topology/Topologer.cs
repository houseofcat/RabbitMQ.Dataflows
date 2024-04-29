using HouseofCat.RabbitMQ.Pools;
using HouseofCat.Utilities;
using HouseofCat.Utilities.Errors;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HouseofCat.RabbitMQ;

public interface ITopologer
{
    RabbitOptions Options { get; }

    Task<bool> BindExchangeToExchangeAsync(string childExchangeName, string parentExchangeName, string routingKey = "", IDictionary<string, object> args = null);
    Task<bool> BindQueueToExchangeAsync(string queueName, string exchangeName, string routingKey = "", IDictionary<string, object> args = null);
    Task<bool> CreateExchangeAsync(string exchangeName, string exchangeType, bool durable = true, bool autoDelete = false, IDictionary<string, object> args = null);
    Task<bool> CreateQueueAsync(string queueName, bool durable = true, bool exclusive = false, bool autoDelete = false, IDictionary<string, object> args = null);
    Task CreateTopologyAsync(TopologyConfig topologyConfig);
    Task CreateTopologyFromFileAsync(string fileNamePath);
    Task<bool> DeleteExchangeAsync(string exchangeName, bool onlyIfUnused = false);
    Task<bool> DeleteQueueAsync(string queueName, bool onlyIfUnused = false, bool onlyIfEmpty = false);
    Task<bool> UnbindExchangeFromExchangeAsync(string childExchangeName, string parentExchangeName, string routingKey = "", IDictionary<string, object> args = null);
    Task<bool> UnbindQueueFromExchangeAsync(string queueName, string exchangeName, string routingKey = "", IDictionary<string, object> args = null);
}

public sealed class Topologer : ITopologer
{
    private readonly IChannelPool _channelPool;
    public RabbitOptions Options { get; }

    public Topologer(RabbitOptions options) : this(new ChannelPool(options))
    { }

    public Topologer(IChannelPool channelPool)
    {
        Guard.AgainstNull(channelPool, nameof(channelPool));

        Options = channelPool.Options;
        _channelPool = channelPool;
    }

    public async Task CreateTopologyAsync(TopologyConfig topologyConfig)
    {
        Guard.AgainstNull(topologyConfig, nameof(topologyConfig));

        if (topologyConfig.Exchanges is not null)
        {
            for (var i = 0; i < topologyConfig.Exchanges.Length; i++)
            {
                try
                {
                    await CreateExchangeAsync(
                        topologyConfig.Exchanges[i].Name,
                        topologyConfig.Exchanges[i].Type,
                        topologyConfig.Exchanges[i].Durable,
                        topologyConfig.Exchanges[i].AutoDelete,
                        topologyConfig.Exchanges[i].Args).ConfigureAwait(false);
                }
                catch { /* SWALLOW */ }
            }
        }

        if (topologyConfig.Queues is not null)
        {
            for (var i = 0; i < topologyConfig.Queues.Length; i++)
            {
                try
                {
                    await CreateQueueAsync(
                        topologyConfig.Queues[i].Name,
                        topologyConfig.Queues[i].Durable,
                        topologyConfig.Queues[i].Exclusive,
                        topologyConfig.Queues[i].AutoDelete,
                        topologyConfig.Queues[i].Args).ConfigureAwait(false);
                }
                catch { /* SWALLOW */ }
            }
        }

        if (topologyConfig.ExchangeBindings is not null)
        {
            for (var i = 0; i < topologyConfig.ExchangeBindings.Length; i++)
            {
                try
                {
                    await BindExchangeToExchangeAsync(
                        topologyConfig.ExchangeBindings[i].ChildExchange,
                        topologyConfig.ExchangeBindings[i].ParentExchange,
                        topologyConfig.ExchangeBindings[i].RoutingKey,
                        topologyConfig.ExchangeBindings[i].Args).ConfigureAwait(false);
                }
                catch { /* SWALLOW */ }
            }
        }

        if (topologyConfig.QueueBindings is not null)
        {
            for (var i = 0; i < topologyConfig.QueueBindings.Length; i++)
            {
                try
                {
                    await BindQueueToExchangeAsync(
                        topologyConfig.QueueBindings[i].QueueName,
                        topologyConfig.QueueBindings[i].ExchangeName,
                        topologyConfig.QueueBindings[i].RoutingKey,
                        topologyConfig.QueueBindings[i].Args).ConfigureAwait(false);
                }
                catch { /* SWALLOW */ }
            }
        }
    }

    public async Task CreateTopologyFromFileAsync(string fileNamePath)
    {
        if (string.IsNullOrWhiteSpace(fileNamePath)) throw new ArgumentNullException(nameof(fileNamePath));

        var config = await JsonFileReader
            .ReadFileAsync<TopologyConfig>(fileNamePath)
            .ConfigureAwait(false);

        await CreateTopologyAsync(config).ConfigureAwait(false);
    }

    /// <summary>
    /// Create a queue asynchronously.
    /// <para>Returns success or failure.</para>
    /// </summary>
    /// <param name="queueName"></param>
    /// <param name="durable"></param>
    /// <param name="exclusive"></param>
    /// <param name="autoDelete"></param>
    /// <param name="args"></param>
    /// <returns>A bool indicating failure.</returns>
    public async Task<bool> CreateQueueAsync(
        string queueName,
        bool durable = true,
        bool exclusive = false,
        bool autoDelete = false,
        IDictionary<string, object> args = null)
    {
        Guard.AgainstNullOrEmpty(queueName, nameof(queueName));

        var error = false;
        var chanHost = await _channelPool.GetChannelAsync().ConfigureAwait(false);

        try
        {
            chanHost.Channel.QueueDeclare(
                queue: queueName,
                durable: durable,
                exclusive: exclusive,
                autoDelete: autoDelete,
                arguments: args);
        }
        catch { error = true; }
        finally { await _channelPool.ReturnChannelAsync(chanHost, error).ConfigureAwait(false); }

        return error;
    }

    /// <summary>
    /// Delete a queue asynchronously.
    /// <para>Returns success or failure.</para>
    /// </summary>
    /// <param name="queueName"></param>
    /// <param name="onlyIfUnused"></param>
    /// <param name="onlyIfEmpty"></param>
    /// <returns>A bool indicating failure.</returns>
    public async Task<bool> DeleteQueueAsync(
        string queueName,
        bool onlyIfUnused = false,
        bool onlyIfEmpty = false)
    {
        Guard.AgainstNullOrEmpty(queueName, nameof(queueName));

        var error = false;
        var chanHost = await _channelPool.GetChannelAsync().ConfigureAwait(false);

        try
        {
            chanHost.Channel.QueueDelete(
                queue: queueName,
                ifUnused: onlyIfUnused,
                ifEmpty: onlyIfEmpty);
        }
        catch { error = true; }
        finally { await _channelPool.ReturnChannelAsync(chanHost, error).ConfigureAwait(false); }

        return error;
    }

    /// <summary>
    /// Bind a queue to exchange asynchronously.
    /// <para>Returns success or failure.</para>
    /// </summary>
    /// <param name="queueName"></param>
    /// <param name="exchangeName"></param>
    /// <param name="routingKey"></param>
    /// <param name="args"></param>
    /// <returns>A bool indicating failure.</returns>
    public async Task<bool> BindQueueToExchangeAsync(
        string queueName,
        string exchangeName,
        string routingKey = "",
        IDictionary<string, object> args = null)
    {
        Guard.AgainstNullOrEmpty(exchangeName, nameof(exchangeName));
        Guard.AgainstNullOrEmpty(queueName, nameof(queueName));

        var error = false;
        var chanHost = await _channelPool.GetChannelAsync().ConfigureAwait(false);

        try
        {
            chanHost.Channel.QueueBind(
                queue: queueName,
                exchange: exchangeName,
                routingKey: routingKey,
                arguments: args);
        }
        catch { error = true; }
        finally { await _channelPool.ReturnChannelAsync(chanHost, error).ConfigureAwait(false); }

        return error;
    }

    /// <summary>
    /// Unbind a queue from Exchange asynchronously.
    /// <para>Returns success or failure.</para>
    /// </summary>
    /// <param name="queueName"></param>
    /// <param name="exchangeName"></param>
    /// <param name="routingKey"></param>
    /// <param name="args"></param>
    /// <returns>A bool indicating failure.</returns>
    public async Task<bool> UnbindQueueFromExchangeAsync(
        string queueName,
        string exchangeName,
        string routingKey = "",
        IDictionary<string, object> args = null)
    {
        Guard.AgainstNullOrEmpty(exchangeName, nameof(exchangeName));
        Guard.AgainstNullOrEmpty(queueName, nameof(queueName));

        var error = false;
        var chanHost = await _channelPool.GetChannelAsync().ConfigureAwait(false);

        try
        {
            chanHost.Channel.QueueUnbind(
                queue: queueName,
                exchange: exchangeName,
                routingKey: routingKey,
                arguments: args);
        }
        catch { error = true; }
        finally { await _channelPool.ReturnChannelAsync(chanHost, error).ConfigureAwait(false); }

        return error;
    }

    /// <summary>
    /// Create an Exchange asynchronously.
    /// <para>Returns success or failure.</para>
    /// </summary>
    /// <param name="exchangeName"></param>
    /// <param name="exchangeType"></param>
    /// <param name="durable"></param>
    /// <param name="autoDelete"></param>
    /// <param name="args"></param>
    /// <returns>A bool indicating failure.</returns>
    public async Task<bool> CreateExchangeAsync(
        string exchangeName,
        string exchangeType,
        bool durable = true,
        bool autoDelete = false,
        IDictionary<string, object> args = null)
    {
        Guard.AgainstNullOrEmpty(exchangeName, nameof(exchangeName));

        var error = false;
        var chanHost = await _channelPool.GetChannelAsync().ConfigureAwait(false);

        try
        {
            chanHost.Channel.ExchangeDeclare(
                exchange: exchangeName,
                type: exchangeType,
                durable: durable,
                autoDelete: autoDelete,
                arguments: args);
        }
        catch { error = true; }
        finally { await _channelPool.ReturnChannelAsync(chanHost, error).ConfigureAwait(false); }

        return error;
    }

    /// <summary>
    /// Delete an Exchange asynchronously.
    /// <para>Returns success or failure.</para>
    /// </summary>
    /// <param name="exchangeName"></param>
    /// <param name="onlyIfUnused"></param>
    /// <returns>A bool indicating failure.</returns>
    public async Task<bool> DeleteExchangeAsync(string exchangeName, bool onlyIfUnused = false)
    {
        Guard.AgainstNullOrEmpty(exchangeName, nameof(exchangeName));

        var error = false;
        var chanHost = await _channelPool.GetChannelAsync().ConfigureAwait(false);

        try
        {
            chanHost.Channel.ExchangeDelete(
                exchange: exchangeName,
                ifUnused: onlyIfUnused);
        }
        catch { error = true; }
        finally { await _channelPool.ReturnChannelAsync(chanHost, error).ConfigureAwait(false); }

        return error;
    }

    /// <summary>
    /// Bind an Exchange to another Exchange asynchronously.
    /// <para>Returns success or failure.</para>
    /// </summary>
    /// <param name="childExchangeName"></param>
    /// <param name="parentExchangeName"></param>
    /// <param name="routingKey"></param>
    /// <param name="args"></param>
    /// <returns>A bool indicating failure.</returns>
    public async Task<bool> BindExchangeToExchangeAsync(
        string childExchangeName,
        string parentExchangeName,
        string routingKey = "",
        IDictionary<string, object> args = null)
    {
        Guard.AgainstNullOrEmpty(parentExchangeName, nameof(parentExchangeName));
        Guard.AgainstNullOrEmpty(childExchangeName, nameof(childExchangeName));

        var error = false;
        var chanHost = await _channelPool.GetChannelAsync().ConfigureAwait(false);

        try
        {
            chanHost.Channel.ExchangeBind(
                destination: childExchangeName,
                source: parentExchangeName,
                routingKey: routingKey,
                arguments: args);
        }
        catch { error = true; }
        finally { await _channelPool.ReturnChannelAsync(chanHost, error).ConfigureAwait(false); }

        return error;
    }

    /// <summary>
    /// Unbind an Exchange from another Exchange asynchronously.
    /// <para>Returns success or failure.</para>
    /// </summary>
    /// <param name="childExchangeName"></param>
    /// <param name="parentExchangeName"></param>
    /// <param name="routingKey"></param>
    /// <param name="args"></param>
    /// <returns>A bool indicating failure.</returns>
    public async Task<bool> UnbindExchangeFromExchangeAsync(
        string childExchangeName,
        string parentExchangeName,
        string routingKey = "",
        IDictionary<string, object> args = null)
    {
        Guard.AgainstNullOrEmpty(parentExchangeName, nameof(parentExchangeName));
        Guard.AgainstNullOrEmpty(childExchangeName, nameof(childExchangeName));

        var error = false;
        var chanHost = await _channelPool.GetChannelAsync().ConfigureAwait(false);

        try
        {
            chanHost.Channel.ExchangeUnbind(
                destination: childExchangeName,
                source: parentExchangeName,
                routingKey: routingKey,
                arguments: args);
        }
        catch { error = true; }
        finally { await _channelPool.ReturnChannelAsync(chanHost, error).ConfigureAwait(false); }

        return error;
    }
}
