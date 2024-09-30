using NTDLS.Katzebase.Parsers.Query.Class.WithOptions;
using NTDLS.Katzebase.Parsers.Query.SupportingTypes;
using NTDLS.Katzebase.Parsers.Tokens;
using static NTDLS.Katzebase.Parsers.Constants;

namespace NTDLS.Katzebase.Parsers.Query.Class
{
    public static class StaticParserAlterConfiguration
    {
        internal static PreparedQuery Parse(QueryBatch queryBatch, Tokenizer tokenizer)
        {
            var query = new PreparedQuery(queryBatch, QueryType.Alter)
            {
                SubQueryType = SubQueryType.Configuration
            };

            tokenizer.EatIfNext("with");

            var options = new ExpectedWithOptions
            {
                { "BaseAddress", typeof(string) },
                { "DataRootPath", typeof(string) },
                { "TransactionDataPath", typeof(string) },
                { "LogDirectory", typeof(string) },
                { "FlushLog", typeof(bool) },
                { "DefaultDocumentPageSize", typeof(int) },
                { "UseCompression", typeof(bool) },
                { "HealthMonitoringEnabled", typeof(bool) },
                { "HealthMonitoringCheckpointSeconds", typeof(int) },
                { "HealthMonitoringInstanceLevelEnabled", typeof(bool) },
                { "HealthMonitoringInstanceLevelTimeToLiveSeconds", typeof(int) },
                { "MaxIdleConnectionSeconds", typeof(int) },
                { "DefaultIndexPartitions", typeof(int) },
                { "DeferredIOEnabled", typeof(bool) },
                { "WriteTraceData", typeof(bool) },
                { "CacheEnabled", typeof(bool) },
                { "CacheMaxMemory", typeof(int) },
                { "CacheScavengeInterval", typeof(int) },
                { "CachePartitions", typeof(int) },
                { "CacheSeconds", typeof(int) }
            };

            query.AddAttributes(StaticParserWithOptions.Parse(tokenizer, options));

            return query;
        }
    }
}
