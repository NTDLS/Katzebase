using Katzebase.Engine.Documents;
using Katzebase.Engine.Indexes;
using Katzebase.Engine.Query;
using Katzebase.Engine.Trace;
using Katzebase.Engine.Transactions;
using Katzebase.PublicLibrary;
using Katzebase.PublicLibrary.Exceptions;
using Katzebase.PublicLibrary.Payloads;
using System.Text;
using static Katzebase.Engine.KbLib.EngineConstants;
using static Katzebase.Engine.Schemas.PhysicalSchema;
using static Katzebase.Engine.Trace.PerformanceTrace;

namespace Katzebase.Engine.Schemas
{
    /// <summary>
    /// This is the class that all API controllers should interface with for schema access.
    /// </summary>
    public class SchemaManager
    {
        private Core core;
        private string rootCatalogFile;
        private PhysicalSchema? rootPhysicalSchema = null;

        public PhysicalSchema RootPhysicalSchema
        {
            get
            {
                rootPhysicalSchema ??= new PhysicalSchema()
                {
                    Id = RootSchemaGUID,
                    DiskPath = core.Settings.DataRootPath,
                    VirtualPath = string.Empty,
                    //Exists = true,
                    Name = string.Empty,
                };
                return rootPhysicalSchema;
            }
        }

        public SchemaManager(Core core)
        {
            this.core = core;

            rootCatalogFile = Path.Combine(core.Settings.DataRootPath, SchemaCatalogFile);

            //If the catalog doesnt exist, create a new empty one.
            if (File.Exists(rootCatalogFile) == false)
            {
                Directory.CreateDirectory(core.Settings.DataRootPath);

                core.IO.PutJsonNonTracked(Path.Combine(core.Settings.DataRootPath, SchemaCatalogFile), new PhysicalSchemaCatalog());
                core.IO.PutJsonNonTracked(Path.Combine(core.Settings.DataRootPath, DocumentPageCatalogFile), new PhysicalDocumentPageCatalog());
                core.IO.PutJsonNonTracked(Path.Combine(core.Settings.DataRootPath, IndexCatalogFile), new PhysicalIndexCatalog());
            }
        }

        #region Query Handlers.

