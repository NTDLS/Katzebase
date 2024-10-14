using NTDLS.Helpers;
using NTDLS.Katzebase.Api.Exceptions;
using NTDLS.Katzebase.Api.Payloads.Response;
using NTDLS.Katzebase.Engine.Atomicity;
using NTDLS.Katzebase.Engine.Interactions.APIHandlers;
using NTDLS.Katzebase.Engine.Interactions.QueryProcessors;
using NTDLS.Katzebase.Engine.Scripts;
using NTDLS.Katzebase.Engine.Security;
using NTDLS.Katzebase.PersistentTypes.Policy;
using NTDLS.Semaphore;
using System.Data;
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

        readonly OptimisticCriticalResource<Dictionary<string, Dictionary<SecurityPolicyPermission, AccountPolicyDescriptor>>> _schemaPolicyCache = new();

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

        /// <summary>
        /// Creates an account.
        /// </summary>
        internal KbQueryResultCollection CreateAccount(Transaction transaction, string username, string passwordHash)
        {
            try
            {
                var result = _core.Query.ExecuteNonQuery(transaction.Session, EmbeddedScripts.Load("CreateAccount.kbs"),
                    new
                    {
                        Id = Guid.NewGuid(),
                        UserName = username,
                        PasswordHash = passwordHash
                    });

                _schemaPolicyCache.Write(o => o.Clear());
                return result;
            }
            catch (Exception ex)
            {
                LogManager.Error($"{new StackFrame(1).GetMethod()} failed for process: [{transaction.ProcessId}].", ex);
                throw;
            }
        }

        /// <summary>
        /// Deletes an account.
        /// </summary>
        internal KbQueryResultCollection DropAccount(Transaction transaction, string username)
        {
            try
            {
                var result = _core.Query.ExecuteNonQuery(transaction.Session, EmbeddedScripts.Load("DropAccount.kbs"),
                    new
                    {
                        Id = Guid.NewGuid(),
                        UserName = username
                    });

                _schemaPolicyCache.Write(o => o.Clear());
                return result;
            }
            catch (Exception ex)
            {
                LogManager.Error($"{new StackFrame(1).GetMethod()} failed for process: [{transaction.ProcessId}].", ex);
                throw;
            }
        }

        #endregion

        #region Create/Drop role.

        /// <summary>
        /// Creates a role.
        /// </summary>
        internal KbQueryResultCollection CreateRole(Transaction transaction, string roleName, bool isAdministrator)
        {
            try
            {
                var result = _core.Query.ExecuteNonQuery(transaction.Session, EmbeddedScripts.Load("CreateRole.kbs"),
                    new
                    {
                        Id = Guid.NewGuid(),
                        Name = roleName,
                        IsAdministrator = isAdministrator
                    });

                _schemaPolicyCache.Write(o => o.Clear());
                return result;
            }
            catch (Exception ex)
            {
                LogManager.Error($"{new StackFrame(1).GetMethod()} failed for process: [{transaction.ProcessId}].", ex);
                throw;
            }
        }

        /// <summary>
        /// Deletes a role.
        /// </summary>
        internal KbQueryResultCollection DropRole(Transaction transaction, string roleName)
        {
            try
            {
                var result = _core.Query.ExecuteNonQuery(transaction.Session, EmbeddedScripts.Load("DropRole.kbs"),
                    new
                    {
                        Id = Guid.NewGuid(),
                        Name = roleName
                    });

                _schemaPolicyCache.Write(o => o.Clear());
                return result;
            }
            catch (Exception ex)
            {
                LogManager.Error($"{new StackFrame(1).GetMethod()} failed for process: [{transaction.ProcessId}].", ex);
                throw;
            }
        }

        #endregion

        #region Add/Remove role memberhship.

        /// <summary>
        /// Adds an account to a role.
        /// </summary>
        internal KbQueryResultCollection AddAccountToRole(Transaction transaction, string roleName, string username)
        {
            try
            {
                var result = _core.Query.ExecuteNonQuery(transaction.Session, EmbeddedScripts.Load("AddUserToRole.kbs"),
                    new
                    {
                        Id = Guid.NewGuid(),
                        RoleName = roleName,
                        Username = username
                    });

                _schemaPolicyCache.Write(o => o.Clear());
                return result;
            }
            catch (Exception ex)
            {
                LogManager.Error($"{new StackFrame(1).GetMethod()} failed for process: [{transaction.ProcessId}].", ex);
                throw;
            }
        }

        /// <summary>
        /// Removes an account from a role.
        /// </summary>
        internal KbQueryResultCollection RemoveAccountFromRole(Transaction transaction, string roleName, string username)
        {
            try
            {
                var result = _core.Query.ExecuteNonQuery(transaction.Session, EmbeddedScripts.Load("RemoveUserFromRole.kbs"),
                    new
                    {
                        Id = Guid.NewGuid(),
                        RoleName = roleName,
                        Username = username
                    });

                _schemaPolicyCache.Write(o => o.Clear());
                return result;
            }
            catch (Exception ex)
            {
                LogManager.Error($"{new StackFrame(1).GetMethod()} failed for process: [{transaction.ProcessId}].", ex);
                throw;
            }
        }

        #endregion

        #region Grant/Deny/Revoke on schema.

        /// <summary>
        /// Grants a permission to a role on a given schema.
        /// </summary>
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
                    var allPermissions = Enum.GetValues<SecurityPolicyPermission>().Where(o => o != SecurityPolicyPermission.All);
                    foreach (var allPermission in allPermissions)
                    {
                        policyCatalog.Add(new PhysicalPolicy
                        {
                            Rule = SecurityPolicyRule.Grant,
                            Permission = allPermission,
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

                _schemaPolicyCache.Write(o => o.Clear());
                return new KbQueryResult();
            }
            catch (Exception ex)
            {
                LogManager.Error($"{new StackFrame(1).GetMethod()} failed for process: [{transaction.ProcessId}].", ex);
                throw;
            }
        }

        /// <summary>
        /// Denies a permission to a role on a given schema.
        /// </summary>
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
                    var allPermissions = Enum.GetValues<SecurityPolicyPermission>().Where(o => o != SecurityPolicyPermission.All);
                    foreach (var allPermission in allPermissions)
                    {
                        policyCatalog.Add(new PhysicalPolicy
                        {
                            Rule = SecurityPolicyRule.Deny,
                            Permission = allPermission,
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

                _schemaPolicyCache.Write(o => o.Clear());
                return new KbQueryResult();
            }
            catch (Exception ex)
            {
                LogManager.Error($"{new StackFrame(1).GetMethod()} failed for process: [{transaction.ProcessId}].", ex);
                throw;
            }
        }

        /// <summary>
        /// Removes explicit permission from a role on a given schema.
        /// </summary>
        internal KbQueryResult RevokeRole(Transaction transaction, string schemaName, Guid roleId, SecurityPolicyPermission permission)
        {
            try
            {
                var physicalSchema = _core.Schemas.Acquire(transaction, schemaName, LockOperation.Write);
                var policyCatalog = _core.IO.GetJson<PhysicalPolicyCatalog>(transaction, physicalSchema.PolicyCatalogFileFilePath(), LockOperation.Write);

                policyCatalog.Collection.RemoveAll(o => o.RoleId == roleId
                    && (o.Permission == permission || permission == SecurityPolicyPermission.All));

                _core.IO.PutJson(transaction, physicalSchema.PolicyCatalogFileFilePath(), policyCatalog);

                _schemaPolicyCache.Write(o => o.Clear());
                return new KbQueryResult();
            }
            catch (Exception ex)
            {
                LogManager.Error($"{new StackFrame(1).GetMethod()} failed for process: [{transaction.ProcessId}].", ex);
                throw;
            }
        }

        #endregion


        /// <summary>
        /// Throws an exception if the current user does not hold the specified permission on the given schema.
        /// </summary>
        internal void EnforceSchemaPolicy(Transaction transaction, string schemaName, SecurityPolicyPermission requiredPermission)
        {
            var heldPermissions = GetCurrentAccountSchemaPermission(transaction, schemaName);
            if (!heldPermissions.Any(o => o.Value.Permission == requiredPermission && o.Value.Rule == SecurityPolicyRule.Grant))
            {
                throw new KbPermissionNotHeld($"Permission not held: [{requiredPermission}] on [{schemaName}]");
            }
        }

        /// <summary>
        /// Throws an exception if the current user is not a member of an administrators role.
        /// </summary>
        internal void EnforceAdministratorPolicy(Transaction transaction)
        {
            if (!transaction.Session.Roles.Any(o => o.IsAdministrator))
            {
                throw new KbPermissionNotHeld($"Permission not held: [Administrator]");
            }
        }

        /// <summary>
        /// Returns the realized security policy on the given schema foe the current account.
        /// This result is cached.
        /// </summary>
        internal Dictionary<SecurityPolicyPermission, AccountPolicyDescriptor> GetCurrentAccountSchemaPermission(Transaction transaction, string schemaName)
        {
            try
            {
                string cacheKey = $"[{transaction.Session.Username.ToLowerInvariant()}]:[{schemaName.ToLowerInvariant()}]";

                return _schemaPolicyCache.UpgradableRead(readCache =>
                {
                    if (readCache.TryGetValue(cacheKey, out var cachedPolicy))
                    {
                        return cachedPolicy;
                    }

                    //Nothing cached for this schema, walk the tree and determine the applicable permissions.
                    return _schemaPolicyCache.Write(writeCache =>
                    {
                        var applicablePolicies = new Dictionary<SecurityPolicyPermission, AccountPolicyDescriptor>();

                        //Add all un-set permissions to the result.
                        var allPermissions = Enum.GetValues<SecurityPolicyPermission>().Where(o => o != SecurityPolicyPermission.All);
                        foreach (var allPermission in allPermissions)
                        {
                            applicablePolicies.Add(allPermission, new AccountPolicyDescriptor()
                            {
                                Permission = allPermission,
                                Rule = SecurityPolicyRule.Deny
                            });
                        }

                        var administratorRole = transaction.Session.Roles.FirstOrDefault(o => o.IsAdministrator);
                        if (administratorRole != null)
                        {
                            //Administrators have all permissions.
                            foreach (var applicablePolicy in applicablePolicies)
                            {
                                var policy = applicablePolicies[applicablePolicy.Key];
                                policy.Rule = SecurityPolicyRule.Grant;
                                policy.InheritedFromRole = administratorRole.Name;
                                policy.IsSet = true;
                            }

                            var adminCachePolicy = applicablePolicies.ToDictionary(o => o.Key, o => o.Value.EnsureNotNull());
                            writeCache[cacheKey] = adminCachePolicy;
                            return adminCachePolicy;
                        }

                        var userRoleIds = transaction.Session.Roles.Select(o => o.Id);

                        var physicalSchema = _core.Schemas.Acquire(transaction, schemaName, LockOperation.Stability);
                        var givenSchemaId = physicalSchema.Id;

                        while (true)
                        {
                            var policyCatalog = _core.IO.GetJson<PhysicalPolicyCatalog>(transaction, physicalSchema.PolicyCatalogFileFilePath(), LockOperation.Read);

                            var schemaRoles = policyCatalog.Collection.Where(o => userRoleIds.Contains(o.RoleId)).ToList();

                            if (schemaRoles.Count > 0)
                            {
                                //Loop though all policies that have yet to be defined.
                                foreach (var applicablePolicy in applicablePolicies.Where(o => o.Value.IsSet == false))
                                {
                                    //Get the security policies for the schema that matches the policy that we have yet to define.
                                    //We only look for recursive policies, unless the schema is the one that was passed in.
                                    var schemaPoliciesForUndefinedPermission = schemaRoles
                                        .Where(o => o.Permission == applicablePolicy.Key && (o.IsRecursive || physicalSchema.Id == givenSchemaId));

                                    var deniedByRole = schemaPoliciesForUndefinedPermission.FirstOrDefault(o => o.Rule == SecurityPolicyRule.Deny);
                                    var grantedByRole = schemaPoliciesForUndefinedPermission.FirstOrDefault(o => o.Rule == SecurityPolicyRule.Grant);

                                    if (deniedByRole != null)
                                    {
                                        //If there are any deny policies, then they take precedent.
                                        var policy = applicablePolicies[applicablePolicy.Key];
                                        policy.Rule = SecurityPolicyRule.Deny;
                                        policy.InheritedFromRole = transaction.Session.Roles.First(o => o.Id == deniedByRole.RoleId).Name;
                                        policy.InheritedFromSchema = physicalSchema.VirtualPath;
                                        policy.IsSet = true;
                                    }
                                    else if (grantedByRole != null)
                                    {
                                        //If there ary any grant policies, then use that rule.
                                        var policy = applicablePolicies[applicablePolicy.Key];
                                        policy.Rule = SecurityPolicyRule.Grant;
                                        policy.InheritedFromRole = transaction.Session.Roles.First(o => o.Id == grantedByRole.RoleId).Name;
                                        policy.InheritedFromSchema = physicalSchema.VirtualPath;
                                        policy.IsSet = true;
                                    }
                                    else
                                    {
                                        //Policy for permission is still undefined.
                                    }
                                }

                                if (applicablePolicies.All(o => o.Value.IsSet))
                                {
                                    //If all policies have been defined, then there is no reason to continue.
                                }
                            }

                            if (physicalSchema == _core.Schemas.RootPhysicalSchema)
                            {
                                //When we reach the root schema, we are done.
                                break;
                            }

                            physicalSchema = _core.Schemas.AcquireParent(transaction, physicalSchema, LockOperation.Stability);
                        }

                        foreach (var applicablePolicy in applicablePolicies.Where(o => o.Value == null))
                        {
                            //If there were any policies left undefined, then default to deny.
                            var policy = applicablePolicies[applicablePolicy.Key];
                            policy.Rule = SecurityPolicyRule.Deny;
                            policy.IsSet = true;
                        }

                        var resulingPolicy = applicablePolicies.ToDictionary(o => o.Key, o => o.Value.EnsureNotNull());
                        writeCache[cacheKey] = resulingPolicy;
                        return resulingPolicy;
                    });
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
