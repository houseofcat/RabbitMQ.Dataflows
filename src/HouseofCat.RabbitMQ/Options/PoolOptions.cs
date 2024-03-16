namespace HouseofCat.RabbitMQ;

public class PoolOptions
{
    /// <summary>
    /// Value to configure the ConnectionPool prefix for display names on RabbitMQ server.
    /// </summary>
    public string ServiceName { get; set; } = string.Empty;

    /// <summary>
    /// Number of connections to be created in the ConnectionPool. Used in round-robin to create channels.
    /// <para>Deafult valuse is 2.</para>
    /// </summary>
    public ushort MaxConnections { get; set; } = 2;

    /// <summary>
    /// Number of channels to keep in each of the channel pool. Used in round-robin to perform actions.
    /// <para>Default value is 0.</para>
    /// </summary>
    public ushort MaxChannels { get; set; }

    /// <summary>
    /// Number of ackable channels to keep in each of the channel pool. Used in round-robin to perform actions.
    /// <para>Default value is 10.</para>
    /// </summary>
    public ushort MaxAckableChannels { get; set; } = 10;

    /// <summary>
    /// During retry, the library may have determined the Connection is healthy but Channel is still not open. This is the number
    /// of times it will attempt to re-check a channel's health before destroying it and creating a new one. Internal AutoRecovery
    /// of channels has a known delay from the connection. Each check will sleep once using SleepOnErrorInterval before
    /// checking the status of the channel again.
    /// <para>Default value is 1.</para>
    /// <para>Recommended maximum value is 5.</para>
    /// </summary>
    public ushort MaxLastChannelHealthCheck { get; set; } = 1;

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
    public bool OnlyTransientChannels { get; set; } = false;
}
