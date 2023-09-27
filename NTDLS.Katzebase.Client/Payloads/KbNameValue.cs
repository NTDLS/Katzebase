using static NTDLS.Katzebase.Client.KbConstants;

namespace NTDLS.Katzebase.Client.Payloads
{
    public class KbMetric
    {
        public string Name { get; set; }
        public double Value { get; set; } = 0;
        /// <summary>
        /// The number of times that a KbMetricType.Cumulative value was set.
        /// </summary>
        public double Count { get; set; } = 0;
        public KbMetricType MetricType { get; set; }

        public KbMetric()
        {
            Name = string.Empty;
        }

        public KbMetric(KbMetricType metricType, string name)
        {
            MetricType = metricType;
            Name = name;
        }

        public KbMetric(KbMetricType metricType, string name, double value)
        {
            MetricType = metricType;
            Name = name;
            Value = value;
        }
    }
}
