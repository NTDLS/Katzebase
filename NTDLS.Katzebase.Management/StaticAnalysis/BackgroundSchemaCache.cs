using NTDLS.Helpers;
using NTDLS.Katzebase.Client;
using NTDLS.Katzebase.Client.Payloads;

namespace NTDLS.Katzebase.Management.StaticAnalysis
{
    /// <summary>
    /// Maintains a cache of schema information from the server.
    /// </summary>
    internal class BackgroundSchemaCache
    {
        private static readonly object _instantiationLock = new();
        private static BackgroundSchemaCache? _instance;

        public static BackgroundSchemaCache Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_instantiationLock)
                    {
                        if (_instance == null)
                        {
                            var instance = new BackgroundSchemaCache();

                            _instance = instance;
                        }
                    }
                }

                return _instance;
            }
        }

        public List<CachedSchema> GetCache()
        {
            lock (_schemaCache)
            {
                var results = new List<CachedSchema>();
                results.AddRange(_schemaCache);
            }
            return _schemaCache;
        }

        private Thread? _thread;
        private KbClient? _client;
        private bool _keepRunning = false;
        private bool _resetState = false;

        /// <summary>
        /// The schemas that we will lazy load next.
        /// </summary>
        List<QueuedSchema> _schemaWorkQueue = new();

        /// <summary>
        /// The cache of schemas.
        /// </summary>
        readonly List<CachedSchema> _schemaCache = new();

        /// <summary>
        /// Starts the background worker or changes the client if its already running.
        /// </summary>
        public void StartOrReset(KbClient client)
        {
            _client = client;

            _resetState = true;

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
                                ProcessSchemaQueue();
                                Thread.Sleep(1000);
                            }
                        }
                        catch
                        {
                        }

                        if (_keepRunning)
                        {
                            //An exception occured, sleep and retry the connection.
                            Thread.Sleep(1000 * 10);
                        }
                    }
                });
            }
        }

        public void Stop()
        {
            _keepRunning = false;
            _thread?.Join();
            _thread = null;
        }

        private void ProcessSchemaQueue()
        {
            if (_schemaWorkQueue.Count == 0)
            {
                _schemaWorkQueue.Add(new(Guid.Empty, "", "", ""));
            }

            var newQueue = new List<QueuedSchema>();

            //Create a list of schemas that we need to queue up for retrevial.
            foreach (var queued in _schemaWorkQueue)
            {
                List<KbSchemaItem>? serverSchemas = null;

                try
                {
                    if (_client == null || _client.IsConnected == false)
                    {
                        return;
                    }
                    serverSchemas = _client.Schema.List(queued.Path).Collection;
                }
                catch (Exception ex)
                {
                    _schemaWorkQueue.Remove(queued);
                    break;
                }

                //The list of schemas we obtained from the current work queue schema.
                var schemasInParent = new List<QueuedSchema>();

                foreach (var serverSchema in serverSchemas)
                {
                    if (serverSchema.Name != null && serverSchema.Path != null && serverSchema.ParentPath != null)
                    {
                        var queuedSchema = new QueuedSchema(serverSchema.Id.EnsureNotNull(), serverSchema.Name, serverSchema.Path, serverSchema.ParentPath);
                        schemasInParent.Add(queuedSchema);

                        lock (_schemaCache)
                        {
                            //Add newly obtained schemas to the cache.
                            if (_schemaCache.Any(o => o.Id == serverSchema.Id) == false)
                            {
                                var cachedSchema = new CachedSchema(serverSchema.Id.EnsureNotNull(), serverSchema.Name, serverSchema.Path, serverSchema.ParentPath);
                                _schemaCache.Add(cachedSchema);
                            }
                        }
                    }
                }

                lock (_schemaCache)
                {
                    //Find all cached schemas in the parent schema that we just scanned, remove cache items that do not exist anymore.
                    _schemaCache.RemoveAll(o => o.ParentPath == queued.Path && schemasInParent.Any(s => s.Id == o.Id) == false);
                }

                newQueue.AddRange(schemasInParent);
            }

            _schemaWorkQueue = newQueue;
        }
    }
}
