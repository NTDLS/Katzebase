namespace NTDLS.Katzebase.Engine.Interactions.QueryHandlers
{
    /// <summary>
    /// Internal class methods for handling query requests related to health.
    /// </summary>
    internal class HealthQueryHandlers<TData> where TData : IStringable
    {
        private readonly EngineCore<TData> _core;

        public HealthQueryHandlers(EngineCore<TData> core)
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
