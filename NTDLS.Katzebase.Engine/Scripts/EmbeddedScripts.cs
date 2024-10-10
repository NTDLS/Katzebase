using System.Reflection;
using System.Runtime.Caching;

namespace NTDLS.Katzebase.Engine.Scripts
{
    internal class EmbeddedScripts
    {
        private static readonly MemoryCache _cache = new("ManagedDataStorageInstance");

        public static string Load(string scriptNameOrText)
        {
            string cacheKey = $":{scriptNameOrText.ToLowerInvariant()}".Replace('.', ':').Replace('\\', ':').Replace('/', ':');

            if (_cache.Get(cacheKey) is string cachedScriptText)
            {
                return cachedScriptText;
            }

            var assemblies = AppDomain.CurrentDomain.GetAssemblies();

            foreach (var assembly in assemblies)
            {
                var scriptText = SearchAssembly(assembly, cacheKey);
                if (scriptText != null)
                {
                    return scriptText;
                }
            }

            throw new Exception($"Embedded script not found: [{scriptNameOrText}]");
        }

        /// <summary>
        /// Searches the given assembly for a script file.
        /// </summary>
        private static string? SearchAssembly(Assembly assembly, string scriptName)
        {
            string cacheKey = scriptName;

            var allScriptNames = _cache.Get($"TranslateSqlScript:SearchAssembly:{assembly.FullName}") as List<string>;
            if (allScriptNames == null)
            {
                allScriptNames = assembly.GetManifestResourceNames().Where(o => o.EndsWith(".kbs", StringComparison.InvariantCultureIgnoreCase))
                    .Select(o => $":{o}".Replace('.', ':')).ToList();
                _cache.Add("TranslateSqlScript:Names", allScriptNames, new CacheItemPolicy
                {
                    SlidingExpiration = new TimeSpan(1, 0, 0)
                });
            }

            if (allScriptNames.Count > 0)
            {
                var script = allScriptNames.Where(o => o.EndsWith(cacheKey, StringComparison.InvariantCultureIgnoreCase)).ToList();
                if (script.Count > 1)
                {
                    throw new Exception($"Ambiguous script name: [{cacheKey}].");
                }
                else if (script == null || script.Count == 0)
                {
                    return null;
                }

                using var stream = assembly.GetManifestResourceStream(script.Single().Replace(':', '.').Trim(new char[] { '.' }))
                    ?? throw new InvalidOperationException($"Script not found: [{cacheKey}].");

                using var reader = new StreamReader(stream);
                var scriptText = reader.ReadToEnd();

                _cache.Add(cacheKey, allScriptNames, new CacheItemPolicy
                {
                    SlidingExpiration = new TimeSpan(1, 0, 0)
                });

                return scriptText;
            }

            return null;
        }
    }
}
