using NTDLS.Katzebase.Client.Payloads.Queries;

namespace NTDLS.Katzebase.Client.Service.Controllers
{
    public static class IndexesController
    {
        public static KbQueryIndexCreateReply Create(KbQueryIndexCreate param)
        {
            try
            {
                var processId = Program.Core.Sessions.UpsertSessionId(param.SessionId);
                Thread.CurrentThread.Name = Thread.CurrentThread.Name = $"KbAPI:{processId}:{KbUtility.GetCurrentMethod()}";
                Program.Core.Log.Trace(Thread.CurrentThread.Name);

                return Program.Core.Indexes.APIHandlers.CreateIndex(processId, param.Schema, param.Index);
            }
            catch (Exception ex)
            {
                return new KbQueryIndexCreateReply
                {
                    ExceptionText = ex.Message,
                    Success = false
                };
            }
        }

        /// <summary>
        /// Rebuilds a single index.
        /// </summary>
        /// <param name="schema"></param>
        public static KbQueryIndexRebuildReply Rebuild(KbQueryIndexRebuild param)
        {
            try
            {
                var processId = Program.Core.Sessions.UpsertSessionId(param.SessionId);
                Thread.CurrentThread.Name = Thread.CurrentThread.Name = $"KbAPI:{processId}:{KbUtility.GetCurrentMethod()}";
                Program.Core.Log.Trace(Thread.CurrentThread.Name);

                return Program.Core.Indexes.APIHandlers.RebuildIndex(processId, param.Schema, param.IndexName, param.NewPartitionCount);
            }
            catch (Exception ex)
            {
                return new KbQueryIndexRebuildReply
                {
                    ExceptionText = ex.Message,
                    Success = false
                };
            }
        }

        /// <summary>
        /// Drops a single index.
        /// </summary>
        /// <param name="schema"></param>
        public static KbQueryIndexDropReply Drop(KbQueryIndexDrop param)
        {
            try
            {
                var processId = Program.Core.Sessions.UpsertSessionId(param.SessionId);
                Thread.CurrentThread.Name = Thread.CurrentThread.Name = $"KbAPI:{processId}:{KbUtility.GetCurrentMethod()}";
                Program.Core.Log.Trace(Thread.CurrentThread.Name);

                return Program.Core.Indexes.APIHandlers.DropIndex(processId, param.Schema, param.IndexName);
            }
            catch (Exception ex)
            {
                return new KbQueryIndexDropReply
                {
                    ExceptionText = ex.Message,
                    Success = false
                };
            }
        }

        /// <summary>
        /// Checks for the existence of an index.
        /// </summary>
        /// <param name="schema"></param>
        public static KbQueryIndexExistsReply Exists(KbQueryIndexExists param)
        {
            try
            {
                var processId = Program.Core.Sessions.UpsertSessionId(param.SessionId);
                Thread.CurrentThread.Name = Thread.CurrentThread.Name = $"KbAPI:{processId}:{KbUtility.GetCurrentMethod()}";
                Program.Core.Log.Trace(Thread.CurrentThread.Name);

                return Program.Core.Indexes.APIHandlers.DoesIndexExist(processId, param.Schema, param.IndexName);
            }
            catch (Exception ex)
            {
                return new KbQueryIndexExistsReply
                {
                    ExceptionText = ex.Message,
                    Success = false
                };
            }
        }

        /// <summary>
        /// Gets an index from a specific schema.
        /// </summary>
        /// <param name="schema"></param>
        public static KbQueryIndexGetReply Get(KbQueryIndexGet param)
        {
            try
            {
                var processId = Program.Core.Sessions.UpsertSessionId(param.SessionId);
                Thread.CurrentThread.Name = Thread.CurrentThread.Name = $"KbAPI:{processId}:{KbUtility.GetCurrentMethod()}";
                Program.Core.Log.Trace(Thread.CurrentThread.Name);

                return Program.Core.Indexes.APIHandlers.Get(processId, param.Schema, param.IndexName);
            }
            catch (Exception ex)
            {
                return new KbQueryIndexGetReply
                {
                    ExceptionText = ex.Message,
                    Success = false
                };
            }
        }

        /// <summary>
        /// Lists the existing indexes within a given schema.
        /// </summary>
        /// <param name="schema"></param>
        public static KbQueryIndexListReply List(KbQueryIndexList param)
        {
            try
            {
                var processId = Program.Core.Sessions.UpsertSessionId(param.SessionId);
                Thread.CurrentThread.Name = Thread.CurrentThread.Name = $"KbAPI:{processId}:{KbUtility.GetCurrentMethod()}";
                Program.Core.Log.Trace(Thread.CurrentThread.Name);

                return Program.Core.Indexes.APIHandlers.ListIndexes(processId, param.Schema);
            }
            catch (Exception ex)
            {
                return new KbQueryIndexListReply
                {
                    ExceptionText = ex.Message,
                    Success = false
                };
            }
        }
    }
}
