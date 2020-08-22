using HouseofCat.RabbitMQ.WorkState;

namespace ConsumerDataflowMetrics.Models
{
    public class WorkState : RabbitWorkState
    {
        public Message Message { get; set; }
        public bool DeserializeStepSuccess => Message != null;
        public bool ProcessStepSuccess { get; set; }
        public bool AcknowledgeStepSuccess { get; set; }
        public bool AllStepsSuccess => DeserializeStepSuccess && ProcessStepSuccess && AcknowledgeStepSuccess;
    }
}
