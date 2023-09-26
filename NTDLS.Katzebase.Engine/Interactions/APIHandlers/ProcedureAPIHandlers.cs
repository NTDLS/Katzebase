namespace NTDLS.Katzebase.Engine.Interactions.APIHandlers
{
    /// <summary>
    /// Public class methods for handling API requests related to procedures.
    /// </summary>
    public class ProcedureAPIHandlers
    {
        private readonly Core core;

        public ProcedureAPIHandlers(Core core)
        {
            this.core = core;

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
