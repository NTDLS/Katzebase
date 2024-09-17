using NTDLS.Katzebase.Engine.Parsers.Query.SupportingTypes;

namespace NTDLS.Katzebase.Engine.Parsers.Query.WhereAndJoinConditions
{
    /// <summary>
    /// Contains the collection of ConditionSets, each group contains AND expressions (NO OR expressions) as there
    ///     is a seperate ConditionGroup for each OR expression and for each expression contained in parentheses.
    /// </summary>
    internal class Old_ConditionCollection : Old_ConditionSetCollection
    {
        /// <summary>
        /// For conditions on joins, this is the alias of the schema that these conditions are for.
        /// </summary>
        public string? SchemaAlias { get; set; }

        /// <summary>
        /// After parsing, this will contain a mathematical expression, containing
        ///     variables, that can be evaluated to determine a document match.
        /// </summary>
        public string MathematicalExpression { get; set; } = string.Empty;

        /// <summary>
        /// Hash of the MathematicalExpression.
        /// </summary>
        public string? Hash { get; set; }

        public QueryFieldCollection FieldCollection { get; set; }

        private int _nextExpressionVariable = 0;
        public string NextExpressionVariable()
            => $"v{_nextExpressionVariable++}";

        public Old_ConditionCollection(QueryBatch queryBatch, string mathematicalExpression, string? schemaAlias = null)
            : base(Library.EngineConstants.LogicalConnector.None)
        {
            FieldCollection = new(queryBatch);
            MathematicalExpression = mathematicalExpression;
            SchemaAlias = schemaAlias;
        }

        public Old_ConditionCollection(QueryBatch queryBatch)
            : base(Library.EngineConstants.LogicalConnector.None)
        {
            FieldCollection = new(queryBatch);
        }

        public string ExplainOperations()
        {
            throw new NotImplementedException();
        }

        public Old_ConditionCollection Clone()
        {
            var clone = new Old_ConditionCollection(FieldCollection.QueryBatch, MathematicalExpression, SchemaAlias)
            {
                Hash = Hash,
                _nextExpressionVariable = _nextExpressionVariable
            };

            foreach (var conditionSet in this)
            {
                clone.Add(conditionSet.Clone());
            }

            return clone;
        }
    }
}
