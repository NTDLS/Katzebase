using NTDLS.Katzebase.Api.Exceptions;
using NTDLS.Katzebase.Api.Models;
using NTDLS.Katzebase.Api.Payloads.Response;
using NTDLS.Katzebase.Engine.Interactions.Management;
using NTDLS.Katzebase.Engine.Sessions;
using NTDLS.Katzebase.Parsers.Query.SupportingTypes;
using System.Diagnostics;
using static NTDLS.Katzebase.Parsers.Constants;
using static NTDLS.Katzebase.Shared.EngineConstants;

namespace NTDLS.Katzebase.Engine.Interactions.QueryProcessors
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
                LogManager.Error($"Failed to instantiate schema query handler.", ex);
                throw;
            }

        }

        internal KbQueryResult ExecuteAnalyze(SessionState session, PreparedQuery query)
        {
            try
            {
                using var transactionReference = _core.Transactions.APIAcquire(session);
                string schemaName = query.Schemas.First().Name;

                #region Security policy enforcment.

                _core.Policy.EnforceSchemaPolicy(transactionReference.Transaction, schemaName, SecurityPolicyPermission.Manage);

                #endregion

                var result = new KbQueryResult();

                if (query.SubQueryType == SubQueryType.Schema)
                {
                    var includePhysicalPages = query.GetAttribute(PreparedQuery.Attribute.IncludePhysicalPages, false);
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
                LogManager.Error($"{new StackFrame(1).GetMethod()} failed for process: [{session.ProcessId}].", ex);
                throw;
            }
        }

        internal KbActionResponse ExecuteDrop(SessionState session, PreparedQuery query)
        {
            try
            {
                using var transactionReference = _core.Transactions.APIAcquire(session);
                string schemaName = query.Schemas.First().Name;

                #region Security policy enforcment.

                _core.Policy.EnforceSchemaPolicy(transactionReference.Transaction, schemaName, SecurityPolicyPermission.Manage);

                #endregion

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
                LogManager.Error($"{new StackFrame(1).GetMethod()} failed for process: [{session.ProcessId}].", ex);
                throw;
            }
        }

        internal KbActionResponse ExecuteAlter(SessionState session, PreparedQuery query)
        {
            try
            {
                using var transactionReference = _core.Transactions.APIAcquire(session);

                string schemaName = query.Schemas.Single().Name;

                #region Security policy enforcment.

                _core.Policy.EnforceSchemaPolicy(transactionReference.Transaction, schemaName, SecurityPolicyPermission.Manage);

                #endregion

                var pageSize = query.GetAttribute(PreparedQuery.Attribute.PageSize, _core.Settings.DefaultDocumentPageSize);
                _core.Schemas.Alter(transactionReference.Transaction, schemaName, pageSize);

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

                #region Security policy enforcment.

                _core.Policy.EnforceSchemaPolicy(transactionReference.Transaction, schemaName, SecurityPolicyPermission.Manage);

                #endregion

                var pageSize = query.GetAttribute(PreparedQuery.Attribute.PageSize, _core.Settings.DefaultDocumentPageSize);
                _core.Schemas.CreateSingleSchema(transactionReference.Transaction, schemaName, pageSize);

                return transactionReference.CommitAndApplyMetricsThenReturnResults();
            }
            catch (Exception ex)
            {
                LogManager.Error($"{new StackFrame(1).GetMethod()} failed for process: [{session.ProcessId}].", ex);
                throw;
            }
        }

        internal KbQueryResult ExecuteList(SessionState session, PreparedQuery query)
        {
            try
            {
                using var transactionReference = _core.Transactions.APIAcquire(session);
                var result = new KbQueryResult();

                var schemaName = query.Schemas.Single().Name;

                #region Security policy enforcment.

                _core.Policy.EnforceSchemaPolicy(transactionReference.Transaction, schemaName, SecurityPolicyPermission.Manage);

                #endregion

                var schemaList = _core.Schemas.GetListOfChildren(
                    transactionReference.Transaction, schemaName, query.RowLimit);

                result.Fields.Add(new KbQueryField("Name"));
                result.Fields.Add(new KbQueryField("Path"));

                result.Rows.AddRange(schemaList.Select(o => new KbQueryRow([o.Item1, o.Item2])));

                return transactionReference.CommitAndApplyMetricsThenReturnResults(result, 0);
            }
            catch (Exception ex)
            {
                LogManager.Error($"{new StackFrame(1).GetMethod()} failed for process: [{session.ProcessId}].", ex);
                throw;
            }
        }
    }
}
