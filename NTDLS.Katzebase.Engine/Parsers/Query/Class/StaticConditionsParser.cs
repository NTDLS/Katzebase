using NTDLS.Katzebase.Engine.Parsers.Query.Fields.Expressions;
using NTDLS.Katzebase.Engine.Parsers.Query.SupportingTypes;
using NTDLS.Katzebase.Engine.Parsers.Query.WhereAndJoinConditions;
using NTDLS.Katzebase.Engine.Parsers.Tokens;
using static NTDLS.Katzebase.Engine.Library.EngineConstants;

namespace NTDLS.Katzebase.Engine.Parsers.Query.Class
{
    internal static class StaticConditionsParser
    {
        /// <summary>
        /// Contains the collection of ConditionGroup, each group is comprised of AND expressions,
        /// and there is a seperate ConditionGroup for each OR expression and for each expression in parentheses.
        /// </summary>
        public class ConditionCollection : List<ConditionGroup>
        {
            /// <summary>
            /// For conditions on joins, this is the alias of the schema that these conditions are for.
            /// </summary>
            public string? SchemaAlias { get; set; }
            public string FinalExpression { get; set; }

            private int _nextExpressionVariable = 0;

            public string NextExpressionVariable()
                => $"v{_nextExpressionVariable++}";

            public ConditionCollection(string finalExpression, string? schemaAlias = null)
            {
                FinalExpression = finalExpression;
                SchemaAlias = schemaAlias;
            }
        }

        public class ConditionGroup : List<Condition>
        {
            public LogicalConnector LogicalConnector { get; set; }

            public ConditionGroup(LogicalConnector logicalConnector)
            {
                LogicalConnector = logicalConnector;
            }
        }

        public class Condition
        {
            public string ExpressionVariable { get; set; }

            public string? LeftValue { get; set; }
            public LogicalQualifier Qualifier { get; set; }
            public QueryField RightValue { get; set; }

            public List<ConditionGroup> Children { get; set; } = new();


            public Condition(string expressionVariable, string? leftValue, LogicalQualifier qualifier, QueryField rightValue)
            {
                ExpressionVariable = expressionVariable;
                LeftValue = leftValue;
                Qualifier = qualifier;
                RightValue = rightValue;
            }
        }

        /// <summary>
        /// Parse the conditions, we are going to build a full expression with all of the condition values replaced with tokens so we
        /// can build a mathematical expression, but we are also going to build sets of groups for which there will be one per OR expression,
        /// and all conditions in a ConditionGroup will be comprised solely of AND conditions. This way we can use the groups to match indexes
        /// before evaluating the whole expression on the limited set of documents we derived from the indexing operations.
        /// </summary>
        public static void Parse(QueryBatch queryBatch, Tokenizer parentTokenizer, string conditionsText, string leftHandAliasOfJoin = "")
        {

            var conditionCollection = new ConditionCollection(conditionsText, leftHandAliasOfJoin);

            conditionCollection.FinalExpression = conditionCollection.FinalExpression
                .Replace(" OR ", " || ", StringComparison.InvariantCultureIgnoreCase)
                .Replace(" AND ", " && ", StringComparison.InvariantCultureIgnoreCase);


            if (conditionsText.Contains("LanguageId"))
            {
            }

            ParseRecursive(queryBatch, parentTokenizer, conditionCollection, conditionCollection, conditionsText, LogicalConnector.None);

            conditionCollection.FinalExpression = conditionCollection.FinalExpression.Replace("  ", " ").Trim();

            if (conditionsText.Contains("LanguageId"))
            {
            }
        }

        private static void ParseRecursive(QueryBatch queryBatch, Tokenizer parentTokenizer,
            ConditionCollection conditionCollection, List<ConditionGroup> parentConditionGroup, string conditionsText, LogicalConnector givenLogicalConnector)
        {
            var tokenizer = new Tokenizer(conditionsText);

            var lastLogicalConnector = LogicalConnector.None;

            ConditionGroup? conditionGroup = null;
            Condition? lastCondition = null;

            while (!tokenizer.IsExhausted())
            {
                if (tokenizer.TryIsNextCharacter('('))
                {
                    string subConditionsText = tokenizer.EatMatchingScope();
                    ParseRecursive(queryBatch, parentTokenizer, conditionCollection, lastCondition?.Children ?? parentConditionGroup, subConditionsText, lastLogicalConnector);

                    if (!tokenizer.IsExhausted())
                    {
                        if (tokenizer.EatIsNextEnumToken<LogicalConnector>() == LogicalConnector.Or)
                        {
                            conditionGroup = new ConditionGroup(LogicalConnector.Or);
                            parentConditionGroup.Add(conditionGroup);
                        }
                    }

                    continue;
                }

                int startOfSubExpression = tokenizer.Caret;

                var leftAndRight = ParseRightAndLeft(queryBatch, parentTokenizer, tokenizer);

                var leftToken = tokenizer.EatGetNext();
                var logicalQualifier = StaticConditionHelpers.ParseLogicalQualifier(tokenizer.EatGetNext());
                var rightToken = tokenizer.EatGetNext();

                var subExpressionText = tokenizer.SubString(startOfSubExpression, tokenizer.Caret - startOfSubExpression);

                var expressionVariable = conditionCollection.NextExpressionVariable();

                conditionCollection.FinalExpression = conditionCollection.FinalExpression.Replace(subExpressionText, $" {expressionVariable} ");

                var rightConditionValue = new QueryFieldExpressionString
                {
                    Value = rightToken
                };

                var rightQueryField = new QueryField("", 0, rightConditionValue);

                lastCondition = new Condition(expressionVariable, leftToken, logicalQualifier, rightQueryField);

                if (conditionGroup == null)
                {
                    //We late initialize here because the first thing we encounter might be a parenthesis
                    //instead of a condition and we don't want to have added an empty condition group.
                    conditionGroup = new ConditionGroup(givenLogicalConnector);
                    parentConditionGroup.Add(conditionGroup);
                }

                conditionGroup.Add(lastCondition);

                if (!tokenizer.IsExhausted())
                {
                    lastLogicalConnector = tokenizer.EatIsNextEnumToken<LogicalConnector>();
                    if (lastLogicalConnector == LogicalConnector.Or)
                    {
                        conditionGroup = new ConditionGroup(LogicalConnector.Or);
                        parentConditionGroup.Add(conditionGroup);
                    }
                }
            }
        }

        static (string right, LogicalQualifier qualifier, string left)
            ParseRightAndLeft(QueryBatch queryBatch, Tokenizer parentTokenizer, Tokenizer tokenizer)
        {
            (string right, LogicalQualifier qualifier, string left) result = new();

            //sw.Text LIKE $s_0$ and Length ( sw.Text ) / $n_11$ < sw.Id and sw.Text != sw.Text + $s_1$ and sw.Text LIKE $s_2$ and sw.Text like $s_3$ and ( sw.LanguageId >= $n_12$ OR sw.LanguageId <= $n_13$ ) and sw.Id > $n_14$
            if (tokenizer.Contains("Length"))
            {
            }
            

            //queryBatch.que

            string token;

            token = tokenizer.EatGetNext();

            if (parentTokenizer.PredefinedConstants.TryGetValue(token, out var constant))
            {

            }

            return result;
        }
    }
}
