using NTDLS.Katzebase.Engine.Interactions.Management;
using NTDLS.ReliableMessaging;

namespace NTDLS.Katzebase.Engine.Interactions.APIHandlers
{
    /// <summary>
    /// Public class methods for handling API requests related to policies.
    /// </summary>
    public class PolicyAPIHandlers : IRmMessageHandler
    {
        private readonly EngineCore _core;

        public PolicyAPIHandlers(EngineCore core)
        {
            _core = core;

            try
            {
            }
            catch (Exception ex)
            {
                LogManager.Error($"Failed to instantiate policy API handlers.", ex);
                throw;
            }
        }
    }
}
