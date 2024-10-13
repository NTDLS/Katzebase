using NTDLS.Helpers;
using NTDLS.Katzebase.Api.Exceptions;
using NTDLS.Katzebase.Api.Payloads.Response;
using NTDLS.Katzebase.Engine.Atomicity;
using NTDLS.Katzebase.Engine.Instrumentation;
using NTDLS.Katzebase.Engine.Interactions.APIHandlers;
using NTDLS.Katzebase.Engine.Interactions.QueryProcessors;
using NTDLS.Katzebase.Engine.Scripts;
using NTDLS.Katzebase.PersistentTypes.Document;
using NTDLS.Katzebase.PersistentTypes.Index;
using NTDLS.Katzebase.PersistentTypes.Policy;
using NTDLS.Katzebase.PersistentTypes.Schema;
using System.Diagnostics;
using System.Text;
using static NTDLS.Katzebase.Engine.Instrumentation.InstrumentationTracker;
using static NTDLS.Katzebase.PersistentTypes.Schema.PhysicalSchema;
using static NTDLS.Katzebase.Shared.EngineConstants;

namespace NTDLS.Katzebase.Engine.Interactions.Management
{
    /// <summary>
    /// Public core class methods for locking, reading, writing and managing tasks related to schemas.
    /// </summary>
    public class SchemaManager
    {
        private readonly EngineCore _core;
        private readonly string _rootCatalogFile;
        private PhysicalSchema? _rootPhysicalSchema = null;

        internal SchemaQueryHandlers QueryHandlers { get; private set; }

        public SchemaAPIHandlers APIHandlers { get; private set; }

        internal PhysicalSchema RootPhysicalSchema
        {
            get
            {
                try
                {
                    _rootPhysicalSchema ??= new PhysicalSchema()
                    {
                        Id = RootSchemaGUID,
                        DiskPath = _core.Settings.DataRootPath,
                        VirtualPath = string.Empty,
                        Name = string.Empty,
                        IsTemporary = false,
                        PageSize = _core.Settings.DefaultDocumentPageSize,
                    };
                    return _rootPhysicalSchema;
                }
                catch (Exception ex)
                {
                    LogManager.Error($"Failed to obtain root schema.", ex);
                    throw;
                }
            }
        }

        internal SchemaManager(EngineCore core)
        {
            _core = core;

            try
            {
                QueryHandlers = new SchemaQueryHandlers(core);
                APIHandlers = new SchemaAPIHandlers(core);

                _rootCatalogFile = Path.Combine(core.Settings.DataRootPath, SchemaCatalogFile);
            }
            catch (Exception ex)
            {
                LogManager.Error("Failed to instantiate SchemaManager.", ex);
                throw;
            }
        }

        /// <summary>
        /// To be executed after all other engine components have been initialized, for instance:
        /// we can't insert rows from the SchemaManager constructor because the document manager is not yet ready.
        /// </summary>
        public void PostInitialization()
        {
            //If the root schema doesn't exist, create a new empty one.
            if (File.Exists(_rootCatalogFile) == false)
            {
                LogManager.Information("Initializing root schema.");
                Directory.CreateDirectory(_core.Settings.DataRootPath);
                IOManager.PutJsonNonTracked(Path.Combine(_core.Settings.DataRootPath, SchemaCatalogFile), new PhysicalSchemaCatalog());
                IOManager.PutPBufNonTracked(Path.Combine(_core.Settings.DataRootPath, DocumentPageCatalogFile), new PhysicalDocumentPageCatalog());
                IOManager.PutJsonNonTracked(Path.Combine(_core.Settings.DataRootPath, IndexCatalogFile), new PhysicalIndexCatalog());
                IOManager.PutJsonNonTracked(Path.Combine(_core.Settings.DataRootPath, PolicyCatalogFile), new PhysicalPolicyCatalog());
            }

            using var system = _core.Sessions.CreateEphemeralSystemSession();
            var masterSchema = _core.Schemas.AcquireVirtual(system.Transaction, "Master", LockOperation.Write, LockOperation.Write);
            if (masterSchema.Exists == false)
            {
                LogManager.Information("Initializing master schema.");
                _core.Query.ExecuteNonQuery(system.Session, EmbeddedScripts.Load("CreateMasterSchema.kbs"));
            }
            system.Commit();

            LogManager.Information("Initializing ephemeral schemas.");
            RecycleEphemeralSchemas();
        }

