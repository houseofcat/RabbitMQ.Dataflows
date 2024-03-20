using System.Collections.Generic;

namespace HouseofCat.RabbitMQ;

public class QueueBindingConfig
{
    public string QueueName { get; set; }
    public string ExchangeName { get; set; }
    public string RoutingKey { get; set; }
    public IDictionary<string, object> Args { get; set; }
}
