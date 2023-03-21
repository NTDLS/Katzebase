using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Katzebase.Engine.Transactions
{
    public class TransactionManager
    {
        public List<Transaction> Collection = new List<Transaction>();
        private Core core;

        public TransactionManager(Core core)
        {
            this.core = core;
        }

        public Transaction GetByProcessId(ulong processId)
        {
            lock (Collection)
            {
                var transaction = (from o in Collection where o.ProcessId == processId select o).FirstOrDefault();
                return transaction;
            }
        }

        public void RemoveByProcessId(ulong processId)
        {
            lock (Collection)
            {
                var transaction = GetByProcessId(processId);
                this.Collection.Remove(transaction);
            }
        }

        public void Recover()
        {
            try
            {
                core.Log.Write("Starting recovery.");

                Directory.CreateDirectory(core.settings.TransactionDataPath);

                var transactionFiles = Directory.EnumerateFiles(core.settings.TransactionDataPath, Constants.TransactionActionsFile, SearchOption.AllDirectories);

                if (transactionFiles.Count() > 0)
                {
                    core.Log.Write($"Found {transactionFiles.Count()} open transactions.", Constants.LogSeverity.Warning);
                }

                foreach (string transactionFile in transactionFiles)
                {
                    ulong processId = ulong.Parse(Path.GetFileNameWithoutExtension(Path.GetDirectoryName(transactionFile)));

                    Transaction transaction = new Transaction(core, this, processId, true);

                    var reversibleActions = File.ReadLines(transactionFile).ToList();
                    foreach (var reversibleAction in reversibleActions)
                    {
                        transaction.ReversibleActions.Add(JsonConvert.DeserializeObject<ReversibleAction>(reversibleAction));
                    }

                    core.Log.Write($"Rolling back session {transaction.ProcessId} with {transaction.ReversibleActions.Count} actions.", Constants.LogSeverity.Warning);

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
        /// Begin an atomic operation. If the session already has an open transaction then its reference count is incremented and then decremented on TransactionReference.Dispose();
        /// </summary>
        /// <param name="processId"></param>
        /// <returns></returns>
        public TransactionReference Begin(ulong processId, bool isLongLived)
        {
            try
            {
                lock (Collection)
                {
                    Transaction transaction = GetByProcessId(processId);
                    if (transaction == null)
                    {
                        transaction = new Transaction(core, this, processId, false)
                        {
                            IsLongLived = isLongLived
                        };

                        Collection.Add(transaction);
                    }

                    transaction.AddReference();

                    return new TransactionReference(transaction);
                }
            }
            catch (Exception ex)
            {
                core.Log.Write($"Failed to begin transaction for process {processId}.", ex);
                throw;
            }
        }

        public TransactionReference Begin(ulong processId)
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