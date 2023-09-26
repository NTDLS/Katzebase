namespace NTDLS.Katzebase.Engine.Interactions.APIHandlers
{
    /// <summary>
    /// Public class methods for handling API requests related to sessions.
    /// </summary>
    public class SessionAPIHandlers
    {
        private readonly Core _core;

        public SessionAPIHandlers(Core core)
        {
            _core = core;

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
