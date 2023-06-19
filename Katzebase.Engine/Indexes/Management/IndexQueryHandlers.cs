using Katzebase.Engine.Query;
using Katzebase.PublicLibrary.Payloads;

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

                using (var transaction = core.Transactions.Acquire(processId))
                {
                    var result = new KbActionResponse();
                    string schemaName = preparedQuery.Schemas.First().Name;

                    core.Indexes.DropIndex(transaction, schemaName, preparedQuery.Attribute<string>(PreparedQuery.QueryAttribute.IndexName));

                    transaction.Commit();
                    result.Metrics = transaction.PT?.ToCollection();
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

        internal KbActionResponse ExecuteRebuild(ulong processId, PreparedQuery preparedQuery)
        {
            try
            {
                var session = core.Sessions.ByProcessId(processId);

                using (var transaction = core.Transactions.Acquire(processId))
                {
                    var result = new KbActionResponse();
                    string schemaName = preparedQuery.Schemas.First().Name;

                    core.Indexes.RebuildIndex(transaction, schemaName, preparedQuery.Attribute<string>(PreparedQuery.QueryAttribute.IndexName));

                    transaction.Commit();
                    result.Metrics = transaction.PT?.ToCollection();
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
                using (var transaction = core.Transactions.Acquire(processId))
                {
                    var result = new KbActionResponse();

                    string schemaName = preparedQuery.Schemas.First().Name;

                    var index = new KbIndex
                    {
                        Name = preparedQuery.Attribute<string>(PreparedQuery.QueryAttribute.IndexName),
                        IsUnique = preparedQuery.Attribute<bool>(PreparedQuery.QueryAttribute.IsUnique)
                    };

                    foreach (var field in preparedQuery.SelectFields)
                    {
                        index.Attributes.Add(new KbIndexAttribute() { Field = field.Field });
                    }

                    core.Indexes.CreateIndex(transaction, schemaName, index, out Guid indexId);

                    transaction.Commit();
                    result.Metrics = transaction.PT?.ToCollection();
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
