using NTDLS.Katzebase.Api.Exceptions;
using NTDLS.Katzebase.Api.Payloads.Response;
using NTDLS.Katzebase.Api.Types;
using NTDLS.Katzebase.Engine.Functions.System;
using NTDLS.Katzebase.Engine.Interactions.Management;
using NTDLS.Katzebase.Engine.QueryProcessing.Functions;
using NTDLS.Katzebase.Engine.Sessions;
using NTDLS.Katzebase.Parsers.Functions.System;
using NTDLS.Katzebase.Parsers.Query.Fields;
using NTDLS.Katzebase.Parsers.Query.Fields.Expressions;
using NTDLS.Katzebase.Parsers.Query.SupportingTypes;
using NTDLS.Katzebase.PersistentTypes.Procedure;
using System.Diagnostics;
using static NTDLS.Katzebase.Api.KbConstants;
using static NTDLS.Katzebase.Parsers.Constants;

namespace NTDLS.Katzebase.Engine.Interactions.QueryProcessors
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
                LogManager.Error($"Failed to instantiate procedures query handler.", ex);
                throw;
            }
        }

        /// <summary>
        /// Declares a variable, collapses any expression.
        /// </summary>
        internal KbActionResponse ExecuteDeclare(SessionState session, Query query)
        {
            try
            {
                using var transactionReference = _core.Transactions.APIAcquire(session);

                var variablePlaceholder = query.GetAttribute<string>(Query.Attribute.VariablePlaceholder);
                var expression = query.GetAttribute<string>(Query.Attribute.Expression);

                var mockField = new QueryFieldExpressionString(null, expression);
                var mockFields = new SelectFieldCollection(query.Batch);

                var auxiliaryValues = new KbInsensitiveDictionary<string?>();
                foreach (var literal in query.Batch.Variables.Collection)
                {
                    auxiliaryValues.Add(literal.Key, literal.Value.Value);
                }

                var collapsedExpression = mockField.CollapseScalarQueryField
                            (transactionReference.Transaction, query, mockFields, auxiliaryValues);

                if (double.TryParse(collapsedExpression, out var _))
                {
                    query.Batch.Variables.Collection[variablePlaceholder] = new KbVariable(collapsedExpression, KbBasicDataType.Numeric);
                }
                else
                {
                    query.Batch.Variables.Collection[variablePlaceholder] = new KbVariable(collapsedExpression, KbBasicDataType.String);
                }

                return transactionReference.CommitAndApplyMetricsThenReturnResults();
            }
            catch (Exception ex)
            {
                LogManager.Error($"{new StackFrame(1).GetMethod()} failed for process: [{session.ProcessId}].", ex);
                throw;
            }
        }

        internal KbActionResponse ExecuteCreate(SessionState session, Query query)
        {
            try
            {
                using var transactionReference = _core.Transactions.APIAcquire(session);

                if (query.SubQueryType == SubQueryType.Procedure)
                {
                    var objectName = query.GetAttribute<string>(Query.Attribute.ObjectName);
                    var objectSchema = query.GetAttribute<string>(Query.Attribute.Schema);
                    var parameters = query.GetAttribute<List<PhysicalProcedureParameter>>(Query.Attribute.Parameters);
                    var Batches = query.GetAttribute<List<string>>(Query.Attribute.Batches);

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
                LogManager.Error($"{new StackFrame(1).GetMethod()} failed for process: [{session.ProcessId}].", ex);
                throw;
            }
        }

        internal KbQueryResultCollection ExecuteExec(SessionState session, Query query)
        {
            try
            {
                var schemaName = query.GetAttribute<string>(Query.Attribute.Schema);
                var objectName = query.GetAttribute<string>(Query.Attribute.ObjectName);

                using var transactionReference = _core.Transactions.APIAcquire(session);

                var collapsedParameters = new List<string?>();

                if (query.ProcedureParameters != null)
                {
                    foreach (var parameter in query.ProcedureParameters)
                    {
                        var collapsedParameter = parameter.Expression.CollapseScalarQueryField(transactionReference.Transaction, query, query.ProcedureParameters, new());

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
                LogManager.Error($"{new StackFrame(1).GetMethod()} failed for process: [{session.ProcessId}].", ex);
                throw;
            }
        }
    }
}
