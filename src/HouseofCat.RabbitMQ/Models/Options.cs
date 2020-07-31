using System;
using System.Collections.Generic;
using System.Globalization;

namespace HouseofCat.RabbitMQ
{
    public class Options
    {
        /// <summary>
        /// Class to hold settings for ConnectionFactory (RabbitMQ) options.
        /// </summary>
        public FactoryOptions FactoryOptions { get; set; } = new FactoryOptions();

        /// <summary>
        /// Class to hold settings for Channel/ConnectionPool options.
        /// </summary>
        public PoolOptions PoolOptions { get; set; } = new PoolOptions();

        /// <summary>
        /// Class to hold settings for Publisher/AutoPublisher options.
        /// </summary>
        public PublisherOptions PublisherOptions { get; set; } = new PublisherOptions();

        /// <summary>
        /// Class to hold the global Consumer settings. Will apply these to every consumer.
        /// </summary>
        public IDictionary<string, GlobalConsumerOptions> GlobalConsumerOptions { get; set; } = new Dictionary<string, GlobalConsumerOptions>();

        /// <summary>
        /// Dictionary to hold all the ConsumerSettings using the ConsumerOption class.
        /// </summary>
        public IDictionary<string, ConsumerOptions> ConsumerOptions { get; set; } = new Dictionary<string, ConsumerOptions>();

        public ConsumerOptions GetConsumerOptions(string consumerName)
        {
            if (!ConsumerOptions.ContainsKey(consumerName)) throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, ExceptionMessages.NoConsumerSettingsMessage, consumerName));
            return ConsumerOptions[consumerName];
        }

        public void ApplyGlobalConsumerOptions()
        {
            foreach(var kvp in ConsumerOptions)
            {
                // Apply the global consumer settings and global consumer pipeline settings
                // on top of (overriding) individual consumer settings. Opt out by not setting
                // the global settings field.
                if (!string.IsNullOrWhiteSpace(kvp.Value.GlobalSettings)
                    && GlobalConsumerOptions.ContainsKey(kvp.Value.GlobalSettings))
                {
                    kvp.Value.ApplyGlobalOptions(GlobalConsumerOptions[kvp.Value.GlobalSettings]);
                }
            }
        }
    }
}
