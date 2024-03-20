using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace HouseofCat.Utilities.Errors;

public class Stacky
{
    [DataMember(Order = 0)]
    public string ExceptionType { get; set; }

    [DataMember(Order = 1)]
    public string ExceptionMessage { get; set; }

    [DataMember(Order = 2)]
    public string Method { get; set; }

    [DataMember(Order = 3)]
    public Dictionary<string, object> MethodArguments { get; set; } = new Dictionary<string, object>();

    [DataMember(Order = 4)]
    public string FileName { get; set; }

    [DataMember(Order = 5)]
    public int Line { get; set; }

    [DataMember(Order = 6)]
    public List<string> StackLines { get; set; } = new List<string>();

    public string ToJsonString() => JsonSerializer.Serialize(this, Options);

    private static readonly JsonSerializerOptions Options = new JsonSerializerOptions
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = true,
    };
}
