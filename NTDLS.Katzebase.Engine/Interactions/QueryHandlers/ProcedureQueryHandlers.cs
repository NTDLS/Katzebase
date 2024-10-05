using NTDLS.Katzebase.Client.Exceptions;
using NTDLS.Katzebase.Client.Payloads;
using NTDLS.Katzebase.Engine.Functions.System;
using NTDLS.Katzebase.Engine.QueryProcessing.Functions;
using NTDLS.Katzebase.Engine.Sessions;
using NTDLS.Katzebase.Parsers.Functions.System;
using NTDLS.Katzebase.Parsers.Query.SupportingTypes;
using NTDLS.Katzebase.PersistentTypes.Procedure;
using static NTDLS.Katzebase.Parsers.Constants;

namespace NTDLS.Katzebase.Engine.Interactions.QueryHandlers
{
    /// <summary>
    /// Internal class methods for handling query requests related to procedures.
    /// </summary>
    internal class ProcedureQueryHandlers
    {
        private readonly EngineCore _core;

        public ProcedureQueryHandlers(EngineCore core)
        {
            _core = core;

            try
            {
            }
            catch (Exception ex)
            {
                Management.LogManager.Error($"Failed to instantiate procedures query handler.", ex);
                throw;
            }
        }

        internal KbActionResponse ExecuteCreate(SessionState session, PreparedQuery preparedQuery)
        {
            try
            {
                using var transactionReference = _core.Transactions.APIAcquire(session);

                if (preparedQuery.SubQueryType == SubQueryType.Procedure)
                {
                    var objectName = preparedQuery.Attribute<string>(PreparedQuery.QueryAttribute.ObjectName);
                    var objectSchema = preparedQuery.Attribute<string>(PreparedQuery.QueryAttribute.Schema);
                    var parameters = preparedQuery.Attribute<List<PhysicalProcedureParameter>>(PreparedQuery.QueryAttribute.Parameters);
                    var Batches = preparedQuery.Attribute<List<string>>(PreparedQuery.QueryAttribute.Batches);

                    _core.Procedures.CreateCustomProcedure(transactionReference.Transaction, objectSchema, objectName, parameters, Batches);
                }
                else
                {
                    throw new KbNotImplementedException();
                }

                return transactionReference.CommitAndApplyMetricsThenReturnResults();
            }
            catch (Exception ex)
            {
                Management.LogManager.Error($"Failed to execute procedure create for process id {session.ProcessId}.", ex);
                throw;
            }
        }

        internal KbQueryResultCollection ExecuteExec(SessionState session, PreparedQuery preparedQuery)
        {
            try
            {
                var schemaName = preparedQuery.Attribute<string>(PreparedQuery.QueryAttribute.Schema);
                var objectName = preparedQuery.Attribute<string>(PreparedQuery.QueryAttribute.ObjectName);

                using var transactionReference = _core.Transactions.APIAcquire(session);

                var collapsedParameters = new List<string?>();

                if (preparedQuery.ProcedureParameters != null)
                {
                    foreach (var parameter in preparedQuery.ProcedureParameters)
                    {
                        var collapsedParameter = StaticScalarExpressionProcessor.CollapseScalarQueryField(parameter.Expression,
                            transactionReference.Transaction, preparedQuery, preparedQuery.ProcedureParameters, new());

                        collapsedParameters.Add(collapsedParameter);
                    }
                }

                if (string.IsNullOrEmpty(schemaName) || schemaName == ":")
                {
                    if (SystemFunctionCollection.TryGetFunction(objectName, out var systemFunction))
                    {
                        var systemFunctionResult = SystemFunctionImplementation.ExecuteFunction(
                            _core, transactionReference.Transaction, objectName, collapsedParameters);

                        return transactionReference.CommitAndApplyMetricsThenReturnResults(systemFunctionResult, 0);
                    }
                }

                var result = _core.Procedures.ExecuteProcedure(transactionReference.Transaction, schemaName, objectName);
                return transactionReference.CommitAndApplyMetricsThenReturnResults(result, 0);
            }
            catch (Exception ex)
            {
                Management.LogManager.Error($"Failed to execute procedure for process id {session.ProcessId}.", ex);
                throw;
            }
        }
    }
}
