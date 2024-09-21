using NTDLS.Katzebase.Engine.Parsers.Query.SupportingTypes;
using NTDLS.Katzebase.Engine.Parsers.Tokens;
using static NTDLS.Katzebase.Engine.Library.EngineConstants;

namespace NTDLS.Katzebase.Engine.Parsers.Query.Class
{
    internal static class StaticParserAlter
    {
        internal static PreparedQuery Parse(QueryBatch queryBatch, Tokenizer tokenizer)
        {
            var query = new PreparedQuery(queryBatch, QueryType.Alter)
            {
                //SubQueryType = tokenizer.EatIfNextEnum([SubQueryType.None])
            };

            throw new NotImplementedException("reimplement");

            /*
                            if (tokenizer.PeekNext().IsOneOf(["schema", "configuration"]) == false)
                            {
                                throw new KbParserException("Invalid query. Found '" + tokenizer.PeekNext() + "', expected 'schema' or 'configuration'.");
                            }

                            token = tokenizer.GetNext();
                            if (Enum.TryParse(token, true, out SubQueryType subQueryType) == false)
                            {
                                throw new KbParserException("Invalid query. Found '" + token + "', expected: 'schema' or 'configuration'.");
                            }
                            result.SubQueryType = subQueryType;

                            if (result.SubQueryType == SubQueryType.Configuration)
                            {
                                if (tokenizer.PeekNext().Is("with"))
                                {
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
                                    StaticWithOptionsParser.ParseWithOptions(ref tokenizer, options, ref result);
                                }
                            }
                            else if (result.SubQueryType == SubQueryType.Schema)
                            {
                                result.AddAttribute(PreparedQuery.QueryAttribute.IsUnique, (subQueryType == SubQueryType.UniqueKey));

                                token = tokenizer.GetNext();
                                if (token == string.Empty)
                                {
                                    throw new KbParserException("Invalid query. Found '" + token + "', expected: object name.");
                                }
                                result.Schemas.Add(new QuerySchema(token));

                                if (tokenizer.PeekNext().Is("with"))
                                {
                                    var options = new ExpectedWithOptions
                                    {
                                        {"pagesize", typeof(uint) }
                                    };
                                    StaticWithOptionsParser.ParseWithOptions(ref tokenizer, options, ref result);
                                }
                            }
                            else
                            {
                                throw new KbNotImplementedException();
                            }
            */

            return query;
        }
    }
}
