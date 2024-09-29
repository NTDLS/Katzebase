using NTDLS.Helpers;
using NTDLS.Katzebase.Client;
using NTDLS.Katzebase.Client.Exceptions;
using NTDLS.Katzebase.Client.Payloads;
using NTDLS.Katzebase.Engine.Atomicity;
using NTDLS.Katzebase.Engine.Documents;
using NTDLS.Katzebase.Engine.Indexes;
using NTDLS.Katzebase.Engine.Instrumentation;
using NTDLS.Katzebase.Engine.Interactions.APIHandlers;
using NTDLS.Katzebase.Engine.Interactions.QueryHandlers;
using NTDLS.Katzebase.Engine.Schemas;
using NTDLS.Katzebase.Engine.Sessions;
using NTDLS.Katzebase.Shared;
using System.Text;
using static NTDLS.Katzebase.Engine.Instrumentation.InstrumentationTracker;
using static NTDLS.Katzebase.Engine.Library.EngineConstants;
using NTDLS.Katzebase.Engine.Schemas;
//using static NTDLS.Katzebase.Engine.Schemas.PhysicalSchema<TData>;

namespace NTDLS.Katzebase.Engine.Interactions.Management
{
    /// <summary>
    /// Public core class methods for locking, reading, writing and managing tasks related to schemas.
    /// </summary>
    public class SchemaManager<TData> where TData : IStringable
    {
        private readonly EngineCore<TData> _core;
        private readonly string _rootCatalogFile;
        private PhysicalSchema<TData>? _rootPhysicalSchema = null;

        internal SchemaQueryHandlers<TData> QueryHandlers { get; private set; }

        public SchemaAPIHandlers<TData> APIHandlers { get; private set; }

