using Newtonsoft.Json;
using NTDLS.Katzebase.Engine.Atomicity;
using NTDLS.Katzebase.Engine.Instrumentation;
using NTDLS.Katzebase.Engine.Interactions.APIHandlers;
using NTDLS.Katzebase.Engine.Interactions.QueryProcessors;
using NTDLS.Katzebase.Engine.IO;
using NTDLS.Katzebase.Engine.Sessions;
using NTDLS.Katzebase.PersistentTypes.Atomicity;
using NTDLS.Semaphore;
using RocksDbSharp;
using System.Diagnostics;
using static NTDLS.Katzebase.Engine.Instrumentation.InstrumentationTracker;
using static NTDLS.Katzebase.Shared.EngineConstants;

namespace NTDLS.Katzebase.Engine.Interactions.Management
{
    /// <summary>
    /// Public core class methods for locking, reading, writing and managing tasks related to transactions.
    /// </summary>
    public class TransactionManager
    {
        private readonly EngineCore _core;
        private readonly OptimisticCriticalResource<List<Transaction>> _collection = new();

        public ICriticalSection CriticalSection => _collection.CriticalSection;

        internal TransactionQueryHandlers QueryHandlers { get; private set; }
        public TransactionAPIHandlers APIHandlers { get; private set; }

        public Rdb TransactionLogRdb { get; private set; }

        internal List<TransactionSnapshot> Snapshot()
        {
            var collectionClone = new List<Transaction>();

            _collection.Read((obj) => collectionClone.AddRange(obj));

            var clones = new List<TransactionSnapshot>();

            foreach (var item in collectionClone)
            {
                clones.Add(item.Snapshot());
            }

            return clones;
        }

        internal TransactionManager(EngineCore core)
        {
            _core = core;
            try
            {
                QueryHandlers = new TransactionQueryHandlers(core);
                APIHandlers = new TransactionAPIHandlers(core);

                try
                {
                    Directory.CreateDirectory(_core.Settings.TransactionDataPath);
                    TransactionLogRdb = _core.IO.AcquireRdb(_core.Settings.TransactionDataPath);
                }
                catch
                {
                    //If we fail to open the database, then we attempt to create it.
                    var options = new DbOptions().SetCreateIfMissing(true).SetCreateMissingColumnFamilies(true);
                    var noBlockCache = new ColumnFamilyOptions()
                        .SetBlockBasedTableFactory(new BlockBasedTableOptions().SetNoBlockCache(true));
                    var columnFamilies = new ColumnFamilies
                        {
                            //The Identity column contains one record per transaction with the key being the transaction ID and the value being the incrementing value.
                            { KbColumnFamilyName.Identity.ToString(), noBlockCache }
                        };

                    var creation = RocksDb.Open(options, _core.Settings.TransactionDataPath, columnFamilies);
                    creation.Dispose();

                    TransactionLogRdb = _core.IO.AcquireRdb(_core.Settings.TransactionDataPath);
                }

            }
            catch (Exception ex)
            {
                LogManager.Error("Failed to instantiate transaction manager.", ex);
                throw;
            }
        }

        public void Stop()
        {
            try
            {
                TransactionLogRdb.Dispose();
            }
            catch (Exception ex)
            {
                LogManager.Error("Failed to stop transaction manager.", ex);
                throw;
            }
        }

        internal Transaction? GetByProcessId(ulong processId)
        {
            try
            {
                return _collection.Read((obj) => obj.FirstOrDefault(o => o.ProcessId == processId));
            }
            catch (Exception ex)
            {
                LogManager.Error($"{new StackFrame(1).GetMethod()} failed for process: [{processId}].", ex);
                throw;
            }
        }

        internal void RemoveByProcessId(ulong processId)
        {
            try
            {
                _collection.Write((obj) =>
                {
                    var transaction = obj.FirstOrDefault(o => o.ProcessId == processId);
                    if (transaction != null)
                    {
                        obj.Remove(transaction);
                    }
                });
            }
            catch (Exception ex)
            {
                LogManager.Error($"{new StackFrame(1).GetMethod()} failed for process: [{processId}].", ex);
                throw;
            }
        }

        /// <summary>
        /// Kills all transactions associated with the given processID.
        /// This is typically called from the session manager and probably should not be called otherwise.
        /// </summary>
        /// <param name="processIDs"></param>
        internal bool TryCloseByProcessID(ulong processId)
        {
            try
            {
                Transaction? transaction = null;

                // Remove the transaction from the collection first, under the lock, then
                // roll it back outside the lock. Calling Rollback() while holding _collection
                // causes a re-entrant deadlock: Rollback() itself needs _collection.CriticalSection
                // (via WriteAll) and calls RemoveByProcessId() which also writes _collection.
                var wasLockObtained = _collection.TryWrite(100, (obj) =>
                {
                    transaction = obj.FirstOrDefault(o => o.ProcessId == processId);
                    if (transaction != null)
                    {
                        obj.Remove(transaction);
                    }
                });

                if (wasLockObtained)
                {
                    // Rollback outside the lock. If RemoveByProcessId is called again from within
                    // Rollback(), it will find nothing in the collection and be a no-op.
                    transaction?.Rollback();
                }

                return wasLockObtained;
            }
            catch (Exception ex)
            {
                LogManager.Error($"{new StackFrame(1).GetMethod()} failed for process: [{processId}].", ex);
                throw;
            }
        }

