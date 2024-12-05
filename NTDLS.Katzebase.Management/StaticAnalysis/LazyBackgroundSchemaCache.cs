using NTDLS.Helpers;
using NTDLS.Katzebase.Api.Models;
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

        /// <summary>
        /// The schemas that we will lazy load next.
        /// </summary>
        private readonly List<KbSchema> _schemaWorkQueue = new();

        /// <summary>
        /// The cache of schemas.
        /// </summary>
        private readonly List<CachedSchema> _schemaCache = new();

        public List<CachedSchema> GetCache(out int cacheHash)
        {
            lock (_schemaCache)
            {
                var results = new List<CachedSchema>();
                results.AddRange(_schemaCache);
                //Mock in a root schema.
                results.Add(new CachedSchema(new(EngineConstants.RootSchemaGUID, "", "", "", Guid.Empty, 0)));

                cacheHash = 0;

                lock (_schemaCache)
                {
                    foreach (var schema in _schemaCache)
                    {
                        cacheHash = HashCode.Combine(cacheHash, schema.GetHashCode());
                    }
                }
            }
            return _schemaCache;
        }

        /// <summary>
        /// Removes all cache items that start with the given schema path.
        /// </summary>
        /// <param name="schemaPath"></param>
        public void Refresh(Guid schemaId)
        {
            lock (_schemaCache)
            {
                lock (_schemaWorkQueue)
                {
                    if (schemaId == EngineConstants.RootSchemaGUID)
                    {
                        _schemaCache.Clear();
                        _schemaWorkQueue.Clear();
                    }
                    else
                    {
                        var schema = _schemaCache.FirstOrDefault(o => o.Schema.Id == schemaId)?.Schema;
                        if (schema != null)
                        {
                            var invalidateItems = _schemaCache.Where(o => o.Schema.Path.StartsWith(schema.Path, StringComparison.InvariantCultureIgnoreCase));
                            foreach (var item in invalidateItems)
                            {
                                item.CachedDateTime = DateTime.UtcNow.AddSeconds(-(ExistingCacheItemRefreshIntervalSeconds * 2));
                            }

                            _schemaWorkQueue.Insert(0, schema);
                        }
                    }
                }
            }

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

            int lastCacheHash = 0;

            if (_keepRunning == false)
            {
                _keepRunning = true;

                _thread = Threading.StartThread(() =>
                {
                    Thread.CurrentThread.Name = $"LazyBackgroundSchemaCache:{Thread.CurrentThread.ManagedThreadId}";

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
                                        lock (_schemaWorkQueue)
                                        {
                                            _schemaWorkQueue.Clear();
                                            _schemaCache.Clear();
                                        }
                                    }
                                }

                                if (ProcessSchemaQueue())
                                {
                                    var schemaCache = GetCache(out var cacheHash);
                                    if (cacheHash != lastCacheHash)
                                    {
                                        lastCacheHash = cacheHash;
                                        OnCacheUpdated?.Invoke(schemaCache);
                                    }
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

            //Waiting on the thread to end can cause a deadlock since the thread invokes the UI thread.
            //_thread?.Join();
            //_thread = null;
        }

        private bool ProcessSchemaQueue()
        {
            bool wereItemsUpdated = false;

            lock (_schemaWorkQueue)
            {
                if (_schemaWorkQueue.Count == 0)
                {
                    _schemaWorkQueue.Add(new KbSchema(EngineConstants.RootSchemaGUID, "", "", "", Guid.Empty, 0));
                }
            }

            var workingQueue = new List<KbSchema>();

            lock (_schemaWorkQueue)
            {
                if (_schemaWorkQueue.Count > 0)
                {
                    workingQueue.AddRange(_schemaWorkQueue);
                    _schemaWorkQueue.Clear();
                }
            }

            //Create a list of schemas that we need to queue up for retrieval.
            foreach (var queuedSchema in workingQueue)
            {
                List<KbSchema>? childSchemas = null;

                try
                {
                    if (ServerExplorerConnection.Client == null || ServerExplorerConnection.Client.IsConnected == false)
                    {
                        return false;
                    }

                    CacheSchema(queuedSchema);
                    childSchemas = ServerExplorerConnection.Client.Schema.List(queuedSchema.Path).Collection;
                }
                catch
                {
                    wereItemsUpdated = true;
                    continue;
                }

                //The list of schemas we obtained from the current work queue schema.
                var schemasInParent = new List<KbSchema>();

                foreach (var childSchema in childSchemas.OrderBy(o => o.Name))
                {
                    if (childSchema.Name != null && childSchema.Path != null && childSchema.ParentPath != null)
                    {
                        schemasInParent.Add(childSchema);

                        wereItemsUpdated = CacheSchema(childSchema) || wereItemsUpdated;
                    }
                }

                lock (_schemaCache)
                {
                    //Find all cached schemas in the parent schema that we just scanned, remove cache items that do not exist anymore.
                    var schemasToRemove = _schemaCache.Where(o => o.Schema.ParentPath == queuedSchema.Path && schemasInParent.Any(s => s.Id == o.Schema.Id) == false).ToList();

                    schemasToRemove.RemoveAll(o => o.Schema.Id == EngineConstants.RootSchemaGUID);

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

                _schemaWorkQueue.AddRange(schemasInParent);
            }

            return wereItemsUpdated;
        }

        private bool CacheSchema(KbSchema childSchema)
        {
            if (ServerExplorerConnection.Client == null || ServerExplorerConnection.Client.IsConnected == false)
            {
                return false;
            }

            bool schemaCacheItemAdded = false;
            bool schemaCacheItemRefreshed = false;
            bool wereItemsUpdated = false;

            CachedSchema? newlyAddedOrUpdatedSchemaCacheItem = null;

            lock (_schemaCache)
            {
                var existingCacheItem = _schemaCache.FirstOrDefault(o => o.Schema.Id == childSchema.Id);

                if (existingCacheItem == null)
                {
                    //Add newly discovered server schema, add it to the cache.
                    newlyAddedOrUpdatedSchemaCacheItem = new CachedSchema(childSchema);
                    _schemaCache.Add(newlyAddedOrUpdatedSchemaCacheItem);
                    wereItemsUpdated = true; //Items were added.
                    schemaCacheItemAdded = true;
                }

                if (existingCacheItem != null && (DateTime.UtcNow - existingCacheItem.CachedDateTime).TotalSeconds > ExistingCacheItemRefreshIntervalSeconds)
                {
                    //We already have this schema cached, but we want to refresh it incase anything has been added.
                    newlyAddedOrUpdatedSchemaCacheItem = existingCacheItem;

                    existingCacheItem.CachedDateTime = DateTime.UtcNow;
                    existingCacheItem.Schema = childSchema;

                    schemaCacheItemRefreshed = true;
                }
            }

            if (newlyAddedOrUpdatedSchemaCacheItem != null)
            {
                try
                {
                    var indexes = ServerExplorerConnection.Client.Schema.Indexes.List(newlyAddedOrUpdatedSchemaCacheItem.Schema.Path);
                    newlyAddedOrUpdatedSchemaCacheItem.Indexes = indexes.Collection;

                    var fields = ServerExplorerConnection.Client.Schema.FieldSample(newlyAddedOrUpdatedSchemaCacheItem.Schema.Path);
                    newlyAddedOrUpdatedSchemaCacheItem.Fields = fields.Collection.Select(o => o.Name).ToList();
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

            return wereItemsUpdated;
        }
    }
}
