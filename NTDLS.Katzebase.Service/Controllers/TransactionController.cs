using NTDLS.Katzebase.Client.Payloads.Queries;

namespace NTDLS.Katzebase.Client.Service.Controllers
{
    public static class TransactionController
    {
        public static KbQueryTransactionBeginReply Begin(KbQueryTransactionBegin param)
        {
            try
            {
                var processId = Program.Core.Sessions.UpsertSessionId(param.SessionId);
                Thread.CurrentThread.Name = Thread.CurrentThread.Name = $"KbAPI:{processId}:{KbUtility.GetCurrentMethod()}";
                Program.Core.Log.Trace(Thread.CurrentThread.Name);

                Program.Core.Transactions.APIHandlers.Begin(processId);
                return new KbQueryTransactionBeginReply { Success = true };
            }
            catch (Exception ex)
            {
                return new KbQueryTransactionBeginReply
                {
                    ExceptionText = ex.Message,
                    Success = false
                };
            }

        }

        public static KbQueryTransactionCommitReply Commit(KbQueryTransactionCommit param)
        {
            try
            {
                var processId = Program.Core.Sessions.UpsertSessionId(param.SessionId);
                Thread.CurrentThread.Name = Thread.CurrentThread.Name = $"KbAPI:{processId}:{KbUtility.GetCurrentMethod()}";
                Program.Core.Log.Trace(Thread.CurrentThread.Name);

                Program.Core.Transactions.APIHandlers.Commit(processId);
                return new KbQueryTransactionCommitReply { Success = true };
            }
            catch (Exception ex)
            {
                return new KbQueryTransactionCommitReply
                {
                    ExceptionText = ex.Message,
                    Success = false
                };
            }

        }

        public static KbQueryTransactionRollbackReply Rollback(KbQueryTransactionRollback param)
        {
            try
            {
                var processId = Program.Core.Sessions.UpsertSessionId(param.SessionId);
                Thread.CurrentThread.Name = Thread.CurrentThread.Name = $"KbAPI:{processId}:{KbUtility.GetCurrentMethod()}";
                Program.Core.Log.Trace(Thread.CurrentThread.Name);

                Program.Core.Transactions.APIHandlers.Rollback(processId);
                return new KbQueryTransactionRollbackReply { Success = true };
            }
            catch (Exception ex)
            {
                return new KbQueryTransactionRollbackReply
                {
                    ExceptionText = ex.Message,
                    Success = false
                };
            }
        }
    }
}
