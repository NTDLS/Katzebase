using NTDLS.Katzebase.Api.Exceptions;
using NTDLS.Katzebase.Api.Models;
using NTDLS.Katzebase.Api.Payloads.Response;
using NTDLS.Katzebase.Engine.Sessions;
using NTDLS.Katzebase.Parsers.Query.SupportingTypes;
using static NTDLS.Katzebase.Parsers.Constants;

namespace NTDLS.Katzebase.Engine.Interactions.QueryHandlers
{
    /// <summary>
    /// Internal class methods for handling query requests related to schemas.
    /// </summary>
    internal class SchemaQueryHandlers
    {
        private readonly EngineCore _core;

        public SchemaQueryHandlers(EngineCore core)
        {
            _core = core;

            try
            {
            }
            catch (Exception ex)
            {
                Management.LogManager.Error($"Failed to instantiate schema query handler.", ex);
                throw;
            }

        }

        internal KbQueryResult ExecuteAnalyze(SessionState session, Query query)
        {
            try
            {
                using var transactionReference = _core.Transactions.APIAcquire(session);
                string schemaName = query.Schemas.First().Name;

                var result = new KbQueryResult();

                if (query.SubQueryType == SubQueryType.Schema)
                {
                    var includePhysicalPages = query.GetAttribute(Query.Attribute.IncludePhysicalPages, false);
                    result = _core.Schemas.AnalyzePages(transactionReference.Transaction, schemaName, includePhysicalPages);
                }
                else
                {
                    throw new KbNotImplementedException();
                }

                return transactionReference.CommitAndApplyMetricsThenReturnResults(result, 0);
            }
            catch (Exception ex)
            {
                Management.LogManager.Error($"Failed to execute schema drop for process id {session.ProcessId}.", ex);
                throw;
            }
        }

        internal KbActionResponse ExecuteDrop(SessionState session, Query query)
        {
            try
            {
                using var transactionReference = _core.Transactions.APIAcquire(session);
                string schemaName = query.Schemas.First().Name;

                if (query.SubQueryType == SubQueryType.Schema)
                {
                    _core.Schemas.Drop(transactionReference.Transaction, schemaName);
                }
                else
                {
                    throw new KbNotImplementedException();
                }

                return transactionReference.CommitAndApplyMetricsThenReturnResults();
            }
            catch (Exception ex)
            {
                Management.LogManager.Error($"Failed to execute schema drop for process id {session.ProcessId}.", ex);
                throw;
            }
        }

        internal KbActionResponse ExecuteAlter(SessionState session, Query query)
        {
            try
            {
                using var transactionReference = _core.Transactions.APIAcquire(session);

                if (query.SubQueryType == SubQueryType.Schema)
                {
                    var pageSize = query.GetAttribute(Query.Attribute.PageSize, _core.Settings.DefaultDocumentPageSize);
                    string schemaName = query.Schemas.Single().Name;
                    _core.Schemas.Alter(transactionReference.Transaction, schemaName, pageSize);
                }
                else
                {
                    throw new KbNotImplementedException();
                }

                return transactionReference.CommitAndApplyMetricsThenReturnResults();
            }
            catch (Exception ex)
            {
                Management.LogManager.Error($"Failed to execute schema alter for process id {session.ProcessId}.", ex);
                throw;
            }
        }

        internal KbActionResponse ExecuteCreate(SessionState session, Query query)
        {
            try
            {
                using var transactionReference = _core.Transactions.APIAcquire(session);

                if (query.SubQueryType == SubQueryType.Schema)
                {
                    var pageSize = query.GetAttribute(Query.Attribute.PageSize, _core.Settings.DefaultDocumentPageSize);
                    string schemaName = query.Schemas.Single().Name;
                    _core.Schemas.CreateSingleSchema(transactionReference.Transaction, schemaName, pageSize);
                }
                else
                {
                    throw new KbNotImplementedException();
                }

                return transactionReference.CommitAndApplyMetricsThenReturnResults();
            }
            catch (Exception ex)
            {
                Management.LogManager.Error($"Failed to execute schema create for process id {session.ProcessId}.", ex);
                throw;
            }
        }

        internal KbQueryResult ExecuteList(SessionState session, Query query)
        {
            try
            {
                using var transactionReference = _core.Transactions.APIAcquire(session);
                var result = new KbQueryResult();

                if (query.SubQueryType == SubQueryType.Schemas)
                {
                    var schemaList = _core.Schemas.GetListOfChildren(
                        transactionReference.Transaction, query.Schemas.Single().Name, query.RowLimit);

                    result.Fields.Add(new KbQueryField("Name"));
                    result.Fields.Add(new KbQueryField("Path"));

                    result.Rows.AddRange(schemaList.Select(o => new KbQueryRow([o.Item1, o.Item2])));
                }
                else
                {
                    throw new KbEngineException("Invalid list query subtype.");
                }

                return transactionReference.CommitAndApplyMetricsThenReturnResults(result, 0);
            }
            catch (Exception ex)
            {
                Management.LogManager.Error($"Failed to execute schema list for process id {session.ProcessId}.", ex);
                throw;
            }
        }
    }
}
