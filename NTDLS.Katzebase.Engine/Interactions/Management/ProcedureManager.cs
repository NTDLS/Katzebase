using NTDLS.Katzebase.Client.Payloads;
using NTDLS.Katzebase.Engine.Atomicity;
using NTDLS.Katzebase.Engine.Functions.Parameters;
using NTDLS.Katzebase.Engine.Functions.Procedures;
using NTDLS.Katzebase.Engine.Functions.Procedures.Persistent;
using NTDLS.Katzebase.Engine.Interactions.APIHandlers;
using NTDLS.Katzebase.Engine.Interactions.QueryHandlers;
using NTDLS.Katzebase.Engine.Schemas;
using static NTDLS.Katzebase.Engine.Library.EngineConstants;

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

        public ProcedureManager(EngineCore core)
        {
            _core = core;

            try
            {
                QueryHandlers = new ProcedureQueryHandlers(core);
                APIHandlers = new ProcedureAPIHandlers(core);

                ProcedureCollection.Initialize();
            }
            catch (Exception ex)
            {
                core.Log.Write("Failed to instantiate procedures manager.", ex);
                throw;
            }
        }

        internal void CreateCustomProcedure(Transaction transaction, string schemaName, string objectName, List<PhysicalProcedureParameter> parameters, List<string> Batches)
        {
            var physicalSchema = _core.Schemas.Acquire(transaction, schemaName, LockOperation.Write);
            var physicalProcedureCatalog = Acquire(transaction, physicalSchema, LockOperation.Write);

            var physicalProcesure = physicalProcedureCatalog.GetByName(objectName);
            if (physicalProcesure == null)
            {
                physicalProcesure = new PhysicalProcedure()
                {
                    Id = Guid.NewGuid(),
                    Name = objectName,
                    Created = DateTime.UtcNow,
                    Modfied = DateTime.UtcNow,
                    Parameters = parameters,
                    Batches = Batches,
                };

                physicalProcedureCatalog.Add(physicalProcesure);

                _core.IO.PutJson(transaction, physicalSchema.ProcedureCatalogFilePath(), physicalProcedureCatalog);
            }
            else
            {
                physicalProcesure.Parameters = parameters;
                physicalProcesure.Batches = Batches;
                physicalProcesure.Modfied = DateTime.UtcNow;

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

        internal PhysicalProcedure? Acquire(Transaction transaction, PhysicalSchema physicalSchema, string procedureName, LockOperation intendedOperation)
        {
            procedureName = procedureName.ToLower();

            if (File.Exists(physicalSchema.ProcedureCatalogFilePath()) == false)
            {
                _core.IO.PutJson(transaction, physicalSchema.ProcedureCatalogFilePath(), new PhysicalProcedureCatalog());
            }

            var procedureCatalog = _core.IO.GetJson<PhysicalProcedureCatalog>(transaction, physicalSchema.ProcedureCatalogFilePath(), intendedOperation);

            return procedureCatalog.Collection.Where(o => o.Name.ToLower() == procedureName).FirstOrDefault();
        }

        internal KbQueryResultCollection ExecuteProcedure(Transaction transaction, FunctionParameterBase procedureCall)
        {
            return SystemProcedureImplementations.ExecuteProcedure(_core, transaction, procedureCall);
        }
    }
}
