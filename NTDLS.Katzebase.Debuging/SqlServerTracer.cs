using System.Data.SqlClient;
using System.Diagnostics;

namespace NTDLS.Katzebase.Debuging
{
    public static class SqlServerTracer
    {
        public static Guid RunId { get; private set; } = Guid.NewGuid();

        public static class DebugTraceSeverity
        {
            public const string Enter = "Enter";
            public const string Success = "Success";
            public const string Info = "Info";
            public const string Warning = "Warning";
            public const string Fail = "Fail";
        }

        public static void Trace(Guid transactionId, ulong processId, string severity, string text)
        {
            using (var connection = new SqlConnection($"Server=localhost;Database=Katzebase_Debug;Trusted_Connection=True;"))
            {
                connection.Open();

                try
                {
                    using var command = new SqlCommand("WriteSessionTrace", connection);
                    command.CommandType = System.Data.CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("RunId", RunId);
                    command.Parameters.AddWithValue("Thread", Thread.CurrentThread.Name ?? $"Unnamed: {Thread.CurrentThread.ManagedThreadId}");
                    command.Parameters.AddWithValue("Method", (new StackTrace()).GetFrame(1)?.GetMethod()?.Name);
                    command.Parameters.AddWithValue("ProcessId", (int)processId);
                    command.Parameters.AddWithValue("TransactionId", transactionId);
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
