namespace HouseofCat.RabbitMQ
{
    public interface IPublishReceipt
    {
        bool IsError { get; set; }
        
        ulong GetMessageId();
        IMessage GetOriginalMessage();
    }
}