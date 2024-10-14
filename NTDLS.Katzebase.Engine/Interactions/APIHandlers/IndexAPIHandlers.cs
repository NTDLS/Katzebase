using NTDLS.Katzebase.Api.Payloads;
using NTDLS.Katzebase.Engine.Interactions.Management;
using NTDLS.Katzebase.PersistentTypes.Index;
using NTDLS.ReliableMessaging;
using System.Diagnostics;
using static NTDLS.Katzebase.Shared.EngineConstants;

namespace NTDLS.Katzebase.Engine.Interactions.APIHandlers
{
    /// <summary>
    /// Public class methods for handling API requests related to indexes.
    /// </summary>
    public class IndexAPIHandlers : IRmMessageHandler
    {
        private readonly EngineCore _core;

        public IndexAPIHandlers(EngineCore core)
        {
            _core = core;

            try
            {
            }
            catch (Exception ex)
            {
                LogManager.Error($"Failed to instantiate index API handlers.", ex);
                throw;
            }
        }

        public KbQueryIndexGetReply Get(RmContext context, KbQueryIndexGet param)
        {
            var session = _core.Sessions.GetSession(context.ConnectionId);
#if DEBUG
            Thread.CurrentThread.Name = $"KbAPI:{session.ProcessId}:{param.GetType().Name}";
            LogManager.Debug(Thread.CurrentThread.Name);
#endif
            try
            {
                using var transactionReference = _core.Transactions.APIAcquire(session);

                #region Security policy enforcment.

                _core.Policy.EnforceSchemaPolicy(transactionReference.Transaction, param.Schema, SecurityPolicyPermission.Manage);

                #endregion

                var indexCatalog = _core.Indexes.AcquireIndexCatalog(transactionReference.Transaction, param.Schema, LockOperation.Read);

                var physicalIndex = indexCatalog.GetByName(param.IndexName);

                var apiResults = new KbQueryIndexGetReply()
                {
                    Index = PhysicalIndex.ToApiPayload(physicalIndex)
                };

                return transactionReference.CommitAndApplyMetricsThenReturnResults(apiResults);
            }
            catch (Exception ex)
            {
                LogManager.Error($"{new StackFrame(1).GetMethod()} failed for process: [{session.ProcessId}].", ex);
                throw;
            }
        }

        public KbQueryIndexListReply ListIndexes(RmContext context, KbQueryIndexList param)
        {
            var session = _core.Sessions.GetSession(context.ConnectionId);
#if DEBUG
            Thread.CurrentThread.Name = $"KbAPI:{session.ProcessId}:{param.GetType().Name}";
            LogManager.Debug(Thread.CurrentThread.Name);
#endif
            try
            {
                using var transactionReference = _core.Transactions.APIAcquire(session);

                #region Security policy enforcment.

                _core.Policy.EnforceSchemaPolicy(transactionReference.Transaction, param.Schema, SecurityPolicyPermission.Manage);

                #endregion

                var indexCatalog = _core.Indexes.AcquireIndexCatalog(transactionReference.Transaction, param.Schema, LockOperation.Read);
                var apiResults = new KbQueryIndexListReply();

                foreach (var index in indexCatalog.Collection)
                {
                    var apiPayload = PhysicalIndex.ToApiPayload(index);
                    if (apiPayload != null)
                    {
                        apiResults.Add(apiPayload);
                    }
                }

                return transactionReference.CommitAndApplyMetricsThenReturnResults(apiResults, apiResults.Collection.Count);
            }
            catch (Exception ex)
            {
                LogManager.Error($"{new StackFrame(1).GetMethod()} failed for process: [{session.ProcessId}].", ex);
                throw;
            }
        }

