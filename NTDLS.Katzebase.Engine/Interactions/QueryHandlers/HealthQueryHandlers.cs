namespace NTDLS.Katzebase.Engine.Interactions.QueryHandlers
{
    /// <summary>
    /// Internal class methods for handling query requests related to health.
    /// </summary>
    internal class HealthQueryHandlers
    {
        private readonly EngineCore _core;

        public HealthQueryHandlers(EngineCore core)
        {
            _core = core;

            try
            {
            }
            catch (Exception ex)
            {
                Management.LogManager.Error($"Failed to instantiate health query handler.", ex);
                throw;
            }
        }
    }
}
