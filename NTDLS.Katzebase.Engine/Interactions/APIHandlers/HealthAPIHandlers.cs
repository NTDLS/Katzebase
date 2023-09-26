namespace NTDLS.Katzebase.Engine.Interactions.APIHandlers
{
    /// <summary>
    /// Public class methods for handling API requests related to health.
    /// </summary>
    public class HealthAPIHandlers
    {
        private readonly Core _core;

        public HealthAPIHandlers(Core core)
        {
            _core = core;

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
