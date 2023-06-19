using Katzebase.Engine.Trace;
using Katzebase.PublicLibrary;
using Newtonsoft.Json;
using static Katzebase.Engine.KbLib.EngineConstants;
using static Katzebase.Engine.Trace.PerformanceTrace;
using static Katzebase.PublicLibrary.Constants;

namespace Katzebase.Engine.Atomicity.Management
{
    /// <summary>
    /// Public core class methods for locking, reading, writing and managing tasks related to transactions.
    /// </summary>
    public class TransactionManager
    {
        internal TransactionQueryHandlers QueryHandlers { get; set; }
        public TransactiontAPIHandlers APIHandlers { get; set; }
        internal List<Transaction> Collection = new();
        private readonly Core core;
        internal Transaction Acquire(ulong processId) => Acquire(processId, false);

        public TransactionManager(Core core)
        {
            this.core = core;
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
                core.Log.Write($"Failed to get transaction by process id for process id {processId}.", ex);
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
                core.Log.Write($"Failed to remove transaction by process id for process {processId}.", ex);
                throw;
            }
        }

        internal void Recover()
        {
            try
            {
                core.Log.Write("Starting recovery.");

                Directory.CreateDirectory(core.Settings.TransactionDataPath);

                var transactionFiles = Directory.EnumerateFiles(core.Settings.TransactionDataPath, TransactionActionsFile, SearchOption.AllDirectories);
                if (transactionFiles.Any())
                {
                    core.Log.Write($"Found {transactionFiles.Count()} open transactions.", LogSeverity.Warning);
                }

                foreach (string transactionFile in transactionFiles)
                {
                    var processIdString = Path.GetFileNameWithoutExtension(Path.GetDirectoryName(transactionFile));
                    Utility.EnsureNotNull(processIdString);

                    ulong processId = ulong.Parse(processIdString);

                    var transaction = new Transaction(core, this, processId, true);

                    var atoms = File.ReadLines(transactionFile).ToList();
                    foreach (var atom in atoms)
                    {
                        var ra = JsonConvert.DeserializeObject<Atom>(atom);
                        Utility.EnsureNotNull(ra);
                        transaction.Atoms.Add(ra);
                    }

                    core.Log.Write($"Rolling back session {transaction.ProcessId} with {transaction.Atoms.Count} actions.", LogSeverity.Warning);

                    try
                    {
                        transaction.Rollback();
                    }
                    catch (Exception ex)
                    {
                        core.Log.Write($"Failed to rollback transaction for process {transaction.ProcessId}.", ex);
                    }
                }

                core.Log.Write("Recovery complete.");
            }
            catch (Exception ex)
            {
                core.Log.Write("Failed to recover uncomitted transations.", ex);
                throw;
            }
        }

        /// <summary>
        /// Begin an atomic operation. If the session already has an open transaction then its
        /// reference count is incremented and then decremented on TransactionReference.Dispose();
        /// </summary>
        /// <param name="processId"></param>
        /// <returns></returns>
        internal Transaction Acquire(ulong processId, bool isUserCreated)
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
                        transaction = new Transaction(core, this, processId, false)
                        {
                            IsUserCreated = isUserCreated
                        };

                        ptAcquireTransaction = transaction.PT?.CreateDurationTracker(PerformanceTraceCumulativeMetricType.AcquireTransaction);

                        Collection.Add(transaction);
                    }

                    transaction.AddReference();

                    ptAcquireTransaction?.StopAndAccumulate((DateTime.UtcNow - startTime).TotalMilliseconds);

                    Utility.EnsureNotNull(transaction);

                    return transaction;
                }
            }
            catch (Exception ex)
            {
                core.Log.Write($"Failed to acquire transaction for process {processId}.", ex);
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
                core.Log.Write($"Failed to commit transaction for process {processId}.", ex);
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
                core.Log.Write($"Failed to rollback transaction for process {processId}.", ex);
                throw;
            }
        }
    }
}
