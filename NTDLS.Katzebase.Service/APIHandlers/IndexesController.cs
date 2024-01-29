using NTDLS.Katzebase.Client;
using NTDLS.Katzebase.Client.Payloads.Queries;
using NTDLS.Katzebase.Engine;

namespace NTDLS.Katzebase.Service.APIHandlers
{
    public class IndexesController
    {
        private readonly EngineCore _core;
        public IndexesController(EngineCore core)
        {
            _core = core;
        }

        public KbQueryIndexCreateReply Create(KbQueryIndexCreate param)
        {
            try
            {
                var processId = _core.Sessions.UpsertSessionId(param.SessionId);
                Thread.CurrentThread.Name = Thread.CurrentThread.Name = $"KbAPI:{processId}:{KbUtility.GetCurrentMethod()}";
                _core.Log.Trace(Thread.CurrentThread.Name);

                return _core.Indexes.APIHandlers.CreateIndex(processId, param.Schema, param.Index);
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
        public KbQueryIndexRebuildReply Rebuild(KbQueryIndexRebuild param)
        {
            try
            {
                var processId = _core.Sessions.UpsertSessionId(param.SessionId);
                Thread.CurrentThread.Name = Thread.CurrentThread.Name = $"KbAPI:{processId}:{KbUtility.GetCurrentMethod()}";
                _core.Log.Trace(Thread.CurrentThread.Name);

                return _core.Indexes.APIHandlers.RebuildIndex(processId, param.Schema, param.IndexName, param.NewPartitionCount);
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
        public KbQueryIndexDropReply Drop(KbQueryIndexDrop param)
        {
            try
            {
                var processId = _core.Sessions.UpsertSessionId(param.SessionId);
                Thread.CurrentThread.Name = Thread.CurrentThread.Name = $"KbAPI:{processId}:{KbUtility.GetCurrentMethod()}";
                _core.Log.Trace(Thread.CurrentThread.Name);

                return _core.Indexes.APIHandlers.DropIndex(processId, param.Schema, param.IndexName);
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
        public KbQueryIndexExistsReply Exists(KbQueryIndexExists param)
        {
            try
            {
                var processId = _core.Sessions.UpsertSessionId(param.SessionId);
                Thread.CurrentThread.Name = Thread.CurrentThread.Name = $"KbAPI:{processId}:{KbUtility.GetCurrentMethod()}";
                _core.Log.Trace(Thread.CurrentThread.Name);

                return _core.Indexes.APIHandlers.DoesIndexExist(processId, param.Schema, param.IndexName);
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
        public KbQueryIndexGetReply Get(KbQueryIndexGet param)
        {
            try
            {
                var processId = _core.Sessions.UpsertSessionId(param.SessionId);
                Thread.CurrentThread.Name = Thread.CurrentThread.Name = $"KbAPI:{processId}:{KbUtility.GetCurrentMethod()}";
                _core.Log.Trace(Thread.CurrentThread.Name);

                return _core.Indexes.APIHandlers.Get(processId, param.Schema, param.IndexName);
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
        public KbQueryIndexListReply List(KbQueryIndexList param)
        {
            try
            {
                var processId = _core.Sessions.UpsertSessionId(param.SessionId);
                Thread.CurrentThread.Name = Thread.CurrentThread.Name = $"KbAPI:{processId}:{KbUtility.GetCurrentMethod()}";
                _core.Log.Trace(Thread.CurrentThread.Name);

                return _core.Indexes.APIHandlers.ListIndexes(processId, param.Schema);
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
