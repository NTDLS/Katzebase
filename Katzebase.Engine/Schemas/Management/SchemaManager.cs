using Katzebase.Engine.Atomicity;
using Katzebase.Engine.Documents;
using Katzebase.Engine.Indexes;
using Katzebase.Engine.Query;
using Katzebase.Engine.Trace;
using Katzebase.PublicLibrary;
using Katzebase.PublicLibrary.Exceptions;
using Katzebase.PublicLibrary.Payloads;
using System.Diagnostics;
using static Katzebase.Engine.Library.EngineConstants;
using static Katzebase.Engine.Schemas.PhysicalSchema;
using static Katzebase.Engine.Trace.PerformanceTrace;

namespace Katzebase.Engine.Schemas.Management
{
    /// <summary>
    /// Public core class methods for locking, reading, writing and managing tasks related to schemas.
    /// </summary>
    public class SchemaManager
    {
        private readonly Core core;
        private readonly string rootCatalogFile;
        private PhysicalSchema? rootPhysicalSchema = null;
        internal SchemaQueryHandlers QueryHandlers { get; set; }
        public SchemaAPIHandlers APIHandlers { get; set; }

        public PhysicalSchema RootPhysicalSchema
        {
            get
            {
                try
                {
                    rootPhysicalSchema ??= new PhysicalSchema()
                    {
                        Id = RootSchemaGUID,
                        DiskPath = core.Settings.DataRootPath,
                        VirtualPath = string.Empty,
                        Name = string.Empty,
                    };
                    return rootPhysicalSchema;
                }
                catch (Exception ex)
                {
                    core.Log.Write($"Failed to obtain root schema.", ex);
                    throw;
                }
            }
        }

        public SchemaManager(Core core)
        {
            this.core = core;

            try
            {
                QueryHandlers = new SchemaQueryHandlers(core);
                APIHandlers = new SchemaAPIHandlers(core);

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
            catch (Exception ex)
            {
                core.Log.Write("Failed to instanciate SchemaManager.", ex);
                throw;
            }
        }

        internal void CreateSingleSchema(Transaction transaction, string schemaName)
        {
            try
            {
                var physicalSchema = AcquireVirtual(transaction, schemaName, LockOperation.Write);
                if (physicalSchema.Exists)
                {
                    return; //The schema already exists, not much else to do.
                }

                var parentPhysicalSchema = AcquireParent(transaction, physicalSchema, LockOperation.Write);

                core.IO.CreateDirectory(transaction, physicalSchema.DiskPath);
                core.IO.PutJson(transaction, physicalSchema.SchemaCatalogFilePath(), new PhysicalSchemaCatalog());
                core.IO.PutJson(transaction, physicalSchema.DocumentPageCatalogFilePath(), new PhysicalDocumentPageCatalog());
                core.IO.PutJson(transaction, physicalSchema.IndexCatalogFilePath(), new PhysicalIndexCatalog());

                var parentCatalog = core.IO.GetJson<PhysicalSchemaCatalog>(transaction, parentPhysicalSchema.SchemaCatalogFilePath(), LockOperation.Write);

                if (parentCatalog.ContainsName(schemaName) == false)
                {
                    parentCatalog.Add(new PhysicalSchema
                    {
                        Id = Guid.NewGuid(),
                        Name = physicalSchema.Name
                    });

                    core.IO.PutJson(transaction, parentPhysicalSchema.SchemaCatalogFilePath(), parentCatalog);
                }
            }
            catch (Exception ex)
            {
                core.Log.Write($"Failed to create single schema manager for process id {transaction.ProcessId}.", ex);
                throw;
            }
        }

        internal void Drop(Transaction transaction, string schemaName)
        {
            try
            {
                var physicalSchema = AcquireVirtual(transaction, schemaName, LockOperation.Write);
                if (physicalSchema.Exists == false)
                {
                    return; //The schema does not exists, not much else to do.
                }

                var parentPhysicalSchema = AcquireParent(transaction, physicalSchema, LockOperation.Write);
                core.IO.DeletePath(transaction, physicalSchema.DiskPath);

                var parentCatalog = core.IO.GetJson<PhysicalSchemaCatalog>(transaction, parentPhysicalSchema.SchemaCatalogFilePath(), LockOperation.Write);

                parentCatalog.Collection.RemoveAll(o => o.Name.ToLower() == schemaName.ToLower());

                core.IO.PutJson(transaction, parentPhysicalSchema.SchemaCatalogFilePath(), parentCatalog);
            }
            catch (Exception ex)
            {
                core.Log.Write($"Failed to create single schema manager for process id {transaction.ProcessId}.", ex);
                throw;
            }
        }

        internal List<PhysicalSchema> AcquireChildren(Transaction transaction, PhysicalSchema physicalSchema, LockOperation intendedOperation)
        {
            try
            {
                var schemas = new List<PhysicalSchema>();


                if (core.IO.FileExists(transaction, physicalSchema.SchemaCatalogFilePath(), intendedOperation))
                {
                    var schemaCatalog = core.IO.GetJson<PhysicalSchemaCatalog>(transaction, physicalSchema.SchemaCatalogFilePath(), intendedOperation);

                    foreach (var catalogItem in schemaCatalog.Collection)
                    {
                        schemas.Add(new PhysicalSchema()
                        {
                            DiskPath = physicalSchema.DiskPath + "\\" + catalogItem.Name,
                            Id = catalogItem.Id,
                            Name = catalogItem.Name,
                            VirtualPath = physicalSchema.VirtualPath + ":" + catalogItem.Name
                        });
                    }
                }

                return schemas;
            }
            catch (Exception ex)
            {
                core.Log.Write($"Failed to acquire schema children for process id {transaction.ProcessId}.", ex);
                throw;
            }
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
                core.Log.Write($"Failed to acquire parent schema for process id {transaction.ProcessId}.", ex);
                throw;
            }
        }

