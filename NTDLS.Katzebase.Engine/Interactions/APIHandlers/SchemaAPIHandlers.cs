using NTDLS.Katzebase.Api.Exceptions;
using NTDLS.Katzebase.Api.Payloads;
using NTDLS.Katzebase.Engine.Interactions.Management;
using NTDLS.Katzebase.Engine.QueryProcessing.Searchers;
using NTDLS.Katzebase.PersistentTypes.Schema;
using NTDLS.ReliableMessaging;
using System.Diagnostics;
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

                #region Security policy enforcment.

                _core.Policy.EnforceSchemaPolicy(transactionReference.Transaction, param.Schema, SecurityPolicyPermission.Read);

                #endregion

                var physicalSchema = _core.Schemas.Acquire(transactionReference.Transaction, param.Schema, LockOperation.Read);

                var apiResults = new KbQuerySchemaListReply();

                if (physicalSchema.DiskPath == null)
                {
                    throw new KbNullException($"Value should not be null [{nameof(physicalSchema.DiskPath)}].");
                }

                var schemaCatalog = _core.IO.GetJson<PhysicalSchemaCatalog>(
                    transactionReference.Transaction, physicalSchema.SchemaCatalogFilePath(), LockOperation.Read);

                foreach (var item in schemaCatalog.Collection)
                {
                    apiResults.Collection.Add(item.ToClientPayload(physicalSchema.Id, param.Schema));
                }

                return transactionReference.CommitAndApplyMetricsThenReturnResults(apiResults);
            }
            catch (Exception ex)
            {
                LogManager.Error($"{new StackFrame(1).GetMethod()} failed for process: [{session.ProcessId}].", ex);
                throw;
            }
        }

        public KbQuerySchemaFieldSampleReply SchemaFieldSample(RmContext context, KbQuerySchemaFieldSample param)
        {
            var session = _core.Sessions.GetSession(context.ConnectionId);
#if DEBUG
            Thread.CurrentThread.Name = $"KbAPI:{session.ProcessId}:{param.GetType().Name}";
            LogManager.Debug(Thread.CurrentThread.Name);
#endif
            try
            {
                using var transactionReference = _core.Transactions.APIAcquire(session);

                #region Security policy enforcment.

                _core.Policy.EnforceSchemaPolicy(transactionReference.Transaction, param.Schema, SecurityPolicyPermission.Read);

                #endregion

                var physicalSchema = _core.Schemas.Acquire(transactionReference.Transaction, param.Schema, LockOperation.Read);

                var apiResults = new KbQuerySchemaFieldSampleReply();

                if (physicalSchema.DiskPath == null)
                {
                    throw new KbNullException($"Value should not be null [{nameof(physicalSchema.DiskPath)}].");
                }

                var result = StaticSearcherProcessor.SampleSchemaDocuments(
                    _core, transactionReference.Transaction, param.Schema, 1);

                foreach (var field in result.Fields)
                {
                    apiResults.Collection.Add(new Api.Models.KbResponseFieldSampleItem(field.Name));
                }

                return transactionReference.CommitAndApplyMetricsThenReturnResults(apiResults);
            }
            catch (Exception ex)
            {
                LogManager.Error($"{new StackFrame(1).GetMethod()} failed for process: [{session.ProcessId}].", ex);
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

                #region Security policy enforcment.

                _core.Policy.EnforceSchemaPolicy(transactionReference.Transaction, param.Schema, SecurityPolicyPermission.Manage);

                #endregion

                var segments = param.Schema.Split(':');
                var pathBuilder = new StringBuilder();

                foreach (string name in segments)
                {
                    pathBuilder.Append(name);
                    _core.Schemas.CreateSingleSchema(transactionReference.Transaction, pathBuilder.ToString(), param.PageSize);
                    pathBuilder.Append(':');
                }

                var apiResults = new KbQuerySchemaCreateReply();

                return transactionReference.CommitAndApplyMetricsThenReturnResults(apiResults);
            }
            catch (Exception ex)
            {
                LogManager.Error($"{new StackFrame(1).GetMethod()} failed for process: [{session.ProcessId}].", ex);
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

                #region Security policy enforcment.

                _core.Policy.EnforceSchemaPolicy(transactionReference.Transaction, param.Schema, SecurityPolicyPermission.Read);

                #endregion

                var segments = param.Schema.Split(':');
                var pathBuilder = new StringBuilder();
                bool doesSchemaExists = false;

                foreach (string name in segments)
                {
                    pathBuilder.Append(name);
                    var schema = _core.Schemas.AcquireVirtual(transactionReference.Transaction, pathBuilder.ToString(), LockOperation.Read, LockOperation.Stability);

                    doesSchemaExists = schema != null && schema.Exists;
                    if (doesSchemaExists == false)
                    {
                        break;
                    }

                    pathBuilder.Append(':');
                }

                var apiResults = new KbQuerySchemaExistsReply(doesSchemaExists);

                return transactionReference.CommitAndApplyMetricsThenReturnResults(apiResults);
            }
            catch (Exception ex)
            {
                LogManager.Error($"{new StackFrame(1).GetMethod()} failed for process: [{session.ProcessId}].", ex);
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

                #region Security policy enforcment.

                _core.Policy.EnforceSchemaPolicy(transactionReference.Transaction, param.Schema, SecurityPolicyPermission.Manage);

                #endregion

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

                var apiResults = new KbQuerySchemaDropReply();

                return transactionReference.CommitAndApplyMetricsThenReturnResults(apiResults);
            }
            catch (Exception ex)
            {
                LogManager.Error($"{new StackFrame(1).GetMethod()} failed for process: [{session.ProcessId}].", ex);
                throw;
            }
        }
    }
}
