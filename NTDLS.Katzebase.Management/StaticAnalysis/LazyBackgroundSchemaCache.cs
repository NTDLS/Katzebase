using NTDLS.Helpers;
using NTDLS.Katzebase.Api;
using NTDLS.Katzebase.Api.Models;
using NTDLS.Katzebase.Management.Classes;
using NTDLS.Katzebase.Parsers;
using NTDLS.Katzebase.Shared;
using static NTDLS.Katzebase.Parsers.Constants;

namespace NTDLS.Katzebase.Management.StaticAnalysis
{
    /// <summary>
    /// Maintains a cache of the server's schemas (and their fields and indexes) for the server explorer and static analysis.
    ///
    /// All server calls and all changes to the cache are made by a single background worker thread, which works through
    /// three prioritized queues:
    ///   High:       Work requested by the user: explicit refreshes, expanding a node and schemas affected by scripts
    ///               executed in the editor. Processed immediately and back to back.
    ///   Children:   Listing the child schemas of a schema (discovery and periodic re-validation).
    ///   Details:    Loading a schema's fields and indexes (the field sample is the most expensive call).
    /// Background (Children/Details) work is paced so that a large server is not flooded with requests, but the worker wakes
    /// immediately when high priority work arrives.
    ///
    /// Readers never touch the worker's state: whenever the cache changes, an immutable SchemaCacheSnapshot is published.
    /// Item events (added/removed/refreshed) are raised on the worker thread as changes are made, and OnCacheUpdated is raised
    /// (coalesced) after the snapshot reflecting those changes has been published.
    /// </summary>
    public class LazyBackgroundSchemaCache
    {
        #region Tuning.

        /// <summary>
        /// How often known schemas have their children re-listed to pick up changes made by other sessions.
        /// </summary>
        private static readonly TimeSpan ChildrenRefreshInterval = TimeSpan.FromSeconds(10);

        /// <summary>
        /// How often known schemas have their fields and indexes reloaded to pick up changes made by other sessions.
        /// </summary>
        private static readonly TimeSpan DetailsRefreshInterval = TimeSpan.FromSeconds(60);

        /// <summary>
        /// How often the cache is checked for schemas that are due to be refreshed. Much shorter than the refresh intervals
        /// so that an item is refreshed promptly once it becomes due, rather than up to a whole interval later.
        /// </summary>
        private static readonly TimeSpan SweepInterval = TimeSpan.FromSeconds(1);

        /// <summary>
        /// Background (non user-requested) work is paced by waiting, after each server call, as long as the call took
        /// (within these bounds). This keeps the cache's load on the server to at most about half of one connection,
        /// without slowing discovery down when calls are fast.
        /// </summary>
        private const int MinBackgroundPacingMilliseconds = 2;
        private const int MaxBackgroundPacingMilliseconds = 250;

        /// <summary>
        /// While a burst of work is being processed, OnCacheUpdated is raised at most this often.
        /// </summary>
        private static readonly TimeSpan PublishCoalesceInterval = TimeSpan.FromMilliseconds(150);

        /// <summary>
        /// Number of times a failed server call is attempted before it is left to the next periodic refresh.
        /// </summary>
        private const int MaxAttempts = 3;

        #endregion

        #region Events.

        public delegate void CacheUpdated(SchemaCacheSnapshot snapshot);
        public delegate void CacheItemUpdated(CachedSchema schemaItem);

        /// <summary>
        /// Raised (on the worker thread) after the cache has changed and a new snapshot has been published.
        /// </summary>
        public event CacheUpdated? OnCacheUpdated;

        /// <summary>
        /// Raised (on the worker thread) when a server schema is discovered. Parents are always raised before their children.
        /// </summary>
        public event CacheItemUpdated? OnCacheItemAdded;

        /// <summary>
        /// Raised (on the worker thread) when a server schema no longer exists. Only raised for the top-most removed schema,
        /// its descendants are implicitly removed with it.
        /// </summary>
        public event CacheItemUpdated? OnCacheItemRemoved;

