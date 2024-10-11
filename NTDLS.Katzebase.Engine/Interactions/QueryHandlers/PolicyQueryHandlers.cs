using NTDLS.Katzebase.Api.Payloads.Response;
using NTDLS.Katzebase.Engine.Sessions;
using NTDLS.Katzebase.Parsers.Query.SupportingTypes;

namespace NTDLS.Katzebase.Engine.Interactions.QueryHandlers
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
                Management.LogManager.Error($"Failed to instantiate schema query handler.", ex);
                throw;
            }
        }

        internal KbActionResponse ExecuteCreateAccount(SessionState session, Query query)
        {
            try
            {
                using var transactionReference = _core.Transactions.APIAcquire(session);

                var accountName = query.GetAttribute<string>(Query.Attribute.Username);
                var passwordHash = query.GetAttribute<string>(Query.Attribute.PasswordHash);

                var results = _core.Policies.CreateAccount(transactionReference.Transaction, accountName, passwordHash);
                return transactionReference.CommitAndApplyMetricsNonQuery(results);
            }
            catch (Exception ex)
            {
                Management.LogManager.Error($"Failed to execute account create for process id {session.ProcessId}.", ex);
                throw;
            }
        }

        internal KbActionResponse ExecuteCreateRole(SessionState session, Query query)
        {
            try
            {
                using var transactionReference = _core.Transactions.APIAcquire(session);

                var roleName = query.GetAttribute<string>(Query.Attribute.RoleName);
                var IsAdministrator = query.GetAttribute(Query.Attribute.IsAdministrator, false);

                var results = _core.Policies.CreateRole(transactionReference.Transaction, roleName, IsAdministrator);
                return transactionReference.CommitAndApplyMetricsNonQuery(results);
            }
            catch (Exception ex)
            {
                Management.LogManager.Error($"Failed to execute role create for process id {session.ProcessId}.", ex);
                throw;
            }
        }

        internal KbActionResponse ExecuteAddUserToRole(SessionState session, Query query)
        {
            try
            {
                using var transactionReference = _core.Transactions.APIAcquire(session);

                var roleName = query.GetAttribute<string>(Query.Attribute.RoleName);
                var username = query.GetAttribute<string>(Query.Attribute.Username);

                var results = _core.Policies.AddUserToRole(transactionReference.Transaction, roleName, username);
                return transactionReference.CommitAndApplyMetricsNonQuery(results);
            }
            catch (Exception ex)
            {
                Management.LogManager.Error($"Failed to execute add user to role for process id {session.ProcessId}.", ex);
                throw;
            }
        }

        internal KbActionResponse ExecuteRemoveUserFromRole(SessionState session, Query query)
        {
            try
            {
                using var transactionReference = _core.Transactions.APIAcquire(session);

                var roleName = query.GetAttribute<string>(Query.Attribute.RoleName);
                var username = query.GetAttribute<string>(Query.Attribute.Username);

                var results = _core.Policies.RemoveUserFromRole(transactionReference.Transaction, roleName, username);
                return transactionReference.CommitAndApplyMetricsNonQuery(results);
            }
            catch (Exception ex)
            {
                Management.LogManager.Error($"Failed to execute add user to role for process id {session.ProcessId}.", ex);
                throw;
            }
        }
    }
}
