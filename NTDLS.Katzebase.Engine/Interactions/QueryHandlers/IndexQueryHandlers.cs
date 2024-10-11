using NTDLS.Katzebase.Api.Exceptions;
using NTDLS.Katzebase.Api.Models;
using NTDLS.Katzebase.Api.Payloads.Response;
using NTDLS.Katzebase.Engine.Sessions;
using NTDLS.Katzebase.Parsers.Query.SupportingTypes;
using static NTDLS.Katzebase.Api.KbConstants;
using static NTDLS.Katzebase.Parsers.Constants;

namespace NTDLS.Katzebase.Engine.Interactions.QueryHandlers
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
                Management.LogManager.Error($"Failed to instantiate index query handler.", ex);
                throw;
            }
        }

        internal KbActionResponse ExecuteDrop(SessionState session, Query query)
        {
            try
            {
                using var transactionReference = _core.Transactions.APIAcquire(session);
                string schemaName = query.Schemas.First().Name;

                if (query.SubQueryType == SubQueryType.Index || query.SubQueryType == SubQueryType.UniqueKey)
                {
                    _core.Indexes.DropIndex(transactionReference.Transaction, schemaName,
                        query.GetAttribute<string>(Query.Attribute.IndexName));
                }
                else
                {
                    throw new KbNotImplementedException();
                }

                return transactionReference.CommitAndApplyMetricsThenReturnResults();
            }
            catch (Exception ex)
            {
                Management.LogManager.Error($"Failed to execute index drop for process id {session.ProcessId}.", ex);
                throw;
            }
        }

        internal KbQueryResult ExecuteAnalyze(SessionState session, Query query)
        {
            try
            {
                using var transactionReference = _core.Transactions.APIAcquire(session);

                var analysis = _core.Indexes.AnalyzeIndex(transactionReference.Transaction,
                    query.GetAttribute<string>(Query.Attribute.Schema),
                    query.GetAttribute<string>(Query.Attribute.IndexName));

                transactionReference.Transaction.AddMessage(analysis, KbMessageType.Verbose);

                //TODO: Maybe we should return a table here too? Maybe more than one?
                return transactionReference.CommitAndApplyMetricsThenReturnResults(new KbQueryResult(), 0);
            }
            catch (Exception ex)
            {
                Management.LogManager.Error($"Failed to execute index rebuild for process id {session.ProcessId}.", ex);
                throw;
            }
        }

        internal KbActionResponse ExecuteRebuild(SessionState session, Query query)
        {
            try
            {
                using var transactionReference = _core.Transactions.APIAcquire(session);
                string schemaName = query.Schemas.First().Name;

                var indexName = query.GetAttribute<string>(Query.Attribute.IndexName);
                var indexPartitions = query.GetAttribute(Query.Attribute.Partitions, _core.Settings.DefaultIndexPartitions);

                _core.Indexes.RebuildIndex(transactionReference.Transaction, schemaName, indexName, indexPartitions);
                return transactionReference.CommitAndApplyMetricsThenReturnResults();
            }
            catch (Exception ex)
            {
                Management.LogManager.Error($"Failed to execute index rebuild for process id {session.ProcessId}.", ex);
                throw;
            }
        }

        internal KbActionResponse ExecuteCreate(SessionState session, Query query)
        {
            try
            {
                using var transactionReference = _core.Transactions.APIAcquire(session);

                if (query.SubQueryType == SubQueryType.Index || query.SubQueryType == SubQueryType.UniqueKey)
                {
                    var indexPartitions = query.GetAttribute(
                        Query.Attribute.Partitions, _core.Settings.DefaultIndexPartitions);

                    var index = new KbIndex
                    {
                        Name = query.GetAttribute<string>(Query.Attribute.IndexName),
                        IsUnique = query.GetAttribute<bool>(Query.Attribute.IsUnique),
                        Partitions = indexPartitions
                    };

                    foreach (var field in query.CreateIndexFields)
                    {
                        index.AddAttribute(new KbIndexAttribute() { Field = field });
                    }

                    string schemaName = query.Schemas.Single().Name;
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
                Management.LogManager.Error($"Failed to execute index create for process id {session.ProcessId}.", ex);
                throw;
            }
        }
    }
}
