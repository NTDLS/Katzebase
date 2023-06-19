using Katzebase.Engine.Atomicity;
using Katzebase.Engine.Query;
using Katzebase.PublicLibrary.Exceptions;
using Katzebase.PublicLibrary.Payloads;
using static Katzebase.Engine.KbLib.EngineConstants;

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
        }

        internal KbQueryResult ExecuteList(ulong processId, PreparedQuery preparedQuery)
        {
            try
            {
                var result = new KbQueryResult();

                using (var transaction = core.Transactions.Acquire(processId))
                {
                    if (preparedQuery.SubQueryType == SubQueryType.Schemas)
                    {
                        result = GetListByPreparedQuery(transaction, preparedQuery);
                    }
                    else
                    {
                        throw new KbParserException("Invalid list query subtype.");
                    }

                    transaction.Commit();

                    result.Metrics = transaction.PT?.ToCollection();
                }

                return result;
            }
            catch (Exception ex)
            {
                core.Log.Write($"Failed to ExecuteSelect for process {processId}.", ex);
                throw;
            }
        }

        private KbQueryResult GetListByPreparedQuery(Transaction transaction, PreparedQuery preparedQuery)
        {
            var result = new KbQueryResult();

            var schema = preparedQuery.Schemas.First();

            var physicalSchema = core.Schemas.Acquire(transaction, schema.Name, LockOperation.Read);

            //Lock the schema catalog:
            var filePath = Path.Combine(physicalSchema.DiskPath, SchemaCatalogFile);
            var schemaCatalog = core.IO.GetJson<PhysicalSchemaCatalog>(transaction, filePath, LockOperation.Read);

            result.Fields.Add(new KbQueryField("Name"));
            result.Fields.Add(new KbQueryField("Path"));

            foreach (var item in schemaCatalog.Collection)
            {
                if (preparedQuery.RowLimit > 0 && result.Rows.Count >= preparedQuery.RowLimit)
                {
                    break;
                }
                var resultRow = new KbQueryRow();

                resultRow.AddValue(item.Name);
                resultRow.AddValue($"{physicalSchema.VirtualPath}:{item.Name}");

                result.Rows.Add(resultRow);
            }

            return result;
        }
    }
}
