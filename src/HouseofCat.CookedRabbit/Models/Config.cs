using System;
using System.Collections.Generic;
using System.Globalization;

namespace CookedRabbit.Core
{
    public class Config
    {
        /// <summary>
        /// Class to hold settings for ConnectionFactory (RabbitMQ) options.
        /// </summary>
        public FactoryOptions FactorySettings { get; set; } = new FactoryOptions();

        /// <summary>
        /// Class to hold settings for Channel/ConnectionPool options.
        /// </summary>
        public PoolOptions PoolSettings { get; set; } = new PoolOptions();

        /// <summary>
        /// Class to hold settings for Publisher/AutoPublisher options.
        /// </summary>
        public PublisherOptions PublisherSettings { get; set; } = new PublisherOptions();

        /// <summary>
        /// Class to hold the global Consumer settings. Will apply these to every consumer.
        /// </summary>
        public IDictionary<string, GlobalConsumerOptions> GlobalConsumerSettings { get; set; } = new Dictionary<string, GlobalConsumerOptions>();

        /// <summary>
        /// Dictionary to hold all the ConsumerSettings using the ConsumerOption class.
        /// </summary>
        public IDictionary<string, ConsumerOptions> ConsumerSettings { get; set; } = new Dictionary<string, ConsumerOptions>();

        public ConsumerOptions GetConsumerSettings(string consumerName)
        {
            if (!ConsumerSettings.ContainsKey(consumerName)) throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, Utils.ExceptionMessages.NoConsumerSettingsMessage, consumerName));
            return ConsumerSettings[consumerName];
        }
    }
}
