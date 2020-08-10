using HouseofCat.RabbitMQ.Pipelines;

namespace HouseofCat.RabbitMQ.Services
{
    public class TwilioWorkState : WorkState
    {
        public TextMessage TextMessage { get; set; }
        public ulong LetterId { get; set; }
        public bool DeserializeStepSuccess { get; set; }
        public bool ProcessStepSuccess { get; set; }
        public bool AcknowledgeStepSuccess { get; set; }
        public bool AllStepsSuccess => DeserializeStepSuccess && ProcessStepSuccess && AcknowledgeStepSuccess;
    }
}
