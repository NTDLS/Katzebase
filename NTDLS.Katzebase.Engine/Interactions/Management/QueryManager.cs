using NTDLS.Katzebase.Api;
using NTDLS.Katzebase.Api.Exceptions;
using NTDLS.Katzebase.Api.Models;
using NTDLS.Katzebase.Api.Payloads.Response;
using NTDLS.Katzebase.Engine.Interactions.APIHandlers;
using NTDLS.Katzebase.Engine.Sessions;
using NTDLS.Katzebase.Parsers.Functions.Aggregate;
using NTDLS.Katzebase.Parsers.Functions.Scalar;
using NTDLS.Katzebase.Parsers.Functions.System;
using NTDLS.Katzebase.Parsers.Query;
using NTDLS.Katzebase.Parsers.Query.SupportingTypes;
using System.Diagnostics;
using System.Text;
using static NTDLS.Katzebase.Parsers.Constants;
using static NTDLS.Katzebase.Shared.EngineConstants;

namespace NTDLS.Katzebase.Engine.Interactions.Management
{
    /// <summary>
    /// Public core class methods for locking, reading, writing and managing tasks related to queries.
    /// </summary>
    public class QueryManager
    {
        private readonly EngineCore _core;
        public QueryAPIHandlers APIHandlers { get; private set; }

        private readonly QueryType[] _nonQueryTypes =
            [
                QueryType.Delete,
                QueryType.Rebuild,
                QueryType.Create,
                QueryType.Alter,
                QueryType.Set,
                QueryType.Kill,
                QueryType.Drop,
                QueryType.Begin,
                QueryType.Commit,
                QueryType.Insert,
                QueryType.Update,
                QueryType.SelectInto,
                QueryType.Rollback,
                QueryType.Declare
            ];

        internal QueryManager(EngineCore core)
        {
            _core = core;
            APIHandlers = new QueryAPIHandlers(core);

            SystemFunctionCollection.Initialize();
            ScalarFunctionCollection.Initialize();
            AggregateFunctionCollection.Initialize();
        }

        #region Internal helpers.

        /// <summary>
        /// Executes a query and returns the mapped object.
        /// </summary>
        internal IEnumerable<T> ExecuteQuery<T>(SessionState session, string queryText, object? userParameters = null) where T : new()
        {
            var queries = StaticParserBatch.Parse(queryText, _core.GlobalConstants, userParameters.ToUserParametersInsensitiveDictionary());
            if (queries.Count > 1)
            {
                throw new KbMultipleRecordSetsException("Prepare batch resulted in more than one query.");
            }
            var results = _core.Query.ExecuteQuery(session, queries[0]);
            if (queries.Count > 1)
            {
                throw new KbMultipleRecordSetsException();
            }
            return results.Collection[0].MapTo<T>();
        }

        /// <summary>
        /// Executes a query without a result. This function is designed to be used internally and will happily parse a batch unlike the internal ExecuteQuery().
        /// </summary>
        internal KbQueryResultCollection ExecuteNonQuery(SessionState session, string queryText, object? userParameters = null)
        {
            var results = new KbQueryResultCollection();

            session.SetCurrentQuery(queryText);

            foreach (var query in StaticParserBatch.Parse(queryText, _core.GlobalConstants, userParameters.ToUserParametersInsensitiveDictionary()))
            {
                results.Add(_core.Query.ExecuteQuery(session, query));
            }

            session.ClearCurrentQuery();

            return results;
        }

        #endregion

        internal KbQueryExplain ExplainPlan(SessionState session, Query query)
        {
            try
            {
                if (query.QueryType == QueryType.Select
                    || query.QueryType == QueryType.Delete
                    || query.QueryType == QueryType.Update)
                {
                    return _core.Documents.QueryHandlers.ExecuteExplainPlan(session, query);
                }
                else if (query.QueryType == QueryType.Set)
                {
                    return new KbQueryExplain();
                }
                else
                {
                    throw new NotImplementedException();
                }
            }
            catch (Exception ex)
            {
                LogManager.Error($"{new StackFrame(1).GetMethod()} failed for process: [{session.ProcessId}].", ex);
                throw;
            }
        }

        internal KbQueryExplain ExplainOperations(SessionState session, Query query)
        {
            try
            {
                switch (query.QueryType)
                {
                    case QueryType.Delete:
                    case QueryType.Insert:
                    case QueryType.Select:
                    case QueryType.Update:
                        return _core.Documents.QueryHandlers.ExecuteExplainOperations(session, query);
                    default:
                        return new KbQueryExplain(); //No explanation for these operations.
                }
            }
            catch (Exception ex)
            {
                LogManager.Error($"{new StackFrame(1).GetMethod()} failed for process: [{session.ProcessId}].", ex);
                throw;
            }
        }

