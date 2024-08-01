namespace NTDLS.Katzebase.Engine.Interactions.APIHandlers
{
    /// <summary>
    /// Public class methods for handling API requests related to configuration.
    /// </summary>
    public class EnvironmentAPIHandlers
    {
        private readonly EngineCore _core;

        public EnvironmentAPIHandlers(EngineCore core)
        {
            _core = core;

            try
            {
            }
            catch (Exception ex)
            {
                Management.LogManager.Error($"Failed to instantiate environment API handlers.", ex);
                throw;
            }
        }
    }
}