        /// <summary>
        /// Raised (on the worker thread) when a cached schema's name, fields or indexes change.
        /// </summary>
        public event CacheItemUpdated? OnCacheItemRefreshed;

        #endregion

        /// <summary>
        /// Supplies the client used to query the server. May return null (or a disconnected client) while not connected.
        /// </summary>
        private readonly Func<KbClient?> _clientProvider;

        private enum WorkKind { Children, Details }
        private enum WorkPriority { High, Children, Details }

        private readonly record struct WorkItem(WorkKind Kind, string Path)
        {
            /// <summary>
            /// Paths are case-insensitive, so work is de-duplicated on the lowercase path.
            /// </summary>
            public (WorkKind, string) Key => (Kind, Path.ToLowerInvariant());
        }

        private readonly object _queueLock = new();
        private readonly LinkedList<WorkItem> _highQueue = new();
        private readonly LinkedList<WorkItem> _childrenQueue = new();
        private readonly LinkedList<WorkItem> _detailsQueue = new();
        private readonly Dictionary<(WorkKind, string), LinkedListNode<WorkItem>> _queuedItems = new();
        private readonly AutoResetEvent _workSignal = new(false);
        private volatile bool _sweepRequested = false;

        /// <summary>
        /// The cache itself. Only ever accessed by the worker thread.
        /// </summary>
        private readonly Dictionary<string, CachedSchema> _cache = new(StringComparer.InvariantCultureIgnoreCase);
        private readonly Dictionary<(WorkKind, string), int> _failedAttempts = new();
        private bool _hasUnpublishedChanges = false;
        private DateTime _lastPublishedUtc = DateTime.MinValue;
        private DateTime _nextSweepUtc = DateTime.MinValue;
        private long _version = 0;

        private volatile SchemaCacheSnapshot _snapshot = SchemaCacheSnapshot.Empty;
        private volatile bool _keepRunning = true;

        /// <summary>
        /// Starts the background schema lazy loader process.
        /// </summary>
        public LazyBackgroundSchemaCache(ServerExplorerConnection serverExplorerConnection)
            : this(() => serverExplorerConnection.Client)
        {
        }

        /// <summary>
        /// Starts the background schema lazy loader process using the given client provider.
        /// </summary>
        public LazyBackgroundSchemaCache(Func<KbClient?> clientProvider)
        {
            _clientProvider = clientProvider;

            Threading.StartThread(WorkerThreadProc);
        }

        /// <summary>
        /// Stops the background worker. The thread is not joined because it may be waiting on the UI thread.
        /// </summary>
        public void Stop()
        {
            _keepRunning = false;

            OnCacheUpdated = null;
            OnCacheItemAdded = null;
            OnCacheItemRemoved = null;
            OnCacheItemRefreshed = null;

            _workSignal.Set();
        }

        #region Public API.

        /// <summary>
        /// Returns the most recently published, immutable view of the cache.
        /// </summary>
        public SchemaCacheSnapshot GetSnapshot() => _snapshot;

        /// <summary>
        /// Loads the given schema's children and details ahead of all background work, if they have not been loaded yet.
        /// Called when the user expands a schema node in the tree.
        /// </summary>
        public void PrioritizeSchema(Guid schemaId)
        {
            if (_snapshot.TryGet(schemaId, out var cachedSchema))
            {
                if (cachedSchema.ChildrenLoadedUtc == null)
                {
                    Enqueue(new WorkItem(WorkKind.Children, cachedSchema.Schema.Path), WorkPriority.High);
                }
                if (cachedSchema.DetailsLoadedUtc == null)
                {
                    Enqueue(new WorkItem(WorkKind.Details, cachedSchema.Schema.Path), WorkPriority.High);
                }
            }
        }

