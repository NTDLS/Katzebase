using NTDLS.Helpers;
using NTDLS.Katzebase.Client.Exceptions;
using NTDLS.Katzebase.Engine.Functions.Scaler;
using NTDLS.Katzebase.Engine.Library;
using NTDLS.Katzebase.Engine.Parsers.Query.Fields;
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

            public QueryFieldCollection FieldCollection;

            private int _nextExpressionVariable = 0;
            public string NextExpressionVariable()
                => $"v{_nextExpressionVariable++}";

            public ConditionCollection(QueryBatch queryBatch, string finalExpression, string? schemaAlias = null)
            {
                FieldCollection = new(queryBatch);
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
            public IQueryField LeftValue { get; set; }
            public LogicalQualifier Qualifier { get; set; }
            public IQueryField RightValue { get; set; }

            public List<ConditionGroup> Children { get; set; } = new();

            public Condition(string expressionVariable, IQueryField leftValue, LogicalQualifier qualifier, IQueryField rightValue)
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
            var conditionCollection = new ConditionCollection(queryBatch, conditionsText, leftHandAliasOfJoin);

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

                var leftAndRight = ParseRightAndLeft(conditionCollection, parentTokenizer, tokenizer);

                //var subExpressionText = tokenizer.Substring(startOfSubExpression, tokenizer.Caret - startOfSubExpression);
                //var expressionVariable = conditionCollection.NextExpressionVariable();

                lastCondition = new Condition(leftAndRight.ExpressionVariable, leftAndRight.Left, leftAndRight.Qualifier, leftAndRight.Right);

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

        static ConditionLeftAndRight ParseRightAndLeft(ConditionCollection conditionCollection, Tokenizer parentTokenizer, Tokenizer tokenizer)
        {
            //sw.Text LIKE $s_0$ and Length ( sw.Text ) / $n_11$ < sw.Id and sw.Text != sw.Text + $s_1$ and sw.Text LIKE $s_2$ and sw.Text like $s_3$ and ( sw.LanguageId >= $n_12$ OR sw.LanguageId <= $n_13$ ) and sw.Id > $n_14$
            if (tokenizer.Contains("Length"))
            {
                //
            }

            int startLeftRightCaret = tokenizer.Caret;
            int startConditionSetCaret = tokenizer.Caret;
            string? leftExpressionString = null;
            string? rightExpressionString = null;

            LogicalQualifier logicalQualifier = LogicalQualifier.None;

            //Here we are just validating the condition tokens and finding the end of the condition pair values as well as the logical qualifier.
            while (!tokenizer.IsExhausted())
            {
                string token = tokenizer.GetNext();

                if (StaticConditionHelpers.IsLogicalQualifier(token))
                {
                    //This is a logical qualifier, we're all good. We now have the left expression and the qualifier.
                    leftExpressionString = tokenizer.Substring(startLeftRightCaret, tokenizer.Caret - startLeftRightCaret).Trim();
                    logicalQualifier = StaticConditionHelpers.ParseLogicalQualifier(token);
                    tokenizer.EatNext();
                    startLeftRightCaret = tokenizer.Caret;
                }
                else if (StaticConditionHelpers.IsLogicalConnector(token))
                {
                    //This is a logical qualifier, we're all good. We now have the right expression.
                    break;
                }
                else if (token.Length == 1 && (token[0].IsTokenConnectorCharacter() || token[0].IsMathematicalOperator()))
                {
                    //This is a connector character, we're all good.
                    tokenizer.EatNext();
                }
                else if (token.StartsWith("$s_") && token.EndsWith('$')) //A string placeholder.
                {
                    //This is a string placeholder, we're all good.
                    tokenizer.EatNext();
                }
                else if (token.StartsWith("$n_") && token.EndsWith('$')) //A numeric placeholder.
                {
                    //This is a numeric placeholder, we're all good.
                    tokenizer.EatNext();
                }
                else if (token.StartsWith("$n_") && token.EndsWith('$')) //A numeric placeholder.
                {
                    //This is a numeric placeholder, we're all good.
                    tokenizer.EatNext();
                }
                else if (ScalerFunctionCollection.TryGetFunction(token, out var scalerFunction))
                {
                    if (!tokenizer.IsNextNonIdentifier(['(']))
                    {
                        throw new KbParserException($"Function [{token}] must be called with parentheses.");
                    }
                    tokenizer.EatNext();

                    tokenizer.EatGetMatchingScope('(', ')');

                    //This is a scaler function, we're all good.
                }
                else if (token.IsQueryFieldIdentifier())
                {
                    if (tokenizer.IsNextNonIdentifier(['(']))
                    {
                        //The character after this identifier is an open parenthesis, so this
                        //  looks like a function call but the function is undefined.
                        throw new KbParserException($"Function [{token}] is undefined.");
                    }

                    //This is a document field, we're all good.
                    tokenizer.EatNext();
                }
                else
                {
                    throw new NotImplementedException();
                }
            }

            rightExpressionString = tokenizer.Substring(startLeftRightCaret, tokenizer.Caret - startLeftRightCaret).Trim();

            if (tokenizer.Contains("Length"))
            {
                //
            }

            var left = StaticParserField.Parse(parentTokenizer, leftExpressionString.EnsureNotNullOrEmpty(), ref conditionCollection.FieldCollection);
            var right = StaticParserField.Parse(parentTokenizer, rightExpressionString.EnsureNotNullOrEmpty(), ref conditionCollection.FieldCollection);

            var result = new ConditionLeftAndRight(conditionCollection.NextExpressionVariable(), left, logicalQualifier, right);

            string conditionSetText = tokenizer.Substring(startConditionSetCaret, tokenizer.Caret - startConditionSetCaret).Trim();
            conditionCollection.FinalExpression = conditionCollection.FinalExpression.ReplaceFirst(conditionSetText, result.ExpressionVariable);

            return result;
        }
    }
}
