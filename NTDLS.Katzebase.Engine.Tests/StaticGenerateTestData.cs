using NTDLS.Katzebase.Api;
using NTDLS.Katzebase.Engine.Scripts;
using NTDLS.Katzebase.Engine.Tests.Utilities;

namespace NTDLS.Katzebase.Engine.Tests
{
    internal class StaticGenerateTestData
    {
        /// <summary>
        /// Used by the EngineCoreFixture to generate data for all of the tests.
        /// </summary>
        /// <param name="rootDirectoryFreshlyCreated">Denotes whether the root directory needed to be created.</param>
        public static void GenerateTestData(bool rootDirectoryFreshlyCreated)
        {
            if (rootDirectoryFreshlyCreated == false)
            {
                var client = new KbClient(Constants.HOST_NAME, Constants.LISTEN_PORT, Constants.USER_NAME, KbClient.HashPassword(Constants.PASSWORD));

                client.Query.ExecuteNonQuery(EmbeddedScripts.Load(@"Initialization\CreateTestDataSchema.kbs"));
                client.Query.ExecuteNonQuery(EmbeddedScripts.Load(@"MockERPQueries\CreateSchema.kbs"));
                client.Query.ExecuteNonQuery(EmbeddedScripts.Load(@"MockERPQueries\CreateSchemaData.kbs"));

                DataImporter.ImportTabSeparatedFiles(client, "Data.WordList", "TestData:WordList");

                client.Query.ExecuteNonQuery(EmbeddedScripts.Load(@"TestCases\WordList\CreateIndexes.kbs"));
            }
        }
    }
}