        public void RecycleEphemeralSchemas()
        {
            using var systemSession = _core.Sessions.CreateEphemeralSystemSession();

            //Drop and create "Temporary" schema.
            if (AcquireVirtual(systemSession.Transaction, "Temporary", LockOperation.Read, LockOperation.Stability).Exists)
            {
                Drop(systemSession.Transaction, "Temporary");
            }
            CreateSingleSchema(systemSession.Transaction, "Temporary");

            //Drop and create "Single" schema (then insert a single row).
            if (AcquireVirtual(systemSession.Transaction, "Single", LockOperation.Read, LockOperation.Stability).Exists)
            {
                Drop(systemSession.Transaction, "Single");
            }
            CreateSingleSchema(systemSession.Transaction, "Single");
            _core.Documents.InsertDocument(systemSession.Transaction, "Single", "{ephemeral: null}");

            systemSession.Commit();
        }

        internal void Alter(Transaction transaction, string schemaName, uint pageSize = 0)
        {
            try
            {
                if (pageSize == 0)
                {
                    pageSize = _core.Settings.DefaultDocumentPageSize;
                }

                var physicalSchema = Acquire(transaction, schemaName, LockOperation.Write);
                var parentPhysicalSchema = AcquireParent(transaction, physicalSchema, LockOperation.Write);
                var parentCatalog = _core.IO.GetJson<PhysicalSchemaCatalog>(transaction, parentPhysicalSchema.SchemaCatalogFilePath(), LockOperation.Write);

                var singleSchema = parentCatalog.GetByName(physicalSchema.Name)
                    ?? throw new KbObjectNotFoundException($"Schema not found: [{physicalSchema.Name}].");

                singleSchema.PageSize = pageSize;

                _core.IO.PutJson(transaction, parentPhysicalSchema.SchemaCatalogFilePath(), parentCatalog);

                if (physicalSchema.IsTemporary)
                {
                    if (transaction.IsUserCreated)
                    {
                        //If this is a long standing transaction, then we can keep track of these temp schemas and delete them automatically.
                        transaction.TemporarySchemas.Write((obj) => obj.Add(physicalSchema.VirtualPath));
                    }
                }
            }
            catch (Exception ex)
            {
                LogManager.Error($"{new StackFrame(1).GetMethod()} failed for process: [{transaction.ProcessId}].", ex);
                throw;
            }
        }

