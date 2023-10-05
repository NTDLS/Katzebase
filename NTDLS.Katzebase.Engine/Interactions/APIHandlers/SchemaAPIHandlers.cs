using NTDLS.Katzebase.Client.Exceptions;
using NTDLS.Katzebase.Client.Payloads;
using NTDLS.Katzebase.Engine.Schemas;
using System.Text;
using static NTDLS.Katzebase.Engine.Library.EngineConstants;

namespace NTDLS.Katzebase.Engine.Interactions.APIHandlers
{
    /// <summary>
    /// Public class methods for handling API requests related to schema.
    /// </summary>
    public class SchemaAPIHandlers
    {
        private readonly EngineCore _core;

        public SchemaAPIHandlers(EngineCore core)
        {
            _core = core;

            try
            {
            }
            catch (Exception ex)
            {
                core.Log.Write($"Failed to instantiate schema API handlers.", ex);
                throw;
            }
        }

        public KbActionResponseSchemaCollection ListSchemas(ulong processId, string schemaName)
        {
            try
            {
                using var transactionReference = _core.Transactions.Acquire(processId);
                var physicalSchema = _core.Schemas.Acquire(transactionReference.Transaction, schemaName, LockOperation.Read);

                var result = new KbActionResponseSchemaCollection();

                if (physicalSchema.DiskPath == null)
                {
                    throw new KbNullException($"Value should not be null {nameof(physicalSchema.DiskPath)}.");
                }

                var schemaCatalog = _core.IO.GetJson<PhysicalSchemaCatalog>(transactionReference.Transaction, physicalSchema.SchemaCatalogFilePath(), LockOperation.Read);

                foreach (var item in schemaCatalog.Collection)
                {
                    result.Collection.Add(item.ToClientPayload());
                }

                return transactionReference.CommitAndApplyMetricsThenReturnResults(result, 0);
            }
            catch (Exception ex)
            {
                _core.Log.Write($"Failed to get schema list for process {processId}.", ex);
                throw;
            }
        }


        /// <summary>
        /// Creates a structure of schemas, denotaed by colons.
        /// </summary>
        /// <param name="schemaPath"></param>
        public KbActionResponse CreateSchema(ulong processId, string schemaName, uint pageSize = 0)
        {
            try
            {
                using var transactionReference = _core.Transactions.Acquire(processId);
                var segments = schemaName.Split(':');
                var pathBuilder = new StringBuilder();

                foreach (string name in segments)
                {
                    pathBuilder.Append(name);
                    _core.Schemas.CreateSingleSchema(transactionReference.Transaction, pathBuilder.ToString(), pageSize);
                    pathBuilder.Append(":");
                }

                return transactionReference.CommitAndApplyMetricsThenReturnResults();
            }
            catch (Exception ex)
            {
                _core.Log.Write($"Failed to create schema lineage for process {processId}.", ex);
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
                using var transactionReference = _core.Transactions.Acquire(processId);
                var segments = schemaName.Split(':');
                var pathBuilder = new StringBuilder();
                bool schemaExists = false;

                foreach (string name in segments)
                {
                    pathBuilder.Append(name);
                    var schema = _core.Schemas.AcquireVirtual(transactionReference.Transaction, pathBuilder.ToString(), LockOperation.Read);

                    schemaExists = schema != null && schema.Exists;
                    if (schemaExists == false)
                    {
                        break;
                    }

                    pathBuilder.Append(":");
                }

                return transactionReference.CommitAndApplyMetricsThenReturnResults(new KbActionResponseBoolean(schemaExists), 0);
            }
            catch (Exception ex)
            {
                _core.Log.Write($"Failed to confirm schema for process {processId}.", ex);
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
                using var transactionReference = _core.Transactions.Acquire(processId);
                var segments = schemaName.Split(':');
                var parentSchemaName = segments[segments.Count() - 1];

                var physicalSchema = _core.Schemas.Acquire(transactionReference.Transaction, schemaName, LockOperation.Write);
                var parentPhysicalSchema = _core.Schemas.AcquireParent(transactionReference.Transaction, physicalSchema, LockOperation.Write);

                if (parentPhysicalSchema.DiskPath == null || physicalSchema.DiskPath == null)
                    throw new KbNullException($"Value should not be null {nameof(physicalSchema.DiskPath)}.");

                var parentSchemaCatalogFile = parentPhysicalSchema.SchemaCatalogFilePath();
                var parentCatalog = _core.IO.GetJson<PhysicalSchemaCatalog>(transactionReference.Transaction, parentSchemaCatalogFile, LockOperation.Write);

                var nsItem = parentCatalog.Collection.FirstOrDefault(o => o.Name == parentSchemaName);
                if (nsItem != null)
                {
                    parentCatalog.Collection.Remove(nsItem);

                    _core.IO.DeletePath(transactionReference.Transaction, physicalSchema.DiskPath);

                    _core.IO.PutJson(transactionReference.Transaction, parentSchemaCatalogFile, parentCatalog);
                }

                return transactionReference.CommitAndApplyMetricsThenReturnResults();
            }
            catch (Exception ex)
            {
                _core.Log.Write($"Failed to drop schema for process {processId}.", ex);
                throw;
            }
        }
    }
}
