using HouseofCat.RabbitMQ.WorkState;

namespace HouseofCat.RabbitMQ.Services;

public class TwilioWorkState : RabbitWorkState
{
    public TextMessage TextMessage { get; set; }
    public bool DeserializeStepSuccess { get; set; }
    public bool ProcessStepSuccess { get; set; }
    public bool AcknowledgeStepSuccess { get; set; }
    public bool AllStepsSuccess => DeserializeStepSuccess && ProcessStepSuccess && AcknowledgeStepSuccess;
}
