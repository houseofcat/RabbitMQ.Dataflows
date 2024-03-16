namespace HouseofCat.RabbitMQ;

public class Envelope
{
    public string Exchange { get; set; }
    public string RoutingKey { get; set; }
    public RoutingOptions RoutingOptions { get; set; }
}
