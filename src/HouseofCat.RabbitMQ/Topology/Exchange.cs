using System.Collections.Generic;

namespace HouseofCat.RabbitMQ;

public sealed record Exchange
{
    public string Name { get; set; }
    public string Type { get; set; }
    public bool Durable { get; set; }
    public bool AutoDelete { get; set; }
    public IDictionary<string, object> Args { get; set; }
}
