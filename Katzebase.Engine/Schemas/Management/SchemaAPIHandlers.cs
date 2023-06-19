using Katzebase.PublicLibrary.Exceptions;
using Katzebase.PublicLibrary.Payloads;
using System.Text;
using static Katzebase.Engine.KbLib.EngineConstants;

namespace Katzebase.Engine.Schemas.Management
{
    public class SchemaAPIHandlers
    {
        private readonly Core core;

        public SchemaAPIHandlers(Core core)
        {
            this.core = core;
        }

        public KbActionResponseSchemaCollection ListSchemas(ulong processId, string schemaName)
        {
            try
            {
                using (var transaction = core.Transactions.Begin(processId))
                {
                    var physicalSchema = core.Schemas.Acquire(transaction, schemaName, LockOperation.Read);

                    var result = new KbActionResponseSchemaCollection();

                    if (physicalSchema.DiskPath == null)
                    {
                        throw new KbNullException($"Value should not be null {nameof(physicalSchema.DiskPath)}.");
                    }

                    var filePath = Path.Combine(physicalSchema.DiskPath, SchemaCatalogFile);
                    var schemaCatalog = core.IO.GetJson<PhysicalSchemaCatalog>(transaction, filePath, LockOperation.Read);

                    foreach (var item in schemaCatalog.Collection)
                    {
                        result.Add(item.ToClientPayload());
                    }

                    transaction.Commit();

                    result.Metrics = transaction.PT?.ToCollection();

                    return result;
                }
            }
            catch (Exception ex)
            {
                core.Log.Write($"Failed to get schema list for process {processId}.", ex);
                throw;
            }
        }


        /// <summary>
        /// Creates a structure of schemas, denotaed by colons.
        /// </summary>
        /// <param name="schemaPath"></param>
        public void CreateSchema(ulong processId, string schemaName)
        {
            try
            {
                var segments = schemaName.Split(':');

                StringBuilder pathBuilder = new StringBuilder();

                foreach (string name in segments)
                {
                    pathBuilder.Append(name);
                    core.Schemas.CreateSingleSchema(processId, pathBuilder.ToString());
                    pathBuilder.Append(":");
                }
            }
            catch (Exception ex)
            {
                core.Log.Write($"Failed to create schema lineage for process {processId}.", ex);
                throw;
            }
        }

        /// <summary>
        /// Returns true if the schema exists.
        /// </summary>
        /// <param name="schemaPath"></param>
        public bool DoesSchemaExist(ulong processId, string schemaName)
        {
            try
            {
                bool result = false;

                using (var transaction = core.Transactions.Begin(processId))
                {
                    var segments = schemaName.Split(':');

                    StringBuilder pathBuilder = new StringBuilder();

                    foreach (string name in segments)
                    {
                        pathBuilder.Append(name);
                        var schema = core.Schemas.AcquireVirtual(transaction, pathBuilder.ToString(), LockOperation.Read);

                        result = schema != null && schema.Exists;

                        if (result == false)
                        {
                            break;
                        }

                        pathBuilder.Append(":");
                    }

                    transaction.Commit();
                    return result;
                }
            }
            catch (Exception ex)
            {
                core.Log.Write($"Failed to confirm schema for process {processId}.", ex);
                throw;
            }
        }

        /// <summary>
        /// Drops a single schema or an entire schema path.
        /// </summary>
        /// <param name="schema"></param>
        public void DropSchema(ulong processId, string schemaName)
        {
            try
            {
                using (var transaction = core.Transactions.Begin(processId))
                {
                    var segments = schemaName.Split(':');
                    string parentSchemaName = segments[segments.Count() - 1];

                    var physicalSchema = core.Schemas.Acquire(transaction, schemaName, LockOperation.Write);
                    var parentPhysicalSchema = core.Schemas.AcquireParent(transaction, physicalSchema, LockOperation.Write);

                    if (parentPhysicalSchema.DiskPath == null || physicalSchema.DiskPath == null)
                        throw new KbNullException($"Value should not be null {nameof(physicalSchema.DiskPath)}.");

                    string parentSchemaCatalogFile = Path.Combine(parentPhysicalSchema.DiskPath, SchemaCatalogFile);
                    var parentCatalog = core.IO.GetJson<PhysicalSchemaCatalog>(transaction, parentSchemaCatalogFile, LockOperation.Write);

                    var nsItem = parentCatalog.Collection.FirstOrDefault(o => o.Name == parentSchemaName);
                    if (nsItem != null)
                    {
                        parentCatalog.Collection.Remove(nsItem);

                        core.IO.DeletePath(transaction, physicalSchema.DiskPath);

                        core.IO.PutJson(transaction, parentSchemaCatalogFile, parentCatalog);
                    }

                    transaction.Commit();
                }
            }
            catch (Exception ex)
            {
                core.Log.Write($"Failed to drop schema for process {processId}.", ex);
                throw;
            }
        }

    }
}
