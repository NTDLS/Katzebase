namespace Katzebase.Engine.Health.Management
{
    /// <summary>
    /// Public class methods for handling API requests related to configuration.
    /// </summary>
    public class EnvironmentAPIHandlers
    {
        private readonly Core core;

        public EnvironmentAPIHandlers(Core core)
        {
            this.core = core;

            try
            {
            }
            catch (Exception ex)
            {
                core.Log.Write($"Failed to instanciate environment API handlers.", ex);
                throw;
            }
        }
    }
}
