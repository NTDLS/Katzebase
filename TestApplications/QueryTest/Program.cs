namespace QueryTest
{
    internal static class Program
    {
        const string _serverHost = "127.0.0.1";
        const int _serverPort = 6858;
        const string _username = "admin";
        const string _password = "";

        static void Main()
        {
            try
            {
                //using var client = new KbClient(_serverHost, _serverPort, _username, KbClient.HashPassword(_password));

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
            Console.WriteLine("Press [enter] to exit.");
            Console.ReadLine();
        }
    }
}
