using NTDLS.Katzebase.Client.Exceptions;
using NTDLS.Katzebase.Client.Payloads;
using NTDLS.Katzebase.Engine.Functions.Aggregate;
using NTDLS.Katzebase.Engine.Functions.Scaler;
using NTDLS.Katzebase.Engine.Interactions.APIHandlers;
using NTDLS.Katzebase.Engine.Query;
using NTDLS.Katzebase.Engine.Sessions;
using System.Text;
using static NTDLS.Katzebase.Engine.Library.EngineConstants;

namespace NTDLS.Katzebase.Engine.Interactions.Management
{
    /// <summary>
    /// Public core class methods for locking, reading, writing and managing tasks related to queries.
    /// </summary>
    public class QueryManager
    {
        private readonly EngineCore _core;
        public QueryAPIHandlers APIHandlers { get; private set; }

        public QueryManager(EngineCore core)
        {
            _core = core;
            APIHandlers = new QueryAPIHandlers(core);

            ScalerFunctionCollection.Initialize();
            AggregateFunctionCollection.Initialize();
        }

        internal KbQueryExplain ExplainQuery(SessionState session, PreparedQuery preparedQuery)
        {
            try
            {
                if (preparedQuery.QueryType == QueryType.Select
                    || preparedQuery.QueryType == QueryType.Delete
                    || preparedQuery.QueryType == QueryType.Update)
                {
                    return _core.Documents.QueryHandlers.ExecuteExplain(session, preparedQuery);
                }
                else if (preparedQuery.QueryType == QueryType.Set)
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
                LogManager.Error($"Failed to explain query for process id {session.ProcessId}.", ex);
                throw;
            }
        }

        internal KbQueryResultCollection ExecuteProcedure(SessionState session, KbProcedure procedure)
        {
            var statement = new StringBuilder($"EXEC {procedure.SchemaName}:{procedure.ProcedureName}");

            using (var transactionReference = _core.Transactions.Acquire(session))
            {
                var physicalSchema = _core.Schemas.Acquire(transactionReference.Transaction, procedure.SchemaName, LockOperation.Read);

                var physicalProcedure = _core.Procedures.Acquire(transactionReference.Transaction, physicalSchema, procedure.ProcedureName, LockOperation.Read)
                    ?? throw new KbEngineException($"Procedure [{procedure.ProcedureName}] was not found in schema [{procedure.SchemaName}]");

                if (physicalProcedure.Parameters.Count > 0)
                {
                    statement.Append('(');

                    foreach (var parameter in physicalProcedure.Parameters)
                    {
                        if (procedure.Parameters.Collection.TryGetValue(parameter.Name.ToLowerInvariant(), out var value) == false)
                        {
                            throw new KbEngineException($"Parameter [{parameter.Name}] was not passed when calling procedure [{procedure.ProcedureName}] in schema [{procedure.SchemaName}]");
                        }
                        statement.Append($"'{value}',");
                    }
                    statement.Length--; //Remove the trailing ','.
                    statement.Append(')');
                }

                var batch = StaticQueryParser.PrepareBatch(statement.ToString());
                if (batch.Count > 1)
                {
                    throw new KbEngineException("Expected only one procedure call per batch.");
                }
                return _core.Procedures.QueryHandlers.ExecuteExec(session, batch.First());
            }
        }

        internal KbQueryResultCollection ExecuteQuery(SessionState session, PreparedQuery preparedQuery)
        {
            try
            {
                if (preparedQuery.QueryType == QueryType.Select)
                {
                    return _core.Documents.QueryHandlers.ExecuteSelect(session, preparedQuery).ToCollection();
                }
                else if (preparedQuery.QueryType == QueryType.Sample)
                {
                    return _core.Documents.QueryHandlers.ExecuteSample(session, preparedQuery).ToCollection();
                }
                else if (preparedQuery.QueryType == QueryType.Exec)
                {
                    return _core.Procedures.QueryHandlers.ExecuteExec(session, preparedQuery);
                }
                else if (preparedQuery.QueryType == QueryType.List)
                {
                    if (preparedQuery.SubQueryType == SubQueryType.Documents)
                    {
                        return _core.Documents.QueryHandlers.ExecuteList(session, preparedQuery).ToCollection();
                    }
                    else if (preparedQuery.SubQueryType == SubQueryType.Schemas)
                    {
                        return _core.Schemas.QueryHandlers.ExecuteList(session, preparedQuery).ToCollection();
                    }
                    throw new KbEngineException("Invalid list query subtype.");
                }
                if (preparedQuery.QueryType == QueryType.Analyze)
                {
                    if (preparedQuery.SubQueryType == SubQueryType.Index)
                    {
                        return _core.Indexes.QueryHandlers.ExecuteAnalyze(session, preparedQuery).ToCollection();
                    }
                    else if (preparedQuery.SubQueryType == SubQueryType.Schema)
                    {
                        return _core.Schemas.QueryHandlers.ExecuteAnalyze(session, preparedQuery).ToCollection();
                    }
                    throw new KbEngineException("Invalid analyze query subtype.");
                }
                else if (preparedQuery.QueryType == QueryType.Delete
                    || preparedQuery.QueryType == QueryType.Rebuild
                    || preparedQuery.QueryType == QueryType.Create
                    || preparedQuery.QueryType == QueryType.Alter
                    || preparedQuery.QueryType == QueryType.Set
                    || preparedQuery.QueryType == QueryType.Kill
                    || preparedQuery.QueryType == QueryType.Drop
                    || preparedQuery.QueryType == QueryType.Begin
                    || preparedQuery.QueryType == QueryType.Commit
                    || preparedQuery.QueryType == QueryType.Insert
                    || preparedQuery.QueryType == QueryType.Update
                    || preparedQuery.QueryType == QueryType.SelectInto
                    || preparedQuery.QueryType == QueryType.Rollback)
                {
                    //Reroute to non-query as appropriate:
                    return KbQueryDocumentListResult.FromActionResponse(ExecuteNonQuery(session, preparedQuery)).ToCollection();
                }
                else
                {
                    throw new NotImplementedException();
                }
            }
            catch (Exception ex)
            {
                LogManager.Error($"Failed to execute query for process id {session.ProcessId}.", ex);
                throw;
            }
        }

