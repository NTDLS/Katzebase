namespace NTDLS.Katzebase.Engine.Interactions.QueryHandlers
{
    /// <summary>
    /// Internal class methods for handling query requests related to health.
    /// </summary>
    internal class HealthQueryHandlers
    {
        private readonly Core core;

        public HealthQueryHandlers(Core core)
        {
            this.core = core;

            try
            {
            }
            catch (Exception ex)
            {
                core.Log.Write($"Failed to instanciate health query handler.", ex);
                throw;
            }
        }
    }
}
