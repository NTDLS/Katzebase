using NTDLS.Katzebase.Parsers.Conditions;

namespace NTDLS.Katzebase.Parsers.SupportingTypes
{
    public class QuerySchema
    {
        public enum QuerySchemaUsageType
        {
            /// <summary>
            /// Schema is the primary (or root) schema in a query where the conditions values are located in the where clause.
            /// Also used for schema when the "query" is is not a query but another statement like "create index" or "drop schema".
            /// </summary>
            Primary,
            /// <summary>
            /// Schema is used to inner-join to another schema in the query and the condition values are located in another schema's rows.
            /// </summary>
            InnerJoin,
            /// <summary>
            /// Schema is used to inner-join to another schema in the query and the condition values are located in another schema's rows.
            /// </summary>
            OuterJoin
        }

        public string Name { get; set; }
        public string Alias { get; set; } = string.Empty;
        public ConditionCollection? Conditions { get; set; }
        public QuerySchemaUsageType SchemaUsageType { get; private set; }
        public int? ScriptLine { get; set; }

        public QuerySchema(int? scriptLine, string name, QuerySchemaUsageType schemaUsageType, string prefix, ConditionCollection conditions)
        {
            ScriptLine = scriptLine;
            Name = name;
            SchemaUsageType = schemaUsageType;
            Alias = prefix;
            Conditions = conditions;
        }

        public QuerySchema(int? scriptLine, string name, QuerySchemaUsageType schemaUsageType, string prefix)
        {
            ScriptLine = scriptLine;
            Name = name;
            SchemaUsageType = schemaUsageType;
            Alias = prefix;
        }

        public QuerySchema(int? scriptLine, string name, QuerySchemaUsageType schemaUsageType)
        {
            ScriptLine = scriptLine;
            SchemaUsageType = schemaUsageType;
            Name = name;
        }
    }
}
