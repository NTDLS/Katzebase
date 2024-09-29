using NTDLS.Katzebase.Client.Exceptions;
using NTDLS.Katzebase.Client.Payloads;
using NTDLS.Katzebase.Engine.Parsers.Query.SupportingTypes;
using NTDLS.Katzebase.Engine.Sessions;
using System.Linq;
using static NTDLS.Katzebase.Engine.Library.EngineConstants;

namespace NTDLS.Katzebase.Engine.Interactions.QueryHandlers
{
    /// <summary>
    /// Internal class methods for handling query requests related to schemas.
    /// </summary>
    internal class SchemaQueryHandlers<TData> where TData: IStringable
    {
        private readonly EngineCore<TData> _core;

        public SchemaQueryHandlers(EngineCore<TData> core)
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

        internal KbQueryDocumentListResult<TData> ExecuteAnalyze(SessionState session, PreparedQuery<TData> preparedQuery)
        {
            try
            {
                using var transactionReference = _core.Transactions.Acquire(session);
                string schemaName = preparedQuery.Schemas.First().Name;

                var result = new KbQueryDocumentListResult<TData>();

                if (preparedQuery.SubQueryType == SubQueryType.Schema)
                {
                    var includePhysicalPages = preparedQuery.Attribute(PreparedQuery<TData>.QueryAttribute.IncludePhysicalPages, false);
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

        internal KbActionResponse ExecuteDrop(SessionState session, PreparedQuery<TData> preparedQuery)
        {
            try
            {
                using var transactionReference = _core.Transactions.Acquire(session);
                string schemaName = preparedQuery.Schemas.First().Name;

                if (preparedQuery.SubQueryType == SubQueryType.Schema)
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

        internal KbActionResponse ExecuteAlter(SessionState session, PreparedQuery<TData> preparedQuery)
        {
            try
            {
                using var transactionReference = _core.Transactions.Acquire(session);

                if (preparedQuery.SubQueryType == SubQueryType.Schema)
                {
                    var pageSize = preparedQuery.Attribute(PreparedQuery<TData>.QueryAttribute.PageSize, _core.Settings.DefaultDocumentPageSize);
                    string schemaName = preparedQuery.Schemas.Single().Name;
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

        internal KbActionResponse ExecuteCreate(SessionState session, PreparedQuery<TData> preparedQuery)
        {
            try
            {
                using var transactionReference = _core.Transactions.Acquire(session);

                if (preparedQuery.SubQueryType == SubQueryType.Schema)
                {
                    var pageSize = preparedQuery.Attribute(PreparedQuery<TData>.QueryAttribute.PageSize, _core.Settings.DefaultDocumentPageSize);
                    string schemaName = preparedQuery.Schemas.Single().Name;
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

        internal KbQueryDocumentListResult<TData> ExecuteList(SessionState session, PreparedQuery<TData> preparedQuery)
        {
            try
            {
                using var transactionReference = _core.Transactions.Acquire(session);
                var result = new KbQueryDocumentListResult<TData>();

                if (preparedQuery.SubQueryType == SubQueryType.Schemas)
                {
                    var schemaList = _core.Schemas.GetListByPreparedQuery(
                        transactionReference.Transaction, preparedQuery.Schemas.Single().Name, preparedQuery.RowLimit);

                    result.Fields.Add(new KbQueryField("Name"));
                    result.Fields.Add(new KbQueryField("Path"));


                    //var toAdd = schemaList.Select(o => new KbQueryRow<TData>([o.Item1, o.Item2]));
                    var toAdd = schemaList.Select(o => {

                        var rList = new List<TData>(new TData[] {
                            o.Item1.CastToT<TData> (EngineCore<TData>.StrCast),
                            o.Item2.CastToT<TData> (EngineCore<TData>.StrCast)
                        });

                        return new KbQueryRow<TData>(rList);
                    
                    });

                    result.Rows.AddRange(toAdd);
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
