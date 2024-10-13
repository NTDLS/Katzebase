﻿using NTDLS.Katzebase.Api.Payloads.Response;
using NTDLS.Katzebase.Engine.Interactions.Management;
using NTDLS.Katzebase.Engine.Scripts;
using NTDLS.Katzebase.Engine.Sessions;
using NTDLS.Katzebase.Parsers.Query.SupportingTypes;
using System.Diagnostics;

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

        internal KbActionResponse ExecuteGrant(SessionState session, PreparedQuery query)
        {
            try
            {
                using var transactionReference = _core.Transactions.APIAcquire(session);

                var schemaName = query.Schemas.Single().Name;
                var roleName = query.GetAttribute<string>(PreparedQuery.Attribute.RoleName);
                var isRecursive = query.GetAttribute(PreparedQuery.Attribute.Recursive, false);

                var roleId = _core.Query.ExecuteScalar<Guid>(session, EmbeddedScripts.Load("GetRoleId.kbs"), new
                {
                    Name = roleName
                });

                var results = _core.Schemas.Grant(transactionReference.Transaction, schemaName, roleId, isRecursive);
                return transactionReference.CommitAndApplyMetricsNonQuery(results);
            }
            catch (Exception ex)
            {
                LogManager.Error($"{new StackFrame(1).GetMethod()} failed for process: [{session.ProcessId}].", ex);
                throw;
            }
        }

        internal KbActionResponse ExecuteDeny(SessionState session, PreparedQuery query)
        {
            try
            {
                using var transactionReference = _core.Transactions.APIAcquire(session);

                var schemaName = query.Schemas.Single().Name;
                var roleName = query.GetAttribute<string>(PreparedQuery.Attribute.RoleName);
                var isRecursive = query.GetAttribute(PreparedQuery.Attribute.Recursive, false);

                var roleId = _core.Query.ExecuteScalar<Guid>(session, EmbeddedScripts.Load("GetRoleId.kbs"), new
                {
                    Name = roleName
                });

                var results = _core.Schemas.Deny(transactionReference.Transaction, schemaName, roleId, isRecursive);
                return transactionReference.CommitAndApplyMetricsNonQuery(results);
            }
            catch (Exception ex)
            {
                LogManager.Error($"{new StackFrame(1).GetMethod()} failed for process: [{session.ProcessId}].", ex);
                throw;
            }
        }

        internal KbActionResponse ExecuteDropRole(SessionState session, PreparedQuery query)
        {
            try
            {
                using var transactionReference = _core.Transactions.APIAcquire(session);

                var roleName = query.GetAttribute<string>(PreparedQuery.Attribute.RoleName);

                var results = _core.Policies.DropRole(transactionReference.Transaction, roleName);
                return transactionReference.CommitAndApplyMetricsNonQuery(results);
            }
            catch (Exception ex)
            {
                LogManager.Error($"{new StackFrame(1).GetMethod()} failed for process: [{session.ProcessId}].", ex);
                throw;
            }
        }

        internal KbActionResponse ExecuteDropAccount(SessionState session, PreparedQuery query)
        {
            try
            {
                using var transactionReference = _core.Transactions.APIAcquire(session);

                var username = query.GetAttribute<string>(PreparedQuery.Attribute.UserName);

                var results = _core.Policies.DropAccount(transactionReference.Transaction, username);
                return transactionReference.CommitAndApplyMetricsNonQuery(results);
            }
            catch (Exception ex)
            {
                LogManager.Error($"{new StackFrame(1).GetMethod()} failed for process: [{session.ProcessId}].", ex);
                throw;
            }
        }

        internal KbActionResponse ExecuteCreateAccount(SessionState session, PreparedQuery query)
        {
            try
            {
                using var transactionReference = _core.Transactions.APIAcquire(session);

                var username = query.GetAttribute<string>(PreparedQuery.Attribute.UserName);
                var passwordHash = query.GetAttribute<string>(PreparedQuery.Attribute.PasswordHash);

                var results = _core.Policies.CreateAccount(transactionReference.Transaction, username, passwordHash);
                return transactionReference.CommitAndApplyMetricsNonQuery(results);
            }
            catch (Exception ex)
            {
                LogManager.Error($"{new StackFrame(1).GetMethod()} failed for process: [{session.ProcessId}].", ex);
                throw;
            }
        }

        internal KbActionResponse ExecuteCreateRole(SessionState session, PreparedQuery query)
        {
            try
            {
                using var transactionReference = _core.Transactions.APIAcquire(session);

                var roleName = query.GetAttribute<string>(PreparedQuery.Attribute.RoleName);
                var IsAdministrator = query.GetAttribute(PreparedQuery.Attribute.IsAdministrator, false);

                var results = _core.Policies.CreateRole(transactionReference.Transaction, roleName, IsAdministrator);
                return transactionReference.CommitAndApplyMetricsNonQuery(results);
            }
            catch (Exception ex)
            {
                LogManager.Error($"{new StackFrame(1).GetMethod()} failed for process: [{session.ProcessId}].", ex);
                throw;
            }
        }

        internal KbActionResponse ExecuteAddUserToRole(SessionState session, PreparedQuery query)
        {
            try
            {
                using var transactionReference = _core.Transactions.APIAcquire(session);

                var roleName = query.GetAttribute<string>(PreparedQuery.Attribute.RoleName);
                var username = query.GetAttribute<string>(PreparedQuery.Attribute.UserName);

                var results = _core.Policies.AddUserToRole(transactionReference.Transaction, roleName, username);
                return transactionReference.CommitAndApplyMetricsNonQuery(results);
            }
            catch (Exception ex)
            {
                LogManager.Error($"{new StackFrame(1).GetMethod()} failed for process: [{session.ProcessId}].", ex);
                throw;
            }
        }

        internal KbActionResponse ExecuteRemoveUserFromRole(SessionState session, PreparedQuery query)
        {
            try
            {
                using var transactionReference = _core.Transactions.APIAcquire(session);

                var roleName = query.GetAttribute<string>(PreparedQuery.Attribute.RoleName);
                var username = query.GetAttribute<string>(PreparedQuery.Attribute.UserName);

                var results = _core.Policies.RemoveUserFromRole(transactionReference.Transaction, roleName, username);
                return transactionReference.CommitAndApplyMetricsNonQuery(results);
            }
            catch (Exception ex)
            {
                LogManager.Error($"{new StackFrame(1).GetMethod()} failed for process: [{session.ProcessId}].", ex);
                throw;
            }
        }
    }
}
