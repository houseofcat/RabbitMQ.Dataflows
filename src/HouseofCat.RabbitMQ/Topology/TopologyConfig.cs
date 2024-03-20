namespace HouseofCat.RabbitMQ;

public class TopologyConfig
{
    public ExchangeConfig[] Exchanges { get; set; }
    public QueueConfig[] Queues { get; set; }
    public ExchangeBindingConfig[] ExchangeBindings { get; set; }
    public QueueBindingConfig[] QueueBindings { get; set; }
}