        /// <summary>
        /// Immediately reloads the given schema (and everything beneath it) from the server.
        /// For the root schema, this reloads the entire cache.
        /// </summary>
        public void Refresh(Guid schemaId)
        {
            var snapshot = _snapshot;

            string schemaPath;
            if (schemaId == EngineConstants.RootSchemaGUID)
            {
                schemaPath = string.Empty;
            }
            else if (snapshot.TryGet(schemaId, out var cachedSchema))
            {
                schemaPath = cachedSchema.Schema.Path;

                //Re-list the parent too, to pick up the schema itself having been dropped or recreated.
                Enqueue(new WorkItem(WorkKind.Children, SchemaCacheSnapshot.ParentPathOf(schemaPath)), WorkPriority.High);
            }
            else
            {
                return;
            }

            //Parents before children, so newly discovered schemas are processed in tree order.
            var subtree = snapshot.Schemas
                .Where(o => SchemaCacheSnapshot.IsSameOrDescendant(o.Schema.Path, schemaPath))
                .OrderBy(o => o.Schema.Path.Count(c => c == ':'))
                .ThenBy(o => o.Schema.Path, StringComparer.InvariantCultureIgnoreCase)
                .ToList();

            if (subtree.Count == 0 && schemaPath.Length == 0)
            {
                Enqueue(new WorkItem(WorkKind.Children, string.Empty), WorkPriority.High);
            }

            foreach (var item in subtree)
            {
                Enqueue(new WorkItem(WorkKind.Children, item.Schema.Path), WorkPriority.High);
            }
            foreach (var item in subtree.Where(o => o.Schema.Id != EngineConstants.RootSchemaGUID))
            {
                Enqueue(new WorkItem(WorkKind.Details, item.Schema.Path), WorkPriority.High);
            }
        }

        /// <summary>
        /// Immediately reloads the given schemas from the server: the parent's children (to pick up creates and drops) and,
        /// optionally, the schema's own children, fields and indexes.
        /// </summary>
        public void Invalidate(IEnumerable<string> schemaPaths, bool includeChildren = true, bool includeParent = true)
        {
            foreach (var rawPath in schemaPaths)
            {
                var schemaPath = SchemaCacheSnapshot.NormalizePath(rawPath);
                if (schemaPath.Length == 0 || schemaPath.StartsWith('#'))
                {
                    continue; //The root has no details, and temporary schemas are session specific.
                }

                if (includeParent)
                {
                    Enqueue(new WorkItem(WorkKind.Children, SchemaCacheSnapshot.ParentPathOf(schemaPath)), WorkPriority.High);
                }
                if (includeChildren)
                {
                    Enqueue(new WorkItem(WorkKind.Children, schemaPath), WorkPriority.High);
                }
                Enqueue(new WorkItem(WorkKind.Details, schemaPath), WorkPriority.High);
            }
        }

        /// <summary>
        /// Called after a script has been executed on this server. Immediately reloads the schemas that the script may
        /// have changed, so the explorer and static analysis reflect the user's own changes without waiting for the
        /// periodic refresh.
        /// </summary>
        public void NotifyScriptExecuted(string scriptText)
        {
            PreparedQueryBatch batch;
            try
            {
                batch = StaticBatchParser.Parse(KbTextUtility.RemoveNonCode(scriptText), MockEngineCore.Instance.GlobalTokenizerConstants);
            }
            catch
            {
                //We can't tell what the script touched, so revalidate everything (in the background).
                RequestSweep();
                return;
            }

            foreach (var query in batch)
            {
                var schemaPaths = query.Schemas.Select(o => o.Name).ToList();
                if (query.TryGetAttribute<string>(PreparedQuery.Attribute.TargetSchemaName, out var targetSchemaName))
                {
                    schemaPaths.Add(targetSchemaName);
                }

                switch (query.QueryType)
                {
                    case QueryType.Create:
                    case QueryType.Drop:
                    case QueryType.Alter:
                    case QueryType.Attach:
                    case QueryType.Detach:
                    case QueryType.SelectInto:
                        //May have created or dropped schemas (or indexes, or changed fields).
                        Invalidate(schemaPaths);
                        break;
                    case QueryType.Rebuild:
                    case QueryType.Insert:
                    case QueryType.Update:
                    case QueryType.Delete:
                        //Can only change the fields and indexes of existing schemas.
                        Invalidate(schemaPaths, includeChildren: false, includeParent: false);
                        break;
                    case QueryType.Exec:
                        //A procedure can do anything.
                        RequestSweep();
                        break;
                }
            }
        }

