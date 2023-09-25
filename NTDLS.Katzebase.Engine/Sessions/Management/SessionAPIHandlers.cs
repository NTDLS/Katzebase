namespace Katzebase.Engine.Sessions.Management
{
    /// <summary>
    /// Public class methods for handling API requests related to sessions.
    /// </summary>
    public class SessionAPIHandlers
    {
        private readonly Core core;

        public SessionAPIHandlers(Core core)
        {
            this.core = core;

            try
            {
            }
            catch (Exception ex)
            {
                core.Log.Write($"Failed to instanciate session API handlers.", ex);
                throw;
            }
        }
    }
}
