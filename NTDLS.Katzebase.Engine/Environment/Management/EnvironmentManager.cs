﻿using Katzebase.Engine.Atomicity;
using Katzebase.Shared;
using Katzebase;
using Katzebase.Exceptions;
using System.Text.Json;
using static Katzebase.Engine.Query.PreparedQuery;

namespace Katzebase.Engine.Health.Management
{
    /// <summary>
    /// Public core class methods for locking, reading, writing and managing tasks related to configuration.
    /// </summary>
    public class EnvironmentManager
    {
        private readonly Core core;

        internal EnvironmentQueryHandlers QueryHandlers { get; set; }
        public EnvironmentAPIHandlers APIHandlers { get; set; }

        public EnvironmentManager(Core core)
        {
            this.core = core;

            try
            {
                QueryHandlers = new EnvironmentQueryHandlers(core);
                APIHandlers = new EnvironmentAPIHandlers(core);
            }
            catch (Exception ex)
            {
                core.Log.Write("Failed to instanciate environment manager.", ex);
                throw;
            }
        }

        static void UpdateSettingProperty<T>(T obj, string propertyName, object newValue)
        {
            Type type = typeof(T);
            var property = type.GetProperty(propertyName);
            if (property != null && property.CanWrite)
            {
                property.SetValue(obj, Convert.ChangeType(newValue, property.PropertyType));
            }
        }

        internal void Alter(Transaction transaction, Dictionary<QueryAttribute, object> attributes)
        {
            try
            {
                var appSettingsPath = Path.Combine(AppContext.BaseDirectory, "appsettings.json");

                if (!File.Exists(appSettingsPath))
                {
                    throw new KbEngineException($"Could not locate configuration file: '{appSettingsPath}'.");
                }

                string json = File.ReadAllText(appSettingsPath);

                // Parse the JSON into a JsonDocument
                using (JsonDocument document = JsonDocument.Parse(json))
                {
                    var root = document.RootElement;
                    var settingsElement = root.GetProperty("Settings");
                    var settings = JsonSerializer.Deserialize<KatzebaseSettings>(settingsElement.ToString());
                    KbUtility.EnsureNotNull(settings);

                    foreach (var settingElement in settingsElement.EnumerateObject())
                    {
                        if (Enum.TryParse(settingElement.Name, true, out QueryAttribute optionType))
                        {
                            if (attributes.TryGetValue(optionType, out var value))
                            {
                                UpdateSettingProperty(settings, settingElement.Name, value); //Save the value in the JSON settings file.
                                UpdateSettingProperty(core.Settings, settingElement.Name, value); //Save the setting in the live core.
                            }
                        }
                    }

                    string updatedSettingsJson = JsonSerializer.Serialize(settings);
                    using (var file = File.Create(appSettingsPath))
                    {
                        using (var writer = new Utf8JsonWriter(file, new JsonWriterOptions { Indented = true }))
                        {
                            writer.WriteStartObject();
                            foreach (var property in root.EnumerateObject())
                            {
                                if (property.Name == "Settings")
                                {
                                    writer.WritePropertyName(property.Name);
                                    writer.WriteRawValue(updatedSettingsJson);
                                }
                                else
                                {
                                    property.WriteTo(writer);
                                }
                            }
                            writer.WriteEndObject();
                        }
                        file.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                core.Log.Write($"Failed to alter environment manager for process id {transaction.ProcessId}.", ex);
                throw;
            }
        }

    }
}