using Katzebase.Engine.Query;
using Katzebase.PublicLibrary.Exceptions;
using Katzebase.PublicLibrary.Payloads;
using static Katzebase.Engine.Library.EngineConstants;

namespace Katzebase.Engine.Schemas.Management
{
    /// <summary>
    /// Internal class methods for handling query requests related to schemas.
    /// </summary>
    internal class SchemaQueryHandlers
    {
        private readonly Core core;

        public SchemaQueryHandlers(Core core)
        {
            this.core = core;

            try
            {
            }
            catch (Exception ex)
            {
                core.Log.Write($"Failed to instanciate schema query handler.", ex);
                throw;
            }

        }

        internal KbQueryResult ExecuteList(ulong processId, PreparedQuery preparedQuery)
        {
            try
            {
                using (var txRef = core.Transactions.Acquire(processId))
                {
                    var result = new KbQueryResult();

                    if (preparedQuery.SubQueryType == SubQueryType.Schemas)
                    {
                        var schemaList = core.Schemas.GetListByPreparedQuery(txRef.Transaction, preparedQuery.Schemas.Single().Name, preparedQuery.RowLimit);

                        result.Fields.Add(new KbQueryField("Name"));
                        result.Fields.Add(new KbQueryField("Path"));

                        result.Rows.AddRange(schemaList.Select(o => new KbQueryRow(new List<string?> { o.Item1, o.Item2 })));
                    }
                    else
                    {
                        throw new KbEngineException("Invalid list query subtype.");
                    }

                    txRef.Commit();
                    result.Metrics = txRef.Transaction.PT?.ToCollection();
                    result.Messages = txRef.Transaction.Messages;
                    result.Warnings = txRef.Transaction.Warnings;
                    return result;
                }
            }
            catch (Exception ex)
            {
                core.Log.Write($"Failed to execute schema list for process id {processId}.", ex);
                throw;
            }
        }
    }
}
