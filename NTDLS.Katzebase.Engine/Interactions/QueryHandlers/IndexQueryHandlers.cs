using NTDLS.Katzebase.Api.Exceptions;
using NTDLS.Katzebase.Api.Payloads;
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

        internal KbActionResponse ExecuteDrop(SessionState session, PreparedQuery preparedQuery)
        {
            try
            {
                using var transactionReference = _core.Transactions.APIAcquire(session);
                string schemaName = preparedQuery.Schemas.First().Name;

                if (preparedQuery.SubQueryType == SubQueryType.Index || preparedQuery.SubQueryType == SubQueryType.UniqueKey)
                {
                    _core.Indexes.DropIndex(transactionReference.Transaction, schemaName,
                        preparedQuery.GetAttribute<string>(PreparedQuery.QueryAttribute.IndexName));
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

        internal KbQueryDocumentListResult ExecuteAnalyze(SessionState session, PreparedQuery preparedQuery)
        {
            try
            {
                using var transactionReference = _core.Transactions.APIAcquire(session);

                var analysis = _core.Indexes.AnalyzeIndex(transactionReference.Transaction,
                    preparedQuery.GetAttribute<string>(PreparedQuery.QueryAttribute.Schema),
                    preparedQuery.GetAttribute<string>(PreparedQuery.QueryAttribute.IndexName));

                transactionReference.Transaction.AddMessage(analysis, KbMessageType.Verbose);

                //TODO: Maybe we should return a table here too? Maybe more than one?
                return transactionReference.CommitAndApplyMetricsThenReturnResults(new KbQueryDocumentListResult(), 0);
            }
            catch (Exception ex)
            {
                Management.LogManager.Error($"Failed to execute index rebuild for process id {session.ProcessId}.", ex);
                throw;
            }
        }

        internal KbActionResponse ExecuteRebuild(SessionState session, PreparedQuery preparedQuery)
        {
            try
            {
                using var transactionReference = _core.Transactions.APIAcquire(session);
                string schemaName = preparedQuery.Schemas.First().Name;

                var indexName = preparedQuery.GetAttribute<string>(PreparedQuery.QueryAttribute.IndexName);
                var indexPartitions = preparedQuery.TryGetAttribute(PreparedQuery.QueryAttribute.Partitions, _core.Settings.DefaultIndexPartitions);

                _core.Indexes.RebuildIndex(transactionReference.Transaction, schemaName, indexName, indexPartitions);
                return transactionReference.CommitAndApplyMetricsThenReturnResults();
            }
            catch (Exception ex)
            {
                Management.LogManager.Error($"Failed to execute index rebuild for process id {session.ProcessId}.", ex);
                throw;
            }
        }

        internal KbActionResponse ExecuteCreate(SessionState session, PreparedQuery preparedQuery)
        {
            try
            {
                using var transactionReference = _core.Transactions.APIAcquire(session);

                if (preparedQuery.SubQueryType == SubQueryType.Index || preparedQuery.SubQueryType == SubQueryType.UniqueKey)
                {
                    var indexPartitions = preparedQuery.TryGetAttribute(
                        PreparedQuery.QueryAttribute.Partitions, _core.Settings.DefaultIndexPartitions);

                    var index = new KbIndex
                    {
                        Name = preparedQuery.GetAttribute<string>(PreparedQuery.QueryAttribute.IndexName),
                        IsUnique = preparedQuery.GetAttribute<bool>(PreparedQuery.QueryAttribute.IsUnique),
                        Partitions = indexPartitions
                    };

                    foreach (var field in preparedQuery.CreateIndexFields)
                    {
                        index.AddAttribute(new KbIndexAttribute() { Field = field });
                    }

                    string schemaName = preparedQuery.Schemas.Single().Name;
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
