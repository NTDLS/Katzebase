﻿using NTDLS.Katzebase.Engine.Interactions.Management;

namespace NTDLS.Katzebase.Engine.Interactions.APIHandlers
{
    /// <summary>
    /// Public class methods for handling API requests related to health.
    /// </summary>
    public class HealthAPIHandlers
    {
        private readonly EngineCore _core;

        public HealthAPIHandlers(EngineCore core)
        {
            _core = core;

            try
            {
            }
            catch (Exception ex)
            {
                LogManager.Error($"Failed to instantiate health API handlers.", ex);
                throw;
            }
        }
    }
}
