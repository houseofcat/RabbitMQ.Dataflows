﻿using OpenTelemetry.Trace;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;

namespace HouseofCat.Dataflows;

public interface IWorkState
{
    IDictionary<string, object> Data { get; set; }

    // Routing Logic
    IDictionary<string, bool> StepSuccess { get; set; }
    string StepIdentifier { get; set; }

    // Error Handling
    bool IsFaulted { get; set; }
    ExceptionDispatchInfo EDI { get; set; }

    // Outbound
    byte[] SendData { get; set; }

    // RootSpan or ChildSpan derived from TraceParentHeader
    TelemetrySpan WorkflowSpan { get; set; }
}
