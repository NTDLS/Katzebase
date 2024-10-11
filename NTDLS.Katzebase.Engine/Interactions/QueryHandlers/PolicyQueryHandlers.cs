using NTDLS.Katzebase.Api.Exceptions;
using NTDLS.Katzebase.Api.Payloads.Response;
using NTDLS.Katzebase.Engine.Sessions;
using NTDLS.Katzebase.Parsers.Query.SupportingTypes;
using static NTDLS.Katzebase.Parsers.Constants;

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

                if (query.SubQueryType == SubQueryType.Account)
                {
                    var accountName = query.GetAttribute<string>(Query.Attribute.Username);
                    var passwordHash = query.GetAttribute<string>(Query.Attribute.PasswordHash);
                    _core.Policies.CreateAccount(transactionReference.Transaction, accountName, passwordHash);
                }
                else
                {
                    throw new KbNotImplementedException();
                }

                return transactionReference.CommitAndApplyMetricsThenReturnResults();
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

                if (query.SubQueryType == SubQueryType.Role)
                {
                    var roleName = query.GetAttribute<string>(Query.Attribute.RoleName);
                    var IsAdministrator = query.GetAttribute(Query.Attribute.IsAdministrator, false);
                    _core.Policies.CreateRole(transactionReference.Transaction, roleName, IsAdministrator);
                }
                else
                {
                    throw new KbNotImplementedException();
                }

                return transactionReference.CommitAndApplyMetricsThenReturnResults();
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

                if (query.SubQueryType == SubQueryType.AddUserToRole)
                {
                    var roleName = query.GetAttribute<string>(Query.Attribute.RoleName);
                    var username = query.GetAttribute<string>(Query.Attribute.Username);
                    _core.Policies.AddUserToRole(transactionReference.Transaction, roleName, username);
                }
                else
                {
                    throw new KbNotImplementedException();
                }

                return transactionReference.CommitAndApplyMetricsThenReturnResults();
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

                if (query.SubQueryType == SubQueryType.RemoveUserFromRole)
                {
                    var roleName = query.GetAttribute<string>(Query.Attribute.RoleName);
                    var username = query.GetAttribute<string>(Query.Attribute.Username);
                    _core.Policies.RemoveUserFromRole(transactionReference.Transaction, roleName, username);
                }
                else
                {
                    throw new KbNotImplementedException();
                }

                return transactionReference.CommitAndApplyMetricsThenReturnResults();
            }
            catch (Exception ex)
            {
                Management.LogManager.Error($"Failed to execute add user to role for process id {session.ProcessId}.", ex);
                throw;
            }
        }
    }
}
