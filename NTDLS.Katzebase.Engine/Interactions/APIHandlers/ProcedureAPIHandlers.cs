namespace NTDLS.Katzebase.Engine.Interactions.APIHandlers
{
    /// <summary>
    /// Public class methods for handling API requests related to procedures.
    /// </summary>
    public class ProcedureAPIHandlers
    {
        private readonly Core _core;

        public ProcedureAPIHandlers(Core core)
        {
            _core = core;

            try
            {
            }
            catch (Exception ex)
            {
                core.Log.Write($"Failed to instanciate procedures API handlers.", ex);
                throw;
            }
        }
    }
}