        internal void Recover()
        {
            try
            {
                var rdb = _core.IO.AcquireRdb(_core.Settings.TransactionDataPath);

                // Use the Identity CF as the source of truth for orphaned transactions rather than
                // ListColumnFamilies. The Identity entry is removed in CleanupTransaction(), so
                // even if DropColumnFamily() doesn't permanently remove the CF file on disk, the
                // same GUID won't be re-processed on subsequent restarts.
                var transactionIDs = new HashSet<Guid>();
                var identityCF = rdb.GetColumnFamily(KbColumnFamilyName.Identity);
                using (var iterator = rdb.NewIterator(identityCF))
                {
                    for (iterator.SeekToFirst(); iterator.Valid(); iterator.Next())
                    {
                        var keyBytes = iterator.Key();
                        if (keyBytes.Length == 16)
                            transactionIDs.Add(new Guid(keyBytes));
                    }
                }

                if (transactionIDs.Count == 0)
                {
                    return;
                }

                LogManager.Warning($"Rolling back {transactionIDs.Count} open transactions.");

                foreach (var transactionId in transactionIDs)
                {
                    // Assign the orphaned transaction's ID so Rollback() looks up the correct
                    // column family and CleanupTransaction() drops it afterwards.
                    var transaction = new Transaction(_core, this, 0, true) { Id = transactionId };

                    long lastSequence = 0;

                    var columnFamily = rdb.GetColumnFamily(new RdbKey(transactionId));
                    using (var iterator = rdb.NewIterator(columnFamily))
                    {
                        iterator.SeekToLast();
                        if (iterator.Valid())
                        {
                            var lastAtom = JsonConvert.DeserializeObject<Atom>(iterator.StringValue());
                            lastSequence = lastAtom?.Sequence ?? 0;
                        }
                    } // Iterator closed before Rollback so CleanupTransaction can drop the CF.

                    LogManager.Warning($"Rolling back orphaned transaction {transactionId} with {lastSequence:N0} actions.");

                    try
                    {
                        transaction.Rollback();
                    }
                    catch (Exception ex)
                    {
                        LogManager.Error($"Failed to rollback orphaned transaction {transactionId}.", ex);
                    }
                }
            }
            catch (Exception ex)
            {
                LogManager.Error($"{new StackFrame(1).GetMethod()} failed.", ex);
                throw;
            }
        }

        /// <summary>
        /// Begin an atomic operation. If the session already has an open transaction then its
        /// reference count is incremented and then decremented on TransactionReference.Dispose();
        /// 
        /// Keep in mind that while transactions do support multithreaded client operations, that 
        /// all operations for a single client share the same transaction, therefore it is important
        /// that multithreaded client operations be aware that if any one client rolls back an operation
        /// that this will cause all active processes for that client connection to also be cancelled.
        /// </summary>
        internal TransactionReference APIAcquire(SessionState session)
        {
            var transactionReference = Acquire(session, false);

            var stackFrames = (new StackTrace()).GetFrames();
            if (stackFrames.Length >= 2)
            {
                //Since we go though Interactions.APIHandlers, the top level function will be the name of the API.
                transactionReference.Transaction.TopLevelOperation = stackFrames[1].GetMethod()?.Name ?? string.Empty;
            }

            return transactionReference;
        }

        /// <summary>
        /// Begin an atomic operation. If the session already has an open transaction then its
        /// reference count is incremented and then decremented on TransactionReference.Dispose();
        /// 
        /// Keep in mind that while transactions do support multithreaded client operations, that 
        /// all operations for a single client share the same transaction, therefore it is important
        /// that multithreaded client operations be aware that if any one client rolls back an operation
        /// that this will cause all active processes for that client connection to also be cancelled.
        /// </summary>
        internal TransactionReference Acquire(SessionState session, bool isUserCreated)
        {
            var startTime = DateTime.UtcNow;

            try
            {
                return _collection.Write((obj) =>
                {
                    InstrumentationDurationToken? ptAcquireTransaction = null;
                    var transaction = GetByProcessId(session.ProcessId);
                    if (transaction == null)
                    {
                        transaction = new Transaction(_core, this, session.ProcessId, false)
                        {
                            IsUserCreated = isUserCreated
                        };

                        ptAcquireTransaction = transaction.Instrumentation.CreateToken(PerformanceCounter.AcquireTransaction);

                        obj.Add(transaction);
                    }

                    if (isUserCreated)
                    {
                        //We might be several transactions deep when we see the first user created transaction.
                        //That means we need to convert this transaction to a user transaction.
                        transaction.IsUserCreated = true;
                    }

                    transaction.AddReference();

                    ptAcquireTransaction?.StopAndAccumulate((DateTime.UtcNow - startTime).TotalMilliseconds);

                    return new TransactionReference(transaction);
                });
            }
            catch (Exception ex)
            {
                LogManager.Error($"{new StackFrame(1).GetMethod()} failed for process: [{session.ProcessId}].", ex);
                throw;
            }
        }

        internal void Commit(SessionState session)
            => Commit(session.ProcessId);

        internal void Commit(ulong processId)
        {
            try
            {
                GetByProcessId(processId)?.Commit();
            }
            catch (Exception ex)
            {
                LogManager.Error($"{new StackFrame(1).GetMethod()} failed for process: [{processId}].", ex);
                throw;
            }
        }

        internal void Rollback(SessionState session)
            => Rollback(session.ProcessId);

        internal void Rollback(ulong processId)
        {
            try
            {
                GetByProcessId(processId)?.Rollback();
            }
            catch (Exception ex)
            {
                LogManager.Error($"{new StackFrame(1).GetMethod()} failed for process: [{processId}].", ex);
                throw;
            }
        }
    }
}
