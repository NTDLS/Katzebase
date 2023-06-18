using Katzebase.Engine.Query;
using Katzebase.PublicLibrary.Payloads;

namespace Katzebase.Engine.Indexes.Management
{
    internal class IndexQueryHandlers
    {
        private readonly Core core;

        public IndexQueryHandlers(Core core)
        {
            this.core = core;
        }

        internal KbActionResponse ExecuteDrop(ulong processId, PreparedQuery preparedQuery)
        {
            try
            {
                var result = new KbActionResponse();
                var session = core.Sessions.ByProcessId(processId);

                using (var txRef = core.Transactions.Begin(processId))
                {
                    string schemaName = preparedQuery.Schemas.First().Name;

                    core.Indexes.DropIndex(txRef.Transaction, schemaName, preparedQuery.Attribute<string>(PreparedQuery.QueryAttribute.IndexName));

                    txRef.Commit();
                    result.Metrics = txRef.Transaction.PT?.ToCollection();
                }

                return result;
            }
            catch (Exception ex)
            {
                core.Log.Write($"Failed to ExecuteSelect for process {processId}.", ex);
                throw;
            }
        }

        internal KbActionResponse ExecuteRebuild(ulong processId, PreparedQuery preparedQuery)
        {
            try
            {
                var result = new KbActionResponse();
                var session = core.Sessions.ByProcessId(processId);

                using (var txRef = core.Transactions.Begin(processId))
                {
                    string schemaName = preparedQuery.Schemas.First().Name;

                    core.Indexes.RebuildIndex(txRef.Transaction, schemaName, preparedQuery.Attribute<string>(PreparedQuery.QueryAttribute.IndexName));

                    txRef.Commit();
                    result.Metrics = txRef.Transaction.PT?.ToCollection();
                }

                return result;
            }
            catch (Exception ex)
            {
                core.Log.Write($"Failed to ExecuteSelect for process {processId}.", ex);
                throw;
            }
        }

        internal KbActionResponse ExecuteCreate(ulong processId, PreparedQuery preparedQuery)
        {
            try
            {
                var result = new KbActionResponse();

                using (var txRef = core.Transactions.Begin(processId))
                {
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

                    core.Indexes.CreateIndex(txRef.Transaction, schemaName, index, out Guid indexId);

                    txRef.Commit();
                    result.Metrics = txRef.Transaction.PT?.ToCollection();
                }

                return result;
            }
            catch (Exception ex)
            {
                core.Log.Write($"Failed to ExecuteSelect for process {processId}.", ex);
                throw;
            }
        }
    }
}
