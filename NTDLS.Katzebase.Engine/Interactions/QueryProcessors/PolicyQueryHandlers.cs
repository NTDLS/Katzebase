using NTDLS.Helpers;
using NTDLS.Katzebase.Api.Exceptions;
using NTDLS.Katzebase.Api.Payloads.Response;
using NTDLS.Katzebase.Engine.Interactions.Management;
using NTDLS.Katzebase.Engine.Scripts;
using NTDLS.Katzebase.Engine.Sessions;
using NTDLS.Katzebase.Parsers.Query.SupportingTypes;
using System.Diagnostics;
using static NTDLS.Katzebase.Shared.EngineConstants;

namespace NTDLS.Katzebase.Engine.Interactions.QueryProcessors
{
    /// <summary>
    /// Internal class methods for handling query requests related to policies.
    /// </summary>
    internal class PolicyQueryHandlers
    {
        private readonly EngineCore _core;

        public PolicyQueryHandlers(EngineCore core)
        {
            _core = core;

            try
            {
            }
            catch (Exception ex)
            {
                LogManager.Error($"Failed to instantiate schema query handler.", ex);
                throw;
            }
        }

        #region Grant/Deny/Revoke on schema.

        /// <summary>
        /// Grants a permission to a role on a given schema.
        /// </summary>
        internal KbActionResponse ExecuteGrant(SessionState session, PreparedQuery query)
        {
            try
            {
                using var transactionReference = _core.Transactions.APIAcquire(session);
                var schemaName = query.Schemas.Single().Name;
                var roleName = query.GetAttribute<string>(PreparedQuery.Attribute.RoleName);
                var permission = query.GetAttribute<SecurityPolicyPermission>(PreparedQuery.Attribute.SecurityPolicyPermission);
                var isRecursive = query.GetAttribute(PreparedQuery.Attribute.Recursive, false);

                using var systemSession = _core.Sessions.CreateEphemeralSystemSession();
                var roleId = _core.Query.ExecuteScalar<Guid?>(systemSession.Session, EmbeddedScripts.Load("GetRoleId.kbs"), new
                {
                    Name = roleName
                });
                systemSession.Commit();

                if (roleId == null)
                {
                    throw new KbObjectNotFoundException($"Role not found: [{roleName}].");
                }

                var results = _core.Policy.GrantRole(transactionReference.Transaction, schemaName, roleId.EnsureNotNull(), permission, isRecursive);
                return transactionReference.CommitAndApplyMetricsNonQuery(results);
            }
            catch (Exception ex)
            {
                LogManager.Error($"{new StackFrame(1).GetMethod()} failed for process: [{session.ProcessId}].", ex);
                throw;
            }
        }

        /// <summary>
        /// Denies a permission to a role on a given schema.
        /// </summary>
        internal KbActionResponse ExecuteDeny(SessionState session, PreparedQuery query)
        {
            try
            {
                using var transactionReference = _core.Transactions.APIAcquire(session);
                var schemaName = query.Schemas.Single().Name;
                var roleName = query.GetAttribute<string>(PreparedQuery.Attribute.RoleName);
                var permission = query.GetAttribute<SecurityPolicyPermission>(PreparedQuery.Attribute.SecurityPolicyPermission);
                var isRecursive = query.GetAttribute(PreparedQuery.Attribute.Recursive, false);

                using var systemSession = _core.Sessions.CreateEphemeralSystemSession();
                var roleId = _core.Query.ExecuteScalar<Guid?>(systemSession.Session, EmbeddedScripts.Load("GetRoleId.kbs"), new
                {
                    Name = roleName
                });
                systemSession.Commit();

                if (roleId == null)
                {
                    throw new KbObjectNotFoundException($"Role not found: [{roleName}].");
                }

                var results = _core.Policy.DenyRole(transactionReference.Transaction, schemaName, roleId.EnsureNotNull(), permission, isRecursive);
                return transactionReference.CommitAndApplyMetricsNonQuery(results);
            }
            catch (Exception ex)
            {
                LogManager.Error($"{new StackFrame(1).GetMethod()} failed for process: [{session.ProcessId}].", ex);
                throw;
            }
        }

        /// <summary>
        /// Removes explicit permission from a role on a given schema.
        /// </summary>
        internal KbActionResponse ExecuteRevoke(SessionState session, PreparedQuery query)
        {
            try
            {
                using var transactionReference = _core.Transactions.APIAcquire(session);
                var schemaName = query.Schemas.Single().Name;
                var roleName = query.GetAttribute<string>(PreparedQuery.Attribute.RoleName);
                var permission = query.GetAttribute<SecurityPolicyPermission>(PreparedQuery.Attribute.SecurityPolicyPermission);

                using var systemSession = _core.Sessions.CreateEphemeralSystemSession();
                var roleId = _core.Query.ExecuteScalar<Guid?>(systemSession.Session, EmbeddedScripts.Load("GetRoleId.kbs"), new
                {
                    Name = roleName
                });
                systemSession.Commit();

                if (roleId == null)
                {
                    throw new KbObjectNotFoundException($"Role not found: [{roleName}].");
                }

                var results = _core.Policy.RevokeRole(transactionReference.Transaction, schemaName, roleId.EnsureNotNull(), permission);
                return transactionReference.CommitAndApplyMetricsNonQuery(results);
            }
            catch (Exception ex)
            {
                LogManager.Error($"{new StackFrame(1).GetMethod()} failed for process: [{session.ProcessId}].", ex);
                throw;
            }
        }

