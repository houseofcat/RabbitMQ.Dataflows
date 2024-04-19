using System.Collections.Generic;

namespace HouseofCat.RabbitMQ;

public sealed record Queue
{
    public string Name { get; set; }
    public bool Durable { get; set; }
    public bool Exclusive { get; set; }
    public bool AutoDelete { get; set; }
    public IDictionary<string, object> Args { get; set; }
}
