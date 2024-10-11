using NTDLS.Katzebase.Parsers.Query.SupportingTypes;
using NTDLS.Katzebase.Parsers.Tokens;
using NTDLS.Katzebase.Shared;
using System.Reflection;
using static NTDLS.Katzebase.Parsers.Constants;

namespace NTDLS.Katzebase.Parsers.Query.Specific
{
    public static class StaticParserAlterConfiguration
    {
        internal static SupportingTypes.Query Parse(QueryBatch queryBatch, Tokenizer tokenizer)
        {
            var query = new SupportingTypes.Query(queryBatch, QueryType.Alter, tokenizer.GetCurrentLineNumber())
            {
                SubQueryType = SubQueryType.Configuration
            };

            tokenizer.EatIfNext("with");

            var options = new ExpectedQueryAttributes();

            var properties = typeof(KatzebaseSettings).GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach (var property in properties)
            {
                options.Add(property.Name, property.PropertyType);
            }

            query.AddAttributes(StaticParserAttributes.Parse(tokenizer, options));

            return query;
        }
    }
}
