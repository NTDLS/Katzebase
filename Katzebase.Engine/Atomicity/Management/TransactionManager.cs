using Katzebase.Engine.Atomicity;
using Katzebase.Engine.Trace;
using Katzebase.PublicLibrary;
using Newtonsoft.Json;
using static Katzebase.Engine.KbLib.EngineConstants;
using static Katzebase.Engine.Trace.PerformanceTrace;
using static Katzebase.PublicLibrary.Constants;

namespace Katzebase.Engine.Atomicity.Management
{
    /// <summary>
    /// This is the class that all API controllers should interface with for transaction access.
    /// </summary>
    public class TransactionManager
    {
        internal TransactionQueryHandlers QueryHandlers { get; set; }
        public TransactiontAPIHandlers APIHandlers { get; set; }
        internal List<Transaction> Collection = new();
        private Core core;

        public TransactionManager(Core core)
        {
            this.core = core;
            QueryHandlers = new TransactionQueryHandlers(core);
            APIHandlers = new TransactiontAPIHandlers(core);
        }

        internal Transaction? GetByProcessId(ulong processId)
        {
            lock (Collection)
            {
                var transaction = (from o in Collection where o.ProcessId == processId select o).FirstOrDefault();
                return transaction;
            }
        }

        internal void RemoveByProcessId(ulong processId)
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

        internal void Recover()
        {
            try
            {
                core.Log.Write("Starting recovery.");

                Directory.CreateDirectory(core.Settings.TransactionDataPath);

                var transactionFiles = Directory.EnumerateFiles(core.Settings.TransactionDataPath, TransactionActionsFile, SearchOption.AllDirectories);

                if (transactionFiles.Count() > 0)
                {
                    core.Log.Write($"Found {transactionFiles.Count()} open transactions.", LogSeverity.Warning);
                }

                foreach (string transactionFile in transactionFiles)
                {
                    var processIdString = Path.GetFileNameWithoutExtension(Path.GetDirectoryName(transactionFile));
                    if (processIdString == null)
                    {
                        throw new ArgumentNullException(nameof(processIdString));
                    }

                    ulong processId = ulong.Parse(processIdString);

                    Transaction transaction = new Transaction(core, this, processId, true);

                    var reversibleActions = File.ReadLines(transactionFile).ToList();
                    foreach (var reversibleAction in reversibleActions)
                    {
                        var ra = JsonConvert.DeserializeObject<ReversibleAction>(reversibleAction);
                        Utility.EnsureNotNull(ra);
                        transaction.ReversibleActions.Add(ra);
                    }

                    core.Log.Write($"Rolling back session {transaction.ProcessId} with {transaction.ReversibleActions.Count} actions.", LogSeverity.Warning);

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
                core.Log.Write("Could not recover uncomitted transations.", ex);
                throw;
            }
        }

        /// <summary>
        /// Begin an atomic operation. If the session already has an open transaction then its
        /// reference count is incremented and then decremented on TransactionReference.Dispose();
        /// </summary>
        /// <param name="processId"></param>
        /// <returns></returns>
        internal Transaction Begin(ulong processId, bool isUserCreated)
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
                core.Log.Write($"Failed to begin transaction for process {processId}.", ex);
                throw;
            }
        }

        internal Transaction Begin(ulong processId)
        {
            return Begin(processId, false);
        }

        public void Commit(ulong processId)
        {
            try
            {
                var transaction = GetByProcessId(processId);
                if (transaction != null)
                {
                    transaction.Commit();
                }
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
                var transaction = GetByProcessId(processId);
                if (transaction != null)
                {
                    transaction.Rollback();
                }
            }
            catch (Exception ex)
            {
                core.Log.Write($"Failed to rollback transaction for process {processId}.", ex);
                throw;
            }

        }
    }
}