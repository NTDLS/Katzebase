using NTDLS.Katzebase.Engine.Interactions.Management;

namespace NTDLS.Katzebase.Engine.Interactions.APIHandlers
{
    /// <summary>
    /// Public class methods for handling API requests related to procedures.
    /// </summary>
    public class ProcedureAPIHandlers<TData> where TData : IStringable
    {
        private readonly EngineCore<TData> _core;

        public ProcedureAPIHandlers(EngineCore<TData> core)
        {
            _core = core;

            try
            {
            }
            catch (Exception ex)
            {
                LogManager.Error($"Failed to instantiate procedures API handlers.", ex);
                throw;
            }
        }
    }
}
