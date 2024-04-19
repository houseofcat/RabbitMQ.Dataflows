namespace HouseofCat.RabbitMQ;

public sealed record TopologyConfig
{
    public Exchange[] Exchanges { get; set; }
    public Queue[] Queues { get; set; }
    public ExchangeBinding[] ExchangeBindings { get; set; }
    public QueueBinding[] QueueBindings { get; set; }
}
