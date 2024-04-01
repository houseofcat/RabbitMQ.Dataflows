using System;
using System.Collections.Generic;
using System.Globalization;

namespace HouseofCat.RabbitMQ;

public class RabbitOptions
{
    /// <summary>
    /// Class to hold settings for Channel/ConnectionPool options.
    /// </summary>
    public PoolOptions PoolOptions { get; set; } = new PoolOptions();

    /// <summary>
    /// Class to hold settings for Publisher/AutoPublisher options.
    /// </summary>
    public PublisherOptions PublisherOptions { get; set; } = new PublisherOptions();

    /// <summary>
    /// Dictionary to hold all the ConsumerOptions using the ConsumerOptions class.
    /// </summary>
    public IDictionary<string, ConsumerOptions> ConsumerOptions { get; set; } = new Dictionary<string, ConsumerOptions>();

    public ConsumerOptions GetConsumerOptions(string consumerName)
    {
        if (!ConsumerOptions.TryGetValue(consumerName, out ConsumerOptions value))
        {
            throw new ArgumentException(string.Format(ExceptionMessages.NoConsumerOptionsMessage, consumerName));
        }
        return value;
    }
}
