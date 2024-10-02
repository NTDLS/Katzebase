using NTDLS.Helpers;
using NTDLS.Katzebase.Client.Payloads;
using NTDLS.Katzebase.Management.Classes;
using NTDLS.Katzebase.Shared;

namespace NTDLS.Katzebase.Management.StaticAnalysis
{
    /// <summary>
    /// Maintains a cache of schema information from the server.
    /// </summary>
    public class LazyBackgroundSchemaCache
    {
        /// <summary>
        /// The interval in which the lazy-loader will refresh information about already cached schemas.
        /// </summary>
        private const int ExistingCacheItemRefreshIntervalSeconds = 10;

        public delegate void CacheUpdated(List<CachedSchema> schemaCache);
        public event CacheUpdated? OnCacheUpdated;

        public delegate void CacheItemUpdated(CachedSchema schemaItem);

        public ServerExplorerConnection ServerExplorerConnection { get; private set; }

        /// <summary>
        /// Event called when a server schema is discovered.
        /// </summary>
        public event CacheItemUpdated? OnCacheItemAdded;
        /// <summary>
        /// Event called when a server schema is removed from the server.
        /// </summary>
        public event CacheItemUpdated? OnCacheItemRemoved;
        /// <summary>
        /// Event called when a the background lazy-load is doing a periodic refresh of the schema.
        /// </summary>
        public event CacheItemUpdated? OnCacheItemRefreshed;

        private Thread? _thread;
        private bool _keepRunning = false;
        private bool _resetState = false;
        private string? _refreshSchemaPath = null;

        /// <summary>
        /// The schemas that we will lazy load next.
        /// </summary>
        private List<KbSchema> _schemaWorkQueue = new();

        /// <summary>
        /// The cache of schemas.
        /// </summary>
        private readonly List<CachedSchema> _schemaCache = new();

        public List<CachedSchema> GetCache()
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
        /// Removes all cache items that start with the given schema path.
        /// </summary>
        /// <param name="schemaPath"></param>
        public void Refresh(string? schemaPath)
        {
            _refreshSchemaPath = schemaPath;
        }

        /// <summary>
        /// Starts the background schema lazy loader process.
        /// </summary>
        public LazyBackgroundSchemaCache(ServerExplorerConnection _serverExplorerConnection)
        {
            ServerExplorerConnection = _serverExplorerConnection;

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
                        try
                        {
                            while (_keepRunning)
                            {
                                if (_resetState)
                                {
                                    _resetState = false;
                                    lock (_schemaCache)
                                    {
                                        _schemaWorkQueue.Clear();
                                        _schemaCache.Clear();
                                    }
                                }

                                if (_refreshSchemaPath != null)
                                {
                                    lock (_schemaCache)
                                    {
                                        _schemaWorkQueue.Clear();

                                        _schemaCache.RemoveAll(o => o.Schema.Path.StartsWith(_refreshSchemaPath, StringComparison.InvariantCultureIgnoreCase));

                                        _schemaCache.Clear();
                                    }

                                    _refreshSchemaPath = null;
                                }

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

        public void Stop()
        {
            _keepRunning = false;

            OnCacheUpdated = null;
            OnCacheItemAdded = null;
            OnCacheItemRemoved = null;

            _thread?.Join();
            _thread = null;
        }

        private bool ProcessSchemaQueue()
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
                    if (ServerExplorerConnection.Client == null || ServerExplorerConnection.Client.IsConnected == false)
                    {
                        return false;
                    }
                    serverSchemas = ServerExplorerConnection.Client.Schema.List(queued.Path).Collection;
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

                foreach (var serverSchema in serverSchemas.OrderBy(o => o.Name))
                {
                    if (serverSchema.Name != null && serverSchema.Path != null && serverSchema.ParentPath != null)
                    {
                        schemasInParent.Add(serverSchema);

                        bool schemaCacheItemAdded = false;
                        bool schemaCacheItemRefreshed = false;

                        CachedSchema? newlyAddedOrUpdatedSchemaCacheItem = null;

                        lock (_schemaCache)
                        {
                            var existingCacheItem = _schemaCache.FirstOrDefault(o => o.Schema.Id == serverSchema.Id);

                            if (existingCacheItem == null)
                            {
                                //Add newly discovered server schema, add it to the cache.
                                newlyAddedOrUpdatedSchemaCacheItem = new CachedSchema(serverSchema);
                                _schemaCache.Add(newlyAddedOrUpdatedSchemaCacheItem);
                                wereItemsUpdated = true; //Items were added.
                                schemaCacheItemAdded = true;
                            }

                            if (existingCacheItem != null && (DateTime.UtcNow - existingCacheItem.CachedDateTime).TotalSeconds > ExistingCacheItemRefreshIntervalSeconds)
                            {
                                //We already have this schema cached, but we want to refresh it incase anything has been added.
                                newlyAddedOrUpdatedSchemaCacheItem = existingCacheItem;

                                existingCacheItem.CachedDateTime = DateTime.UtcNow;
                                existingCacheItem.Schema = serverSchema;

                                schemaCacheItemRefreshed = true;
                            }
                        }

                        if (newlyAddedOrUpdatedSchemaCacheItem != null)
                        {
                            try
                            {
                                var indexes = ServerExplorerConnection.Client.Schema.Indexes.List(newlyAddedOrUpdatedSchemaCacheItem.Schema.Path);
                                newlyAddedOrUpdatedSchemaCacheItem.Indexes = indexes.Collection;
                            }
                            catch
                            {
                                //Probably a timeout, carry on...
                            }
                        }

                        if (schemaCacheItemAdded && newlyAddedOrUpdatedSchemaCacheItem != null)
                        {
                            OnCacheItemAdded?.Invoke(newlyAddedOrUpdatedSchemaCacheItem);
                        }
                        if (schemaCacheItemRefreshed && newlyAddedOrUpdatedSchemaCacheItem != null)
                        {
                            OnCacheItemRefreshed?.Invoke(newlyAddedOrUpdatedSchemaCacheItem);
                        }
                    }
                }

                lock (_schemaCache)
                {
                    //Find all cached schemas in the parent schema that we just scanned, remove cache items that do not exist anymore.
                    var schemasToRemove = _schemaCache.Where(o => o.Schema.ParentPath == queued.Path && schemasInParent.Any(s => s.Id == o.Schema.Id) == false).ToList();

                    foreach (var schemaToRemove in schemasToRemove)
                    {
                        _schemaCache.Remove(schemaToRemove);
                        OnCacheItemRemoved?.Invoke(schemaToRemove);

                        //Remove children of the deleted schema.
                        var childSchemasToRemove = _schemaCache.Where(o => o.Schema.Path.StartsWith(schemaToRemove.Schema.Path, StringComparison.InvariantCultureIgnoreCase)).ToList();
                        _schemaCache.RemoveAll(o => childSchemasToRemove.Contains(o));

                        foreach (var childSchemaToRemove in childSchemasToRemove)
                        {
                            OnCacheItemRemoved?.Invoke(childSchemaToRemove);
                        }
                    }

                    wereItemsUpdated = wereItemsUpdated || schemasToRemove.Any(); //Items were removed.
                }

                newQueue.AddRange(schemasInParent);
            }

            _schemaWorkQueue = newQueue;

            return wereItemsUpdated;
        }
    }
}
