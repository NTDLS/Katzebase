using NTDLS.Katzebase.Client;
using System.Xml;

namespace ImportSOUses
{
    internal class Program
    {
        static void Main()
        {
            using var client = new KbClient("127.0.0.1", 6858);
            client.Schema.Create("SoUsers");

            client.Transaction.Begin();

            int rowCount = 0;
            int rowsPerTransaction = 10000;

            using (XmlReader reader = XmlReader.Create("C:\\Katzebase\\@External\\Users.xml"))
            {
                while (reader.Read())
                {
                    if (reader.NodeType == XmlNodeType.Element && reader.Name == "row")
                    {
                        var record = new
                        {
                            Id = reader.GetAttribute("Id"),
                            Reputation = reader.GetAttribute("Reputation"),
                            CreationDate = reader.GetAttribute("CreationDate"),
                            DisplayName = reader.GetAttribute("DisplayName"),
                            Location = reader.GetAttribute("Location"),
                            LastAccessDate = reader.GetAttribute("LastAccessDate"),
                            AboutMe = reader.GetAttribute("AboutMe"),
                            WebsiteUrl = reader.GetAttribute("WebsiteUrl"),
                            Views = reader.GetAttribute("Views"),
                            UpVotes = reader.GetAttribute("UpVotes"),
                            DownVotes = reader.GetAttribute("DownVotes")
                        };

                        client.Document.Store("SoUsers", record);

                        if (rowCount++ > 0 && (rowCount % rowsPerTransaction) == 0)
                        {
                            Console.WriteLine($"Committing... {rowCount}");
                            client.Transaction.Commit();
                            client.Transaction.Begin();
                        }
                    }
                }
            }

            client.Transaction.Commit();
        }
    }
}