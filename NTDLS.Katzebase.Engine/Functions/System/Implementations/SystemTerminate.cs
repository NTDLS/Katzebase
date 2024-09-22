using NTDLS.Katzebase.Client.Payloads;
using NTDLS.Katzebase.Engine.Atomicity;

namespace NTDLS.Katzebase.Engine.Functions.System.Implementations
{
    internal static class SystemTerminate
    {
        public static KbQueryResultCollection Execute(EngineCore core, Transaction transaction, SystemFunctionParameterValueCollection function)
        {
            var collection = new KbQueryResultCollection();
            var result = collection.AddNew();

            var processId = function.Get<ulong>("processId");

            core.Sessions.CloseByProcessId(processId);

            return new KbQueryResultCollection();
        }
    }
}
