namespace NTDLS.Katzebase.Engine.Interactions.APIHandlers
{
    /// <summary>
    /// Public class methods for handling API requests related to configuration.
    /// </summary>
    public class EnvironmentAPIHandlers
    {
        private readonly Core _core;

        public EnvironmentAPIHandlers(Core core)
        {
            _core = core;

            try
            {
            }
            catch (Exception ex)
            {
                core.Log.Write($"Failed to instantiate environment API handlers.", ex);
                throw;
            }
        }
    }
}
