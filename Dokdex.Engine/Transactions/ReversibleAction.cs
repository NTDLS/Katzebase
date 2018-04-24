using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using static Dokdex.Engine.Constants;

namespace Dokdex.Engine.Transactions
{
    public class ReversibleAction
    {
        [JsonConverter(typeof(StringEnumConverter))]
        public ActionType Action { get; set; }
        public string OriginalPath { get; set; }
        public string BackupPath { get; set; }
        public int Sequence { get; set; }
    }
}
