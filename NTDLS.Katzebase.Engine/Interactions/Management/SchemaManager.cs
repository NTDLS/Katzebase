using NTDLS.Katzebase.Client;
using NTDLS.Katzebase.Client.Exceptions;
using NTDLS.Katzebase.Client.Payloads;
using NTDLS.Katzebase.Engine.Atomicity;
using NTDLS.Katzebase.Engine.Documents;
using NTDLS.Katzebase.Engine.Indexes;
using NTDLS.Katzebase.Engine.Interactions.APIHandlers;
using NTDLS.Katzebase.Engine.Interactions.QueryHandlers;
using NTDLS.Katzebase.Engine.Schemas;
using NTDLS.Katzebase.Engine.Trace;
using System.Text;
using static NTDLS.Katzebase.Engine.Library.EngineConstants;
using static NTDLS.Katzebase.Engine.Schemas.PhysicalSchema;
using static NTDLS.Katzebase.Engine.Trace.PerformanceTrace;

namespace NTDLS.Katzebase.Engine.Interactions.Management
{
    /// <summary>
    /// Public core class methods for locking, reading, writing and managing tasks related to schemas.
    /// </summary>
    public class SchemaManager
    {
        private readonly Core _core;
        private readonly string _rootCatalogFile;
        private PhysicalSchema? _rootPhysicalSchema = null;

        internal SchemaQueryHandlers QueryHandlers { get; private set; }

        public SchemaAPIHandlers APIHandlers { get; private set; }

