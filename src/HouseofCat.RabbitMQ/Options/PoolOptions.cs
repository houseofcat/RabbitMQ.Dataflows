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
    /// Number of channels to keep in each of the channel pools. Used in round-robin to perform actions.
    /// <para>Default value is 10.</para>
    /// </summary>
    public ushort MaxChannels { get; set; } = 10;

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
}
