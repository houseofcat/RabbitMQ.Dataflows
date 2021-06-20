namespace HouseofCat.RabbitMQ
{
    public class PoolOptions
    {
        /// <summary>
        /// Value to configure the ConnectionPool prefix for display names on RabbitMQ server.
        /// </summary>
        public string ServiceName { get; set; } = string.Empty;

        /// <summary>
        /// Number of connections to be created in the ConnectionPool. Used in round-robin to create channels.
        /// </summary>
        public ushort MaxConnections { get; set; } = 5;

        /// <summary>
        /// Number of channels to keep in each of the channel pools. Used in round-robin to perform actions.
        /// </summary>
        public ushort MaxChannels { get; set; } = 25;

        /// <summary>
        /// The time to sleep (in ms) when an error occurs on Channel or Connection creation. It's best not to be hyper aggressive with this value.
        /// </summary>
        public int SleepOnErrorInterval { get; set; } = 1000;
        
        /// <summary>
        /// Whether to lazy initialize channels and connections after creation.
        /// </summary>
        public bool LazyInitialize { get; set; }
    }
}
