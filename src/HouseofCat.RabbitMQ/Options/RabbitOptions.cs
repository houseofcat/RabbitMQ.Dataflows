using System;
using System.Collections.Generic;
using System.Globalization;

namespace HouseofCat.RabbitMQ;

public class RabbitOptions
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
    /// Class to hold the global Consumer options. Will apply these to every consumer.
    /// </summary>
    public IDictionary<string, GlobalConsumerOptions> GlobalConsumerOptions { get; set; } = new Dictionary<string, GlobalConsumerOptions>();

    /// <summary>
    /// Dictionary to hold all the ConsumerOptions using the ConsumerOptions class.
    /// </summary>
    public IDictionary<string, ConsumerOptions> ConsumerOptions { get; set; } = new Dictionary<string, ConsumerOptions>();

    public ConsumerOptions GetConsumerOptions(string consumerName)
    {
        if (!ConsumerOptions.TryGetValue(consumerName, out ConsumerOptions value)) throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, ExceptionMessages.NoConsumerOptionsMessage, consumerName));
        return value;
    }

    public void ApplyGlobalConsumerOptions()
    {
        foreach (var kvp in ConsumerOptions)
        {
            // Apply the global consumer settings and global consumer pipeline settings
            // on top of (overriding) individual consumer settings. Opt out by not setting
            // the global settings field.
            if (!string.IsNullOrWhiteSpace(kvp.Value.GlobalSettings)
                && GlobalConsumerOptions.TryGetValue(kvp.Value.GlobalSettings, out GlobalConsumerOptions value))
            {
                kvp.Value.ApplyGlobalOptions(value);
            }
        }
    }
}
