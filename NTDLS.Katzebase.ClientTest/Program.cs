using NTDLS.Katzebase.Client;

namespace NTDLS.Katzebase.ClientTest
{
    internal class Program
    {
        static void Main()
        {
            using (var client = new KbClient("http://localhost:6858"))
            {
                if (client.Schema.Exists("ClientTest") == false)
                {
                    client.Query.ExecuteNonQuery("CREATE SCHEMA ClientTest");
                }
            }
        }
    }
}