        #endregion

        #region Work queue.

        private void Enqueue(WorkItem item, WorkPriority priority)
        {
            item = item with { Path = SchemaCacheSnapshot.NormalizePath(item.Path) };

            lock (_queueLock)
            {
                if (_queuedItems.TryGetValue(item.Key, out var existingNode))
                {
                    if (priority != WorkPriority.High || existingNode.List == _highQueue)
                    {
                        return; //Already queued at the same or a higher priority.
                    }

                    //Promote it.
                    existingNode.List!.Remove(existingNode);
                }

                var queue = priority switch
                {
                    WorkPriority.High => _highQueue,
                    WorkPriority.Children => _childrenQueue,
                    _ => _detailsQueue
                };
                _queuedItems[item.Key] = queue.AddLast(item);
            }

            _workSignal.Set();
        }

        private bool TryDequeue(out WorkItem item, out WorkPriority priority)
        {
            lock (_queueLock)
            {
                foreach (var (queue, queuePriority) in new[] {
                    (_highQueue, WorkPriority.High), (_childrenQueue, WorkPriority.Children), (_detailsQueue, WorkPriority.Details) })
                {
                    if (queue.First != null)
                    {
                        item = queue.First.Value;
                        priority = queuePriority;
                        queue.RemoveFirst();
                        _queuedItems.Remove(item.Key);
                        return true;
                    }
                }
            }

            item = default;
            priority = default;
            return false;
        }

        private bool IsHighPriorityWorkPending()
        {
            lock (_queueLock)
            {
                return _highQueue.Count > 0;
            }
        }

        /// <summary>
        /// Requests that every known schema be revalidated in the background as soon as possible.
        /// </summary>
        private void RequestSweep()
        {
            _sweepRequested = true;
            _workSignal.Set();
        }

        /// <summary>
        /// Queues background revalidation of every cached schema whose children or details are older than their refresh interval.
        /// </summary>
        private void ScheduleSweep(bool force)
        {
            var now = DateTime.UtcNow;
            _nextSweepUtc = now + SweepInterval;

            if (_cache.Count == 0)
            {
                Enqueue(new WorkItem(WorkKind.Children, string.Empty), WorkPriority.Children);
                return;
            }

            foreach (var item in _cache.Values.OrderBy(o => o.Schema.Path.Count(c => c == ':')))
            {
                if (force || item.ChildrenLoadedUtc == null || now - item.ChildrenLoadedUtc > ChildrenRefreshInterval)
                {
                    Enqueue(new WorkItem(WorkKind.Children, item.Schema.Path), WorkPriority.Children);
                }
                if (item.Schema.Id != EngineConstants.RootSchemaGUID
                    && (force || item.DetailsLoadedUtc == null || now - item.DetailsLoadedUtc > DetailsRefreshInterval))
                {
                    Enqueue(new WorkItem(WorkKind.Details, item.Schema.Path), WorkPriority.Details);
                }
            }
        }

        #endregion

        #region Worker.

