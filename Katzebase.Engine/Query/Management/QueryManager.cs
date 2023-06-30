using Katzebase.Engine.Query.Function.Scaler;
using Katzebase.PublicLibrary.Exceptions;
using Katzebase.PublicLibrary.Payloads;
using static Katzebase.Engine.Library.EngineConstants;

namespace Katzebase.Engine.Query.Management
{
    /// <summary>
    /// Public core class methods for locking, reading, writing and managing tasks related to queries.
    /// </summary>
    public class QueryManager
    {
        private readonly Core core;
        public QueryAPIHandlers APIHandlers { get; set; }

        public QueryManager(Core core)
        {
            this.core = core;
            APIHandlers = new QueryAPIHandlers(core);

            QueryScalerFunctionCollection.Initialize();
        }

        internal KbQueryResult ExplainQuery(ulong processId, PreparedQuery preparedQuery)
        {
            try
            {
                if (preparedQuery.QueryType == QueryType.Select
                    || preparedQuery.QueryType == QueryType.Delete
                    || preparedQuery.QueryType == QueryType.Update)
                {
                    return core.Documents.QueryHandlers.ExecuteExplain(processId, preparedQuery);
                }
                else if (preparedQuery.QueryType == QueryType.Set)
                {
                    return new KbQueryResult();
                }
                else
                {
                    throw new NotImplementedException();
                }
            }
            catch (Exception ex)
            {
                core.Log.Write($"Failed to explain query for process id {processId}.", ex);
                throw;
            }
        }

        internal KbQueryResult ExecuteQuery(ulong processId, PreparedQuery preparedQuery)
        {
            try
            {
                if (preparedQuery.QueryType == QueryType.Select)
                {
                    return core.Documents.QueryHandlers.ExecuteSelect(processId, preparedQuery);
                }
                else if (preparedQuery.QueryType == QueryType.Insert)
                {
                    return core.Documents.QueryHandlers.ExecuteInsert(processId, preparedQuery);
                }
                else if (preparedQuery.QueryType == QueryType.Sample)
                {
                    return core.Documents.QueryHandlers.ExecuteSample(processId, preparedQuery);
                }
                else if (preparedQuery.QueryType == QueryType.Exec)
                {
                    return core.Functions.QueryHandlers.ExecuteExec(processId, preparedQuery);
                }
                else if (preparedQuery.QueryType == QueryType.List)
                {
                    if (preparedQuery.SubQueryType == SubQueryType.Documents)
                    {
                        return core.Documents.QueryHandlers.ExecuteList(processId, preparedQuery);
                    }
                    else if (preparedQuery.SubQueryType == SubQueryType.Schemas)
                    {
                        return core.Schemas.QueryHandlers.ExecuteList(processId, preparedQuery);
                    }
                    else
                    {
                        throw new KbParserException("Invalid list query subtype.");
                    }
                }
                else if (preparedQuery.QueryType == QueryType.Delete
                    || preparedQuery.QueryType == QueryType.Rebuild
                    || preparedQuery.QueryType == QueryType.Create
                    || preparedQuery.QueryType == QueryType.Set
                    || preparedQuery.QueryType == QueryType.Kill
                    || preparedQuery.QueryType == QueryType.Drop
                    || preparedQuery.QueryType == QueryType.Begin
                    || preparedQuery.QueryType == QueryType.Commit
                    || preparedQuery.QueryType == QueryType.Rollback)
                {
                    //Reroute to non-query as appropriate:
                    return KbQueryResult.FromActionResponse(ExecuteNonQuery(processId, preparedQuery));
                }
                else
                {
                    throw new NotImplementedException();
                }
            }
            catch (Exception ex)
            {
                core.Log.Write($"Failed to execute query for process id {processId}.", ex);
                throw;
            }
        }

        internal KbActionResponse ExecuteNonQuery(ulong processId, PreparedQuery preparedQuery)
        {
            try
            {
                if (preparedQuery.QueryType == QueryType.Delete)
                {
                    return core.Documents.QueryHandlers.ExecuteDelete(processId, preparedQuery);
                }
                else if (preparedQuery.QueryType == QueryType.Kill)
                {
                    return core.Sessions.QueryHandlers.ExecuteKillProcess(processId, preparedQuery);
                }
                else if (preparedQuery.QueryType == QueryType.Set)
                {
                    return core.Sessions.QueryHandlers.ExecuteSetVariable(processId, preparedQuery);
                }
                else if (preparedQuery.QueryType == QueryType.Rebuild)
                {
                    if (preparedQuery.SubQueryType == SubQueryType.Index
                        || preparedQuery.SubQueryType == SubQueryType.UniqueKey)
                    {
                        return core.Indexes.QueryHandlers.ExecuteRebuild(processId, preparedQuery);
                    }
                    throw new NotImplementedException();
                }
                else if (preparedQuery.QueryType == QueryType.Create)
                {
                    if (preparedQuery.SubQueryType == SubQueryType.Index
                        || preparedQuery.SubQueryType == SubQueryType.UniqueKey
                        || preparedQuery.SubQueryType == SubQueryType.Schema)
                    {
                        return core.Indexes.QueryHandlers.ExecuteCreate(processId, preparedQuery);
                    }
                    throw new NotImplementedException();
                }
                else if (preparedQuery.QueryType == QueryType.Drop)
                {
                    if (preparedQuery.SubQueryType == SubQueryType.Index
                        || preparedQuery.SubQueryType == SubQueryType.UniqueKey
                        || preparedQuery.SubQueryType == SubQueryType.Schema)
                    {
                        return core.Indexes.QueryHandlers.ExecuteDrop(processId, preparedQuery);
                    }
                    throw new NotImplementedException();
                }
                else if (preparedQuery.QueryType == QueryType.Begin)
                {
                    if (preparedQuery.SubQueryType == SubQueryType.Transaction)
                    {
                        core.Transactions.QueryHandlers.Begin(processId);
                        return new KbActionResponse { Success = true };
                    }
                    throw new NotImplementedException();
                }
                else if (preparedQuery.QueryType == QueryType.Rollback)
                {
                    if (preparedQuery.SubQueryType == SubQueryType.Transaction)
                    {
                        core.Transactions.QueryHandlers.Rollback(processId);
                        return new KbActionResponse { Success = true };
                    }
                    throw new NotImplementedException();
                }
                else if (preparedQuery.QueryType == QueryType.Commit)
                {
                    if (preparedQuery.SubQueryType == SubQueryType.Transaction)
                    {
                        core.Transactions.QueryHandlers.Commit(processId);
                        return new KbActionResponse { Success = true };
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
                core.Log.Write($"Failed to execute non-query for process id {processId}.", ex);
                throw;
            }
        }
    }
}
