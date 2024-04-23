using RabbitMQ.Client;
using System;

namespace HouseofCat.RabbitMQ;

public sealed class PoolOptions
{
    /// <summary>
    /// ConnectionFactory (RabbitMQ) Uri connection string. Set to null to use individual properties.
    /// <para>amqp(s)://guest:guest@localhost:5672/vhost</para>
    /// </summary>
    public Uri Uri { get; set; } = new Uri("amqp://guest:guest@localhost:5672/");

    /// <summary>
    /// ConnectionFactory (RabbitMQ) virtual host property. Use in lieu of Uri connection string.
    /// </summary>
    public string VirtualHost { get; set; } = "";

    /// <summary>
    /// ConnectionFactory (RabbitMQ) username property. Use in lieu of Uri connection string.
    /// </summary>
    public string UserName { get; set; } = "guest";

    /// <summary>
    /// ConnectionFactory (RabbitMQ) password property. Use in lieu of Uri connection string.
    /// </summary>
    public string Password { get; set; } = "guest";

    /// <summary>
    /// ConnectionFactory (RabbitMQ) host name property. Use in lieu of Uri connection string.
    /// </summary>
    public string HostName { get; set; } = "localhost";

    /// <summary>
    /// ConnectionFactory (RabbitMQ) port property. Use in lieu of Uri connection string.
    /// </summary>
    public int Port { get; set; } = AmqpTcpEndpoint.UseDefaultPort;

    /// <summary>
    /// ConnectionFactory (RabbitMQ) max connection property.
    /// </summary>
    public ushort MaxChannelsPerConnection { get; set; } = 100;

    /// <summary>
    /// ConnectionFactory (RabbitMQ) timespan (in seconds) between heartbeats. More than two timeouts in a row trigger RabbitMQ AutoRecovery.
    /// </summary>
    public ushort HeartbeatInterval { get; set; } = 6;

    /// <summary>
    /// ConnectionFactory (RabbitMQ) topology recovery property.
    /// </summary>
    public bool TopologyRecovery { get; set; } = true;

    /// <summary>
    /// ConnectionFactory (RabbitMQ) the amount of time to wait before netrecovery begins (seconds).
    /// </summary>
    public ushort NetRecoveryTimeout { get; set; } = 5;

    /// <summary>
    /// ConnectionFactory (RabbitMQ) specify the amount of time before timeout on protocol operations (seconds).
    /// </summary>
    public ushort ContinuationTimeout { get; set; } = 10;

    /// <summary>
    /// ConnectionFactory (RabbitMQ) property to enable Async consumers. Can't be true and retrieve regular consumers.
    /// </summary>
    public bool EnableDispatchConsumersAsync { get; set; }

    /// <summary>
    /// Class to hold settings for ChannelFactory/SSL (RabbitMQ) settings.
    /// </summary>
    public SslOptions SslOptions { get; set; } = new SslOptions();

    /// <summary>
    /// Class to hold settings for OAuth2 (RabbitMQ) settings.
    /// </summary>
    public OAuth2Options OAuth2Options { get; set; } = new OAuth2Options();

    /// <summary>
    /// Value to configure the ConnectionPool prefix for display names on RabbitMQ server.
    /// </summary>
    public string ServiceName { get; set; } = string.Empty;

    /// <summary>
    /// Number of connections to be created in the ConnectionPool. Used in round-robin to create channels.
    /// <para>Deafult valuse is 2.</para>
    /// </summary>
    public ushort Connections { get; set; } = 2;

    /// <summary>
    /// Number of channels to keep in each of the channel pool. Used in round-robin to perform actions.
    /// <para>Default value is 0.</para>
    /// </summary>
    public ushort Channels { get; set; } = 1;

    /// <summary>
    /// Number of ackable channels to keep in each of the channel pool. Used in round-robin to perform actions.
    /// <para>Default value is 10.</para>
    /// </summary>
    public ushort AckableChannels { get; set; } = 1;

    /// <summary>
    /// The time to sleep (in ms) when an error occurs on Channel or Connection creation. It's best not to be hyper aggressive with this value.
    /// <para>Default value is 1000.</para>
    /// </summary>
    public int SleepOnErrorInterval { get; set; } = 1000;

    /// <summary>
    /// All Transient Channels will be created in this range. This is to help identify transient channels
    /// used in logging internally. Can not be lower than 10000.
    /// <para>Default value is 10000.</para>
    /// </summary>
    public ulong TansientChannelStartRange { get; set; } = 10000;

    /// <summary>
    /// Stops the ConnectionPool from creating tracked internal channels. This can be useful
    /// if you wish to only use the ConnectionPool for building channels on demand. Transient
    /// channels are created on demand and that means this can slow down internally or at the RabbitMQ
    /// server if you aren't re-using the transient channel you just created.
    /// </summary>
    public bool OnlyTransientChannels { get; set; }
}