        /// <summary>
        /// Opens a schema for a desired access. Takes a virtual schema path (schema:schema2:scheams3) and converts to to a physical location
        /// </summary>
        internal PhysicalSchema Acquire(Transaction transaction, string schemaName, LockOperation intendedOperation)
        {
            PerformanceTraceDurationTracker? ptLockSchema = null;

            try
            {
                ptLockSchema = transaction.PT?.CreateDurationTracker<PhysicalSchema>(PerformanceTraceCumulativeMetricType.Lock);
                schemaName = schemaName.Trim(new char[] { ':' }).Trim();

                if (schemaName == string.Empty)
                {
                    return RootPhysicalSchema;
                }
                else
                {
                    var segments = schemaName.Split(':');
                    var parentSchemaame = segments[segments.Count() - 1];

                    var schemaDiskPath = Path.Combine(core.Settings.DataRootPath, string.Join("\\", segments));
                    var parentSchemaDiskPath = Directory.GetParent(schemaDiskPath)?.FullName;
                    KbUtility.EnsureNotNull(parentSchemaDiskPath);

                    var parentCatalogDiskPath = Path.Combine(parentSchemaDiskPath, SchemaCatalogFile);

                    if (core.IO.FileExists(transaction, parentCatalogDiskPath, intendedOperation) == false)
                    {
                        throw new KbObjectNotFoundException($"The schema [{schemaName}] does not exist.");
                    }

                    var parentCatalog = core.IO.GetJson<PhysicalSchemaCatalog>(transaction,
                        Path.Combine(parentSchemaDiskPath, SchemaCatalogFile), intendedOperation);

                    var physicalSchema = parentCatalog.GetByName(parentSchemaame);
                    if (physicalSchema != null)
                    {
                        physicalSchema.Name = parentSchemaame;
                        physicalSchema.DiskPath = schemaDiskPath;
                        physicalSchema.VirtualPath = schemaName;
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
                core.Log.Write($"Failed to acquire schema for process id {transaction.ProcessId}.", ex);
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
        internal VirtualSchema AcquireVirtual(Transaction transaction, string schemaName, LockOperation intendedOperation)
        {
            PerformanceTraceDurationTracker? ptLockSchema = null;

            try
            {
                ptLockSchema = transaction.PT?.CreateDurationTracker<PhysicalSchema>(PerformanceTraceCumulativeMetricType.Lock);
                schemaName = schemaName.Trim(new char[] { ':' }).Trim();

                if (schemaName == string.Empty)
                {
                    return RootPhysicalSchema.ToVirtual();
                }
                else
                {
                    var segments = schemaName.Split(':');
                    var parentSchemaName = segments[segments.Count() - 1];

                    var schemaDiskPath = Path.Combine(core.Settings.DataRootPath, string.Join("\\", segments));
                    var parentSchemaDiskPath = Directory.GetParent(schemaDiskPath)?.FullName;
                    KbUtility.EnsureNotNull(parentSchemaDiskPath);

                    var parentCatalogDiskPath = Path.Combine(parentSchemaDiskPath, SchemaCatalogFile);

                    if (core.IO.FileExists(transaction, parentCatalogDiskPath, intendedOperation) == false)
                    {
                        throw new KbObjectNotFoundException($"The schema [{schemaName}] does not exist.");
                    }

                    var parentCatalog = core.IO.GetJson<PhysicalSchemaCatalog>(transaction,
                        Path.Combine(parentSchemaDiskPath, SchemaCatalogFile), intendedOperation);

                    var virtualSchema = parentCatalog.GetByName(parentSchemaName)?.ToVirtual();
                    if (virtualSchema != null)
                    {
                        virtualSchema.Name = parentSchemaName;
                        virtualSchema.DiskPath = schemaDiskPath;
                        virtualSchema.VirtualPath = schemaName;
                        virtualSchema.Exists = true;
                    }
                    else
                    {
                        virtualSchema = new VirtualSchema()
                        {
                            Name = parentSchemaName,
                            DiskPath = core.Settings.DataRootPath + "\\" + schemaName.Replace(':', '\\'),
                            VirtualPath = schemaName,
                            Exists = false
                        };
                    }

                    transaction.LockDirectory(intendedOperation, virtualSchema.DiskPath);

                    return virtualSchema;
                }
            }
            catch (Exception ex)
            {
                core.Log.Write($"Failed to acquire virtual schema for process id {transaction.ProcessId}.", ex);
                throw;
            }
            finally
            {
                ptLockSchema?.StopAndAccumulate();
            }
        }

        internal PhysicalSchemaCatalog AcquireCatalog(Transaction transaction, string schemaName, LockOperation intendedOperation)
        {
            var physicalSchema = core.Schemas.Acquire(transaction, schemaName, intendedOperation);
            return core.IO.GetJson<PhysicalSchemaCatalog>(transaction, physicalSchema.DocumentPageCatalogFilePath(), intendedOperation);
        }

        internal List<Tuple<string, string>> GetListByPreparedQuery(Transaction transaction, string schemaName, int rowLimit)
        {
            try
            {
                var physicalSchema = core.Schemas.Acquire(transaction, schemaName,  LockOperation.Read);
                var schemaCatalog = core.IO.GetJson<PhysicalSchemaCatalog>(transaction, physicalSchema.DocumentPageCatalogFilePath(), LockOperation.Read);

                var result = new List<Tuple<string, string>>();

                foreach (var item in schemaCatalog.Collection)
                {
                    if (rowLimit > 0 && result.Count >= rowLimit)
                    {
                        break;
                    }

                    result.Add(new Tuple<string, string>(item.Name, $"{physicalSchema.VirtualPath}:{item.Name}"));
                }

                return result;
            }
            catch (Exception ex)
            {
                core.Log.Write($"Failed to get schema list for process id {transaction.ProcessId}.", ex);
                throw;
            }
        }

    }
}
