using OpenTelemetry.Trace;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;

namespace HouseofCat.Dataflows;

public interface IWorkState
{
    IDictionary<string, object> Data { get; set; }

    bool IsFaulted { get; set; }
    ExceptionDispatchInfo EDI { get; set; }

    // RootSpan or ChildSpan derived from TraceParentHeader
    TelemetrySpan WorkflowSpan { get; set; }
}
