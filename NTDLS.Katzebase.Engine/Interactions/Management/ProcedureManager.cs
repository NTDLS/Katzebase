using NTDLS.Helpers;
using NTDLS.Katzebase.Api.Payloads.Response;
using NTDLS.Katzebase.Engine.Atomicity;
using NTDLS.Katzebase.Engine.Interactions.APIHandlers;
using NTDLS.Katzebase.Engine.Interactions.QueryProcessors;
using NTDLS.Katzebase.PersistentTypes.Procedure;
using NTDLS.Katzebase.PersistentTypes.Schema;
using static NTDLS.Katzebase.Shared.EngineConstants;

namespace NTDLS.Katzebase.Engine.Interactions.Management
{
    /// <summary>
    /// Public core class methods for locking, reading, writing and managing tasks related to procedures.
    /// </summary>
    public class ProcedureManager
    {
        private readonly EngineCore _core;

        internal ProcedureQueryHandlers QueryHandlers { get; private set; }
        public ProcedureAPIHandlers APIHandlers { get; private set; }

        internal ProcedureManager(EngineCore core)
        {
            _core = core;

            try
            {
                QueryHandlers = new ProcedureQueryHandlers(core);
                APIHandlers = new ProcedureAPIHandlers(core);

                //ProcedureCollection.Initialize();
            }
            catch (Exception ex)
            {
                LogManager.Error("Failed to instantiate procedures manager.", ex);
                throw;
            }
        }

        internal void CreateCustomProcedure(Transaction transaction, string schemaName,
            string objectName, List<PhysicalProcedureParameter> parameters, List<string> Batches)
        {
            var physicalSchema = _core.Schemas.Acquire(transaction, schemaName, LockOperation.Write);
            var physicalProcedureCatalog = Acquire(transaction, physicalSchema, LockOperation.Write);

            var physicalProcedure = physicalProcedureCatalog.GetByName(objectName);
            if (physicalProcedure == null)
            {
                physicalProcedure = new PhysicalProcedure()
                {
                    Id = Guid.NewGuid(),
                    Name = objectName,
                    Created = DateTime.UtcNow,
                    Modified = DateTime.UtcNow,
                    Parameters = parameters,
                    Batches = Batches,
                };

                physicalProcedureCatalog.Add(physicalProcedure);

                _core.IO.PutJson(transaction, physicalSchema.ProcedureCatalogFilePath(), physicalProcedureCatalog);
            }
            else
            {
                physicalProcedure.Parameters = parameters;
                physicalProcedure.Batches = Batches;
                physicalProcedure.Modified = DateTime.UtcNow;

                _core.IO.PutJson(transaction, physicalSchema.ProcedureCatalogFilePath(), physicalProcedureCatalog);
            }
        }

        internal PhysicalProcedureCatalog Acquire(Transaction transaction, PhysicalSchema physicalSchema, LockOperation intendedOperation)
        {
            if (File.Exists(physicalSchema.ProcedureCatalogFilePath()) == false)
            {
                _core.IO.PutJson(transaction, physicalSchema.ProcedureCatalogFilePath(), new PhysicalProcedureCatalog());
            }

            return _core.IO.GetJson<PhysicalProcedureCatalog>(transaction, physicalSchema.ProcedureCatalogFilePath(), intendedOperation);
        }

        internal PhysicalProcedure? Acquire(Transaction transaction,
            PhysicalSchema physicalSchema, string procedureName, LockOperation intendedOperation)
        {
            procedureName = procedureName.ToLowerInvariant();

            if (File.Exists(physicalSchema.ProcedureCatalogFilePath()) == false)
            {
                _core.IO.PutJson(transaction, physicalSchema.ProcedureCatalogFilePath(), new PhysicalProcedureCatalog());
            }

            var procedureCatalog = _core.IO.GetJson<PhysicalProcedureCatalog>(
                transaction, physicalSchema.ProcedureCatalogFilePath(), intendedOperation);

            return procedureCatalog.Collection.FirstOrDefault(o => o.Name.Is(procedureName));
        }

        internal KbQueryResultCollection ExecuteProcedure(Transaction transaction, string schemaName, string procedureName)
        {
            throw new NotImplementedException("Reimplement user procedures");
        }
    }
}