        public PhysicalSchema RootPhysicalSchema
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
                    };
                    return _rootPhysicalSchema;
                }
                catch (Exception ex)
                {
                    _core.Log.Write($"Failed to obtain root schema.", ex);
                    throw;
                }
            }
        }

        public SchemaManager(Core core)
        {
            _core = core;

            try
            {
                QueryHandlers = new SchemaQueryHandlers(core);
                APIHandlers = new SchemaAPIHandlers(core);

                _rootCatalogFile = Path.Combine(core.Settings.DataRootPath, SchemaCatalogFile);

                //If the catalog doesnt exist, create a new empty one.
                if (File.Exists(_rootCatalogFile) == false)
                {
                    Directory.CreateDirectory(core.Settings.DataRootPath);

                    var physicalSchemaCatalog = new PhysicalSchemaCatalog();
                    physicalSchemaCatalog.Collection.Add(new PhysicalSchema()
                    {
                        Name = "Temporary",
                        VirtualPath = "Temporary",
                        IsTemporary = false,
                        Id = Guid.NewGuid(),
                        DiskPath = Path.Combine(core.Settings.DataRootPath, "Temporary")
                    });

                    core.IO.PutJsonNonTracked(Path.Combine(core.Settings.DataRootPath, SchemaCatalogFile), physicalSchemaCatalog);
                    core.IO.PutPBufNonTracked(Path.Combine(core.Settings.DataRootPath, DocumentPageCatalogFile), new PhysicalDocumentPageCatalog());
                    core.IO.PutJsonNonTracked(Path.Combine(core.Settings.DataRootPath, IndexCatalogFile), new PhysicalIndexCatalog());
                }

                var temporarySchemaPath = Path.Combine(core.Settings.DataRootPath, "Temporary");

                try
                {
                    if (Directory.Exists(temporarySchemaPath))
                    {
                        Directory.Delete(temporarySchemaPath, true);
                    }
                }
                catch { }

                Directory.CreateDirectory(temporarySchemaPath);
                core.IO.PutJsonNonTracked(Path.Combine(temporarySchemaPath, SchemaCatalogFile), new PhysicalSchemaCatalog());
                core.IO.PutPBufNonTracked(Path.Combine(temporarySchemaPath, DocumentPageCatalogFile), new PhysicalDocumentPageCatalog());
                core.IO.PutJsonNonTracked(Path.Combine(temporarySchemaPath, IndexCatalogFile), new PhysicalIndexCatalog());
            }
            catch (Exception ex)
            {
                core.Log.Write("Failed to instanciate SchemaManager.", ex);
                throw;
            }
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

                var singleSchema = parentCatalog.GetByName(physicalSchema.Name);
                if (singleSchema == null)
                {
                    throw new KbObjectNotFoundException($"Schema not found: '{physicalSchema.Name}'");
                }
                singleSchema.PageSize = pageSize;

                _core.IO.PutJson(transaction, parentPhysicalSchema.SchemaCatalogFilePath(), parentCatalog);

                if (physicalSchema.IsTemporary)
                {
                    if (transaction.IsUserCreated)
                    {
                        //If this is a long standing transaction, then we can keep track of these temp schemas and delete them automatically.
                        transaction.TemporarySchemas.Add(physicalSchema.VirtualPath);
                    }
                }
            }
            catch (Exception ex)
            {
                _core.Log.Write($"Failed to alter schema manager for process id {transaction.ProcessId}.", ex);
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

                var physicalSchema = AcquireVirtual(transaction, schemaName, LockOperation.Write);
                if (physicalSchema.Exists)
                {
                    return; //The schema already exists, not much else to do.
                }

                var parentPhysicalSchema = AcquireParent(transaction, physicalSchema, LockOperation.Write);

                _core.IO.CreateDirectory(transaction, physicalSchema.DiskPath);
                _core.IO.PutJson(transaction, physicalSchema.SchemaCatalogFilePath(), new PhysicalSchemaCatalog());
                _core.IO.PutPBuf(transaction, physicalSchema.DocumentPageCatalogFilePath(), new PhysicalDocumentPageCatalog());
                _core.IO.PutJson(transaction, physicalSchema.IndexCatalogFilePath(), new PhysicalIndexCatalog());

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
                            transaction.TemporarySchemas.Add(physicalSchema.VirtualPath);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _core.Log.Write($"Failed to create single schema manager for process id {transaction.ProcessId}.", ex);
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

                _core.IO.DeletePath(transaction, physicalSchema.DiskPath);

                var parentCatalog = _core.IO.GetJson<PhysicalSchemaCatalog>(transaction, parentPhysicalSchema.SchemaCatalogFilePath(), LockOperation.Write);

                parentCatalog.Collection.RemoveAll(o => o.Name.ToLower() == physicalSchema.Name.ToLower());

                _core.IO.PutJson(transaction, parentPhysicalSchema.SchemaCatalogFilePath(), parentCatalog);
            }
            catch (Exception ex)
            {
                _core.Log.Write($"Failed to create single schema manager for process id {transaction.ProcessId}.", ex);
                throw;
            }
        }

        internal List<PhysicalSchema> AcquireChildren(Transaction transaction, PhysicalSchema physicalSchema, LockOperation intendedOperation)
        {
            try
            {
                var schemas = new List<PhysicalSchema>();


                if (_core.IO.FileExists(transaction, physicalSchema.SchemaCatalogFilePath(), intendedOperation))
                {
                    var schemaCatalog = _core.IO.GetJson<PhysicalSchemaCatalog>(transaction, physicalSchema.SchemaCatalogFilePath(), intendedOperation);

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
                _core.Log.Write($"Failed to acquire schema children for process id {transaction.ProcessId}.", ex);
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
                _core.Log.Write($"Failed to acquire parent schema for process id {transaction.ProcessId}.", ex);
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
                bool isTemporary = false;
                if (schemaName.StartsWith('#'))
                {
                    var session = _core.Sessions.ByProcessId(transaction.ProcessId);
                    schemaName = $"Temporary:{schemaName.Substring(1).Replace(':', '_')}_{session.SessionId}";
                    isTemporary = true;
                }

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

                    var schemaDiskPath = Path.Combine(_core.Settings.DataRootPath, string.Join("\\", segments));
                    var parentSchemaDiskPath = Directory.GetParent(schemaDiskPath)?.FullName;
                    KbUtility.EnsureNotNull(parentSchemaDiskPath);

                    var parentCatalogDiskPath = Path.Combine(parentSchemaDiskPath, SchemaCatalogFile);

                    if (_core.IO.FileExists(transaction, parentCatalogDiskPath, LockOperation.Read) == false)
                    {
                        throw new KbObjectNotFoundException($"The schema [{schemaName}] does not exist.");
                    }

                    var parentCatalog = _core.IO.GetJson<PhysicalSchemaCatalog>(transaction,
                        Path.Combine(parentSchemaDiskPath, SchemaCatalogFile), LockOperation.Read);

                    var physicalSchema = parentCatalog.GetByName(parentSchemaame);
                    if (physicalSchema != null)
                    {
                        physicalSchema.Name = parentSchemaame;
                        physicalSchema.DiskPath = schemaDiskPath;
                        physicalSchema.VirtualPath = schemaName;
                        physicalSchema.IsTemporary = isTemporary;
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
                _core.Log.Write($"Failed to acquire schema for process id {transaction.ProcessId}.", ex);
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
                bool isTemporary = false;
                if (schemaName.StartsWith('#'))
                {
                    var session = _core.Sessions.ByProcessId(transaction.ProcessId);
                    schemaName = $"Temporary:{schemaName.Substring(1).Replace(':', '_')}_{session.SessionId}";
                    isTemporary = true;
                }

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

                    var schemaDiskPath = Path.Combine(_core.Settings.DataRootPath, string.Join("\\", segments));
                    var parentSchemaDiskPath = Directory.GetParent(schemaDiskPath)?.FullName;
                    KbUtility.EnsureNotNull(parentSchemaDiskPath);

                    var parentCatalogDiskPath = Path.Combine(parentSchemaDiskPath, SchemaCatalogFile);

                    if (_core.IO.FileExists(transaction, parentCatalogDiskPath, intendedOperation) == false)
                    {
                        throw new KbObjectNotFoundException($"The schema [{schemaName}] does not exist.");
                    }

                    var parentCatalog = _core.IO.GetJson<PhysicalSchemaCatalog>(transaction,
                        Path.Combine(parentSchemaDiskPath, SchemaCatalogFile), intendedOperation);

                    var virtualSchema = parentCatalog.GetByName(parentSchemaName)?.ToVirtual();
                    if (virtualSchema != null)
                    {
                        virtualSchema.Name = parentSchemaName;
                        virtualSchema.DiskPath = schemaDiskPath;
                        virtualSchema.VirtualPath = schemaName;
                        virtualSchema.Exists = true;
                        virtualSchema.IsTemporary = isTemporary;

                    }
                    else
                    {
                        virtualSchema = new VirtualSchema()
                        {
                            Name = parentSchemaName,
                            DiskPath = _core.Settings.DataRootPath + "\\" + schemaName.Replace(':', '\\'),
                            VirtualPath = schemaName,
                            Exists = false,
                            IsTemporary = isTemporary

                        };
                    }

                    transaction.LockDirectory(intendedOperation, virtualSchema.DiskPath);

                    return virtualSchema;
                }
            }
            catch (Exception ex)
            {
                _core.Log.Write($"Failed to acquire virtual schema for process id {transaction.ProcessId}.", ex);
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

        internal List<Tuple<string, string>> GetListByPreparedQuery(Transaction transaction, string schemaName, int rowLimit)
        {
            try
            {
                var physicalSchema = _core.Schemas.Acquire(transaction, schemaName, LockOperation.Read);
                var schemaCatalog = _core.IO.GetJson<PhysicalSchemaCatalog>(transaction, physicalSchema.SchemaCatalogFilePath(), LockOperation.Read);

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
                _core.Log.Write($"Failed to get schema list for process id {transaction.ProcessId}.", ex);
                throw;
            }
        }

        internal KbQueryResult AnalysePages(Transaction transaction, string schemaName, bool includePhysicalPages)
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
                    var physicalDocumentPage = _core.Documents.AcquireDocumentPage(transaction, physicalSchema, page.PageNumber, LockOperation.Read);

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
    }
}
