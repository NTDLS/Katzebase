using NTDLS.Katzebase.Api;
using System.Reflection;

namespace NTDLS.Katzebase.Engine.Tests.Utilities
{
    internal static class DataImporter
    {
        /// <summary>
        /// Locates tab-separated-value text files in the currently executing assembly that are within the
        /// namespace denoted by [pathPart], parses them and imports them into the schema denoted by [targetSchema].
        /// </summary>
        public static void ImportTabSeparatedFiles(KbClient client, string pathPart, string targetSchema)
        {
            var assembly = Assembly.GetExecutingAssembly();

            var dataFiles = assembly.GetManifestResourceNames()
                .Where(o => o.Contains(pathPart, StringComparison.InvariantCultureIgnoreCase)
                && o.EndsWith(".txt", StringComparison.InvariantCultureIgnoreCase));

            client.Schema.DropIfExists(targetSchema);
            client.Schema.Create(targetSchema);

            foreach (var dataFile in dataFiles)
            {
                string schemaName = dataFile.Substring(dataFile[..^4].LastIndexOf('.') + 1)[..^4];

                client.Schema.DropIfExists($"{targetSchema}:{schemaName}");
                client.Schema.Create($"{targetSchema}:{schemaName}");

                using var stream = assembly.GetManifestResourceStream(dataFile)
                    ?? throw new InvalidOperationException($"Data file not found: [{dataFile}].");

                using var reader = new StreamReader(stream);
                var dataTSV = reader.ReadToEnd().Replace("\r\n", "\n");

                bool IsFirst = true;
                var fieldNames = new List<string>();

                var rowTexts = dataTSV.Split("\n", StringSplitOptions.RemoveEmptyEntries);

                client.Transaction.Begin();
                try
                {
                    foreach (var rowText in rowTexts)
                    {
                        var rowValues = rowText.Split('\t').ToList();

                        if (IsFirst)
                        {
                            IsFirst = false;
                            //Get field names.
                            fieldNames.AddRange(rowValues);
                            continue;
                        }

                        if (rowValues.Count != fieldNames.Count)
                        {
                            throw new Exception("Field/value count mismatch.");
                        }

                        var rowPayload = new Dictionary<string, string>();
                        for (int i = 0; i < fieldNames.Count; i++)
                        {
                            rowPayload.Add(fieldNames[i], rowValues[i]);
                        }

                        client.Document.Store($"{targetSchema}:{schemaName}", rowPayload);
                    }
                }
                finally
                {
                    client.Transaction.Commit();
                }
            }
        }
    }
}

