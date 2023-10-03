using System.Data.SqlClient;

namespace NTDLS.Katzebase.Debuging
{
    public static class SqlServerTracer
    {
        public static Guid RunId { get; private set; } = Guid.NewGuid();

        public static class DebugTraceSeverity
        {
            public static string Enter = "Enter";
            public static string Success = "Success";
            public static string Info = "Info";
            public static string Warning = "Warning";
            public static string Fail = "Fail";
        }

        public static void Trace(Guid batch, Guid transactionId, ulong processId, string severity, string text)
        {
            using (var connection = new SqlConnection($"Server=localhost;Database=Katzebase_Debug;Trusted_Connection=True;"))
            {
                connection.Open();

                try
                {
                    using var command = new SqlCommand("WriteSessionTrace", connection);
                    command.CommandType = System.Data.CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("Thread", Thread.CurrentThread.Name ?? $"Unnamed: {Thread.CurrentThread.ManagedThreadId}");
                    command.Parameters.AddWithValue("RunId", RunId);
                    command.Parameters.AddWithValue("TransactionId", transactionId);
                    command.Parameters.AddWithValue("ProcessId", (int)processId);
                    command.Parameters.AddWithValue("Batch", batch);
                    command.Parameters.AddWithValue("Severity", severity);
                    command.Parameters.AddWithValue("Info", text);
                    command.ExecuteNonQuery();
                }
                finally
                {
                    try
                    {
                        connection.Close();
                    }
                    catch { }
                }
            }
        }
    }
}