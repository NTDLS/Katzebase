using NTDLS.Katzebase.Client.Payloads;
using NTDLS.Katzebase.Engine.Atomicity;

namespace NTDLS.Katzebase.Engine.Functions.System.Implementations
{
    internal static class SystemCheckPointHealthCounters
    {
        public static KbQueryResultCollection Execute(EngineCore core, Transaction transaction, SystemFunctionParameterValueCollection function)
        {
            core.Health.Checkpoint();
            return new KbQueryResultCollection();
        }
    }
}
