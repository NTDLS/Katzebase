using NTDLS.Katzebase.Api.Payloads.Response;
using NTDLS.Katzebase.Engine.Atomicity;
using NTDLS.Katzebase.Engine.Interactions.APIHandlers;
using NTDLS.Katzebase.Engine.Interactions.QueryProcessors;
using NTDLS.Katzebase.Engine.Scripts;
using NTDLS.Katzebase.PersistentTypes.Policy;
using System.Diagnostics;
using static NTDLS.Katzebase.Shared.EngineConstants;

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

        #region Create/Drop account.


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

        internal KbQueryResultCollection DropAccount(Transaction transaction, string username)
        {
            try
            {
                return _core.Query.ExecuteNonQuery(transaction.Session, EmbeddedScripts.Load("DropAccount.kbs"),
                    new
                    {
                        Id = Guid.NewGuid(),
                        UserName = username
                    });
            }
            catch (Exception ex)
            {
                LogManager.Error($"{new StackFrame(1).GetMethod()} failed for process: [{transaction.ProcessId}].", ex);
                throw;
            }
        }

        #endregion

        #region Create/Drop role.

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

        internal KbQueryResultCollection DropRole(Transaction transaction, string roleName)
        {
            try
            {
                return _core.Query.ExecuteNonQuery(transaction.Session, EmbeddedScripts.Load("DropRole.kbs"),
                    new
                    {
                        Id = Guid.NewGuid(),
                        Name = roleName
                    });
            }
            catch (Exception ex)
            {
                LogManager.Error($"{new StackFrame(1).GetMethod()} failed for process: [{transaction.ProcessId}].", ex);
                throw;
            }
        }

        #endregion

        #region Add/Remove role memberhship.

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

        #endregion

        #region Grant/Deny/Revoke on schema.

        internal KbQueryResult GrantRole(Transaction transaction, string schemaName, Guid roleId, SecurityPolicyPermission permission, bool isRecursive)
        {
            try
            {
                var physicalSchema = _core.Schemas.Acquire(transaction, schemaName, LockOperation.Write);
                var policyCatalog = _core.IO.GetJson<PhysicalPolicyCatalog>(transaction, physicalSchema.PolicyCatalogFileFilePath(), LockOperation.Write);

                //First revoke the specified policy types.
                policyCatalog.Collection.RemoveAll(o => o.RoleId == roleId
                    && (o.Permission == permission || permission == SecurityPolicyPermission.All));

                if (permission == SecurityPolicyPermission.All)
                {
                    var allSecurityPolicyTypes = Enum.GetValues<SecurityPolicyPermission>().Where(o => o != SecurityPolicyPermission.All);

                    foreach (var policyType in allSecurityPolicyTypes)
                    {
                        policyCatalog.Add(new PhysicalPolicy
                        {
                            Rule = SecurityPolicyRule.Grant,
                            Permission = permission,
                            RoleId = roleId,
                            IsRecursive = isRecursive
                        });
                    }
                }
                else
                {
                    policyCatalog.Add(new PhysicalPolicy
                    {
                        Rule = SecurityPolicyRule.Grant,
                        Permission = permission,
                        RoleId = roleId,
                        IsRecursive = isRecursive
                    });
                }

                _core.IO.PutJson(transaction, physicalSchema.PolicyCatalogFileFilePath(), policyCatalog);

                return new KbQueryResult();
            }
            catch (Exception ex)
            {
                LogManager.Error($"{new StackFrame(1).GetMethod()} failed for process: [{transaction.ProcessId}].", ex);
                throw;
            }
        }

        internal KbQueryResult DenyRole(Transaction transaction, string schemaName, Guid roleId, SecurityPolicyPermission permission, bool isRecursive)
        {
            try
            {
                var physicalSchema = _core.Schemas.Acquire(transaction, schemaName, LockOperation.Write);
                var policyCatalog = _core.IO.GetJson<PhysicalPolicyCatalog>(transaction, physicalSchema.PolicyCatalogFileFilePath(), LockOperation.Write);

                //First revoke the specified policy types.
                policyCatalog.Collection.RemoveAll(o => o.RoleId == roleId
                    && (o.Permission == permission || permission == SecurityPolicyPermission.All));

                if (permission == SecurityPolicyPermission.All)
                {
                    var allSecurityPolicyTypes = Enum.GetValues<SecurityPolicyPermission>().Where(o => o != SecurityPolicyPermission.All);

                    foreach (var policyType in allSecurityPolicyTypes)
                    {
                        policyCatalog.Add(new PhysicalPolicy
                        {
                            Rule = SecurityPolicyRule.Deny,
                            Permission = permission,
                            RoleId = roleId,
                            IsRecursive = isRecursive
                        });
                    }
                }
                else
                {
                    policyCatalog.Add(new PhysicalPolicy
                    {
                        Rule = SecurityPolicyRule.Deny,
                        Permission = permission,
                        RoleId = roleId,
                        IsRecursive = isRecursive
                    });
                }

                _core.IO.PutJson(transaction, physicalSchema.PolicyCatalogFileFilePath(), policyCatalog);

                return new KbQueryResult();
            }
            catch (Exception ex)
            {
                LogManager.Error($"{new StackFrame(1).GetMethod()} failed for process: [{transaction.ProcessId}].", ex);
                throw;
            }
        }

        internal KbQueryResult RevokeRole(Transaction transaction, string schemaName, Guid roleId, SecurityPolicyPermission permission)
        {
            try
            {
                var physicalSchema = _core.Schemas.Acquire(transaction, schemaName, LockOperation.Write);
                var policyCatalog = _core.IO.GetJson<PhysicalPolicyCatalog>(transaction, physicalSchema.PolicyCatalogFileFilePath(), LockOperation.Write);

                policyCatalog.Collection.RemoveAll(o => o.RoleId == roleId
                    && (o.Permission == permission || permission == SecurityPolicyPermission.All));

                _core.IO.PutJson(transaction, physicalSchema.PolicyCatalogFileFilePath(), policyCatalog);

                return new KbQueryResult();
            }
            catch (Exception ex)
            {
                LogManager.Error($"{new StackFrame(1).GetMethod()} failed for process: [{transaction.ProcessId}].", ex);
                throw;
            }
        }

        #endregion
    }
}