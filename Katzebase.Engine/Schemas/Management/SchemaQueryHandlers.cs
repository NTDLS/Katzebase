using Katzebase.Engine.Query;
using Katzebase.PublicLibrary.Exceptions;
using Katzebase.PublicLibrary.Payloads;
using static Katzebase.Engine.Library.EngineConstants;

namespace Katzebase.Engine.Schemas.Management
{
    /// <summary>
    /// Internal class methods for handling query requests related to schemas.
    /// </summary>
    internal class SchemaQueryHandlers
    {
        private readonly Core core;

        public SchemaQueryHandlers(Core core)
        {
            this.core = core;

            try
            {
            }
            catch (Exception ex)
            {
                core.Log.Write($"Failed to instanciate schema query handler.", ex);
                throw;
            }

        }

        internal KbQueryResult ExecuteAnalyze(ulong processId, PreparedQuery preparedQuery)
        {
            try
            {
                using var transactionReference = core.Transactions.Acquire(processId);
                string schemaName = preparedQuery.Schemas.First().Name;

                var result = new KbQueryResult();

                if (preparedQuery.SubQueryType == SubQueryType.Schema)
                {
                    var includePhysicalPages = preparedQuery.Attribute<bool>(PreparedQuery.QueryAttribute.IncludePhysicalPages, false);
                    result = core.Schemas.AnalysePages(transactionReference.Transaction, schemaName, includePhysicalPages);
                }
                else
                {
                    throw new KbNotImplementedException();
                }

                return transactionReference.CommitAndApplyMetricsThenReturnResults(result, 0);
            }
            catch (Exception ex)
            {
                core.Log.Write($"Failed to execute schema drop for process id {processId}.", ex);
                throw;
            }
        }

        internal KbActionResponse ExecuteDrop(ulong processId, PreparedQuery preparedQuery)
        {
            try
            {
                using var transactionReference = core.Transactions.Acquire(processId);
                string schemaName = preparedQuery.Schemas.First().Name;

                if (preparedQuery.SubQueryType == SubQueryType.Schema)
                {
                    core.Schemas.Drop(transactionReference.Transaction, schemaName);
                }
                else
                {
                    throw new KbNotImplementedException();
                }

                return transactionReference.CommitAndApplyMetricsThenReturnResults();
            }
            catch (Exception ex)
            {
                core.Log.Write($"Failed to execute schema drop for process id {processId}.", ex);
                throw;
            }
        }

        internal KbActionResponse ExecuteAlter(ulong processId, PreparedQuery preparedQuery)
        {
            try
            {
                using var transactionReference = core.Transactions.Acquire(processId);

                if (preparedQuery.SubQueryType == SubQueryType.Schema)
                {
                    var pageSize = preparedQuery.Attribute<uint>(PreparedQuery.QueryAttribute.PageSize, core.Settings.DefaultDocumentPageSize);
                    string schemaName = preparedQuery.Schemas.Single().Name;
                    core.Schemas.Alter(transactionReference.Transaction, schemaName, pageSize);
                }
                else
                {
                    throw new KbNotImplementedException();
                }

                return transactionReference.CommitAndApplyMetricsThenReturnResults();
            }
            catch (Exception ex)
            {
                core.Log.Write($"Failed to execute schema alter for process id {processId}.", ex);
                throw;
            }
        }

        internal KbActionResponse ExecuteCreate(ulong processId, PreparedQuery preparedQuery)
        {
            try
            {
                using var transactionReference = core.Transactions.Acquire(processId);

                if (preparedQuery.SubQueryType == SubQueryType.Schema)
                {
                    var pageSize = preparedQuery.Attribute<uint>(PreparedQuery.QueryAttribute.PageSize, core.Settings.DefaultDocumentPageSize);
                    string schemaName = preparedQuery.Schemas.Single().Name;
                    core.Schemas.CreateSingleSchema(transactionReference.Transaction, schemaName, pageSize);
                }
                else
                {
                    throw new KbNotImplementedException();
                }

                return transactionReference.CommitAndApplyMetricsThenReturnResults();
            }
            catch (Exception ex)
            {
                core.Log.Write($"Failed to execute schema create for process id {processId}.", ex);
                throw;
            }
        }

        internal KbQueryResult ExecuteList(ulong processId, PreparedQuery preparedQuery)
        {
            try
            {
                using var transactionReference = core.Transactions.Acquire(processId);
                var result = new KbQueryResult();

                if (preparedQuery.SubQueryType == SubQueryType.Schemas)
                {
                    var schemaList = core.Schemas.GetListByPreparedQuery(transactionReference.Transaction, preparedQuery.Schemas.Single().Name, preparedQuery.RowLimit);

                    result.Fields.Add(new KbQueryField("Name"));
                    result.Fields.Add(new KbQueryField("Path"));

                    result.Rows.AddRange(schemaList.Select(o => new KbQueryRow(new List<string?> { o.Item1, o.Item2 })));
                }
                else
                {
                    throw new KbEngineException("Invalid list query subtype.");
                }

                return transactionReference.CommitAndApplyMetricsThenReturnResults(result, 0);
            }
            catch (Exception ex)
            {
                core.Log.Write($"Failed to execute schema list for process id {processId}.", ex);
                throw;
            }
        }
    }
}
