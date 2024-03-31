using System.Collections.Generic;

namespace HouseofCat.RabbitMQ;

public sealed record QueueBinding
{
    public string QueueName { get; set; }
    public string ExchangeName { get; set; }
    public string RoutingKey { get; set; }
    public IDictionary<string, object> Args { get; set; }
}
