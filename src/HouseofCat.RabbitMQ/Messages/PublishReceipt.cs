namespace HouseofCat.RabbitMQ
{
    public interface IPublishReceipt
    {
        bool IsError { get; set; }
        public string MessageId { get; set; }

        IMessage GetOriginalMessage();
    }

    public struct PublishReceipt : IPublishReceipt
    {
        public bool IsError { get; set; }
        public string MessageId { get; set; }
        public IMessage OriginalLetter { get; set; }

        public IMessage GetOriginalMessage() => OriginalLetter;
    }
}
