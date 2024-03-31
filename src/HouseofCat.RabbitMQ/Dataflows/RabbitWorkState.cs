using HouseofCat.Dataflows;
using OpenTelemetry.Trace;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;
using System.Runtime.Serialization;

namespace HouseofCat.RabbitMQ.Dataflows;

public interface IRabbitWorkState : IWorkState
{
    IReceivedData ReceivedData { get; set; }
    IMessage SendMessage { get; set; }
    bool SendMessageSent { get; set; }
}

public abstract class RabbitWorkState : IRabbitWorkState
{
    [IgnoreDataMember]
    public virtual IReceivedData ReceivedData { get; set; }

    public virtual byte[] SendData { get; set; }
    public virtual IMessage SendMessage { get; set; }

    public virtual bool SendMessageSent { get; set; }

    public virtual IDictionary<string, object> Data { get; set; }

    public virtual IDictionary<string, bool> StepSuccess { get; set; }

    public virtual string StepIdentifier { get; set; }
    public bool IsFaulted { get; set; }
    public ExceptionDispatchInfo EDI { get; set; }

    public TelemetrySpan WorkflowSpan { get; set; }
}
