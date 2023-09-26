using Newtonsoft.Json;
using NTDLS.Katzebase.Engine.Interactions.APIHandlers;
using NTDLS.Katzebase.Engine.Interactions.QueryHandlers;
using NTDLS.Katzebase.Engine.Trace;
using static NTDLS.Katzebase.Engine.Library.EngineConstants;
using static NTDLS.Katzebase.Engine.Trace.PerformanceTrace;
using static NTDLS.Katzebase.KbConstants;

namespace NTDLS.Katzebase.Engine.Atomicity.Management
{
    /// <summary>
    /// Public core class methods for locking, reading, writing and managing tasks related to transactions.
    /// </summary>
    public class TransactionManager
    {
        internal TransactionQueryHandlers QueryHandlers { get; private set; }
        public TransactiontAPIHandlers APIHandlers { get; private set; }
        internal List<Transaction> Collection = new();
        private readonly Core _core;
        internal TransactionReference Acquire(ulong processId) => Acquire(processId, false);

        internal List<Transaction> CloneTransactions()
        {
            lock (Collection)
            {
                var clone = new List<Transaction>();
                clone.AddRange(Collection);
                return clone;
            }
        }

        public TransactionManager(Core core)
        {
            _core = core;
            try
            {
                QueryHandlers = new TransactionQueryHandlers(core);
                APIHandlers = new TransactiontAPIHandlers(core);
            }
            catch (Exception ex)
            {
                core.Log.Write("Failed to instanciate transaction manager.", ex);
                throw;
            }
        }

        internal Transaction? GetByProcessId(ulong processId)
        {
            try
            {
                lock (Collection)
                {
                    var transaction = (from o in Collection where o.ProcessId == processId select o).FirstOrDefault();
                    return transaction;
                }
            }
            catch (Exception ex)
            {
                _core.Log.Write($"Failed to get transaction by process id for process id {processId}.", ex);
                throw;
            }
        }

        internal void RemoveByProcessId(ulong processId)
        {
            try
            {
                lock (Collection)
                {
                    var transaction = GetByProcessId(processId);
                    if (transaction != null)
                    {
                        Collection.Remove(transaction);
                    }
                }
            }
            catch (Exception ex)
            {
                _core.Log.Write($"Failed to remove transaction by process id for process {processId}.", ex);
                throw;
            }
        }

        /// <summary>
        /// Kills all transactions associated with the given processIDs. This is typically called from the session manager and probably should not be called otherwise.
        /// </summary>
        /// <param name="processIDs"></param>
        internal void CloseByProcessIDs(List<ulong> processIDs)
        {
            try
            {
                lock (Collection)
                {
                    foreach (var processId in processIDs)
                    {
                        var transaction = GetByProcessId(processId);
                        if (transaction != null)
                        {
                            transaction.Rollback();
                            Collection.Remove(transaction);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _core.Log.Write($"Failed to remove transactions by processIDs.", ex);
                throw;
            }
        }

        internal void Recover()
        {
            try
            {
                Directory.CreateDirectory(_core.Settings.TransactionDataPath);

                var transactionFiles = Directory.EnumerateFiles(_core.Settings.TransactionDataPath, TransactionActionsFile, SearchOption.AllDirectories).ToList();
                if (transactionFiles.Any())
                {
                    _core.Log.Write($"Found {transactionFiles.Count()} open transactions.", KbLogSeverity.Warning);
                }

                foreach (string transactionFile in transactionFiles)
                {
                    var processIdString = Path.GetFileNameWithoutExtension(Path.GetDirectoryName(transactionFile));
                    KbUtility.EnsureNotNull(processIdString);

                    ulong processId = ulong.Parse(processIdString);

                    var transaction = new Transaction(_core, this, processId, true);

                    var atoms = File.ReadLines(transactionFile).ToList();
                    foreach (var atom in atoms)
                    {
                        var ra = JsonConvert.DeserializeObject<Atom>(atom);
                        KbUtility.EnsureNotNull(ra);
                        transaction.Atoms.Add(ra);
                    }

                    _core.Log.Write($"Rolling back session {transaction.ProcessId} with {transaction.Atoms.Count} actions.", KbLogSeverity.Warning);

                    try
                    {
                        transaction.Rollback();
                    }
                    catch (Exception ex)
                    {
                        _core.Log.Write($"Failed to rollback transaction for process {transaction.ProcessId}.", ex);
                    }
                }
            }
            catch (Exception ex)
            {
                _core.Log.Write("Failed to recover uncomitted transations.", ex);
                throw;
            }
        }

        /// <summary>
        /// Begin an atomic operation. If the session already has an open transaction then its
        /// reference count is incremented and then decremented on TransactionReference.Dispose();
        /// </summary>
        /// <param name="processId"></param>
        /// <returns></returns>
        internal TransactionReference Acquire(ulong processId, bool isUserCreated)
        {
            var startTime = DateTime.UtcNow;

            try
            {
                lock (Collection)
                {
                    PerformanceTraceDurationTracker? ptAcquireTransaction = null;
                    var transaction = GetByProcessId(processId);
                    if (transaction == null)
                    {
                        transaction = new Transaction(_core, this, processId, false)
                        {
                            IsUserCreated = isUserCreated
                        };

                        ptAcquireTransaction = transaction.PT?.CreateDurationTracker(PerformanceTraceCumulativeMetricType.AcquireTransaction);

                        Collection.Add(transaction);
                    }

                    if (isUserCreated)
                    {
                        //We might be several transactions deep when we see the first user created transaction.
                        //That means we need to conver this transaction to a user transaction.
                        transaction.IsUserCreated = true;
                    }

                    transaction.AddReference();

                    ptAcquireTransaction?.StopAndAccumulate((DateTime.UtcNow - startTime).TotalMilliseconds);

                    KbUtility.EnsureNotNull(transaction);

                    return new TransactionReference(transaction);
                }
            }
            catch (Exception ex)
            {
                _core.Log.Write($"Failed to acquire transaction for process {processId}.", ex);
                throw;
            }
        }

        public void Commit(ulong processId)
        {
            try
            {
                GetByProcessId(processId)?.Commit();
            }
            catch (Exception ex)
            {
                _core.Log.Write($"Failed to commit transaction for process {processId}.", ex);
                throw;
            }
        }

        public void Rollback(ulong processId)
        {
            try
            {
                GetByProcessId(processId)?.Rollback();
            }
            catch (Exception ex)
            {
                _core.Log.Write($"Failed to rollback transaction for process {processId}.", ex);
                throw;
            }
        }
    }
}
