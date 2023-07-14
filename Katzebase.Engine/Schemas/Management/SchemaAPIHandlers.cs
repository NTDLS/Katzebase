using Katzebase.PublicLibrary.Exceptions;
using Katzebase.PublicLibrary.Payloads;
using System.Text;
using static Katzebase.Engine.Library.EngineConstants;

namespace Katzebase.Engine.Schemas.Management
{
    /// <summary>
    /// Public class methods for handling API requests related to schema.
    /// </summary>
    public class SchemaAPIHandlers
    {
        private readonly Core core;

        public SchemaAPIHandlers(Core core)
        {
            this.core = core;

            try
            {
            }
            catch (Exception ex)
            {
                core.Log.Write($"Failed to instanciate schema API handlers.", ex);
                throw;
            }
        }

        public KbActionResponseSchemaCollection ListSchemas(ulong processId, string schemaName)
        {
            try
            {
                using (var txRef = core.Transactions.Acquire(processId))
                {
                    var physicalSchema = core.Schemas.Acquire(txRef.Transaction, schemaName, LockOperation.Read);

                    var result = new KbActionResponseSchemaCollection();

                    if (physicalSchema.DiskPath == null)
                    {
                        throw new KbNullException($"Value should not be null {nameof(physicalSchema.DiskPath)}.");
                    }

                    var schemaCatalog = core.IO.GetJson<PhysicalSchemaCatalog>(txRef.Transaction, physicalSchema.SchemaCatalogFilePath(), LockOperation.Read);

                    foreach (var item in schemaCatalog.Collection)
                    {
                        result.Collection.Add(item.ToClientPayload());
                    }

                    txRef.Commit();
                    result.RowCount = 0;
                    result.Metrics = txRef.Transaction.PT?.ToCollection();
                    result.Success = true;
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
        public KbActionResponse CreateSchema(ulong processId, string schemaName)
        {
            try
            {
                using (var txRef = core.Transactions.Acquire(processId))
                {
                    var result = new KbActionResponse();
                    var segments = schemaName.Split(':');
                    var pathBuilder = new StringBuilder();

                    foreach (string name in segments)
                    {
                        pathBuilder.Append(name);
                        core.Schemas.CreateSingleSchema(txRef.Transaction, pathBuilder.ToString());
                        pathBuilder.Append(":");
                    }

                    txRef.Commit();
                    result.RowCount = 0;
                    result.Metrics = txRef.Transaction.PT?.ToCollection();
                    result.Success = true;
                    return result;
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
        public KbActionResponseBoolean DoesSchemaExist(ulong processId, string schemaName)
        {
            try
            {
                using (var txRef = core.Transactions.Acquire(processId))
                {
                    var result = new KbActionResponseBoolean();
                    var segments = schemaName.Split(':');
                    var pathBuilder = new StringBuilder();

                    foreach (string name in segments)
                    {
                        pathBuilder.Append(name);
                        var schema = core.Schemas.AcquireVirtual(txRef.Transaction, pathBuilder.ToString(), LockOperation.Read);

                        result.Value = schema != null && schema.Exists;
                        if (result.Value == false)
                        {
                            break;
                        }

                        pathBuilder.Append(":");
                    }

                    txRef.Commit();
                    result.RowCount = 0;
                    result.Metrics = txRef.Transaction.PT?.ToCollection();
                    result.Success = true;
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
        public KbActionResponse DropSchema(ulong processId, string schemaName)
        {
            try
            {
                using (var txRef = core.Transactions.Acquire(processId))
                {
                    var result = new KbActionResponseBoolean();

                    var segments = schemaName.Split(':');
                    var parentSchemaName = segments[segments.Count() - 1];

                    var physicalSchema = core.Schemas.Acquire(txRef.Transaction, schemaName, LockOperation.Write);
                    var parentPhysicalSchema = core.Schemas.AcquireParent(txRef.Transaction, physicalSchema, LockOperation.Write);

                    if (parentPhysicalSchema.DiskPath == null || physicalSchema.DiskPath == null)
                        throw new KbNullException($"Value should not be null {nameof(physicalSchema.DiskPath)}.");

                    var parentSchemaCatalogFile = parentPhysicalSchema.SchemaCatalogFilePath();
                    var parentCatalog = core.IO.GetJson<PhysicalSchemaCatalog>(txRef.Transaction, parentSchemaCatalogFile, LockOperation.Write);

                    var nsItem = parentCatalog.Collection.FirstOrDefault(o => o.Name == parentSchemaName);
                    if (nsItem != null)
                    {
                        parentCatalog.Collection.Remove(nsItem);

                        core.IO.DeletePath(txRef.Transaction, physicalSchema.DiskPath);

                        core.IO.PutJson(txRef.Transaction, parentSchemaCatalogFile, parentCatalog);
                    }

                    txRef.Commit();
                    result.RowCount = 0;
                    result.Metrics = txRef.Transaction.PT?.ToCollection();
                    return result;
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
