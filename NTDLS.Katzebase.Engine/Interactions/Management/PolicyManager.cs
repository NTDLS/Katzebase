using NTDLS.Katzebase.Api.Exceptions;
using NTDLS.Katzebase.Api.Payloads.Response;
using NTDLS.Katzebase.Engine.Atomicity;
using NTDLS.Katzebase.Engine.Interactions.APIHandlers;
using NTDLS.Katzebase.Engine.Interactions.QueryHandlers;
using NTDLS.Katzebase.Engine.Scripts;
using NTDLS.Katzebase.Engine.Sessions;
using NTDLS.Katzebase.Parsers.Query.SupportingTypes;
using NTDLS.Katzebase.PersistentTypes.Index;
using NTDLS.Katzebase.PersistentTypes.Policy;
using NTDLS.Katzebase.PersistentTypes.Schema;
using System;
using static NTDLS.Katzebase.Parsers.Constants;
using static NTDLS.Katzebase.Shared.EngineConstants;

namespace NTDLS.Katzebase.Engine.Interactions.Management
{
    /// <summary>
    /// Public core class methods for locking, reading, writing and managing tasks related to policies.
    /// </summary>
    public class PolicyManager
    {
        private readonly EngineCore _core;
        internal PolicyQueryHandlers QueryHandlers { get; private set; }
        public PolicyAPIHandlers APIHandlers { get; private set; }

        internal PolicyManager(EngineCore core)
        {
            _core = core;

            try
            {
                QueryHandlers = new PolicyQueryHandlers(core);
                APIHandlers = new PolicyAPIHandlers(core);
            }
            catch (Exception ex)
            {
                LogManager.Error("Failed to instantiate PolicyManager.", ex);
                throw;
            }
        }

        internal PhysicalPolicyCatalog AcquirePolicyCatalog(Transaction transaction,
            PhysicalSchema physicalSchema, LockOperation intendedOperation)
        {
            try
            {
                var policyCatalog = _core.IO.GetJson<PhysicalPolicyCatalog>(
                    transaction, physicalSchema.PolicyCatalogFileFilePath(), intendedOperation);
                return policyCatalog;
            }
            catch (Exception ex)
            {
                LogManager.Error($"Failed to acquire policy catalog for process id {transaction.ProcessId}.", ex);
                throw;
            }
        }

        internal void CreateAccount(Transaction transaction, string accountName, string passwordHash)
        {
            try
            {
                _core.Query.InternalExecuteNonQuery(transaction.Session, EmbeddedScripts.Load("CreateAccount.kbs"),
                    new
                    {
                        Id = Guid.NewGuid(),
                        AccountName = accountName,
                        PasswordHash = passwordHash
                    });
            }
            catch (Exception ex)
            {
                LogManager.Error($"Failed to create account for process id {transaction.ProcessId}.", ex);
                throw;
            }
        }


        internal void CreateRole(Transaction transaction, string accountName)
        {
            try
            {

            }
            catch (Exception ex)
            {
                LogManager.Error($"Failed to create role for process id {transaction.ProcessId}.", ex);
                throw;
            }
        }
    }
}