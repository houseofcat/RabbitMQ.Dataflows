namespace HouseofCat.RabbitMQ
{
    public struct PublishReceipt
    {
        public bool IsError { get; set; }
        public ulong MessageId { get; set; }
        public IMessage OriginalMessage { get; set; }
    }
}
