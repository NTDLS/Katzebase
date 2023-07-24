using Katzebase.Engine.Functions.Procedures.Persistent;
using Katzebase.Engine.Query;
using Katzebase.PublicLibrary.Exceptions;
using Katzebase.PublicLibrary.Payloads;
using static Katzebase.Engine.Library.EngineConstants;
using static Katzebase.PublicLibrary.KbConstants;

namespace Katzebase.Engine.Indexes.Management
{
    /// <summary>
    /// Internal class methods for handling query requests related to indexes.
    /// </summary>
    internal class IndexQueryHandlers
    {
        private readonly Core core;

        public IndexQueryHandlers(Core core)
        {
            this.core = core;

            try
            {
            }
            catch (Exception ex)
            {
                core.Log.Write($"Failed to instanciate index query handler.", ex);
                throw;
            }
        }

        internal KbActionResponse ExecuteDrop(ulong processId, PreparedQuery preparedQuery)
        {
            try
            {
                var session = core.Sessions.ByProcessId(processId);

                using (var txRef = core.Transactions.Acquire(processId))
                {
                    var result = new KbActionResponse();
                    string schemaName = preparedQuery.Schemas.First().Name;

                    if (preparedQuery.SubQueryType == SubQueryType.Schema)
                    {
                        core.Schemas.Drop(txRef.Transaction, schemaName);
                    }
                    else if (preparedQuery.SubQueryType == SubQueryType.Index)
                    {
                        core.Indexes.DropIndex(txRef.Transaction, schemaName, preparedQuery.Attribute<string>(PreparedQuery.QueryAttribute.IndexName));
                    }
                    else
                    {
                        throw new KbNotImplementedException();
                    }

                    txRef.Commit();
                    result.Metrics = txRef.Transaction.PT?.ToCollection();
                    result.Success = true;
                    return result;
                }
            }
            catch (Exception ex)
            {
                core.Log.Write($"Failed to execute index drop for process id {processId}.", ex);
                throw;
            }
        }

        internal KbActionResponse ExecuteAnalyze(ulong processId, PreparedQuery preparedQuery)
        {
            try
            {
                var session = core.Sessions.ByProcessId(processId);

                using (var txRef = core.Transactions.Acquire(processId))
                {
                    var result = new KbActionResponse();
                    string schemaName = preparedQuery.Schemas.First().Name;

                    var analysis = core.Indexes.AnalyzeIndex(txRef.Transaction, schemaName, preparedQuery.Attribute<string>(PreparedQuery.QueryAttribute.IndexName));

                    result.Messages.Add(new KbQueryResultMessage(analysis, KbMessageType.Verbose));

                    txRef.Commit();
                    result.Metrics = txRef.Transaction.PT?.ToCollection();
                    result.Success = true;
                    return result;
                }
            }
            catch (Exception ex)
            {
                core.Log.Write($"Failed to execute index rebuild for process id {processId}.", ex);
                throw;
            }
        }

        internal KbActionResponse ExecuteRebuild(ulong processId, PreparedQuery preparedQuery)
        {
            try
            {
                var session = core.Sessions.ByProcessId(processId);

                using (var txRef = core.Transactions.Acquire(processId))
                {
                    var result = new KbActionResponse();
                    string schemaName = preparedQuery.Schemas.First().Name;

                    core.Indexes.RebuildIndex(txRef.Transaction, schemaName, preparedQuery.Attribute<string>(PreparedQuery.QueryAttribute.IndexName));

                    txRef.Commit();
                    result.Metrics = txRef.Transaction.PT?.ToCollection();
                    result.Success = true;
                    return result;
                }
            }
            catch (Exception ex)
            {
                core.Log.Write($"Failed to execute index rebuild for process id {processId}.", ex);
                throw;
            }
        }

        internal KbActionResponse ExecuteCreate(ulong processId, PreparedQuery preparedQuery)
        {
            try
            {
                using (var txRef = core.Transactions.Acquire(processId))
                {
                    var result = new KbActionResponse();

                    if (preparedQuery.SubQueryType == SubQueryType.Schema)
                    {
                        string schemaName = preparedQuery.Schemas.Single().Name;
                        core.Schemas.CreateSingleSchema(txRef.Transaction, schemaName);
                    }
                    else if (preparedQuery.SubQueryType == SubQueryType.Procedure)
                    {
                        var objectName = preparedQuery.Attribute<string>(PreparedQuery.QueryAttribute.ObjectName);
                        var objectSchema = preparedQuery.Attribute<string>(PreparedQuery.QueryAttribute.Schema);
                        var parameters = preparedQuery.Attribute<List<PhysicalProcedureParameter>>(PreparedQuery.QueryAttribute.Parameters);
                        var Batches = preparedQuery.Attribute<List<string>>(PreparedQuery.QueryAttribute.Batches);

                        core.Procedures.CreateCustomProcedure(txRef.Transaction, objectSchema, objectName, parameters, Batches);
                    }
                    else if (preparedQuery.SubQueryType == SubQueryType.Index || preparedQuery.SubQueryType == SubQueryType.UniqueKey)
                    {
                        var index = new KbIndex
                        {
                            Name = preparedQuery.Attribute<string>(PreparedQuery.QueryAttribute.IndexName),
                            IsUnique = preparedQuery.Attribute<bool>(PreparedQuery.QueryAttribute.IsUnique)
                        };

                        foreach (var field in preparedQuery.CreateFields)
                        {
                            index.Attributes.Add(new KbIndexAttribute() { Field = field.Field });
                        }

                        string schemaName = preparedQuery.Schemas.Single().Name;
                        core.Indexes.CreateIndex(txRef.Transaction, schemaName, index, out Guid indexId);
                    }
                    else
                    {
                        throw new KbNotImplementedException();
                    }

                    txRef.Commit();
                    result.Metrics = txRef.Transaction.PT?.ToCollection();
                    result.Success = true;
                    return result;
                }
            }
            catch (Exception ex)
            {
                core.Log.Write($"Failed to execute index create for process id {processId}.", ex);
                throw;
            }
        }
    }
}