        private void WorkerThreadProc()
        {
            Thread.CurrentThread.Name = $"LazyBackgroundSchemaCache:{Environment.CurrentManagedThreadId}";

            //Start by discovering the top level schemas.
            _cache[string.Empty] = new CachedSchema(new KbSchema(EngineConstants.RootSchemaGUID, string.Empty, string.Empty, string.Empty, Guid.Empty));
            Enqueue(new WorkItem(WorkKind.Children, string.Empty), WorkPriority.High);

            while (_keepRunning)
            {
                try
                {
                    var client = _clientProvider();
                    if (client == null || client.IsConnected == false)
                    {
                        _workSignal.WaitOne(1000);
                        continue;
                    }

                    if (_sweepRequested || DateTime.UtcNow >= _nextSweepUtc)
                    {
                        bool force = _sweepRequested;
                        _sweepRequested = false;
                        ScheduleSweep(force);
                    }

                    if (TryDequeue(out var item, out var priority))
                    {
                        long startTimestamp = System.Diagnostics.Stopwatch.GetTimestamp();
                        ProcessWorkItem(client, item, priority);
                        var callDuration = System.Diagnostics.Stopwatch.GetElapsedTime(startTimestamp);

                        //Keep the UI current during long bursts, and publish as soon as user-requested work is done.
                        PublishSnapshot(force: priority == WorkPriority.High && IsHighPriorityWorkPending() == false);

                        if (priority != WorkPriority.High)
                        {
                            //Pace background work, but wake immediately if high priority work arrives.
                            _workSignal.WaitOne((int)Math.Clamp(callDuration.TotalMilliseconds,
                                MinBackgroundPacingMilliseconds, MaxBackgroundPacingMilliseconds));
                        }
                    }
                    else
                    {
                        PublishSnapshot(force: true);

                        var untilNextSweep = _nextSweepUtc - DateTime.UtcNow;
                        _workSignal.WaitOne(TimeSpan.FromMilliseconds(Math.Clamp(untilNextSweep.TotalMilliseconds, 10, 1000)));
                    }
                }
                catch
                {
                    //Unexpected failure (individual server calls handle their own errors). Back off briefly and carry on.
                    _workSignal.WaitOne(1000);
                }
            }
        }

        private void ProcessWorkItem(KbClient client, WorkItem item, WorkPriority priority)
        {
            try
            {
                if (item.Kind == WorkKind.Children)
                {
                    LoadChildren(client, item.Path);
                }
                else
                {
                    LoadDetails(client, item.Path);
                }
                _failedAttempts.Remove(item.Key);
            }
            catch
            {
                if (client.IsConnected == false)
                {
                    //Retry once reconnected, without counting this as an attempt.
                    Enqueue(item, priority);
                    return;
                }

                //Probably a timeout. Retry a few times, after that the periodic refresh will pick it up.
                int attempts = _failedAttempts.GetValueOrDefault(item.Key) + 1;
                if (attempts < MaxAttempts)
                {
                    _failedAttempts[item.Key] = attempts;
                    Enqueue(item, priority);
                }
                else
                {
                    _failedAttempts.Remove(item.Key);
                }
            }
        }

        /// <summary>
        /// Lists a schema's children from the server: adds newly discovered schemas, removes dropped ones (and everything
        /// beneath them) and updates renamed or recreated ones.
        /// </summary>
        private void LoadChildren(KbClient client, string parentPath)
        {
            if (_cache.TryGetValue(parentPath, out var parent) == false)
            {
                return; //The parent is not (or no longer) known.
            }

            var serverChildren = client.Schema.List(parentPath).Collection
                .Where(o => o.Name != null && o.Path != null && o.ParentPath != null)
                .OrderBy(o => o.Name, StringComparer.InvariantCultureIgnoreCase)
                .ToList();

            var serverChildPaths = new HashSet<string>(serverChildren.Select(o => SchemaCacheSnapshot.NormalizePath(o.Path)),
                StringComparer.InvariantCultureIgnoreCase);

            //Remove cached children which no longer exist on the server.
            var droppedChildren = _cache.Values
                .Where(o => o.Schema.Id != EngineConstants.RootSchemaGUID
                    && SchemaCacheSnapshot.ParentPathOf(o.Schema.Path).Equals(parentPath, StringComparison.InvariantCultureIgnoreCase)
                    && serverChildPaths.Contains(SchemaCacheSnapshot.NormalizePath(o.Schema.Path)) == false)
                .ToList();

            foreach (var droppedChild in droppedChildren)
            {
                RemoveSubtree(droppedChild);
            }

            //Add newly discovered children, update existing ones.
            foreach (var serverChild in serverChildren)
            {
                var childPath = SchemaCacheSnapshot.NormalizePath(serverChild.Path);

                if (_cache.TryGetValue(childPath, out var cachedChild))
                {
                    if (cachedChild.Schema.Id != serverChild.Id)
                    {
                        //Dropped and recreated with the same name.
                        RemoveSubtree(cachedChild);
                        AddSchema(serverChild);
                    }
                    else if (cachedChild.Schema.Name != serverChild.Name)
                    {
                        var updated = cachedChild.WithSchema(serverChild);
                        _cache[childPath] = updated;
                        _hasUnpublishedChanges = true;
                        OnCacheItemRefreshed?.Invoke(updated);
                    }
                }
                else
                {
                    AddSchema(serverChild);
                }
            }

            if (parent.ChildrenLoadedUtc == null)
            {
                //Static analysis can now tell that schemas missing from this parent do not exist.
                _hasUnpublishedChanges = true;
            }
            _cache[parentPath] = parent.WithChildrenLoaded(DateTime.UtcNow);
        }

