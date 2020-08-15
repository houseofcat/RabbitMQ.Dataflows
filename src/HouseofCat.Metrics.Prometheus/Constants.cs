namespace HouseofCat.Metrics
{
    public static class Constants
    {
        public readonly static string CounterAlreadyExists = "Name of the counter must be unique.";
        public readonly static string GaugeAlreadyExists = "Name of the gauge must be unique.";
        public readonly static string GaugeNotExists = "Name of the gauge does not exist.";
        public readonly static string SummaryAlreadyExists = "Name of the summary must be unique.";
        public readonly static string HistogramAlreadyExists = "Name of the histogram must be unique.";
        public readonly static string HistogramNotExists = "Name of the histogram does not exist.";
    }
}