        internal KbBaseActionResponse ExecuteNonQuery(SessionState session, PreparedQuery preparedQuery)
        {
            try
            {
                if (preparedQuery.QueryType == QueryType.Insert)
                {
                    return _core.Documents.QueryHandlers.ExecuteInsert(session, preparedQuery);
                }
                else if (preparedQuery.QueryType == QueryType.Update)
                {
                    return _core.Documents.QueryHandlers.ExecuteUpdate(session, preparedQuery);
                }
                else if (preparedQuery.QueryType == QueryType.SelectInto)
                {
                    return _core.Documents.QueryHandlers.ExecuteSelectInto(session, preparedQuery);
                }
                else if (preparedQuery.QueryType == QueryType.Delete)
                {
                    return _core.Documents.QueryHandlers.ExecuteDelete(session, preparedQuery);
                }
                else if (preparedQuery.QueryType == QueryType.Kill)
                {
                    return _core.Sessions.QueryHandlers.ExecuteKillProcess(session, preparedQuery);
                }
                else if (preparedQuery.QueryType == QueryType.Set)
                {
                    return _core.Sessions.QueryHandlers.ExecuteSetVariable(session, preparedQuery);
                }
                else if (preparedQuery.QueryType == QueryType.Rebuild)
                {
                    if (preparedQuery.SubQueryType == SubQueryType.Index
                        || preparedQuery.SubQueryType == SubQueryType.UniqueKey)
                    {
                        return _core.Indexes.QueryHandlers.ExecuteRebuild(session, preparedQuery);
                    }
                    throw new NotImplementedException();
                }
                else if (preparedQuery.QueryType == QueryType.Create)
                {
                    if (preparedQuery.SubQueryType == SubQueryType.Index || preparedQuery.SubQueryType == SubQueryType.UniqueKey)
                    {
                        return _core.Indexes.QueryHandlers.ExecuteCreate(session, preparedQuery);
                    }
                    else if (preparedQuery.SubQueryType == SubQueryType.Procedure)
                    {
                        return _core.Procedures.QueryHandlers.ExecuteCreate(session, preparedQuery);
                    }
                    else if (preparedQuery.SubQueryType == SubQueryType.Schema)
                    {
                        return _core.Schemas.QueryHandlers.ExecuteCreate(session, preparedQuery);
                    }

                    throw new NotImplementedException();
                }
                else if (preparedQuery.QueryType == QueryType.Alter)
                {
                    if (preparedQuery.SubQueryType == SubQueryType.Schema)
                    {
                        return _core.Schemas.QueryHandlers.ExecuteAlter(session, preparedQuery);
                    }
                    else if (preparedQuery.SubQueryType == SubQueryType.Configuration)
                    {
                        return _core.Environment.QueryHandlers.ExecuteAlter(session, preparedQuery);
                    }
                    throw new NotImplementedException();
                }
                else if (preparedQuery.QueryType == QueryType.Drop)
                {
                    if (preparedQuery.SubQueryType == SubQueryType.Index
                        || preparedQuery.SubQueryType == SubQueryType.UniqueKey)
                    {
                        return _core.Indexes.QueryHandlers.ExecuteDrop(session, preparedQuery);
                    }
                    else if (preparedQuery.SubQueryType == SubQueryType.Schema)
                    {
                        return _core.Schemas.QueryHandlers.ExecuteDrop(session, preparedQuery);
                    }
                    throw new NotImplementedException();
                }
                else if (preparedQuery.QueryType == QueryType.Begin)
                {
                    if (preparedQuery.SubQueryType == SubQueryType.Transaction)
                    {
                        _core.Transactions.QueryHandlers.Begin(session);
                        return new KbActionResponse();
                    }
                    throw new NotImplementedException();
                }
                else if (preparedQuery.QueryType == QueryType.Rollback)
                {
                    if (preparedQuery.SubQueryType == SubQueryType.Transaction)
                    {
                        _core.Transactions.QueryHandlers.Rollback(session);
                        return new KbActionResponse();
                    }
                    throw new NotImplementedException();
                }
                else if (preparedQuery.QueryType == QueryType.Commit)
                {
                    if (preparedQuery.SubQueryType == SubQueryType.Transaction)
                    {
                        _core.Transactions.QueryHandlers.Commit(session);
                        return new KbActionResponse();
                    }
                    throw new NotImplementedException();
                }
                else
                {
                    throw new NotImplementedException();
                }
            }
            catch (Exception ex)
            {
                LogManager.Error($"Failed to execute non-query for process id {session.ProcessId}.", ex);
                throw;
            }
        }
    }
}
