using NTDLS.Katzebase.Api.Payloads.Response;
using NTDLS.Katzebase.Engine.Atomicity;
using NTDLS.Katzebase.Engine.Interactions.APIHandlers;
using NTDLS.Katzebase.Engine.Interactions.QueryProcessors;
using NTDLS.Katzebase.Engine.Scripts;
using System.Diagnostics;

namespace NTDLS.Katzebase.Engine.Interactions.Management
{
    /// <summary>
    /// Public core class methods for locking, reading, writing and managing tasks related to users, roles, membership and policies.
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

        internal KbQueryResultCollection CreateAccount(Transaction transaction, string username, string passwordHash)
        {
            try
            {
                return _core.Query.ExecuteNonQuery(transaction.Session, EmbeddedScripts.Load("CreateAccount.kbs"),
                    new
                    {
                        Id = Guid.NewGuid(),
                        UserName = username,
                        PasswordHash = passwordHash
                    });
            }
            catch (Exception ex)
            {
                LogManager.Error($"{new StackFrame(1).GetMethod()} failed for process: [{transaction.ProcessId}].", ex);
                throw;
            }
        }

        internal KbQueryResultCollection CreateRole(Transaction transaction, string roleName, bool isAdministrator)
        {
            try
            {
                return _core.Query.ExecuteNonQuery(transaction.Session, EmbeddedScripts.Load("CreateRole.kbs"),
                    new
                    {
                        Id = Guid.NewGuid(),
                        Name = roleName,
                        IsAdministrator = isAdministrator
                    });
            }
            catch (Exception ex)
            {
                LogManager.Error($"{new StackFrame(1).GetMethod()} failed for process: [{transaction.ProcessId}].", ex);
                throw;
            }
        }

        internal KbQueryResultCollection AddUserToRole(Transaction transaction, string roleName, string username)
        {
            try
            {
                return _core.Query.ExecuteNonQuery(transaction.Session, EmbeddedScripts.Load("AddUserToRole.kbs"),
                    new
                    {
                        Id = Guid.NewGuid(),
                        RoleName = roleName,
                        Username = username
                    });
            }
            catch (Exception ex)
            {
                LogManager.Error($"{new StackFrame(1).GetMethod()} failed for process: [{transaction.ProcessId}].", ex);
                throw;
            }
        }
        internal KbQueryResultCollection RemoveUserFromRole(Transaction transaction, string roleName, string username)
        {
            try
            {
                return _core.Query.ExecuteNonQuery(transaction.Session, EmbeddedScripts.Load("RemoveUserFromRole.kbs"),
                    new
                    {
                        Id = Guid.NewGuid(),
                        RoleName = roleName,
                        Username = username
                    });
            }
            catch (Exception ex)
            {
                LogManager.Error($"{new StackFrame(1).GetMethod()} failed for process: [{transaction.ProcessId}].", ex);
                throw;
            }

        }
    }
}