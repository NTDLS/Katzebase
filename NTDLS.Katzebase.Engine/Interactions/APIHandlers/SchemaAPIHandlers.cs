using NTDLS.Katzebase.Api.Exceptions;
using NTDLS.Katzebase.Api.Payloads.RoundTrip;
using NTDLS.Katzebase.Engine.Interactions.Management;
using NTDLS.Katzebase.PersistentTypes.Schema;
using NTDLS.ReliableMessaging;
using System.Text;
using static NTDLS.Katzebase.Shared.EngineConstants;

namespace NTDLS.Katzebase.Engine.Interactions.APIHandlers
{
    /// <summary>
    /// Public class methods for handling API requests related to schema.
    /// </summary>
    public class SchemaAPIHandlers : IRmMessageHandler
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
                LogManager.Error($"Failed to instantiate schema API handlers.", ex);
                throw;
            }
        }

        public KbQuerySchemaListReply ListSchemas(RmContext context, KbQuerySchemaList param)
        {
            var session = _core.Sessions.GetSession(context.ConnectionId);
#if DEBUG
            Thread.CurrentThread.Name = $"KbAPI:{session.ProcessId}:{param.GetType().Name}";
            LogManager.Debug(Thread.CurrentThread.Name);
#endif
            try
            {
                using var transactionReference = _core.Transactions.APIAcquire(session);
                var physicalSchema = _core.Schemas.Acquire(transactionReference.Transaction, param.Schema, LockOperation.Read);

                var result = new KbQuerySchemaListReply();

                if (physicalSchema.DiskPath == null)
                {
                    throw new KbNullException($"Value should not be null [{nameof(physicalSchema.DiskPath)}].");
                }

                var schemaCatalog = _core.IO.GetJson<PhysicalSchemaCatalog>(
                    transactionReference.Transaction, physicalSchema.SchemaCatalogFilePath(), LockOperation.Read);

                foreach (var item in schemaCatalog.Collection)
                {
                    result.Collection.Add(item.ToClientPayload(physicalSchema.Id, param.Schema));
                }

                return transactionReference.CommitAndApplyMetricsThenReturnResults(result);
            }
            catch (Exception ex)
            {
                LogManager.Error($"Failed to get schema list for process {session.ProcessId}.", ex);
                throw;
            }
        }

        /// <summary>
        /// Creates a structure of schemas, denoted by colons.
        /// </summary>
        /// <param name="schemaPath"></param>
        public KbQuerySchemaCreateReply CreateSchema(RmContext context, KbQuerySchemaCreate param)
        {
            var session = _core.Sessions.GetSession(context.ConnectionId);
#if DEBUG
            Thread.CurrentThread.Name = $"KbAPI:{session.ProcessId}:{param.GetType().Name}";
            LogManager.Debug(Thread.CurrentThread.Name);
#endif
            try
            {
                using var transactionReference = _core.Transactions.APIAcquire(session);
                var segments = param.Schema.Split(':');
                var pathBuilder = new StringBuilder();

                foreach (string name in segments)
                {
                    pathBuilder.Append(name);
                    _core.Schemas.CreateSingleSchema(transactionReference.Transaction, pathBuilder.ToString(), param.PageSize);
                    pathBuilder.Append(':');
                }

                return transactionReference.CommitAndApplyMetricsThenReturnResults(new KbQuerySchemaCreateReply());
            }
            catch (Exception ex)
            {
                LogManager.Error($"Failed to create schema lineage for process {session.ProcessId}.", ex);
                throw;
            }
        }

        /// <summary>
        /// Returns true if the schema exists.
        /// </summary>
        /// <param name="schemaPath"></param>
        public KbQuerySchemaExistsReply DoesSchemaExist(RmContext context, KbQuerySchemaExists param)
        {
            var session = _core.Sessions.GetSession(context.ConnectionId);
#if DEBUG
            Thread.CurrentThread.Name = $"KbAPI:{session.ProcessId}:{param.GetType().Name}";
            LogManager.Debug(Thread.CurrentThread.Name);
#endif
            try
            {
                using var transactionReference = _core.Transactions.APIAcquire(session);
                var segments = param.Schema.Split(':');
                var pathBuilder = new StringBuilder();
                bool schemaExists = false;

                foreach (string name in segments)
                {
                    pathBuilder.Append(name);
                    var schema = _core.Schemas.AcquireVirtual(transactionReference.Transaction, pathBuilder.ToString(), LockOperation.Read, LockOperation.Stability);

                    schemaExists = schema != null && schema.Exists;
                    if (schemaExists == false)
                    {
                        break;
                    }

                    pathBuilder.Append(':');
                }

                return transactionReference.CommitAndApplyMetricsThenReturnResults(new KbQuerySchemaExistsReply(schemaExists));
            }
            catch (Exception ex)
            {
                LogManager.Error($"Failed to confirm schema for process {session.ProcessId}.", ex);
                throw;
            }
        }

        /// <summary>
        /// Drops a single schema or an entire schema path.
        /// </summary>
        /// <param name="schema"></param>
        public KbQuerySchemaDropReply DropSchema(RmContext context, KbQuerySchemaDrop param)
        {
            var session = _core.Sessions.GetSession(context.ConnectionId);
#if DEBUG
            Thread.CurrentThread.Name = $"KbAPI:{session.ProcessId}:{param.GetType().Name}";
            LogManager.Debug(Thread.CurrentThread.Name);
#endif
            try
            {
                using var transactionReference = _core.Transactions.APIAcquire(session);
                var segments = param.Schema.Split(':');
                var parentSchemaName = segments[^1];

                var physicalSchema = _core.Schemas.Acquire(transactionReference.Transaction, param.Schema, LockOperation.Write);
                var parentPhysicalSchema = _core.Schemas.AcquireParent(transactionReference.Transaction, physicalSchema, LockOperation.Write);

                if (parentPhysicalSchema.DiskPath == null || physicalSchema.DiskPath == null)
                    throw new KbNullException($"Value should not be null [{nameof(physicalSchema.DiskPath)}].");

                var parentSchemaCatalogFile = parentPhysicalSchema.SchemaCatalogFilePath();
                var parentCatalog = _core.IO.GetJson<PhysicalSchemaCatalog>(
                    transactionReference.Transaction, parentSchemaCatalogFile, LockOperation.Write);

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
                LogManager.Error($"Failed to drop schema for process {session.ProcessId}.", ex);
                throw;
            }
        }
    }
}
