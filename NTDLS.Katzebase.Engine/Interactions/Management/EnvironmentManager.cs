using Newtonsoft.Json;
using NTDLS.Katzebase.Api.Exceptions;
using NTDLS.Katzebase.Engine.Atomicity;
using NTDLS.Katzebase.Engine.Interactions.APIHandlers;
using NTDLS.Katzebase.Engine.Interactions.QueryHandlers;
using NTDLS.Katzebase.Parsers.Query.SupportingTypes;

namespace NTDLS.Katzebase.Engine.Interactions.Management
{
    /// <summary>
    /// Public core class methods for locking, reading, writing and managing tasks related to configuration.
    /// </summary>
    public class EnvironmentManager
    {
        private readonly EngineCore _core;

        internal EnvironmentQueryHandlers QueryHandlers { get; private set; }
        public EnvironmentAPIHandlers APIHandlers { get; private set; }

        internal EnvironmentManager(EngineCore core)
        {
            _core = core;

            try
            {
                QueryHandlers = new EnvironmentQueryHandlers(core);
                APIHandlers = new EnvironmentAPIHandlers(core);
            }
            catch (Exception ex)
            {
                LogManager.Error("Failed to instantiate environment manager.", ex);
                throw;
            }
        }

        internal void Alter(Transaction transaction, IReadOnlyDictionary<string, QueryAttribute> configuration)
        {
            try
            {
                var appSettingsPath = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
                if (!File.Exists(appSettingsPath))
                {
                    throw new KbEngineException($"Could not locate configuration file: [{appSettingsPath}[.");
                }

                var settingsType = _core.Settings.GetType();

                foreach (var item in configuration)
                {
                    //Update the running engine setting.
                    var property = settingsType.GetProperty(item.Key);
                    if (property != null && property.CanWrite)
                    {
                        property.SetValue(_core.Settings, Convert.ChangeType(item.Value.Value, property.PropertyType));
                    }
                }

                //Save the new settings to file.
                File.WriteAllText(appSettingsPath, JsonConvert.SerializeObject(_core.Settings, Formatting.Indented));
            }
            catch (Exception ex)
            {
                LogManager.Error($"Failed to alter environment manager for process id {transaction.ProcessId}.", ex);
                throw;
            }
        }
    }
}
