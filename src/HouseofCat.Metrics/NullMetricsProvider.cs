using System;

namespace HouseofCat.Metrics
{
    public class NullMetricsProvider : IMetricsProvider
    {
        public void DecrementCounter(string name)
        {

        }

        public void DecrementGauge(string name)
        {

        }

        public void IncrementCounter(string name)
        {

        }

        public void IncrementGauge(string name)
        {

        }

        public IDisposable MeasureDuration(string name)
        {
            return null;
        }

        public void ObserveValue(string name, double value)
        {

        }

        public void ObserveValueFluctuation(string name, double value)
        {

        }
    }
}