        public KbQueryIndexExistsReply DoesIndexExist(RmContext context, KbQueryIndexExists param)
        {
            var session = _core.Sessions.GetSession(context.ConnectionId);
#if DEBUG
            Thread.CurrentThread.Name = $"KbAPI:{session.ProcessId}:{param.GetType().Name}";
            LogManager.Debug(Thread.CurrentThread.Name);
#endif
            try
            {
                using var transactionReference = _core.Transactions.APIAcquire(session);

                #region Security policy enforcment.

                _core.Policy.EnforceSchemaPolicy(transactionReference.Transaction, param.Schema, SecurityPolicyPermission.Manage);

                #endregion

                var indexCatalog = _core.Indexes.AcquireIndexCatalog(transactionReference.Transaction, param.Schema, LockOperation.Read);

                bool doesIndexExist = indexCatalog.GetByName(param.IndexName) != null;

                var apiResults = new KbQueryIndexExistsReply(doesIndexExist);

                return transactionReference.CommitAndApplyMetricsThenReturnResults(apiResults);
            }
            catch (Exception ex)
            {
                LogManager.Error($"{new StackFrame(1).GetMethod()} failed for process: [{session.ProcessId}].", ex);
                throw;
            }
        }

        public KbQueryIndexCreateReply CreateIndex(RmContext context, KbQueryIndexCreate param)
        {
            var session = _core.Sessions.GetSession(context.ConnectionId);
#if DEBUG
            Thread.CurrentThread.Name = $"KbAPI:{session.ProcessId}:{param.GetType().Name}";
            LogManager.Debug(Thread.CurrentThread.Name);
#endif
            try
            {
                using var transactionReference = _core.Transactions.APIAcquire(session);

                #region Security policy enforcment.

                _core.Policy.EnforceSchemaPolicy(transactionReference.Transaction, param.Schema, SecurityPolicyPermission.Manage);

                #endregion

                _core.Indexes.CreateIndex(transactionReference.Transaction, param.Schema, param.Index, out Guid newId);

                var apiResults = new KbQueryIndexCreateReply(newId);

                return transactionReference.CommitAndApplyMetricsThenReturnResults(apiResults, 0);
            }
            catch (Exception ex)
            {
                LogManager.Error($"{new StackFrame(1).GetMethod()} failed for process: [{session.ProcessId}].", ex);
                throw;
            }
        }

        public KbQueryIndexRebuildReply RebuildIndex(RmContext context, KbQueryIndexRebuild param)
        {
            var session = _core.Sessions.GetSession(context.ConnectionId);
#if DEBUG
            Thread.CurrentThread.Name = $"KbAPI:{session.ProcessId}:{param.GetType().Name}";
            LogManager.Debug(Thread.CurrentThread.Name);
#endif
            try
            {
                using var transactionReference = _core.Transactions.APIAcquire(session);

                #region Security policy enforcment.

                _core.Policy.EnforceSchemaPolicy(transactionReference.Transaction, param.Schema, SecurityPolicyPermission.Manage);

                #endregion

                _core.Indexes.RebuildIndex(transactionReference.Transaction, param.Schema, param.IndexName, param.NewPartitionCount);
                var apiResults = new KbQueryIndexRebuildReply();
                return transactionReference.CommitAndApplyMetricsThenReturnResults(apiResults);
            }
            catch (Exception ex)
            {
                LogManager.Error($"{new StackFrame(1).GetMethod()} failed for process: [{session.ProcessId}].", ex);
                throw;
            }
        }

        public KbQueryIndexDropReply DropIndex(RmContext context, KbQueryIndexDrop param)
        {
            var session = _core.Sessions.GetSession(context.ConnectionId);
#if DEBUG
            Thread.CurrentThread.Name = $"KbAPI:{session.ProcessId}:{param.GetType().Name}";
            LogManager.Debug(Thread.CurrentThread.Name);
#endif
            try
            {
                using var transactionReference = _core.Transactions.APIAcquire(session);

                #region Security policy enforcment.

                _core.Policy.EnforceSchemaPolicy(transactionReference.Transaction, param.Schema, SecurityPolicyPermission.Manage);

                #endregion

                _core.Indexes.DropIndex(transactionReference.Transaction, param.Schema, param.IndexName);
                var apiResults = new KbQueryIndexDropReply();
                return transactionReference.CommitAndApplyMetricsThenReturnResults(apiResults);
            }
            catch (Exception ex)
            {
                LogManager.Error($"{new StackFrame(1).GetMethod()} failed for process: [{session.ProcessId}].", ex);
                throw;
            }
        }
    }
}
