using NTDLS.Katzebase.Engine.Parsers.Query.SupportingTypes;

namespace NTDLS.Katzebase.Engine.Parsers.Query.WhereAndJoinConditions
{
    /// <summary>
    /// Contains the collection of ConditionSets, each group contains AND expressions (NO OR expressions) as there
    ///     is a seperate ConditionGroup for each OR expression and for each expression contained in parentheses.
    /// </summary>
    internal class ConditionCollection : List<ConditionSet>
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

        public ConditionCollection(QueryBatch queryBatch, string mathematicalExpression, string? schemaAlias = null)
        {
            FieldCollection = new(queryBatch);
            MathematicalExpression = mathematicalExpression;
            SchemaAlias = schemaAlias;
        }

        public ConditionCollection(QueryBatch queryBatch)
        {
            FieldCollection = new(queryBatch);
        }

        public string ExplainOperations()
        {
            throw new NotImplementedException();
        }

        public ConditionCollection Clone()
        {
            var clone = new ConditionCollection(FieldCollection.QueryBatch, MathematicalExpression, SchemaAlias)
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
