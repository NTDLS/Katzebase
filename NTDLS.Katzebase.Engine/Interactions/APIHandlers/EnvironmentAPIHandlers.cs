using NTDLS.Katzebase.Engine.Interactions.Management;

namespace NTDLS.Katzebase.Engine.Interactions.APIHandlers
{
    /// <summary>
    /// Public class methods for handling API requests related to configuration.
    /// </summary>
    public class EnvironmentAPIHandlers
    {
        private readonly EngineCore<TData> _core;

        public EnvironmentAPIHandlers(EngineCore core)
        {
            _core = core;

            try
            {
            }
            catch (Exception ex)
            {
                LogManager.Error($"Failed to instantiate environment API handlers.", ex);
                throw;
            }
        }
    }
}
