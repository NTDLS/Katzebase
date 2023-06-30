namespace Katzebase.Engine.Query.Function.Procedures.Management
{
    /// <summary>
    /// Public class methods for handling API requests related to functions.
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
                core.Log.Write($"Failed to instanciate functions API handlers.", ex);
                throw;
            }
        }
    }
}
