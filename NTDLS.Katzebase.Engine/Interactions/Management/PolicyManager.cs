using NTDLS.Katzebase.Engine.Interactions.APIHandlers;
using NTDLS.Katzebase.Engine.Interactions.QueryHandlers;

namespace NTDLS.Katzebase.Engine.Interactions.Management
{
    /// <summary>
    /// Public core class methods for locking, reading, writing and managing tasks related to policies.
    /// </summary>
    public class PolicyManager
    {
        private readonly EngineCore _core;
        internal PolicyQueryHandlers QueryHandlers { get; private set; }
        public PolicyAPIHandlers APIHandlers { get; private set; }

        internal PolicyManager(EngineCore core)
        {
            _core = core;

            try
            {
                QueryHandlers = new PolicyQueryHandlers(core);
                APIHandlers = new PolicyAPIHandlers(core);
            }
            catch (Exception ex)
            {
                LogManager.Error("Failed to instantiate PolicyManager.", ex);
                throw;
            }
        }
    }
}
