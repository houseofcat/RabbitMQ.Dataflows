namespace HouseofCat.RabbitMQ;

public interface IPublishReceipt
{
    bool IsError { get; set; }
    string MessageId { get; set; }
    IMessage OriginalMessage { get; set; }
}

public struct PublishReceipt : IPublishReceipt
{
    public bool IsError { get; set; }
    public string MessageId { get; set; }
    public IMessage OriginalMessage { get; set; }
}
