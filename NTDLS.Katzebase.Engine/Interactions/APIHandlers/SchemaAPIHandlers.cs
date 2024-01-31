using NTDLS.Katzebase.Client.Exceptions;
using NTDLS.Katzebase.Client.Payloads.Queries;
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

        public KbQuerySchemaListReply ListSchemas(ulong processId, KbQuerySchemaList param)
        {
            try
            {
                using var transactionReference = _core.Transactions.Acquire(processId);
                var physicalSchema = _core.Schemas.Acquire(transactionReference.Transaction, param.Schema, LockOperation.Read);

                var result = new KbQuerySchemaListReply();

                if (physicalSchema.DiskPath == null)
                {
                    throw new KbNullException($"Value should not be null {nameof(physicalSchema.DiskPath)}.");
                }

                var schemaCatalog = _core.IO.GetJson<PhysicalSchemaCatalog>(transactionReference.Transaction, physicalSchema.SchemaCatalogFilePath(), LockOperation.Read);

                foreach (var item in schemaCatalog.Collection)
                {
                    result.Collection.Add(item.ToClientPayload());
                }

                return transactionReference.CommitAndApplyMetricsThenReturnResults(result);
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
        public KbQuerySchemaCreateReply CreateSchema(ulong processId, KbQuerySchemaCreate param)
        {
            try
            {
                using var transactionReference = _core.Transactions.Acquire(processId);
                var segments = param.Schema.Split(':');
                var pathBuilder = new StringBuilder();

                foreach (string name in segments)
                {
                    pathBuilder.Append(name);
                    _core.Schemas.CreateSingleSchema(transactionReference.Transaction, pathBuilder.ToString(), param.PageSize);
                    pathBuilder.Append(":");
                }

                return transactionReference.CommitAndApplyMetricsThenReturnResults(new KbQuerySchemaCreateReply());
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
        public KbQuerySchemaExistsReply DoesSchemaExist(ulong processId, KbQuerySchemaExists param)
        {
            try
            {
                using var transactionReference = _core.Transactions.Acquire(processId);
                var segments = param.Schema.Split(':');
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

                return transactionReference.CommitAndApplyMetricsThenReturnResults(new KbQuerySchemaExistsReply(schemaExists));
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
        public KbQuerySchemaDropReply DropSchema(ulong processId, KbQuerySchemaDrop param)
        {
            try
            {
                using var transactionReference = _core.Transactions.Acquire(processId);
                var segments = param.Schema.Split(':');
                var parentSchemaName = segments[segments.Count() - 1];

                var physicalSchema = _core.Schemas.Acquire(transactionReference.Transaction, param.Schema, LockOperation.Write);
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

                return transactionReference.CommitAndApplyMetricsThenReturnResults(new KbQuerySchemaDropReply());
            }
            catch (Exception ex)
            {
                _core.Log.Write($"Failed to drop schema for process {processId}.", ex);
                throw;
            }
        }
    }
}