        #endregion

        #region Create/Drop role.

        /// <summary>
        /// Deletes a role.
        /// </summary>
        internal KbActionResponse ExecuteDropRole(SessionState session, PreparedQuery query)
        {
            try
            {
                using var transactionReference = _core.Transactions.APIAcquire(session);
                var roleName = query.GetAttribute<string>(PreparedQuery.Attribute.RoleName);

                var results = _core.Policy.DropRole(transactionReference.Transaction, roleName);
                return transactionReference.CommitAndApplyMetricsNonQuery(results);
            }
            catch (Exception ex)
            {
                LogManager.Error($"{new StackFrame(1).GetMethod()} failed for process: [{session.ProcessId}].", ex);
                throw;
            }
        }

        /// <summary>
        /// Creates a role.
        /// </summary>
        internal KbActionResponse ExecuteCreateRole(SessionState session, PreparedQuery query)
        {
            try
            {
                using var transactionReference = _core.Transactions.APIAcquire(session);
                var roleName = query.GetAttribute<string>(PreparedQuery.Attribute.RoleName);
                var IsAdministrator = query.GetAttribute(PreparedQuery.Attribute.IsAdministrator, false);

                var results = _core.Policy.CreateRole(transactionReference.Transaction, roleName, IsAdministrator);
                return transactionReference.CommitAndApplyMetricsNonQuery(results);
            }
            catch (Exception ex)
            {
                LogManager.Error($"{new StackFrame(1).GetMethod()} failed for process: [{session.ProcessId}].", ex);
                throw;
            }
        }

        #endregion

        #region Create/Drop account.

        /// <summary>
        /// Creates an account.
        /// </summary>
        internal KbActionResponse ExecuteCreateAccount(SessionState session, PreparedQuery query)
        {
            try
            {
                using var transactionReference = _core.Transactions.APIAcquire(session);

                var username = query.GetAttribute<string>(PreparedQuery.Attribute.UserName);
                var passwordHash = query.GetAttribute<string>(PreparedQuery.Attribute.PasswordHash);

                var results = _core.Policy.CreateAccount(transactionReference.Transaction, username, passwordHash);
                return transactionReference.CommitAndApplyMetricsNonQuery(results);
            }
            catch (Exception ex)
            {
                LogManager.Error($"{new StackFrame(1).GetMethod()} failed for process: [{session.ProcessId}].", ex);
                throw;
            }
        }

        /// <summary>
        /// Deletes an account.
        /// </summary>
        internal KbActionResponse ExecuteDropAccount(SessionState session, PreparedQuery query)
        {
            try
            {
                using var transactionReference = _core.Transactions.APIAcquire(session);

                var username = query.GetAttribute<string>(PreparedQuery.Attribute.UserName);

                var results = _core.Policy.DropAccount(transactionReference.Transaction, username);
                return transactionReference.CommitAndApplyMetricsNonQuery(results);
            }
            catch (Exception ex)
            {
                LogManager.Error($"{new StackFrame(1).GetMethod()} failed for process: [{session.ProcessId}].", ex);
                throw;
            }
        }

        #endregion

        #region Add/Remove role memberhship.

        /// <summary>
        /// Adds an account to a role.
        /// </summary>
        internal KbActionResponse ExecuteAddAccountToRole(SessionState session, PreparedQuery query)
        {
            try
            {
                using var transactionReference = _core.Transactions.APIAcquire(session);
                var roleName = query.GetAttribute<string>(PreparedQuery.Attribute.RoleName);
                var username = query.GetAttribute<string>(PreparedQuery.Attribute.UserName);

                var results = _core.Policy.AddAccountToRole(transactionReference.Transaction, roleName, username);
                return transactionReference.CommitAndApplyMetricsNonQuery(results);
            }
            catch (Exception ex)
            {
                LogManager.Error($"{new StackFrame(1).GetMethod()} failed for process: [{session.ProcessId}].", ex);
                throw;
            }
        }

        /// <summary>
        /// Removes an account from a role.
        /// </summary>
        internal KbActionResponse ExecuteRemoveAccountFromRole(SessionState session, PreparedQuery query)
        {
            try
            {
                using var transactionReference = _core.Transactions.APIAcquire(session);
                var roleName = query.GetAttribute<string>(PreparedQuery.Attribute.RoleName);
                var username = query.GetAttribute<string>(PreparedQuery.Attribute.UserName);

                var results = _core.Policy.RemoveAccountFromRole(transactionReference.Transaction, roleName, username);
                return transactionReference.CommitAndApplyMetricsNonQuery(results);
            }
            catch (Exception ex)
            {
                LogManager.Error($"{new StackFrame(1).GetMethod()} failed for process: [{session.ProcessId}].", ex);
                throw;
            }
        }

        #endregion
    }
}
