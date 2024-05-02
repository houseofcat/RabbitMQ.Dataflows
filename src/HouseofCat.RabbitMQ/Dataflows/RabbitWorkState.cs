using HouseofCat.Dataflows;
using OpenTelemetry.Trace;
using System;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;
using System.Runtime.Serialization;

namespace HouseofCat.RabbitMQ.Dataflows;

public interface IRabbitWorkState : IWorkState
{
    IReceivedMessage ReceivedMessage { get; set; }

    ReadOnlyMemory<byte> SendData { get; set; }
    IMessage SendMessage { get; set; }
}

public abstract class RabbitWorkState : IRabbitWorkState
{
    [IgnoreDataMember]
    public virtual IReceivedMessage ReceivedMessage { get; set; }

    public virtual ReadOnlyMemory<byte> SendData { get; set; }

    public virtual IMessage SendMessage { get; set; }

    public virtual IDictionary<string, object> Data { get; set; }

    public bool IsFaulted { get; set; }
    public ExceptionDispatchInfo EDI { get; set; }

    public TelemetrySpan WorkflowSpan { get; set; }
}
