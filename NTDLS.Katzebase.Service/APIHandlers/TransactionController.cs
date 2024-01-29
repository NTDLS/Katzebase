using NTDLS.Katzebase.Client;
using NTDLS.Katzebase.Client.Payloads.Queries;
using NTDLS.Katzebase.Engine;

namespace NTDLS.Katzebase.Service.APIHandlers
{
    public class TransactionController
    {
        private readonly EngineCore _core;
        public TransactionController(EngineCore core)
        {
            _core = core;
        }

        public KbQueryTransactionBeginReply Begin(KbQueryTransactionBegin param)
        {
            try
            {
                var processId = _core.Sessions.UpsertSessionId(param.SessionId);
                Thread.CurrentThread.Name = Thread.CurrentThread.Name = $"KbAPI:{processId}:{KbUtility.GetCurrentMethod()}";
                _core.Log.Trace(Thread.CurrentThread.Name);

                _core.Transactions.APIHandlers.Begin(processId);
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

        public KbQueryTransactionCommitReply Commit(KbQueryTransactionCommit param)
        {
            try
            {
                var processId = _core.Sessions.UpsertSessionId(param.SessionId);
                Thread.CurrentThread.Name = Thread.CurrentThread.Name = $"KbAPI:{processId}:{KbUtility.GetCurrentMethod()}";
                _core.Log.Trace(Thread.CurrentThread.Name);

                _core.Transactions.APIHandlers.Commit(processId);
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

        public KbQueryTransactionRollbackReply Rollback(KbQueryTransactionRollback param)
        {
            try
            {
                var processId = _core.Sessions.UpsertSessionId(param.SessionId);
                Thread.CurrentThread.Name = Thread.CurrentThread.Name = $"KbAPI:{processId}:{KbUtility.GetCurrentMethod()}";
                _core.Log.Trace(Thread.CurrentThread.Name);

                _core.Transactions.APIHandlers.Rollback(processId);
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
