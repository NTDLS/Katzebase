﻿using NTDLS.Katzebase.Api.Exceptions;
using NTDLS.Katzebase.Api.Models;
using NTDLS.Katzebase.Api.Payloads.Response;
using NTDLS.Katzebase.Engine.Interactions.APIHandlers;
using NTDLS.Katzebase.Engine.Scripts;
using NTDLS.Katzebase.Engine.Sessions;
using NTDLS.Katzebase.Parsers;
using NTDLS.Katzebase.Parsers.Functions.Aggregate;
using NTDLS.Katzebase.Parsers.Functions.Scalar;
using NTDLS.Katzebase.Parsers.Functions.System;
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
                QueryType.Alter,
                QueryType.Begin,
                QueryType.Commit,
                QueryType.Create,
                QueryType.Declare,
                QueryType.Delete,
                QueryType.Deny,
                QueryType.Drop,
                QueryType.Grant,
                QueryType.Insert,
                QueryType.Kill,
                QueryType.Rebuild,
                QueryType.Revoke,
                QueryType.Rollback,
                QueryType.SelectInto,
                QueryType.Set,
                QueryType.Update
            ];

        internal QueryManager(EngineCore core)
        {
            _core = core;
            APIHandlers = new QueryAPIHandlers(core);

            SystemFunctionCollection.Initialize();
            ScalarFunctionCollection.Initialize();
            AggregateFunctionCollection.Initialize();
        }

        #region Internal-system query utilities.

        /// <summary>
        /// Creates an ephemeral system session, executes a query returning the first row and field object, then commits the transaction.
        /// Internal system usage only.
        /// </summary>
        internal T? SystemExecuteScalarAndCommit<T>(string queryText, object? userParameters = null) where T : new()
        {
            queryText = EmbeddedScripts.GetScriptOrLoadFile(queryText);
            using var ephemeral = _core.Sessions.CreateEphemeralSystemSession();
            var result = ephemeral.Transaction.ExecuteScalar<T>(queryText, userParameters);
            ephemeral.Commit();
            return result;
        }

        /// <summary>
        /// Creates an ephemeral system session, executes a query returning the mapped object, then commits the transaction.
        /// Internal system usage only.
        /// </summary>
        internal IEnumerable<T> SystemExecuteQueryAndCommit<T>(string queryText, object? userParameters = null) where T : new()
        {
            queryText = EmbeddedScripts.GetScriptOrLoadFile(queryText);
            using var ephemeral = _core.Sessions.CreateEphemeralSystemSession();
            var result = ephemeral.Transaction.ExecuteQuery<T>(queryText, userParameters);
            ephemeral.Commit();
            return result;
        }

        /// <summary>
        /// Creates an ephemeral system session, executes a query without a result, then commits the transaction.
        /// Internal system usage only.
        /// </summary>
        internal KbQueryResultCollection SystemExecuteAndCommitNonQuery(string queryText, object? userParameters = null)
        {
            queryText = EmbeddedScripts.GetScriptOrLoadFile(queryText);
            using var ephemeral = _core.Sessions.CreateEphemeralSystemSession();
            var result = ephemeral.Transaction.ExecuteNonQuery(queryText, userParameters);
            ephemeral.Commit();
            return result;
        }

        #endregion

        internal KbQueryExplain ExplainPlan(SessionState session, PreparedQuery query)
        {
            try
            {
                switch (query.QueryType)
                {
                    case QueryType.Delete:
                    case QueryType.Insert:
                    case QueryType.Select:
                    case QueryType.Update:
                        return _core.Documents.QueryHandlers.ExecuteExplainPlan(session, query);
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

        internal KbQueryExplain ExplainOperations(SessionState session, PreparedQuery query)
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

            var batch = StaticBatchParser.Parse(statement.ToString(), _core.GlobalConstants);
            if (batch.Count > 1)
            {
                throw new KbProcessingException("Expected only one procedure call per batch.");
            }
            return _core.Procedures.QueryHandlers.ExecuteExec(session, batch.First());
        }

        internal KbQueryResultCollection ExecuteQuery(SessionState session, PreparedQuery query)
        {
            try
            {
                if (_nonQueryTypes.Contains(query.QueryType)) //Reroute to non-query as appropriate:
                {
                    var nonQueryResult = ExecuteNonQuery(session, query);
                    return KbQueryResult.FromActionResponse(nonQueryResult).ToCollection();
                }

                return query.QueryType switch
                {
                    QueryType.Select => _core.Documents.QueryHandlers.ExecuteSelect(session, query).ToCollection(),
                    QueryType.Sample => _core.Documents.QueryHandlers.ExecuteSample(session, query).ToCollection(),
                    QueryType.Exec => _core.Procedures.QueryHandlers.ExecuteExec(session, query),
                    QueryType.List => query.SubQueryType switch
                    {
                        SubQueryType.Documents => _core.Documents.QueryHandlers.ExecuteList(session, query).ToCollection(),
                        SubQueryType.Schemas => _core.Schemas.QueryHandlers.ExecuteList(session, query).ToCollection(),
                        _ => throw new KbEngineException($"Invalid query query subtype: [{query.SubQueryType}] for [{query.QueryType}]."),
                    },
                    QueryType.Analyze => query.SubQueryType switch
                    {
                        SubQueryType.Index => _core.Indexes.QueryHandlers.ExecuteAnalyze(session, query).ToCollection(),
                        SubQueryType.Schema => _core.Schemas.QueryHandlers.ExecuteAnalyze(session, query).ToCollection(),
                        _ => throw new KbEngineException($"Invalid query query subtype: [{query.SubQueryType}] for [{query.QueryType}]."),
                    },
                    _ => throw new KbEngineException("Invalid query type."),
                };
            }
            catch (Exception ex)
            {
                LogManager.Error($"{new StackFrame(1).GetMethod()} failed for process: [{session.ProcessId}].", ex);
                throw;
            }
        }

        internal KbBaseActionResponse ExecuteNonQuery(SessionState session, PreparedQuery query)
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
                    case QueryType.Grant:
                        return _core.Policy.QueryHandlers.ExecuteGrant(session, query);
                    case QueryType.Deny:
                        return _core.Policy.QueryHandlers.ExecuteDeny(session, query);
                    case QueryType.Revoke:
                        return _core.Policy.QueryHandlers.ExecuteRevoke(session, query);
                    case QueryType.Rebuild:
                        return query.SubQueryType switch
                        {
                            SubQueryType.Index or SubQueryType.UniqueKey => _core.Indexes.QueryHandlers.ExecuteRebuild(session, query),
                            _ => throw new KbEngineException($"Invalid query query subtype: [{query.SubQueryType}] for [{query.QueryType}]."),
                        };
                    case QueryType.Create:
                        return query.SubQueryType switch
                        {
                            SubQueryType.Index or SubQueryType.UniqueKey => _core.Indexes.QueryHandlers.ExecuteCreate(session, query),
                            SubQueryType.Procedure => _core.Procedures.QueryHandlers.ExecuteCreate(session, query),
                            SubQueryType.Schema => _core.Schemas.QueryHandlers.ExecuteCreate(session, query),
                            SubQueryType.Account => _core.Policy.QueryHandlers.ExecuteCreateAccount(session, query),
                            SubQueryType.Role => _core.Policy.QueryHandlers.ExecuteCreateRole(session, query),
                            _ => throw new KbEngineException($"Invalid query query subtype: [{query.SubQueryType}] for [{query.QueryType}]."),
                        };
                    case QueryType.Alter:
                        return query.SubQueryType switch
                        {
                            SubQueryType.Schema => _core.Schemas.QueryHandlers.ExecuteAlter(session, query),
                            SubQueryType.Configuration => _core.Environment.QueryHandlers.ExecuteAlter(session, query),
                            SubQueryType.AddUserToRole => _core.Policy.QueryHandlers.ExecuteAddAccountToRole(session, query),
                            SubQueryType.RemoveUserFromRole => _core.Policy.QueryHandlers.ExecuteRemoveAccountFromRole(session, query),
                            _ => throw new KbEngineException($"Invalid query query subtype: [{query.SubQueryType}] for [{query.QueryType}]."),
                        };
                    case QueryType.Drop:
                        return query.SubQueryType switch
                        {
                            SubQueryType.Index or SubQueryType.UniqueKey => _core.Indexes.QueryHandlers.ExecuteDrop(session, query),
                            SubQueryType.Schema => _core.Schemas.QueryHandlers.ExecuteDrop(session, query),
                            SubQueryType.Role => _core.Policy.QueryHandlers.ExecuteDropRole(session, query),
                            SubQueryType.Account => _core.Policy.QueryHandlers.ExecuteDropAccount(session, query),
                            _ => throw new KbEngineException($"Invalid query query subtype: [{query.SubQueryType}] for [{query.QueryType}]."),
                        };
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
                        switch (query.SubQueryType)
                        {
                            case SubQueryType.Transaction:
                                _core.Transactions.QueryHandlers.Commit(session);
                                return new KbActionResponse();
                            default:
                                throw new KbEngineException($"Invalid query query subtype: [{query.SubQueryType}] for [{query.QueryType}].");
                        }
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
