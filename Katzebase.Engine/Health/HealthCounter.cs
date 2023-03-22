using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using static Katzebase.Engine.Constants;

namespace Katzebase.Engine.Health
{
    public class HealthCounter
    {
        [JsonConverter(typeof(StringEnumConverter))]
        public HealthCounterType Type { get; set; }
        public string Instance { get; set; } = string.Empty;
        public double Value { get; set; }
    }
}
