using NTDLS.Katzebase.Client;
using NTDLS.Katzebase.Client.Payloads.Queries;
using NTDLS.Katzebase.Engine;

namespace NTDLS.Katzebase.Service.APIHandlers
{
    public class SchemaController
    {
        private readonly EngineCore _core;
        public SchemaController(EngineCore core)
        {
            _core = core;
        }

        /// <summary>
        /// Lists the existing schemas within a given schema.
        /// </summary>
        /// <param name="schema"></param>
        public KbQuerySchemaListReply List(KbQuerySchemaList param)
        {
            try
            {
                var processId = _core.Sessions.UpsertSessionId(param.SessionId);
                Thread.CurrentThread.Name = Thread.CurrentThread.Name = $"KbAPI:{processId}:{KbUtility.GetCurrentMethod()}";
                _core.Log.Trace(Thread.CurrentThread.Name);

                return _core.Schemas.APIHandlers.ListSchemas(processId, param.Schema);
            }
            catch (Exception ex)
            {
                return new KbQuerySchemaListReply
                {
                    ExceptionText = ex.Message,
                    Success = false
                };
            }
        }

        /// <summary>
        /// Creates a single schema or an entire schema path.
        /// </summary>
        /// <param name="schema"></param>
        public KbQuerySchemaCreateReply Create(KbQuerySchemaCreate param)
        {
            try
            {
                var processId = _core.Sessions.UpsertSessionId(param.SessionId);
                Thread.CurrentThread.Name = Thread.CurrentThread.Name = $"KbAPI:{processId}:{KbUtility.GetCurrentMethod()}";
                _core.Log.Trace(Thread.CurrentThread.Name);

                return _core.Schemas.APIHandlers.CreateSchema(processId, param.Schema, param.PageSize);
            }
            catch (Exception ex)
            {
                return new KbQuerySchemaCreateReply
                {
                    ExceptionText = ex.Message,
                    Success = false
                };
            }
        }

        /// <summary>
        /// Checks for the existence of a schema.
        /// </summary>
        /// <param name="schema"></param>
        public KbQuerySchemaExistsReply Exists(KbQuerySchemaExists param)
        {
            try
            {
                var processId = _core.Sessions.UpsertSessionId(param.SessionId);
                Thread.CurrentThread.Name = Thread.CurrentThread.Name = $"KbAPI:{processId}:{KbUtility.GetCurrentMethod()}";
                _core.Log.Trace(Thread.CurrentThread.Name);

                return _core.Schemas.APIHandlers.DoesSchemaExist(processId, param.Schema);
            }
            catch (Exception ex)
            {
                return new KbQuerySchemaExistsReply
                {
                    ExceptionText = ex.Message,
                    Success = false
                };
            }
        }

        /// <summary>
        /// Drops a single schema or an entire schema path.
        /// </summary>
        /// <param name="schema"></param>
        public KbQuerySchemaDropReply Drop(KbQuerySchemaDrop param)
        {
            try
            {
                var processId = _core.Sessions.UpsertSessionId(param.SessionId);
                Thread.CurrentThread.Name = Thread.CurrentThread.Name = $"KbAPI:{processId}:{KbUtility.GetCurrentMethod()}";
                _core.Log.Trace(Thread.CurrentThread.Name);

                return _core.Schemas.APIHandlers.DropSchema(processId, param.Schema);
            }
            catch (Exception ex)
            {
                return new KbQuerySchemaDropReply
                {
                    ExceptionText = ex.Message,
                    Success = false
                };
            }
        }
    }
}