        internal void CreateSingleSchema(Transaction transaction, string schemaName, uint pageSize = 0)
        {
            try
            {
                if (pageSize == 0)
                {
                    pageSize = _core.Settings.DefaultDocumentPageSize;
                }

                //Lock the given schema, but also go ahead and place the same lock the parent schema to avoid deadlocks.
                var physicalSchema = AcquireVirtual(transaction, schemaName, LockOperation.Write, LockOperation.Write);
                if (physicalSchema.Exists)
                {
                    return; //The schema already exists, not much else to do.
                }

                var parentPhysicalSchema = AcquireParent(transaction, physicalSchema, LockOperation.Write);

                IOManager.CreateDirectory(transaction, physicalSchema.DiskPath);
                _core.IO.PutJson(transaction, physicalSchema.SchemaCatalogFilePath(), new PhysicalSchemaCatalog());
                _core.IO.PutPBuf(transaction, physicalSchema.DocumentPageCatalogFilePath(), new PhysicalDocumentPageCatalog());
                _core.IO.PutJson(transaction, physicalSchema.IndexCatalogFilePath(), new PhysicalIndexCatalog());
                _core.IO.PutJson(transaction, physicalSchema.PolicyCatalogFileFilePath(), new PhysicalPolicyCatalog());

                var parentCatalog = _core.IO.GetJson<PhysicalSchemaCatalog>(transaction, parentPhysicalSchema.SchemaCatalogFilePath(), LockOperation.Write);

                if (parentCatalog.ContainsName(physicalSchema.Name) == false)
                {
                    parentCatalog.Add(new PhysicalSchema
                    {
                        Id = Guid.NewGuid(),
                        Name = physicalSchema.Name,
                        PageSize = pageSize
                    });

                    _core.IO.PutJson(transaction, parentPhysicalSchema.SchemaCatalogFilePath(), parentCatalog);

                    if (physicalSchema.IsTemporary)
                    {
                        if (transaction.IsUserCreated)
                        {
                            //If this is a long standing transaction, then we can keep track of these temp schemas and delete them automatically.
                            transaction.TemporarySchemas.Write((obj) => obj.Add(physicalSchema.VirtualPath));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogManager.Error($"{new StackFrame(1).GetMethod()} failed for process: [{transaction.ProcessId}].", ex);
                throw;
            }
        }

        internal void Drop(Transaction transaction, string schemaName)
        {
            try
            {
                var physicalSchema = AcquireVirtual(transaction, schemaName, LockOperation.Write, LockOperation.Write);
                if (physicalSchema.Exists == false)
                {
                    return; //The schema does not exists, not much else to do.
                }

                _core.IO.DeletePath(transaction, physicalSchema.DiskPath);

                var parentCatalog = _core.IO.GetJson<PhysicalSchemaCatalog>(
                    transaction, physicalSchema.ParentPhysicalSchema.SchemaCatalogFilePath(), LockOperation.Write);

                parentCatalog.Collection.RemoveAll(o => o.Name.Is(physicalSchema.Name));

                _core.IO.PutJson(transaction, physicalSchema.ParentPhysicalSchema.SchemaCatalogFilePath(), parentCatalog);
            }
            catch (Exception ex)
            {
                LogManager.Error($"{new StackFrame(1).GetMethod()} failed for process: [{transaction.ProcessId}].", ex);
                throw;
            }
        }

        internal List<PhysicalSchema> AcquireChildren(Transaction transaction, PhysicalSchema physicalSchema, LockOperation intendedOperation)
        {
            try
            {
                var schemas = new List<PhysicalSchema>();

                if (IOManager.FileExists(transaction, physicalSchema.SchemaCatalogFilePath(), intendedOperation))
                {
                    var schemaCatalog = _core.IO.GetJson<PhysicalSchemaCatalog>(
                        transaction, physicalSchema.SchemaCatalogFilePath(), intendedOperation);

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
                LogManager.Error($"{new StackFrame(1).GetMethod()} failed for process: [{transaction.ProcessId}].", ex);
                throw;
            }
        }

        internal PhysicalSchema AcquireParent(Transaction transaction, PhysicalSchema child, LockOperation intendedOperation)
        {
            try
            {
                if (child == RootPhysicalSchema)
                {
                    throw new KbGenericException("Root schema does not have a parent.");
                }

                if (child.VirtualPath == null)
                {
                    throw new KbNullException($"Value should not be null: [{nameof(child.VirtualPath)}].");
                }

                var segments = child.VirtualPath.Split(':');
                string parentSchema = string.Join(":", segments.Take(segments.Length - 1));
                return Acquire(transaction, parentSchema, intendedOperation);
            }
            catch (Exception ex)
            {
                LogManager.Error($"{new StackFrame(1).GetMethod()} failed for process: [{transaction.ProcessId}].", ex);
                throw;
            }
        }

        /// <summary>
        /// Opens a schema for a desired access. Takes a virtual schema path 
        ///     (schema0:schema2:schema3) and converts to to a physical location.
        /// </summary>
        internal PhysicalSchema Acquire(Transaction transaction, string schemaName, LockOperation intendedOperation)
        {
            InstrumentationDurationToken? ptLockSchema = null;

            try
            {
                bool isTemporary = false;
                if (schemaName.StartsWith('#'))
                {
                    var session = _core.Sessions.ByProcessId(transaction.ProcessId);
                    schemaName = $"Temporary:{schemaName.Substring(1).Replace(':', '_')}_{session.ConnectionId}";
                    isTemporary = true;
                }

                ptLockSchema = transaction.Instrumentation.CreateToken<PhysicalSchema>(PerformanceCounter.Lock);
                schemaName = schemaName.Trim([':']).Trim();

                if (schemaName == string.Empty)
                {
                    return RootPhysicalSchema;
                }
                else
                {
                    var segments = schemaName.Split(':');
                    var thisSchemaName = segments[^1];

                    var schemaDiskPath = Path.Combine(_core.Settings.DataRootPath, string.Join("\\", segments));
                    var parentSchemaDiskPath = Directory.GetParent(schemaDiskPath)?.FullName;

                    var parentCatalogDiskPath = Path.Combine(parentSchemaDiskPath.EnsureNotNull(), SchemaCatalogFile);
                    if (IOManager.FileExists(transaction, parentCatalogDiskPath, LockOperation.Stability, out var _) == false)
                    {
                        throw new KbObjectNotFoundException($"Schema path not found: [{schemaName}].");
                    }

                    var parentCatalog = _core.IO.GetJson<PhysicalSchemaCatalog>(transaction,
                        Path.Combine(parentSchemaDiskPath, SchemaCatalogFile), LockOperation.Stability, out var _);

                    var physicalSchema = parentCatalog.GetByName(thisSchemaName);
                    if (physicalSchema != null)
                    {
                        physicalSchema.Name = thisSchemaName;
                        physicalSchema.DiskPath = schemaDiskPath;
                        physicalSchema.VirtualPath = schemaName;
                        physicalSchema.IsTemporary = isTemporary;
                    }
                    else
                    {
                        throw new KbObjectNotFoundException($"Schema not found: [{schemaName}].");
                    }

                    transaction.LockDirectory(intendedOperation, physicalSchema.DiskPath);

                    return physicalSchema;
                }
            }
            catch (Exception ex)
            {
                LogManager.Error($"{new StackFrame(1).GetMethod()} failed for process: [{transaction.ProcessId}].", ex);
                throw;
            }
            finally
            {
                ptLockSchema?.StopAndAccumulate();
            }
        }

        /// <summary>
        /// Opens a schema for a desired access even if it does not exist. Takes a virtual 
        ///     schema path (schema:schema2:schema3) and converts to to a physical location.
        /// </summary>
        internal VirtualSchema AcquireVirtual(Transaction transaction, string schemaName,
            LockOperation intendedOperation, LockOperation parentIntendedOperation)
        {
            InstrumentationDurationToken? ptLockSchema = null;

            try
            {
                bool isTemporary = false;
                if (schemaName.StartsWith('#'))
                {
                    var session = _core.Sessions.ByProcessId(transaction.ProcessId);
                    schemaName = $"Temporary:{schemaName.Substring(1).Replace(':', '_')}_{session.ConnectionId}";
                    isTemporary = true;
                }

                ptLockSchema = transaction.Instrumentation.CreateToken<PhysicalSchema>(PerformanceCounter.Lock);
                schemaName = schemaName.Trim([':']).Trim();
                if (string.IsNullOrEmpty(schemaName))
                {
                    throw new KbEngineException("Cannot acquire virtual lock of root schema.");
                }

                var schemaSegments = schemaName.Split(':');
                var thisSchema = schemaSegments[^1];
                var parentSchema = string.Join(':', schemaSegments.Take(schemaSegments.Length - 1));

                var parentPhysicalSchema = Acquire(transaction, parentSchema, parentIntendedOperation);

                var parentCatalogDiskPath = parentPhysicalSchema.SchemaCatalogFilePath();

                if (IOManager.FileExists(transaction, parentCatalogDiskPath, parentIntendedOperation, out var _) == false)
                {
                    throw new KbObjectNotFoundException($"Schema not found: [{schemaName}].");
                }
                var parentSchemaCatalog = _core.IO.GetJson<PhysicalSchemaCatalog>(transaction, parentCatalogDiskPath, parentIntendedOperation);

                var virtualSchema = parentSchemaCatalog.GetByName(thisSchema)?.ToVirtual(parentPhysicalSchema);
                if (virtualSchema != null)
                {
                    virtualSchema.Name = thisSchema;
                    virtualSchema.DiskPath = Path.Combine(_core.Settings.DataRootPath, string.Join("\\", schemaSegments));
                    virtualSchema.VirtualPath = schemaName;
                    virtualSchema.Exists = true;
                    virtualSchema.IsTemporary = isTemporary;
                }
                else
                {
                    virtualSchema = new VirtualSchema(parentPhysicalSchema)
                    {
                        Name = thisSchema,
                        DiskPath = _core.Settings.DataRootPath + "\\" + schemaName.Replace(':', '\\'),
                        VirtualPath = schemaName,
                        Exists = false,
                        IsTemporary = isTemporary

                    };
                }

                transaction.LockDirectory(intendedOperation, virtualSchema.DiskPath);

                return virtualSchema;

            }
            catch (Exception ex)
            {
                LogManager.Error($"{new StackFrame(1).GetMethod()} failed for process: [{transaction.ProcessId}].", ex);
                throw;
            }
            finally
            {
                ptLockSchema?.StopAndAccumulate();
            }
        }

        internal PhysicalSchemaCatalog AcquireCatalog(Transaction transaction, string schemaName, LockOperation intendedOperation)
        {
            var physicalSchema = _core.Schemas.Acquire(transaction, schemaName, intendedOperation);
            return _core.IO.GetJson<PhysicalSchemaCatalog>(transaction, physicalSchema.DocumentPageCatalogFilePath(), intendedOperation);
        }

        internal List<Tuple<string, string>> GetListOfChildren(Transaction transaction, string schemaName, int rowLimit)
        {
            try
            {
                var physicalSchema = _core.Schemas.Acquire(transaction, schemaName, LockOperation.Read);
                var schemaCatalog = _core.IO.GetJson<PhysicalSchemaCatalog>(
                    transaction, physicalSchema.SchemaCatalogFilePath(), LockOperation.Read);

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
                LogManager.Error($"{new StackFrame(1).GetMethod()} failed for process: [{transaction.ProcessId}].", ex);
                throw;
            }
        }

        internal KbQueryResult AnalyzePages(Transaction transaction, string schemaName, bool includePhysicalPages)
        {
            var physicalSchema = _core.Schemas.Acquire(transaction, schemaName, LockOperation.Read);
            var pageCatalog = _core.Documents.AcquireDocumentPageCatalog(transaction, physicalSchema, LockOperation.Read);

            var message = new StringBuilder();

            var result = new KbQueryResult();
            result.AddField("CatalogPageNumber");
            result.AddField("CatalogDocumentCount");
            result.AddField("PageFullness");
            if (includePhysicalPages)
            {
                result.AddField("PhysicalPageNumber");
                result.AddField("PhysicalDocumentCount");
                result.AddField("MinDocumentSize (KB)");
                result.AddField("MaxDocumentSize (KB)");
                result.AddField("AvgDocumentSize (KB)");
            }

            foreach (var page in pageCatalog.Catalog)
            {
                double pageFullness = ((double)page.DocumentCount / (double)physicalSchema.PageSize) * 100.0;

                message.AppendLine($"Page {page.PageNumber} ({pageFullness:n2}% full)");

                transaction.EnsureActive();

                var values = new List<string?> {
                    $"{page.PageNumber:n0}",
                    $"{page.DocumentCount:n0}",
                    $"{pageFullness:n2}%" };

                if (includePhysicalPages)
                {
                    //This should not be compressed, right? I intended this to be a raw read.
                    var physicalDocumentPage = _core.Documents.AcquireDocumentPage(
                        transaction, physicalSchema, page.PageNumber, LockOperation.Read);

                    values.Add($"{page.PageNumber:n0}");
                    values.Add($"{physicalDocumentPage.Documents.Count:n0}");

                    values.Add($"{(physicalDocumentPage.Documents.Min(o => o.Value.ContentLength * sizeof(char)) / 1024.0):n2}");
                    values.Add($"{(physicalDocumentPage.Documents.Max(o => o.Value.ContentLength * sizeof(char)) / 1024.0):n2}");
                    values.Add($"{(physicalDocumentPage.Documents.Average(o => o.Value.ContentLength * sizeof(char)) / 1024.0):n2}");

                    /*
                    foreach (var document in physicalDocumentPage.Documents)
                    {
                        var content = document.Value.Elements;
                    }
                    */
                }

                result.AddRow(values);
            }
            return result;
        }

        internal KbQueryResult Grant(Transaction transaction, string schemaName, Guid RoleId, bool isRecursive)
        {
            try
            {
                var physicalSchema = _core.Schemas.Acquire(transaction, schemaName, LockOperation.Write);
                var policyCatalog = _core.IO.GetJson<PhysicalPolicyCatalog>(transaction, physicalSchema.PolicyCatalogFileFilePath(), LockOperation.Write);

                throw new KbNotImplementedException();

                return new KbQueryResult();
            }
            catch (Exception ex)
            {
                LogManager.Error($"{new StackFrame(1).GetMethod()} failed for process: [{transaction.ProcessId}].", ex);
                throw;
            }
        }

        internal KbQueryResult Deny(Transaction transaction, string schemaName, Guid RoleId, bool isRecursive)
        {
            try
            {
                var physicalSchema = _core.Schemas.Acquire(transaction, schemaName, LockOperation.Write);
                var policyCatalog = _core.IO.GetJson<PhysicalPolicyCatalog>(transaction, physicalSchema.PolicyCatalogFileFilePath(), LockOperation.Write);

                throw new KbNotImplementedException();

                return new KbQueryResult();
            }
            catch (Exception ex)
            {
                LogManager.Error($"{new StackFrame(1).GetMethod()} failed for process: [{transaction.ProcessId}].", ex);
                throw;
            }
        }
    }
}
