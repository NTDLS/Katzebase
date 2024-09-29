using Microsoft.Extensions.Caching.Memory;
using NTDLS.Katzebase.Client;
using NTDLS.Katzebase.Client.Exceptions;
using NTDLS.Katzebase.Client.Payloads;
using NTDLS.Katzebase.Client.Types;
using NTDLS.Katzebase.Engine.Functions.Aggregate;
using NTDLS.Katzebase.Engine.Functions.Scaler;
using NTDLS.Katzebase.Engine.Functions.System;
using NTDLS.Katzebase.Engine.Interactions.APIHandlers;
using NTDLS.Katzebase.Engine.Parsers;
using NTDLS.Katzebase.Engine.Parsers.Query.SupportingTypes;
using NTDLS.Katzebase.Engine.Sessions;
using NTDLS.ReliableMessaging.Internal;
using System.Reflection;
using System.Text;
using static NTDLS.Katzebase.Client.KbConstants;
using static NTDLS.Katzebase.Engine.Library.EngineConstants;

namespace NTDLS.Katzebase.Engine.Interactions.Management
{

    public static class QueryExtensions
    {
        private static readonly MemoryCache _cache = new(new MemoryCacheOptions());
        public static IEnumerable<T> MapTo<TData, T>(this KbQueryDocumentListResult<TData> result) where T : new() where TData : IStringable
        {
            var list = new List<T>();
            var properties = GetProperties(typeof(T));

            foreach (var row in result.Rows)
            {
                var obj = new T();
                for (int i = 0; i < result.Fields.Count; i++)
                {
                    if (properties.TryGetValue(result.Fields[i].Name, out var property) && i < row.Values.Count)
                    {
                        var value = row.Values[i];

                        if (value == null)
                        {
                            if (property.PropertyType.IsValueType && Nullable.GetUnderlyingType(property.PropertyType) == null)
                            {
                                continue; // Skip setting value if property is non-nullable value type
                            }
                            else
                            {
                                property.SetValue(obj, null);
                            }
                        }
                        else
                        {
                            // 處理自定義類型的轉換
                            object? convertedValue = null;
                            var targetType = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;

                            if (typeof(IStringable).IsAssignableFrom(targetType))
                            {
                                // 尝试通过构造函数实例化 IStringable 对象
                                //if (targetType.GetConstructor(new[] { typeof(string) }) != null)
                                //{
                                //    // 先创建 IStringable 实例
                                //    var instance = (IStringable)Activator.CreateInstance(targetType, value.ToString())!;

                                //    // 使用 ToT 方法进行进一步的类型转换
                                //    convertedValue = instance.ToT<T>();
                                //}
                                convertedValue = value;
                            }
                            else
                            {
                                // 使用 Convert.ChangeType 进行标准类型转换
                                //convertedValue = Convert.ChangeType(value, targetType);
                                convertedValue = value.ToT(targetType);
                            }

                            property.SetValue(obj, convertedValue);
                        }
                    }
                }
                list.Add(obj);
            }

            return list;
        }
        private static Dictionary<string, PropertyInfo> GetProperties(Type type)
        {
            if (!_cache.TryGetValue(type, out Dictionary<string, PropertyInfo>? properties))
            {
                properties = type.GetProperties().ToDictionary(p => p.Name, StringComparer.InvariantCultureIgnoreCase);

                var cacheEntryOptions = new MemoryCacheEntryOptions
                {
                    SlidingExpiration = TimeSpan.FromMinutes(5)
                };

                _cache.Set(type, properties, cacheEntryOptions);
            }

            return properties;
        }
    }
        /// <summary>
        /// Public core class methods for locking, reading, writing and managing tasks related to queries.
        /// </summary>
    public class QueryManager<TData> where TData : IStringable
    {
        private readonly EngineCore<TData> _core;
        public QueryAPIHandlers<TData> APIHandlers { get; private set; }

        /// <summary>
        /// Tokens that will be replaced by literal values by the tokenizer.
        /// </summary>
        internal KbInsensitiveDictionary<KbConstant> KbGlobalConstants { get; private set; } = new();

        internal QueryManager(EngineCore<TData> core)
        {
            _core = core;
            APIHandlers = new QueryAPIHandlers<TData>(core);

            //Define all query literal constants here, these will be filled in my the tokenizer. Do not use quotes for strings.
            KbGlobalConstants.Add("true", new("1", KbBasicDataType.Numeric));
            KbGlobalConstants.Add("false", new("0", KbBasicDataType.Numeric));
            KbGlobalConstants.Add("null", new(null, KbBasicDataType.Undefined));

            SystemFunctionCollection<TData>.Initialize();
            ScalerFunctionCollection<TData>.Initialize();
            AggregateFunctionCollection<TData>.Initialize();
        }