        /// <summary>
        /// Loads a schema's fields and indexes from the server.
        /// </summary>
        private void LoadDetails(KbClient client, string schemaPath)
        {
            if (schemaPath.Length == 0 || _cache.TryGetValue(schemaPath, out var cachedSchema) == false)
            {
                return; //The root has no details, or the schema is not (or no longer) known.
            }

            var indexes = client.Schema.Indexes.List(schemaPath);
            var fields = client.Schema.FieldSample(schemaPath).Collection.Select(o => o.Name).ToList();

            //The schema may have been removed while we were waiting on the server.
            if (_cache.TryGetValue(schemaPath, out cachedSchema) == false)
            {
                return;
            }

            bool changed = cachedSchema.AreDetailsDifferent(fields, indexes);

            var updated = cachedSchema.WithDetails(fields, indexes, DateTime.UtcNow);
            _cache[schemaPath] = updated;

            if (changed)
            {
                _hasUnpublishedChanges = true;
                OnCacheItemRefreshed?.Invoke(updated);
            }
        }

        private void AddSchema(KbSchema schema)
        {
            var path = SchemaCacheSnapshot.NormalizePath(schema.Path);
            var cachedSchema = new CachedSchema(schema);

            _cache[path] = cachedSchema;
            _hasUnpublishedChanges = true;
            OnCacheItemAdded?.Invoke(cachedSchema);

            //Continue discovery beneath the new schema, and load its fields and indexes, in the background.
            Enqueue(new WorkItem(WorkKind.Children, path), WorkPriority.Children);
            Enqueue(new WorkItem(WorkKind.Details, path), WorkPriority.Details);
        }

        private void RemoveSubtree(CachedSchema topMostRemoved)
        {
            var removedPaths = _cache.Keys
                .Where(o => SchemaCacheSnapshot.IsSameOrDescendant(o, topMostRemoved.Schema.Path))
                .ToList();

            foreach (var path in removedPaths)
            {
                _cache.Remove(path);
            }

            _hasUnpublishedChanges = true;
            OnCacheItemRemoved?.Invoke(topMostRemoved);
        }

        /// <summary>
        /// Publishes a new snapshot if the cache has changed since the last one. Unless forced, publishing is
        /// rate limited so that bulk discovery does not trigger a re-analysis for every schema.
        /// </summary>
        private void PublishSnapshot(bool force)
        {
            if (_hasUnpublishedChanges == false)
            {
                return;
            }

            var now = DateTime.UtcNow;
            if (force == false && now - _lastPublishedUtc < PublishCoalesceInterval)
            {
                return;
            }

            _hasUnpublishedChanges = false;
            _lastPublishedUtc = now;

            var snapshot = new SchemaCacheSnapshot(
                new Dictionary<string, CachedSchema>(_cache, StringComparer.InvariantCultureIgnoreCase), ++_version);
            _snapshot = snapshot;

            OnCacheUpdated?.Invoke(snapshot);
        }

        #endregion
    }
}
