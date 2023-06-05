using Katzebase.Engine.Documents;
using Katzebase.Engine.Indexes;
using Katzebase.Engine.Transactions;
using Katzebase.Library;
using Katzebase.Library.Exceptions;
using System.Text;
using static Katzebase.Engine.Constants;

namespace Katzebase.Engine.Schemas
{
    public class SchemaManager
    {
        private Core core;
        private string rootCatalogFile;
        private PersistSchema? rootSchemaMeta = null;

        public PersistSchema RootSchemaMeta
        {
            get
            {
                if (rootSchemaMeta == null)
                {
                    rootSchemaMeta = new PersistSchema()
                    {
                        Id = Constants.RootSchemaGUID,
                        DiskPath = core.settings.DataRootPath,
                        VirtualPath = string.Empty,
                        Exists = true,
                        Name = string.Empty,
                    };
                }
                return rootSchemaMeta;
            }
        }

        public SchemaManager(Core core)
        {
            this.core = core;

            rootCatalogFile = Path.Combine(core.settings.DataRootPath, Constants.SchemaCatalogFile);

            //If the catalog doesnt exist, create a new empty one.
            if (File.Exists(rootCatalogFile) == false)
            {
                Directory.CreateDirectory(core.settings.DataRootPath);

                core.IO.PutJsonNonTracked(Path.Combine(core.settings.DataRootPath, Constants.SchemaCatalogFile), new PersistSchemaCatalog());
                core.IO.PutJsonNonTracked(Path.Combine(core.settings.DataRootPath, Constants.DocumentCatalogFile), new PersistDocumentCatalog());
                core.IO.PutJsonNonTracked(Path.Combine(core.settings.DataRootPath, Constants.IndexCatalogFile), new PersistIndexCatalog());
            }
        }

        public List<PersistSchema> GetChildrenMeta(Transaction transaction, PersistSchema node, LockOperation intendedOperation)
        {
            List<PersistSchema> metaList = new List<PersistSchema>();

            if (node.DiskPath == null)
            {
                throw new KbNullException($"Value should not be null {nameof(node.DiskPath)}.");
            }

            string schemaCatalogDiskPath = Path.Combine(node.DiskPath, Constants.SchemaCatalogFile);

            if (core.IO.FileExists(transaction, schemaCatalogDiskPath, intendedOperation))
            {
                var schemaCatalog = core.IO.GetJson<PersistSchemaCatalog>(transaction, schemaCatalogDiskPath, intendedOperation);

                Utility.EnsureNotNull(schemaCatalog);
                Utility.EnsureNotNull(schemaCatalog.Collection);

                foreach (var catalogItem in schemaCatalog.Collection)
                {
                    metaList.Add(new PersistSchema()
                    {
                        DiskPath = node.DiskPath + "\\" + catalogItem.Name,
                        Exists = true,
                        Id = catalogItem.Id,
                        Name = catalogItem.Name,
                        VirtualPath = node.VirtualPath + ":" + catalogItem.Name
                    });
                }
            }

            return metaList;
        }

        public PersistSchema? GetParentMeta(Transaction transaction, PersistSchema child, LockOperation intendedOperation)
        {
            try
            {
                if (child == RootSchemaMeta)
                {
                    return null;
                }

                if (child.VirtualPath == null)
                {
                    throw new KbNullException($"Value should not be null {nameof(child.VirtualPath)}.");
                }

                var segments = child.VirtualPath.Split(':').ToList();
                segments.RemoveAt(segments.Count - 1);
                string parentNs = string.Join(":", segments);
                return VirtualPathToMeta(transaction, parentNs, intendedOperation);
            }
            catch (Exception ex)
            {
                core.Log.Write("Failed to get parent schema.", ex);
                throw;
            }
        }

        public PersistSchema VirtualPathToMeta(Transaction transaction, string schemaPath, LockOperation intendedOperation)
        {
            try
            {
                schemaPath = schemaPath.Trim(new char[] { ':' }).Trim();

                if (schemaPath == string.Empty)
                {
                    return RootSchemaMeta;
                }
                else
                {
                    var segments = schemaPath.Split(':');
                    string schemaName = segments[segments.Count() - 1];

                    string schemaDiskPath = Path.Combine(core.settings.DataRootPath, string.Join("\\", segments));
                    string? parentSchemaDiskPath = Directory.GetParent(schemaDiskPath)?.FullName;

                    Utility.EnsureNotNull(parentSchemaDiskPath);

                    string parentCatalogDiskPath = Path.Combine(parentSchemaDiskPath, Constants.SchemaCatalogFile);

                    if (core.IO.FileExists(transaction, parentCatalogDiskPath, intendedOperation) == false)
                    {
                        throw new KbInvalidSchemaException($"The schema [{schemaPath}] does not exist.");
                    }

                    var parentCatalog = core.IO.GetJson<PersistSchemaCatalog>(transaction,
                        Path.Combine(parentSchemaDiskPath, Constants.SchemaCatalogFile), intendedOperation);

                    Utility.EnsureNotNull(parentCatalog);

                    var schemaMeta = parentCatalog.GetByName(schemaName);
                    if (schemaMeta != null)
                    {
                        schemaMeta.Name = schemaName;
                        schemaMeta.DiskPath = schemaDiskPath;
                        schemaMeta.VirtualPath = schemaPath;
                        schemaMeta.Exists = true;
                    }
                    else
                    {
                        schemaMeta = new PersistSchema()
                        {
                            Name = schemaName,
                            DiskPath = core.settings.DataRootPath + "\\" + schemaPath.Replace(':', '\\'),
                            VirtualPath = schemaPath,
                            Exists = false
                        };
                    }

                    transaction.LockDirectory(intendedOperation, schemaMeta.DiskPath);

                    return schemaMeta;
                }
            }
            catch (Exception ex)
            {
                core.Log.Write("Failed to translate virtual path to schema.", ex);
                throw;
            }
        }