        #region Internal helpers.

        /// <summary>
        /// Executes a query and returns the mapped object. This function is designed to be used internally and expects that the "batch" only contains one query.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="session"></param>
        /// <param name="queryText"></param>
        /// <returns></returns>
        /// <exception cref="KbMultipleRecordSetsException"></exception>
        internal IEnumerable<T> ExecuteQuery<T>(SessionState session, string queryText, object? userParameters = null) where T : new()
        {
            var preparedQueries = StaticQueryParser<TData>.ParseBatch(_core, queryText, userParameters.ToUserParametersInsensitiveDictionary());
            if (preparedQueries.Count > 1)
            {
                throw new KbMultipleRecordSetsException("Prepare batch resulted in more than one query.");
            }
            var results = _core.Query.ExecuteQuery(session, preparedQueries[0]);
            if (preparedQueries.Count > 1)
            {
                throw new KbMultipleRecordSetsException();
            }
            return results.Collection[0].MapTo<TData, T>();
        }

        /// <summary>
        /// Executes a query. This function is designed to be used internally and expects that the "batch" only contains one query.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="session"></param>
        /// <param name="queryText"></param>
        /// <returns></returns>
        /// <exception cref="KbMultipleRecordSetsException"></exception>
        internal void ExecuteNonQuery(SessionState session, string queryText)
        {
            var preparedQueries = StaticQueryParser<TData>.ParseBatch(_core, queryText);
            if (preparedQueries.Count > 1)
            {
                throw new KbMultipleRecordSetsException("Prepare batch resulted in more than one query.");
            }
            _core.Query.ExecuteNonQuery(session, preparedQueries[0]);
        }

        #endregion

        internal KbQueryExplain ExplainPlan(SessionState session, PreparedQuery<TData> preparedQuery)
        {
            try
            {
                if (preparedQuery.QueryType == QueryType.Select
                    || preparedQuery.QueryType == QueryType.Delete
                    || preparedQuery.QueryType == QueryType.Update)
                {
                    return _core.Documents.QueryHandlers.ExecuteExplainPlan(session, preparedQuery);
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

        internal KbQueryExplain ExplainOperations(SessionState session, PreparedQuery<TData> preparedQuery)
        {
            try
            {
                if (preparedQuery.QueryType == QueryType.Select
                    || preparedQuery.QueryType == QueryType.Delete
                    || preparedQuery.QueryType == QueryType.Update)
                {
                    return _core.Documents.QueryHandlers.ExecuteExplainOperations(session, preparedQuery);
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

        internal KbQueryResultCollection<TData> ExecuteProcedure(SessionState session, KbProcedure procedure)
        {
            var statement = new StringBuilder($"EXEC {procedure.SchemaName}:{procedure.ProcedureName}");

            using var transactionReference = _core.Transactions.Acquire(session);

            var physicalSchema = _core.Schemas.Acquire(transactionReference.Transaction, procedure.SchemaName, LockOperation.Read);

            var physicalProcedure = _core.Procedures.Acquire(transactionReference.Transaction, physicalSchema, procedure.ProcedureName, LockOperation.Read)
                ?? throw new KbEngineException($"Procedure [{procedure.ProcedureName}] was not found in schema [{procedure.SchemaName}]");

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
                        throw new KbEngineException($"Parameter [{parameter.Name}] was not passed when calling procedure [{procedure.ProcedureName}] in schema [{procedure.SchemaName}]");
                    }
                }
                statement.Length--; //Remove the trailing ','.
                statement.Append(')');
            }

            var batch = StaticQueryParser<TData>.ParseBatch(_core, statement.ToString());
            if (batch.Count > 1)
            {
                throw new KbEngineException("Expected only one procedure call per batch.");
            }
            return _core.Procedures.QueryHandlers.ExecuteExec(session, batch.First());
        }

        internal KbQueryResultCollection<TData> ExecuteQuery(SessionState session, PreparedQuery<TData> preparedQuery)
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
                    return KbQueryDocumentListResult<TData>.FromActionResponse(ExecuteNonQuery(session, preparedQuery)).ToCollection();
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

        internal KbBaseActionResponse ExecuteNonQuery(SessionState session, PreparedQuery<TData> preparedQuery)
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
