namespace CookedRabbit.Core
{
    public struct PublishReceipt
    {
        public bool IsError { get; set; }
        public ulong LetterId { get; set; }
        public Letter OriginalLetter { get; set; }
    }
}