        internal KbQueryResult ExecuteList(ulong processId, PreparedQuery preparedQuery)
        {
            try
            {
                var result = new KbQueryResult();

                using (var txRef = core.Transactions.Begin(processId))
                {
                    if (preparedQuery.SubQueryType == SubQueryType.Schemas)
                    {
                        result = GetListByPreparedQuery(txRef.Transaction, preparedQuery);
                    }
                    else
                    {
                        throw new KbParserException("Invalid list query subtype.");
                    }

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

        private KbQueryResult GetListByPreparedQuery(Transaction transaction, PreparedQuery preparedQuery)
        {
            var result = new KbQueryResult();

            var schema = preparedQuery.Schemas.First();

            //Lock the schema:
            var ptLockSchema = transaction.PT?.CreateDurationTracker<PhysicalSchema>(PerformanceTraceCumulativeMetricType.Lock);
            var physicalSchema = core.Schemas.Acquire(transaction, schema.Name, LockOperation.Read);
            ptLockSchema?.StopAndAccumulate();

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

        #endregion

        #region API handlers.

        public KbActionResponseSchemaCollection GetList(ulong processId, string schema)
        {
            try
            {
                using (var txRef = core.Transactions.Begin(processId))
                {
                    PhysicalSchema physicalSchema = Acquire(txRef.Transaction, schema, LockOperation.Read);

                    var result = new KbActionResponseSchemaCollection();

                    if (physicalSchema.DiskPath == null)
                    {
                        throw new KbNullException($"Value should not be null {nameof(physicalSchema.DiskPath)}.");
                    }

                    var filePath = Path.Combine(physicalSchema.DiskPath, SchemaCatalogFile);
                    var schemaCatalog = core.IO.GetJson<PhysicalSchemaCatalog>(txRef.Transaction, filePath, LockOperation.Read);

                    foreach (var item in schemaCatalog.Collection)
                    {
                        result.Add(ToPayload(item));
                    }

                    txRef.Commit();

                    result.Metrics = txRef.Transaction.PT?.ToCollection();

                    return result;
                }
            }
            catch (Exception ex)
            {
                core.Log.Write($"Failed to get schema list for process {processId}.", ex);
                throw;
            }
        }

        private void CreateSingle(ulong processId, string schema)
        {
            try
            {
                using (var txRef = core.Transactions.Begin(processId))
                {
                    Guid newSchemaId = Guid.NewGuid();

                    var physicalSchema = AcquireVirtual(txRef.Transaction, schema, LockOperation.Write);
                    if (physicalSchema.Exists)
                    {
                        txRef.Commit();
                        //The schema already exists.
                        return;
                    }

                    var parentPhysicalSchema = AcquireParent(txRef.Transaction, physicalSchema, LockOperation.Write);

                    if (parentPhysicalSchema.DiskPath == null || physicalSchema.DiskPath == null)
                    {
                        throw new KbNullException($"Value should not be null {nameof(physicalSchema.DiskPath)}.");
                    }

                    string parentSchemaCatalogFile = Path.Combine(parentPhysicalSchema.DiskPath, SchemaCatalogFile);
                    PhysicalSchemaCatalog? parentCatalog = core.IO.GetJson<PhysicalSchemaCatalog>(txRef.Transaction, parentSchemaCatalogFile, LockOperation.Write);

                    string filePath = string.Empty;

                    core.IO.CreateDirectory(txRef.Transaction, physicalSchema.DiskPath);

                    //Create default schema catalog file.
                    filePath = Path.Combine(physicalSchema.DiskPath, SchemaCatalogFile);
                    if (core.IO.FileExists(txRef.Transaction, filePath, LockOperation.Write) == false)
                    {
                        core.IO.PutJson(txRef.Transaction, filePath, new PhysicalSchemaCatalog());
                    }

                    //Create default document page catalog file.
                    filePath = Path.Combine(physicalSchema.DiskPath, DocumentPageCatalogFile);
                    if (core.IO.FileExists(txRef.Transaction, filePath, LockOperation.Write) == false)
                    {
                        core.IO.PutJson(txRef.Transaction, filePath, new PhysicalDocumentPageCatalog());
                    }

                    //Create default index catalog file.
                    filePath = Path.Combine(physicalSchema.DiskPath, IndexCatalogFile);
                    if (core.IO.FileExists(txRef.Transaction, filePath, LockOperation.Write) == false)
                    {
                        core.IO.PutJson(txRef.Transaction, filePath, new PhysicalIndexCatalog());
                    }

                    if (parentCatalog.ContainsName(schema) == false)
                    {
                        parentCatalog.Add(new PhysicalSchema
                        {
                            Id = newSchemaId,
                            Name = physicalSchema.Name
                        });

                        core.IO.PutJson(txRef.Transaction, parentSchemaCatalogFile, parentCatalog);
                    }

                    txRef.Commit();
                }
            }
            catch (Exception ex)
            {
                core.Log.Write($"Failed to create single schema for process {processId}.", ex);
                throw;
            }
        }

        /// <summary>
        /// Creates a structure of schemas, denotaed by colons.
        /// </summary>
        /// <param name="schemaPath"></param>
        public void Create(ulong processId, string schemaPath)
        {
            try
            {
                var segments = schemaPath.Split(':');

                StringBuilder pathBuilder = new StringBuilder();

                foreach (string name in segments)
                {
                    pathBuilder.Append(name);
                    CreateSingle(processId, pathBuilder.ToString());
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
        public bool Exists(ulong processId, string schemaPath)
        {
            try
            {
                bool result = false;

                using (var txRef = core.Transactions.Begin(processId))
                {
                    var segments = schemaPath.Split(':');

                    StringBuilder pathBuilder = new StringBuilder();

                    foreach (string name in segments)
                    {
                        pathBuilder.Append(name);
                        var schema = AcquireVirtual(txRef.Transaction, pathBuilder.ToString(), LockOperation.Read);

                        result = (schema != null && schema.Exists);

                        if (result == false)
                        {
                            break;
                        }

                        pathBuilder.Append(":");
                    }

                    txRef.Commit();
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
        public void Drop(ulong processId, string schema)
        {
            try
            {
                using (var txRef = core.Transactions.Begin(processId))
                {
                    var segments = schema.Split(':');
                    string schemaName = segments[segments.Count() - 1];

                    var physicalSchema = Acquire(txRef.Transaction, schema, LockOperation.Write);
                    var parentPhysicalSchema = AcquireParent(txRef.Transaction, physicalSchema, LockOperation.Write);

                    if (parentPhysicalSchema.DiskPath == null || physicalSchema.DiskPath == null)
                        throw new KbNullException($"Value should not be null {nameof(physicalSchema.DiskPath)}.");

                    string parentSchemaCatalogFile = Path.Combine(parentPhysicalSchema.DiskPath, SchemaCatalogFile);
                    var parentCatalog = core.IO.GetJson<PhysicalSchemaCatalog>(txRef.Transaction, parentSchemaCatalogFile, LockOperation.Write);

                    var nsItem = parentCatalog.Collection.FirstOrDefault(o => o.Name == schemaName);
                    if (nsItem != null)
                    {
                        parentCatalog.Collection.Remove(nsItem);

                        core.IO.DeletePath(txRef.Transaction, physicalSchema.DiskPath);

                        core.IO.PutJson(txRef.Transaction, parentSchemaCatalogFile, parentCatalog);
                    }

                    txRef.Commit();
                }
            }
            catch (Exception ex)
            {
                core.Log.Write($"Failed to drop schema for process {processId}.", ex);
                throw;
            }
        }

        #endregion

        #region Core methods.

        internal List<PhysicalSchema> GetChildrenMeta(Transaction transaction, PhysicalSchema node, LockOperation intendedOperation)
        {
            List<PhysicalSchema> metaList = new List<PhysicalSchema>();

            if (node.DiskPath == null)
            {
                throw new KbNullException($"Value should not be null {nameof(node.DiskPath)}.");
            }

            string schemaCatalogDiskPath = Path.Combine(node.DiskPath, SchemaCatalogFile);

            if (core.IO.FileExists(transaction, schemaCatalogDiskPath, intendedOperation))
            {
                var schemaCatalog = core.IO.GetJson<PhysicalSchemaCatalog>(transaction, schemaCatalogDiskPath, intendedOperation);

                foreach (var catalogItem in schemaCatalog.Collection)
                {
                    metaList.Add(new PhysicalSchema()
                    {
                        DiskPath = node.DiskPath + "\\" + catalogItem.Name,
                        Id = catalogItem.Id,
                        Name = catalogItem.Name,
                        VirtualPath = node.VirtualPath + ":" + catalogItem.Name
                    });
                }
            }

            return metaList;
        }

        internal PhysicalSchema AcquireParent(Transaction transaction, PhysicalSchema child, LockOperation intendedOperation)
        {
            try
            {
                if (child == RootPhysicalSchema)
                {
                    throw new KbGenericException("The root schema does not have a parent.");
                }

                if (child.VirtualPath == null)
                {
                    throw new KbNullException($"Value should not be null {nameof(child.VirtualPath)}.");
                }

                var segments = child.VirtualPath.Split(':').ToList();
                segments.RemoveAt(segments.Count - 1);
                string parentNs = string.Join(":", segments);
                return Acquire(transaction, parentNs, intendedOperation);
            }
            catch (Exception ex)
            {
                core.Log.Write("Failed to get parent schema.", ex);
                throw;
            }
        }

        /// <summary>
        /// Opens a schema for a desired access. Takes a virtual schema path (schema:schema2:scheams3) and converts to to a physical location
        /// </summary>
        internal PhysicalSchema Acquire(Transaction transaction, string schemaPath, LockOperation intendedOperation)
        {
            PerformanceTraceDurationTracker? ptLockSchema = null;

            try
            {
                ptLockSchema = transaction.PT?.CreateDurationTracker<PhysicalSchema>(PerformanceTraceCumulativeMetricType.Lock);
                schemaPath = schemaPath.Trim(new char[] { ':' }).Trim();

                if (schemaPath == string.Empty)
                {
                    return RootPhysicalSchema;
                }
                else
                {
                    var segments = schemaPath.Split(':');
                    var schemaName = segments[segments.Count() - 1];

                    var schemaDiskPath = Path.Combine(core.Settings.DataRootPath, string.Join("\\", segments));
                    var parentSchemaDiskPath = Directory.GetParent(schemaDiskPath)?.FullName;
                    Utility.EnsureNotNull(parentSchemaDiskPath);

                    var parentCatalogDiskPath = Path.Combine(parentSchemaDiskPath, SchemaCatalogFile);

                    if (core.IO.FileExists(transaction, parentCatalogDiskPath, intendedOperation) == false)
                    {
                        throw new KbObjectNotFoundException($"The schema [{schemaPath}] does not exist.");
                    }

                    var parentCatalog = core.IO.GetJson<PhysicalSchemaCatalog>(transaction,
                        Path.Combine(parentSchemaDiskPath, SchemaCatalogFile), intendedOperation);

                    var physicalSchema = parentCatalog.GetByName(schemaName);
                    if (physicalSchema != null)
                    {
                        physicalSchema.Name = schemaName;
                        physicalSchema.DiskPath = schemaDiskPath;
                        physicalSchema.VirtualPath = schemaPath;
                    }
                    else
                    {
                        throw new KbObjectNotFoundException(schemaName);
                    }

                    transaction.LockDirectory(intendedOperation, physicalSchema.DiskPath);

                    return physicalSchema;
                }
            }
            catch (Exception ex)
            {
                core.Log.Write("Failed to translate virtual path to schema.", ex);
                throw;
            }
            finally
            {
                ptLockSchema?.StopAndAccumulate();
            }
        }

        /// <summary>
        /// Opens a schema for a desired access even if it does not exist. Takes a virtual schema path (schema:schema2:scheams3) and converts to to a physical location
        /// </summary>
        internal VirtualSchema AcquireVirtual(Transaction transaction, string schemaPath, LockOperation intendedOperation)
        {
            PerformanceTraceDurationTracker? ptLockSchema = null;

            try
            {
                ptLockSchema = transaction.PT?.CreateDurationTracker<PhysicalSchema>(PerformanceTraceCumulativeMetricType.Lock);
                schemaPath = schemaPath.Trim(new char[] { ':' }).Trim();

                if (schemaPath == string.Empty)
                {
                    return RootPhysicalSchema.ToVirtual();
                }
                else
                {
                    var segments = schemaPath.Split(':');
                    var schemaName = segments[segments.Count() - 1];

                    var schemaDiskPath = Path.Combine(core.Settings.DataRootPath, string.Join("\\", segments));
                    var parentSchemaDiskPath = Directory.GetParent(schemaDiskPath)?.FullName;
                    Utility.EnsureNotNull(parentSchemaDiskPath);

                    var parentCatalogDiskPath = Path.Combine(parentSchemaDiskPath, SchemaCatalogFile);

                    if (core.IO.FileExists(transaction, parentCatalogDiskPath, intendedOperation) == false)
                    {
                        throw new KbObjectNotFoundException($"The schema [{schemaPath}] does not exist.");
                    }

                    var parentCatalog = core.IO.GetJson<PhysicalSchemaCatalog>(transaction,
                        Path.Combine(parentSchemaDiskPath, SchemaCatalogFile), intendedOperation);

                    var virtualSchema = parentCatalog.GetByName(schemaName)?.ToVirtual();
                    if (virtualSchema != null)
                    {
                        virtualSchema.Name = schemaName;
                        virtualSchema.DiskPath = schemaDiskPath;
                        virtualSchema.VirtualPath = schemaPath;
                        virtualSchema.Exists = true;
                    }
                    else
                    {
                        virtualSchema = new VirtualSchema()
                        {
                            Name = schemaName,
                            DiskPath = core.Settings.DataRootPath + "\\" + schemaPath.Replace(':', '\\'),
                            VirtualPath = schemaPath,
                            Exists = false
                        };
                    }

                    transaction.LockDirectory(intendedOperation, virtualSchema.DiskPath);

                    return virtualSchema;
                }
            }
            catch (Exception ex)
            {
                core.Log.Write("Failed to translate virtual path to schema.", ex);
                throw;
            }
            finally
            {
                ptLockSchema?.StopAndAccumulate();
            }
        }

        #endregion
    }
}
