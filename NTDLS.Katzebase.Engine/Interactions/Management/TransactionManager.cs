﻿using Newtonsoft.Json;
using NTDLS.Helpers;
using NTDLS.Katzebase.Engine.Atomicity;
using NTDLS.Katzebase.Engine.Instrumentation;
using NTDLS.Katzebase.Engine.Interactions.APIHandlers;
using NTDLS.Katzebase.Engine.Interactions.QueryProcessors;
using NTDLS.Katzebase.Engine.Sessions;
using NTDLS.Katzebase.PersistentTypes.Atomicity;
using NTDLS.Semaphore;
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
            }
            catch (Exception ex)
            {
                LogManager.Error("Failed to instantiate transaction manager.", ex);
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
                    var transaction = GetByProcessId(processId);
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
                var wasLockObtained = _collection.TryWrite(100, (obj) =>
                {
                    var transaction = GetByProcessId(processId);
                    if (transaction != null)
                    {
                        transaction.Rollback();
                        obj.Remove(transaction);
                    }
                });

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
                Directory.CreateDirectory(_core.Settings.TransactionDataPath);

                var transactionFiles = Directory.EnumerateFiles(
                    _core.Settings.TransactionDataPath, TransactionActionsFile, SearchOption.AllDirectories).ToList();

                if (transactionFiles.Count != 0)
                {
                    LogManager.Warning($"Found {transactionFiles.Count} open transactions.");
                }

                foreach (string transactionFile in transactionFiles)
                {
                    var processIdString = Path.GetFileNameWithoutExtension(Path.GetDirectoryName(transactionFile));
                    ulong processId = ulong.Parse(processIdString.EnsureNotNull());

                    var transaction = new Transaction(_core, this, processId, true);

                    var atoms = File.ReadLines(transactionFile).ToList();
                    foreach (var atom in atoms)
                    {
                        var ra = JsonConvert.DeserializeObject<Atom>(atom).EnsureNotNull();
                        transaction.Atoms.Write((obj) => obj.Add(ra));
                    }

                    LogManager.Warning($"Rolling back session {transaction.ProcessId} with {atoms.Count} actions.");

                    try
                    {
                        transaction.Rollback();
                    }
                    catch (Exception ex)
                    {
                        LogManager.Error($"Failed to rollback transaction for process {transaction.ProcessId}.", ex);
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