        internal KbQueryResultCollection ExecuteProcedure(SessionState session, KbProcedure procedure)
        {
            var statement = new StringBuilder($"EXEC {procedure.SchemaName}:{procedure.ProcedureName}");

            using var transactionReference = _core.Transactions.APIAcquire(session);

            var physicalSchema = _core.Schemas.Acquire(transactionReference.Transaction, procedure.SchemaName, LockOperation.Read);

            var physicalProcedure = _core.Procedures.Acquire(transactionReference.Transaction, physicalSchema, procedure.ProcedureName, LockOperation.Read)
                ?? throw new KbProcessingException($"Procedure [{procedure.ProcedureName}] was not found in schema [{procedure.SchemaName}]");

            if (physicalProcedure.Parameters.Count > 0)
            {
                statement.Append('(');

                foreach (var parameter in physicalProcedure.Parameters)
                {
                    if (procedure.UserParameters?.TryGetValue(parameter.Name, out var value) == false)
                    {
                        statement.Append($"'{value}',");
                    }
                    else
                    {
                        throw new KbProcessingException($"Parameter [{parameter.Name}] was not passed when calling procedure [{procedure.ProcedureName}] in schema [{procedure.SchemaName}]");
                    }
                }
                statement.Length--; //Remove the trailing ','.
                statement.Append(')');
            }

            var batch = StaticParserBatch.Parse(statement.ToString(), _core.GlobalConstants);
            if (batch.Count > 1)
            {
                throw new KbProcessingException("Expected only one procedure call per batch.");
            }
            return _core.Procedures.QueryHandlers.ExecuteExec(session, batch.First());
        }

        internal KbQueryResultCollection ExecuteQuery(SessionState session, Query query)
        {
            try
            {
                if (_nonQueryTypes.Contains(query.QueryType)) //Reroute to non-query as appropriate:
                {
                    var nonQueryResult = ExecuteNonQuery(session, query);
                    return KbQueryResult.FromActionResponse(nonQueryResult).ToCollection();
                }

                switch (query.QueryType)
                {
                    case QueryType.Select:
                        return _core.Documents.QueryHandlers.ExecuteSelect(session, query).ToCollection();
                    case QueryType.Sample:
                        return _core.Documents.QueryHandlers.ExecuteSample(session, query).ToCollection();
                    case QueryType.Exec:
                        return _core.Procedures.QueryHandlers.ExecuteExec(session, query);
                    case QueryType.List:
                        if (query.SubQueryType == SubQueryType.Documents)
                        {
                            return _core.Documents.QueryHandlers.ExecuteList(session, query).ToCollection();
                        }
                        else if (query.SubQueryType == SubQueryType.Schemas)
                        {
                            return _core.Schemas.QueryHandlers.ExecuteList(session, query).ToCollection();
                        }
                        throw new KbEngineException($"Invalid query query subtype: [{query.SubQueryType}] for [{query.QueryType}].");
                    case QueryType.Analyze:
                        if (query.SubQueryType == SubQueryType.Index)
                        {
                            return _core.Indexes.QueryHandlers.ExecuteAnalyze(session, query).ToCollection();
                        }
                        else if (query.SubQueryType == SubQueryType.Schema)
                        {
                            return _core.Schemas.QueryHandlers.ExecuteAnalyze(session, query).ToCollection();
                        }
                        throw new KbEngineException($"Invalid query query subtype: [{query.SubQueryType}] for [{query.QueryType}].");
                    default:
                        throw new KbEngineException("Invalid query type.");
                }
            }
            catch (Exception ex)
            {
                LogManager.Error($"{new StackFrame(1).GetMethod()} failed for process: [{session.ProcessId}].", ex);
                throw;
            }
        }

