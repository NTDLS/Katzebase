namespace Katzebase.Engine.Health.Management
{
    /// <summary>
    /// Public class methods for handling API requests related to health.
    /// </summary>
    public class HealthAPIHandlers
    {
        private readonly Core core;

        public HealthAPIHandlers(Core core)
        {
            this.core = core;

            try
            {
            }
            catch (Exception ex)
            {
                core.Log.Write($"Failed to instanciate health API handlers.", ex);
                throw;
            }
        }
    }
}
