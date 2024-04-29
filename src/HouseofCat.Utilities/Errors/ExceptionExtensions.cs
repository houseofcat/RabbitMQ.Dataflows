using System;
using System.Diagnostics;
using System.Linq;

namespace HouseofCat.Utilities.Errors;

public static class ExceptionExtensions
{
    private static readonly string _atValue = "   at ";
    private static readonly string _csFileExt = ".cs";
    private static readonly string _stackDomainBoundary = "---";
    private static readonly string _newDomainBoundaryTemplate = "=== Sub-stack {0} ===";

    private static readonly string[] _newLineArray = new[] { $"{Environment.NewLine}" };
    private static readonly string[] _inArray = new[] { " in " };
    private static readonly string[] _atArray = new[] { _atValue };
    private static readonly string[] _forwardSlashArray = new[] { "/" };

    private static readonly string _line = ":line ";
    private static readonly string[] _lineArray = new[] { _line };

    public static Stacky PrettifyStackTrace(this Exception ex)
    {
        var stacky = new Stacky
        {
            ExceptionMessage = ex.Message,
            ExceptionType = ex.GetType().ToString()
        };
        ParseStackTrace(ex, stacky);
        return stacky;
    }

    public static Stacky PrettifyStackTraceWithParameters(this Exception ex, params object[] args)
    {
        var stacky = new Stacky
        {
            ExceptionMessage = ex.Message,
            ExceptionType = ex.GetType().ToString(),
            MethodArguments = new StackTrace(ex, false)
                .GetFrame(0)
                .GetMethod()
                .GetParameters()
                .Select(p => p.Name)
                .Zip(args, (Name, Value) => new { Name, Value })
                .ToDictionary(x => x.Name, x => x.Value)
        };
        ParseStackTrace(ex, stacky);
        return stacky;
    }

    private static void ParseStackTrace(Exception ex, Stacky stacky)
    {
        if (string.IsNullOrEmpty(ex.StackTrace)) return;

        var lines = ex.StackTrace.Split(_newLineArray, StringSplitOptions.RemoveEmptyEntries);
        var stackCount = 0;
        for (var i = 0; i < lines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(stacky.FileName) && lines[i].Contains(_csFileExt))
            {
                var lineAsStrings = lines[i].Split(_lineArray, StringSplitOptions.RemoveEmptyEntries);

                if (lineAsStrings.Length > 1 && int.TryParse(lineAsStrings[1], out var lineNumber))
                {
                    stacky.Line = lineNumber;
                }
                var lineSubStrings = lineAsStrings[0].Split(_inArray, StringSplitOptions.RemoveEmptyEntries);
                if (lineSubStrings.Length > 1)
                {
                    var filePathSubStrings = lineSubStrings[1].Split(_forwardSlashArray, StringSplitOptions.RemoveEmptyEntries);
                    if (filePathSubStrings.Length > 0)
                    {
                        stacky.FileName = filePathSubStrings[^1];
                    }
                }
            }

            if (i == 0)
            {
                var subStrings = lines[i].Split(_inArray, StringSplitOptions.RemoveEmptyEntries);
                if (subStrings.Length > 0)
                {
                    stacky.Method = subStrings[0].Split(_atArray, StringSplitOptions.RemoveEmptyEntries)[0];
                }

                stacky.StackLines.Add($"{i}: {stacky.Method}");
            }
            else if (lines[i].StartsWith(_stackDomainBoundary))
            {
                stackCount++;
                stacky.StackLines.Add(string.Format(_newDomainBoundaryTemplate, stackCount));
            }
            else
            {
                stacky.StackLines.Add(lines[i].Replace(_atValue, $"{i}: @ "));
            }
        }
    }
}