        internal KbBaseActionResponse ExecuteNonQuery(SessionState session, Query query)
        {
            try
            {
                switch (query.QueryType)
                {
                    case QueryType.Declare:
                        return _core.Procedures.QueryHandlers.ExecuteDeclare(session, query);
                    case QueryType.Insert:
                        return _core.Documents.QueryHandlers.ExecuteInsert(session, query);
                    case QueryType.Update:
                        return _core.Documents.QueryHandlers.ExecuteUpdate(session, query);
                    case QueryType.SelectInto:
                        return _core.Documents.QueryHandlers.ExecuteSelectInto(session, query);
                    case QueryType.Delete:
                        return _core.Documents.QueryHandlers.ExecuteDelete(session, query);
                    case QueryType.Kill:
                        return _core.Sessions.QueryHandlers.ExecuteKillProcess(session, query);
                    case QueryType.Set:
                        return _core.Sessions.QueryHandlers.ExecuteSetVariable(session, query);
                    case QueryType.Rebuild:
                        if (query.SubQueryType == SubQueryType.Index
                            || query.SubQueryType == SubQueryType.UniqueKey)
                        {
                            return _core.Indexes.QueryHandlers.ExecuteRebuild(session, query);
                        }
                        throw new KbEngineException($"Invalid query query subtype: [{query.SubQueryType}] for [{query.QueryType}].");
                    case QueryType.Create:
                        if (query.SubQueryType == SubQueryType.Index || query.SubQueryType == SubQueryType.UniqueKey)
                        {
                            return _core.Indexes.QueryHandlers.ExecuteCreate(session, query);
                        }
                        else if (query.SubQueryType == SubQueryType.Procedure)
                        {
                            return _core.Procedures.QueryHandlers.ExecuteCreate(session, query);
                        }
                        else if (query.SubQueryType == SubQueryType.Schema)
                        {
                            return _core.Schemas.QueryHandlers.ExecuteCreate(session, query);
                        }
                        else if (query.SubQueryType == SubQueryType.Account)
                        {
                            return _core.Policies.QueryHandlers.ExecuteCreateAccount(session, query);
                        }
                        else if (query.SubQueryType == SubQueryType.Role)
                        {
                            return _core.Policies.QueryHandlers.ExecuteCreateRole(session, query);
                        }
                        throw new KbEngineException($"Invalid query query subtype: [{query.SubQueryType}] for [{query.QueryType}].");
                    case QueryType.Alter:
                        if (query.SubQueryType == SubQueryType.Schema)
                        {
                            return _core.Schemas.QueryHandlers.ExecuteAlter(session, query);
                        }
                        else if (query.SubQueryType == SubQueryType.Configuration)
                        {
                            return _core.Environment.QueryHandlers.ExecuteAlter(session, query);
                        }
                        else if (query.SubQueryType == SubQueryType.AddUserToRole)
                        {
                            return _core.Policies.QueryHandlers.ExecuteAddUserToRole(session, query);
                        }
                        else if (query.SubQueryType == SubQueryType.RemoveUserFromRole)
                        {
                            return _core.Policies.QueryHandlers.ExecuteRemoveUserFromRole(session, query);
                        }
                        throw new KbEngineException($"Invalid query query subtype: [{query.SubQueryType}] for [{query.QueryType}].");
                    case QueryType.Drop:
                        if (query.SubQueryType == SubQueryType.Index
                            || query.SubQueryType == SubQueryType.UniqueKey)
                        {
                            return _core.Indexes.QueryHandlers.ExecuteDrop(session, query);
                        }
                        else if (query.SubQueryType == SubQueryType.Schema)
                        {
                            return _core.Schemas.QueryHandlers.ExecuteDrop(session, query);
                        }
                        throw new KbEngineException($"Invalid query query subtype: [{query.SubQueryType}] for [{query.QueryType}].");
                    case QueryType.Begin:
                        if (query.SubQueryType == SubQueryType.Transaction)
                        {
                            _core.Transactions.QueryHandlers.Begin(session);
                            return new KbActionResponse();
                        }
                        throw new KbEngineException($"Invalid query query subtype: [{query.SubQueryType}] for [{query.QueryType}].");
                    case QueryType.Rollback:
                        if (query.SubQueryType == SubQueryType.Transaction)
                        {
                            _core.Transactions.QueryHandlers.Rollback(session);
                            return new KbActionResponse();
                        }
                        throw new KbEngineException($"Invalid query query subtype: [{query.SubQueryType}] for [{query.QueryType}].");
                    case QueryType.Commit:
                        if (query.SubQueryType == SubQueryType.Transaction)
                        {
                            _core.Transactions.QueryHandlers.Commit(session);
                            return new KbActionResponse();
                        }
                        throw new KbEngineException($"Invalid query query subtype: [{query.SubQueryType}] for [{query.QueryType}].");
                    default:
                        throw new KbEngineException($"Invalid query type: [{query.QueryType}].");
                }
            }
            catch (Exception ex)
            {
                LogManager.Error($"{new StackFrame(1).GetMethod()} failed for process: [{session.ProcessId}].", ex);
                throw;
            }
        }
    }
}
