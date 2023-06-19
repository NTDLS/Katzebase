using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using static Katzebase.Engine.KbLib.EngineConstants;

namespace Katzebase.Engine.Atomicity
{
    public class ReversibleAction
    {
        [JsonConverter(typeof(StringEnumConverter))]
        public ActionType Action { get; set; }
        public string OriginalPath { get; set; }
        public string? BackupPath { get; set; }
        public int Sequence { get; set; } = 0;

        public ReversibleAction(ActionType action, string originalPath)
        {
            Action = action;
            OriginalPath = originalPath;
        }
    }
}
