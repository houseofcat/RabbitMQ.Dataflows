using System;

namespace HouseofCat.Utilities;

public static class Constants
{
    public static class Stacky
    {
        public const string AtValue = "   at ";
        public const string CsFileExt = ".cs";
        public const string DefaultExceptionFileName = "SystemException";
        public const string StackDomainBoundary = "---";
        public const string NewDomainBoundaryTemplate = "=== Sub-stack {0} ===";

        public static readonly string[] NewLineArray = new[] { $"{Environment.NewLine}" };
        public static readonly string[] InArray = new[] { " in " };
        public static readonly string[] AtArray = new[] { AtValue };
        public static readonly string[] ForwardSlashArray = new[] { "/" };

        public static readonly string Line = ":line ";
        public static readonly string[] LineArray = new[] { Line };
    }
}
