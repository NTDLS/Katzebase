using NTDLS.Katzebase.Api.Exceptions;
using NTDLS.Katzebase.Api.Models;
using NTDLS.Katzebase.Api.Payloads.Response;
using NTDLS.Katzebase.Engine.Interactions.Management;
using NTDLS.Katzebase.Engine.Sessions;
using NTDLS.Katzebase.Parsers.Query.SupportingTypes;
using System.Diagnostics;
using static NTDLS.Katzebase.Api.KbConstants;
using static NTDLS.Katzebase.Parsers.Constants;
using static NTDLS.Katzebase.Shared.EngineConstants;

namespace NTDLS.Katzebase.Engine.Interactions.QueryProcessors
{
    /// <summary>
    /// Internal class methods for handling query requests related to indexes.
    /// </summary>
    internal class IndexQueryHandlers
    {
        private readonly EngineCore _core;

        public IndexQueryHandlers(EngineCore core)
        {
            _core = core;

            try
            {
            }
            catch (Exception ex)
            {
                LogManager.Error($"Failed to instantiate index query handler.", ex);
                throw;
            }
        }

        internal KbActionResponse ExecuteDrop(SessionState session, PreparedQuery query)
        {
            try
            {
                using var transactionReference = _core.Transactions.APIAcquire(session);
                string schemaName = query.Schemas.First().Name;

                #region EnforceSchemaPolicy.

                _core.Policy.EnforceSchemaPolicy(transactionReference.Transaction, schemaName, SecurityPolicyPermission.Manage);

                #endregion

                if (query.SubQueryType == SubQueryType.Index || query.SubQueryType == SubQueryType.UniqueKey)
                {
                    _core.Indexes.DropIndex(transactionReference.Transaction, schemaName,
                        query.GetAttribute<string>(PreparedQuery.Attribute.IndexName));
                }
                else
                {
                    throw new KbNotImplementedException();
                }

                return transactionReference.CommitAndApplyMetricsThenReturnResults();
            }
            catch (Exception ex)
            {
                LogManager.Error($"{new StackFrame(1).GetMethod()} failed for process: [{session.ProcessId}].", ex);
                throw;
            }
        }

        internal KbQueryResult ExecuteAnalyze(SessionState session, PreparedQuery query)
        {
            try
            {
                using var transactionReference = _core.Transactions.APIAcquire(session);

                var schemaName = query.GetAttribute<string>(PreparedQuery.Attribute.Schema);

                #region EnforceSchemaPolicy.

                _core.Policy.EnforceSchemaPolicy(transactionReference.Transaction, schemaName, SecurityPolicyPermission.Manage);

                #endregion

                var analysis = _core.Indexes.AnalyzeIndex(transactionReference.Transaction,
                    schemaName,
                    query.GetAttribute<string>(PreparedQuery.Attribute.IndexName));

                transactionReference.Transaction.AddMessage(analysis, KbMessageType.Verbose);

                //TODO: Maybe we should return a table here too? Maybe more than one?
                return transactionReference.CommitAndApplyMetricsThenReturnResults(new KbQueryResult(), 0);
            }
            catch (Exception ex)
            {
                LogManager.Error($"{new StackFrame(1).GetMethod()} failed for process: [{session.ProcessId}].", ex);
                throw;
            }
        }

        internal KbActionResponse ExecuteRebuild(SessionState session, PreparedQuery query)
        {
            try
            {
                using var transactionReference = _core.Transactions.APIAcquire(session);
                string schemaName = query.Schemas.First().Name;

                #region EnforceSchemaPolicy.

                _core.Policy.EnforceSchemaPolicy(transactionReference.Transaction, schemaName, SecurityPolicyPermission.Manage);

                #endregion

                var indexName = query.GetAttribute<string>(PreparedQuery.Attribute.IndexName);
                var indexPartitions = query.GetAttribute(PreparedQuery.Attribute.Partitions, _core.Settings.DefaultIndexPartitions);

                _core.Indexes.RebuildIndex(transactionReference.Transaction, schemaName, indexName, indexPartitions);
                return transactionReference.CommitAndApplyMetricsThenReturnResults();
            }
            catch (Exception ex)
            {
                LogManager.Error($"{new StackFrame(1).GetMethod()} failed for process: [{session.ProcessId}].", ex);
                throw;
            }
        }

        internal KbActionResponse ExecuteCreate(SessionState session, PreparedQuery query)
        {
            try
            {
                using var transactionReference = _core.Transactions.APIAcquire(session);

                string schemaName = query.Schemas.Single().Name;

                #region EnforceSchemaPolicy.

                _core.Policy.EnforceSchemaPolicy(transactionReference.Transaction, schemaName, SecurityPolicyPermission.Manage);

                #endregion

                if (query.SubQueryType == SubQueryType.Index || query.SubQueryType == SubQueryType.UniqueKey)
                {
                    var indexPartitions = query.GetAttribute(
                        PreparedQuery.Attribute.Partitions, _core.Settings.DefaultIndexPartitions);

                    var index = new KbIndex
                    {
                        Name = query.GetAttribute<string>(PreparedQuery.Attribute.IndexName),
                        IsUnique = query.GetAttribute<bool>(PreparedQuery.Attribute.IsUnique),
                        Partitions = indexPartitions
                    };

                    foreach (var field in query.CreateIndexFields)
                    {
                        index.AddAttribute(new KbIndexAttribute() { Field = field });
                    }

                    _core.Indexes.CreateIndex(transactionReference.Transaction, schemaName, index, out Guid indexId);
                }
                else
                {
                    throw new KbNotImplementedException();
                }

                return transactionReference.CommitAndApplyMetricsThenReturnResults();
            }
            catch (Exception ex)
            {
                LogManager.Error($"{new StackFrame(1).GetMethod()} failed for process: [{session.ProcessId}].", ex);
                throw;
            }
        }
    }
}