        internal PhysicalSchema<TData> RootPhysicalSchema
        {
            get
            {
                try
                {
                    _rootPhysicalSchema ??= new PhysicalSchema<TData>()
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

        internal SchemaManager(EngineCore<TData> core)
        {
            _core = core;

            try
            {
                QueryHandlers = new SchemaQueryHandlers<TData>(core);
                APIHandlers = new SchemaAPIHandlers<TData>(core);

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
                _core.IO.PutJsonNonTracked(Path.Combine(_core.Settings.DataRootPath, SchemaCatalogFile), new PhysicalSchemaCatalog<TData>());
                _core.IO.PutPBufNonTracked(Path.Combine(_core.Settings.DataRootPath, DocumentPageCatalogFile), new PhysicalDocumentPageCatalog());
                _core.IO.PutJsonNonTracked(Path.Combine(_core.Settings.DataRootPath, IndexCatalogFile), new PhysicalIndexCatalog<TData>());

                //Create Master:Account schema and insert the default account.
                using var systemSession = _core.Sessions.CreateEphemeralSystemSession();
                CreateSingleSchema(systemSession.Transaction, "Master");
                CreateSingleSchema(systemSession.Transaction, "Master:Account");
                _core.Documents.InsertDocument(systemSession.Transaction, "Master:Account", new Account<TData>("admin".ParseToT<TData>(EngineCore<TData>.StrCast), KbClient.HashPassword("").ParseToT<TData>(EngineCore<TData>.StrCast)));
                systemSession.Commit();
            }

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

        internal void Alter(Transaction<TData> transaction, string schemaName, uint pageSize = 0)
        {
            try
            {
                if (pageSize == 0)
                {
                    pageSize = _core.Settings.DefaultDocumentPageSize;
                }

                var physicalSchema = Acquire(transaction, schemaName, LockOperation.Write);
                var parentPhysicalSchema = AcquireParent(transaction, physicalSchema, LockOperation.Write);
                var parentCatalog = _core.IO.GetJson<PhysicalSchemaCatalog<TData>>(transaction, parentPhysicalSchema.SchemaCatalogFilePath(), LockOperation.Write);

                var singleSchema = parentCatalog.GetByName(physicalSchema.Name);
                if (singleSchema == null)
                {
                    throw new KbObjectNotFoundException($"Schema not found: [{physicalSchema.Name}].");
                }
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
                LogManager.Error($"Failed to alter schema manager for process id {transaction.ProcessId}.", ex);
                throw;
            }
        }

        internal void CreateSingleSchema(Transaction<TData> transaction, string schemaName, uint pageSize = 0)
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

                _core.IO.CreateDirectory(transaction, physicalSchema.DiskPath);
                _core.IO.PutJson(transaction, physicalSchema.SchemaCatalogFilePath(), new PhysicalSchemaCatalog<TData>());
                _core.IO.PutPBuf(transaction, physicalSchema.DocumentPageCatalogFilePath(), new PhysicalDocumentPageCatalog());
                _core.IO.PutJson(transaction, physicalSchema.IndexCatalogFilePath(), new PhysicalIndexCatalog<TData>());

                var parentCatalog = _core.IO.GetJson<PhysicalSchemaCatalog<TData>>(transaction, parentPhysicalSchema.SchemaCatalogFilePath(), LockOperation.Write);

                if (parentCatalog.ContainsName(physicalSchema.Name) == false)
                {
                    parentCatalog.Add(new PhysicalSchema<TData>
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
                LogManager.Error($"Failed to create single schema manager for process id {transaction.ProcessId}.", ex);
                throw;
            }
        }

        internal void Drop(Transaction<TData> transaction, string schemaName)
        {
            try
            {
                var physicalSchema = AcquireVirtual(transaction, schemaName, LockOperation.Write, LockOperation.Write);
                if (physicalSchema.Exists == false)
                {
                    return; //The schema does not exists, not much else to do.
                }

                //var parentPhysicalSchema = AcquireParent(transaction, physicalSchema, LockOperation.Write); //removed after b3699d63a3337d936e302bcc8fa746376f02b317

                _core.IO.DeletePath(transaction, physicalSchema.DiskPath);

                var parentCatalog = _core.IO.GetJson<PhysicalSchemaCatalog<TData>>(
                    transaction, physicalSchema.ParentPhysicalSchema.SchemaCatalogFilePath(), LockOperation.Write);

                parentCatalog.Collection.RemoveAll(o => o.Name.Is(physicalSchema.Name));

                _core.IO.PutJson(transaction, physicalSchema.ParentPhysicalSchema.SchemaCatalogFilePath(), parentCatalog);
            }
            catch (Exception ex)
            {
                LogManager.Error($"Failed to create single schema manager for process id {transaction.ProcessId}.", ex);
                throw;
            }
        }

        internal List<PhysicalSchema<TData>> AcquireChildren(Transaction<TData> transaction, PhysicalSchema<TData> physicalSchema, LockOperation intendedOperation)
        {
            try
            {
                var schemas = new List<PhysicalSchema<TData>>();

                if (_core.IO.FileExists(transaction, physicalSchema.SchemaCatalogFilePath(), intendedOperation))
                {
                    var schemaCatalog = _core.IO.GetJson<PhysicalSchemaCatalog<TData>>(
                        transaction, physicalSchema.SchemaCatalogFilePath(), intendedOperation);

                    foreach (var catalogItem in schemaCatalog.Collection)
                    {
                        schemas.Add(new PhysicalSchema<TData>()
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
                LogManager.Error($"Failed to acquire schema children for process id {transaction.ProcessId}.", ex);
                throw;
            }
        }

        internal PhysicalSchema<TData> AcquireParent(Transaction<TData> transaction, PhysicalSchema<TData> child, LockOperation intendedOperation)
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
                LogManager.Error($"Failed to acquire parent schema for process id {transaction.ProcessId}.", ex);
                throw;
            }
        }

        /// <summary>
        /// Opens a schema for a desired access. Takes a virtual schema path 
        ///     (schema0:schema2:schema3) and converts to to a physical location.
        /// </summary>
        internal PhysicalSchema<TData> Acquire(Transaction<TData> transaction, string schemaName, LockOperation intendedOperation)
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

                ptLockSchema = transaction.Instrumentation.CreateToken<PhysicalSchema<TData>>(PerformanceCounter.Lock);
                schemaName = schemaName.Trim([':']).Trim();

                if (schemaName == string.Empty)
                {
                    return RootPhysicalSchema;
                }
                else
                {
                    var segments = schemaName.Split(':');
                    var thisSchemaName = segments[segments.Length - 1];

                    var schemaDiskPath = Path.Combine(_core.Settings.DataRootPath, string.Join("\\", segments));
                    var parentSchemaDiskPath = Directory.GetParent(schemaDiskPath)?.FullName;

                    var parentCatalogDiskPath = Path.Combine(parentSchemaDiskPath.EnsureNotNull(), SchemaCatalogFile);
                    if (_core.IO.FileExists(transaction, parentCatalogDiskPath, LockOperation.Stability, out var _) == false)
                    {
                        throw new KbObjectNotFoundException($"Schema not found: [{schemaName}].");
                    }

                    var parentCatalog = _core.IO.GetJson<PhysicalSchemaCatalog<TData>>(transaction,
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
                LogManager.Error($"Failed to acquire schema for process id {transaction.ProcessId}.", ex);
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
        internal PhysicalSchema<TData>.VirtualSchema<TData> AcquireVirtual(Transaction<TData> transaction, string schemaName,
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

                ptLockSchema = transaction.Instrumentation.CreateToken<PhysicalSchema<TData>>(PerformanceCounter.Lock);
                schemaName = schemaName.Trim([':']).Trim();
                if (string.IsNullOrEmpty(schemaName))
                {
                    throw new KbEngineException("Cannot acquire virtual lock of root schema.");
                }

                var schemaSegments = schemaName.Split(':');
                var thisSchema = schemaSegments[schemaSegments.Length - 1];
                var parentSchema = string.Join(':', schemaSegments.Take(schemaSegments.Length - 1));

                var parentPhysicalSchema = Acquire(transaction, parentSchema, parentIntendedOperation);

                var parentCatalogDiskPath = parentPhysicalSchema.SchemaCatalogFilePath();

                if (_core.IO.FileExists(transaction, parentCatalogDiskPath, parentIntendedOperation, out var _) == false)
                {
                    throw new KbObjectNotFoundException($"Schema not found: [{schemaName}].");
                }
                var parentSchemaCatalog = _core.IO.GetJson<PhysicalSchemaCatalog<TData>>(transaction, parentCatalogDiskPath, parentIntendedOperation);

                var virtualSchema = parentSchemaCatalog?.GetByName(thisSchema)?.ToVirtual(parentPhysicalSchema);


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
                    virtualSchema = new PhysicalSchema<TData>.VirtualSchema<TData>(parentPhysicalSchema)
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
                LogManager.Error($"Failed to acquire virtual schema for process id {transaction.ProcessId}.", ex);
                throw;
            }
            finally
            {
                ptLockSchema?.StopAndAccumulate();
            }
        }

        internal PhysicalSchemaCatalog<TData> AcquireCatalog(Transaction<TData> transaction, string schemaName, LockOperation intendedOperation)
        {
            var physicalSchema = _core.Schemas.Acquire(transaction, schemaName, intendedOperation);
            return _core.IO.GetJson<PhysicalSchemaCatalog<TData>>(transaction, physicalSchema.DocumentPageCatalogFilePath(), intendedOperation);
        }

        internal List<Tuple<string, string>> GetListByPreparedQuery(Transaction<TData> transaction, string schemaName, int rowLimit)
        {
            try
            {
                var physicalSchema = _core.Schemas.Acquire(transaction, schemaName, LockOperation.Read);
                var schemaCatalog = _core.IO.GetJson<PhysicalSchemaCatalog<TData>>(
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
                LogManager.Error($"Failed to get schema list for process id {transaction.ProcessId}.", ex);
                throw;
            }
        }

        internal KbQueryDocumentListResult<TData> AnalyzePages(Transaction<TData> transaction, string schemaName, bool includePhysicalPages)
        {
            var physicalSchema = _core.Schemas.Acquire(transaction, schemaName, LockOperation.Read);
            var pageCatalog = _core.Documents.AcquireDocumentPageCatalog(transaction, physicalSchema, LockOperation.Read);

            var message = new StringBuilder();

            var result = new KbQueryDocumentListResult<TData>();
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

                var values = new List<TData?> (new[]{
                    $"{page.PageNumber:n0}",
                    $"{page.DocumentCount:n0}",
                    $"{pageFullness:n2}%" }.Select(s=>s.CastToT<TData>(EngineCore<TData>.StrCast)));

                if (includePhysicalPages)
                {
                    //This should not be compressed, right? I intended this to be a raw read.
                    var physicalDocumentPage = _core.Documents.AcquireDocumentPage(
                        transaction, physicalSchema, page.PageNumber, LockOperation.Read);

                    values.Add($"{page.PageNumber:n0}".CastToT<TData>(EngineCore<TData>.StrCast));
                    values.Add($"{physicalDocumentPage.Documents.Count:n0}".CastToT<TData>(EngineCore<TData>.StrCast));

                    values.Add($"{(physicalDocumentPage.Documents.Min(o => o.Value.ContentLength * sizeof(char)) / 1024.0):n2}".CastToT<TData>(EngineCore<TData>.StrCast));
                    values.Add($"{(physicalDocumentPage.Documents.Max(o => o.Value.ContentLength * sizeof(char)) / 1024.0):n2}".CastToT<TData>(EngineCore<TData>.StrCast));
                    values.Add($"{(physicalDocumentPage.Documents.Average(o => o.Value.ContentLength * sizeof(char)) / 1024.0):n2}".CastToT<TData>(EngineCore<TData>.StrCast));

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
    }
}