        public List<PersistSchema> GetList(ulong processId, string schema)
        {
            try
            {
                using (var txRef = core.Transactions.Begin(processId))
                {
                    PersistSchema schemaMeta = VirtualPathToMeta(txRef.Transaction, schema, LockOperation.Read);
                    if (schemaMeta == null || schemaMeta.Exists == false)
                    {
                        throw new KbInvalidSchemaException(schema);
                    }

                    var list = new List<PersistSchema>();

                    if (schemaMeta.DiskPath == null)
                    {
                        throw new KbNullException($"Value should not be null {nameof(schemaMeta.DiskPath)}.");
                    }

                    var filePath = Path.Combine(schemaMeta.DiskPath, Constants.SchemaCatalogFile);
                    var schemaCatalog = core.IO.GetJson<PersistSchemaCatalog>(txRef.Transaction, filePath, LockOperation.Read);

                    Utility.EnsureNotNull(schemaCatalog);
                    Utility.EnsureNotNull(schemaCatalog.Collection);

                    foreach (var item in schemaCatalog.Collection)
                    {
                        list.Add(item);
                    }

                    txRef.Commit();

                    return list;
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

                    var schemaMeta = VirtualPathToMeta(txRef.Transaction, schema, LockOperation.Write);
                    if (schemaMeta.Exists)
                    {
                        txRef.Commit();
                        //The schema already exists.
                        return;
                    }

                    var parentSchemaMeta = GetParentMeta(txRef.Transaction, schemaMeta, LockOperation.Write);
                    Utility.EnsureNotNull(parentSchemaMeta);

                    if (parentSchemaMeta.Exists == false)
                    {
                        throw new KbInvalidSchemaException("The parent schema does not exists. Use CreateLineage() instead of CreateSingle().");
                    }

                    if (parentSchemaMeta.DiskPath == null || schemaMeta.DiskPath == null)
                    {
                        throw new KbNullException($"Value should not be null {nameof(schemaMeta.DiskPath)}.");
                    }

                    string parentSchemaCatalogFile = Path.Combine(parentSchemaMeta.DiskPath, Constants.SchemaCatalogFile);
                    PersistSchemaCatalog? parentCatalog = core.IO.GetJson<PersistSchemaCatalog>(txRef.Transaction, parentSchemaCatalogFile, LockOperation.Write);
                    Utility.EnsureNotNull(parentCatalog);

                    string filePath = string.Empty;

                    core.IO.CreateDirectory(txRef.Transaction, schemaMeta.DiskPath);

                    //Create default schema catalog file.
                    filePath = Path.Combine(schemaMeta.DiskPath, Constants.SchemaCatalogFile);
                    if (core.IO.FileExists(txRef.Transaction, filePath, LockOperation.Write) == false)
                    {
                        core.IO.PutJson(txRef.Transaction, filePath, new PersistSchemaCatalog());
                    }

                    //Create default document catalog file.
                    filePath = Path.Combine(schemaMeta.DiskPath, Constants.DocumentCatalogFile);
                    if (core.IO.FileExists(txRef.Transaction, filePath, LockOperation.Write) == false)
                    {
                        core.IO.PutJson(txRef.Transaction, filePath, new PersistDocumentCatalog());
                    }

                    //Create default index catalog file.
                    filePath = Path.Combine(schemaMeta.DiskPath, Constants.IndexCatalogFile);
                    if (core.IO.FileExists(txRef.Transaction, filePath, LockOperation.Write) == false)
                    {
                        core.IO.PutJson(txRef.Transaction, filePath, new PersistIndexCatalog());
                    }

                    if (parentCatalog.ContainsName(schema) == false)
                    {
                        parentCatalog.Add(new PersistSchema
                        {
                            Id = newSchemaId,
                            Name = schemaMeta.Name
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
                        var schema = VirtualPathToMeta(txRef.Transaction, pathBuilder.ToString(), LockOperation.Read);

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

                    var schemaMeta = VirtualPathToMeta(txRef.Transaction, schema, LockOperation.Write);
                    if (schemaMeta == null || schemaMeta.Exists == false)
                    {
                        throw new KbInvalidSchemaException(schema);
                    }

                    var parentSchemaMeta = GetParentMeta(txRef.Transaction, schemaMeta, LockOperation.Write);
                    Utility.EnsureNotNull(parentSchemaMeta);

                    if (parentSchemaMeta.DiskPath == null || schemaMeta.DiskPath == null)
                        throw new KbNullException($"Value should not be null {nameof(schemaMeta.DiskPath)}.");

                    string parentSchemaCatalogFile = Path.Combine(parentSchemaMeta.DiskPath, Constants.SchemaCatalogFile);
                    var parentCatalog = core.IO.GetJson<PersistSchemaCatalog>(txRef.Transaction, parentSchemaCatalogFile, LockOperation.Write);
                    Utility.EnsureNotNull(parentCatalog);

                    var nsItem = parentCatalog.Collection.FirstOrDefault(o => o.Name == schemaName);
                    if (nsItem != null)
                    {
                        parentCatalog.Collection.Remove(nsItem);

                        core.IO.DeletePath(txRef.Transaction, schemaMeta.DiskPath);

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
    }
}
