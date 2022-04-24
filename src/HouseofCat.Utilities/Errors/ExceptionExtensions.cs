using System;
using System.Diagnostics;
using System.Linq;

namespace HouseofCat.Utilities.Errors
{
    public static class ExceptionExtensions
    {
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

            var lines = ex.StackTrace.Split(Constants.Stacky.NewLineArray, StringSplitOptions.RemoveEmptyEntries);
            var stackCount = 0;
            for (var i = 0; i < lines.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(stacky.FileName) && lines[i].Contains(Constants.Stacky.CsFileExt))
                {
                    var lineAsStrings = lines[i].Split(Constants.Stacky.LineArray, StringSplitOptions.RemoveEmptyEntries);

                    if (lineAsStrings.Length > 1 && int.TryParse(lineAsStrings[1], out var lineNumber))
                    {
                        stacky.Line = lineNumber;
                    }
                    var lineSubStrings = lineAsStrings[0].Split(Constants.Stacky.InArray, StringSplitOptions.RemoveEmptyEntries);
                    if (lineSubStrings.Length > 1)
                    {
                        var filePathSubStrings = lineSubStrings[1].Split(Constants.Stacky.ForwardSlashArray, StringSplitOptions.RemoveEmptyEntries);
                        if (filePathSubStrings.Length > 0)
                        {
                            stacky.FileName = filePathSubStrings[filePathSubStrings.Length - 1];
                        }
                    }
                }

                if (i == 0)
                {
                    var subStrings = lines[i].Split(Constants.Stacky.InArray, StringSplitOptions.RemoveEmptyEntries);
                    if (subStrings.Length > 0)
                    {
                        stacky.Method = subStrings[0].Split(Constants.Stacky.AtArray, StringSplitOptions.RemoveEmptyEntries)[0];
                    }

                    stacky.StackLines.Add($"{i}: {stacky.Method}");
                }
                else if (lines[i].StartsWith(Constants.Stacky.StackDomainBoundary))
                {
                    stackCount++;
                    stacky.StackLines.Add(string.Format(Constants.Stacky.NewDomainBoundaryTemplate, stackCount));
                }
                else
                {
                    stacky.StackLines.Add(lines[i].Replace(Constants.Stacky.AtValue, $"{i}: @ "));
                }
            }
        }
    }
}
