using NTDLS.Katzebase.Client.Payloads.Queries;

namespace NTDLS.Katzebase.Client.Service.Controllers
{
    public static class SchemaController
    {
        /// <summary>
        /// Lists the existing schemas within a given schema.
        /// </summary>
        /// <param name="schema"></param>
        public static KbQuerySchemaListReply List(KbQuerySchemaList param)
        {
            try
            {
                var processId = Program.Core.Sessions.UpsertSessionId(param.SessionId);
                Thread.CurrentThread.Name = Thread.CurrentThread.Name = $"KbAPI:{processId}:{KbUtility.GetCurrentMethod()}";
                Program.Core.Log.Trace(Thread.CurrentThread.Name);

                return Program.Core.Schemas.APIHandlers.ListSchemas(processId, param.Schema);
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
        public static KbQuerySchemaCreateReply Create(KbQuerySchemaCreate param)
        {
            try
            {
                var processId = Program.Core.Sessions.UpsertSessionId(param.SessionId);
                Thread.CurrentThread.Name = Thread.CurrentThread.Name = $"KbAPI:{processId}:{KbUtility.GetCurrentMethod()}";
                Program.Core.Log.Trace(Thread.CurrentThread.Name);

                return Program.Core.Schemas.APIHandlers.CreateSchema(processId, param.Schema, param.PageSize);
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
        public static KbQuerySchemaExistsReply Exists(KbQuerySchemaExists param)
        {
            try
            {
                var processId = Program.Core.Sessions.UpsertSessionId(param.SessionId);
                Thread.CurrentThread.Name = Thread.CurrentThread.Name = $"KbAPI:{processId}:{KbUtility.GetCurrentMethod()}";
                Program.Core.Log.Trace(Thread.CurrentThread.Name);

                return Program.Core.Schemas.APIHandlers.DoesSchemaExist(processId, param.Schema);
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
        public static KbQuerySchemaDropReply Drop(KbQuerySchemaDrop param)
        {
            try
            {
                var processId = Program.Core.Sessions.UpsertSessionId(param.SessionId);
                Thread.CurrentThread.Name = Thread.CurrentThread.Name = $"KbAPI:{processId}:{KbUtility.GetCurrentMethod()}";
                Program.Core.Log.Trace(Thread.CurrentThread.Name);

                return Program.Core.Schemas.APIHandlers.DropSchema(processId, param.Schema);
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
