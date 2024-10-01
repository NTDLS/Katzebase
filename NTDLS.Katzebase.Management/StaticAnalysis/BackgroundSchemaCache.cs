using NTDLS.Helpers;
using NTDLS.Katzebase.Client;
using NTDLS.Katzebase.Client.Exceptions;
using NTDLS.Katzebase.Client.Payloads;
using NTDLS.Katzebase.Shared;

namespace NTDLS.Katzebase.Management.StaticAnalysis
{
    /// <summary>
    /// Maintains a cache of schema information from the server.
    /// </summary>
    internal static class BackgroundSchemaCache
    {
        /// <summary>
        /// The interval in which the lazy-loader will refresh information about already cached schemas.
        /// </summary>
        private const int ExistingCacheItemRefreshIntervalSeconds = 10;

        public delegate void CacheUpdated(List<CachedSchema> schemaCache);
        public static event CacheUpdated? OnCacheUpdated;

        public delegate void CacheItemUpdated(CachedSchema schemaItem);

        /// <summary>
        /// Event called when a server schema is discovered.
        /// </summary>
        public static event CacheItemUpdated? OnCacheItemAdded;
        /// <summary>
        /// Event called when a server schema is removed from the server.
        /// </summary>
        public static event CacheItemUpdated? OnCacheItemRemoved;
        /// <summary>
        /// Event called when a the background lazy-load is doing a periodic refresh of the schema.
        /// </summary>
        public static event CacheItemUpdated? OnCacheItemRefreshed;

        private static Thread? _thread;
        private static KbClient? _client;
        private static bool _keepRunning = false;
        private static bool _resetState = false;

        /// <summary>
        /// The schemas that we will lazy load next.
        /// </summary>
        private static List<KbSchema> _schemaWorkQueue = new();

        /// <summary>
        /// The cache of schemas.
        /// </summary>
        private static readonly List<CachedSchema> _schemaCache = new();

        public static List<CachedSchema> GetCache()
        {
            lock (_schemaCache)
            {
                var results = new List<CachedSchema>();
                results.AddRange(_schemaCache);
                //Mock in a root schema.
                results.Add(new CachedSchema(new(EngineConstants.RootSchemaGUID, "", "", "", Guid.Empty, 0)));
            }
            return _schemaCache;
        }

        /// <summary>
        /// Starts the background worker or changes the client if its already running.
        /// </summary>
        public static void StartOrReset(KbClient client)
        {
            _client = client;
            _resetState = true;
            OnCacheUpdated = null;
            OnCacheItemAdded = null;
            OnCacheItemRemoved = null;

            if (_keepRunning == false)
            {
                _keepRunning = true;

                _thread = Threading.StartThread(() =>
                {
                    while (_keepRunning)
                    {
                        if (_resetState)
                        {
                            _schemaWorkQueue.Clear();
                            lock (_schemaCache)
                            {
                                _schemaCache.Clear();
                            }
                            _resetState = false;
                        }

                        try
                        {
                            while (_keepRunning)
                            {
                                if (ProcessSchemaQueue())
                                {
                                    var schemaCache = GetCache();
                                    OnCacheUpdated?.Invoke(schemaCache);
                                }
                                Thread.Sleep(1000);
                            }
                        }
                        catch
                        {
                        }

                        if (_keepRunning)
                        {
                            //An exception occurred, sleep and retry the connection.
                            for (int i = 0; _keepRunning && i < 1000; i++)
                            {
                                Thread.Sleep(5);
                            }
                        }
                    }
                });
            }
        }

        public static void Stop()
        {
            _keepRunning = false;

            OnCacheUpdated = null;
            OnCacheItemAdded = null;
            OnCacheItemRemoved = null;

            _thread?.Join();
            _thread = null;
        }

        private static bool ProcessSchemaQueue()
        {
            bool wereItemsUpdated = false;

            if (_schemaWorkQueue.Count == 0)
            {
                _schemaWorkQueue.Add(new KbSchema(EngineConstants.RootSchemaGUID, "", "", "", Guid.Empty, 0));
            }

            var newQueue = new List<KbSchema>();

            //Create a list of schemas that we need to queue up for retrieval.
            foreach (var queued in _schemaWorkQueue)
            {
                List<KbSchema>? serverSchemas = null;

                try
                {
                    if (_client == null || _client.IsConnected == false)
                    {
                        return false;
                    }
                    serverSchemas = _client.Schema.List(queued.Path).Collection;
                }
                //TODO: Do not remove item when the issue is a timeout.
                catch (Exception ex)
                {
                    //We ran into an issue with the query, as a dumb safety measure,
                    //  just remove the schema from the queue and abandon the enumeration.
                    wereItemsUpdated = true;
                    _schemaWorkQueue.Remove(queued);
                    break;
                }

                //The list of schemas we obtained from the current work queue schema.
                var schemasInParent = new List<KbSchema>();

                foreach (var serverSchema in serverSchemas)
                {
                    if (serverSchema.Name != null && serverSchema.Path != null && serverSchema.ParentPath != null)
                    {
                        schemasInParent.Add(serverSchema);

                        CachedSchema? newlyAddedOrUpdatedSchemaCacheItem = null;

                        lock (_schemaCache)
                        {
                            var existingCacheItem = _schemaCache.FirstOrDefault(o => o.Schema.Id == serverSchema.Id);

                            if (existingCacheItem == null)
                            {
                                //Add newly discovered server schema, add it to the cache.
                                newlyAddedOrUpdatedSchemaCacheItem = new CachedSchema(serverSchema);
                                OnCacheItemAdded?.Invoke(newlyAddedOrUpdatedSchemaCacheItem);
                                _schemaCache.Add(newlyAddedOrUpdatedSchemaCacheItem);
                                wereItemsUpdated = true; //Items were added.
                            }

                            if (existingCacheItem != null && (DateTime.UtcNow - existingCacheItem.CachedDateTime).TotalSeconds > ExistingCacheItemRefreshIntervalSeconds)
                            {
                                //We already have this schema cached, but we want to refresh it incase anything has been added.
                                newlyAddedOrUpdatedSchemaCacheItem = existingCacheItem;

                                existingCacheItem.CachedDateTime = DateTime.UtcNow;
                                existingCacheItem.Schema = serverSchema;

                                OnCacheItemRefreshed?.Invoke(newlyAddedOrUpdatedSchemaCacheItem);
                            }
                        }

                        if (newlyAddedOrUpdatedSchemaCacheItem != null)
                        {
                            try
                            {
                                var indexes = _client.Schema.Indexes.List(newlyAddedOrUpdatedSchemaCacheItem.Schema.Path);
                                newlyAddedOrUpdatedSchemaCacheItem.Indexes = indexes.Collection;
                            }
                            catch
                            {
                                //Probably a timeout, carry on...
                            }
                        }
                    }
                }

                lock (_schemaCache)
                {
                    //Find all cached schemas in the parent schema that we just scanned, remove cache items that do not exist anymore.
                    var itemsToRemove = _schemaCache.Where(o => o.Schema.ParentPath == queued.Path && schemasInParent.Any(s => s.Id == o.Schema.Id) == false).ToList();

                    foreach (var itemToRemove in itemsToRemove)
                    {
                        _schemaCache.Remove(itemToRemove);
                        OnCacheItemRemoved?.Invoke(itemToRemove);
                    }

                    wereItemsUpdated = wereItemsUpdated || itemsToRemove.Any(); //Items were removed.
                }

                newQueue.AddRange(schemasInParent);
            }

            _schemaWorkQueue = newQueue;

            return wereItemsUpdated;
        }
    }
}
