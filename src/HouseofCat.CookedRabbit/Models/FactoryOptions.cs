using System;

namespace CookedRabbit.Core
{
    public class FactoryOptions
    {
        /// <summary>
        /// ConnectionFactory (RabbitMQ) Uri connection string.
        /// <para>amqp(s)://guest:guest@localhost:5672/vhost</para>
        /// </summary>
        public Uri Uri { get; set; } = new Uri("amqp://guest:guest@localhost:5672/");

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
        public ushort NetRecoveryTimeout { get; set; } = 10;

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
        public SslOptions SslSettings { get; set; } = new SslOptions();
    }
}
