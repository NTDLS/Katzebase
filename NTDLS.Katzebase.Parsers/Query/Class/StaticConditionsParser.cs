using NTDLS.Helpers;
using NTDLS.Katzebase.Client.Exceptions;
using NTDLS.Katzebase.Parsers.Functions.Scaler;
using NTDLS.Katzebase.Parsers.Query.Class.Helpers;
using NTDLS.Katzebase.Parsers.Query.SupportingTypes;
using NTDLS.Katzebase.Parsers.Query.WhereAndJoinConditions;
using NTDLS.Katzebase.Parsers.Query.WhereAndJoinConditions.Helpers;
using NTDLS.Katzebase.Parsers.Tokens;
using static NTDLS.Katzebase.Parsers.Constants;

namespace NTDLS.Katzebase.Parsers.Query.Class
{
    public static class StaticConditionsParser
    {
        /// <summary>
        /// Parse the conditions, we are going to build a full expression with all of the condition values replaced with tokens so we
        /// can build a mathematical expression, but we are also going to build sets of groups for which there will be one per OR expression,
        /// and all conditions in a ConditionGroup will be comprised solely of AND conditions. This way we can use the groups to match indexes
        /// before evaluating the whole expression on the limited set of documents we derived from the indexing operations.
        /// </summary>
        public static ConditionCollection Parse(QueryBatch queryBatch, Tokenizer tokenizer, string conditionsText, int endOfWhereCaret, string leftHandAliasOfJoin = "")
        {
            var conditionCollection = new ConditionCollection(queryBatch, conditionsText, leftHandAliasOfJoin);

            conditionCollection.MathematicalExpression = conditionCollection.MathematicalExpression
                .Replace(" OR ", " || ", StringComparison.InvariantCultureIgnoreCase)
                .Replace(" AND ", " && ", StringComparison.InvariantCultureIgnoreCase);

            ParseRecursive(queryBatch, tokenizer, conditionCollection, conditionCollection, conditionsText, endOfWhereCaret);

            conditionCollection.MathematicalExpression = conditionCollection.MathematicalExpression.Replace("  ", " ").Trim();
            conditionCollection.Hash = StaticParserUtility.GetSHA256Hash(conditionCollection.MathematicalExpression);

            return conditionCollection;
        }

        private static void ParseRecursive(QueryBatch queryBatch, Tokenizer tokenizer,
            ConditionCollection conditionCollection, ConditionGroup parentConditionGroup,
            string conditionsText, int endOfWhereCaret, ConditionGroup? givenCurrentConditionGroup = null)
        {
            var lastLogicalConnector = LogicalConnector.None;

            ConditionGroup? currentConditionGroup = givenCurrentConditionGroup;

            while (tokenizer.Caret < endOfWhereCaret && !tokenizer.IsExhausted())
            {
                if (tokenizer.TryIsNextCharacter('('))
                {
                    //When we encounter an "(", we create a new condition group.
                    currentConditionGroup = new ConditionGroup(lastLogicalConnector);
                    parentConditionGroup.Collection.Add(currentConditionGroup);

                    string subConditionsText = tokenizer.MatchingScope(out var endOfSubExpressionCaret);

                    tokenizer.EatIfNext('(');

                    ParseRecursive(queryBatch, tokenizer, conditionCollection,
                        currentConditionGroup, subConditionsText, endOfSubExpressionCaret, currentConditionGroup);

                    tokenizer.EatIfNext(')');

                    //After we finish recursively parsing the parentheses, we null out the
                    //  current group because whatever we find next will need to be in a new group.
                    currentConditionGroup = null;
                }
                else
                {
                    if (currentConditionGroup == null)
                    {
                        currentConditionGroup = new ConditionGroup(lastLogicalConnector);
                        parentConditionGroup.Collection.Add(currentConditionGroup);
                    }

                    var leftAndRight = ParseRightAndLeft(conditionCollection, tokenizer, endOfWhereCaret);
                    currentConditionGroup.Collection.Add(new ConditionEntry(leftAndRight));
                }

                if (tokenizer.Caret < endOfWhereCaret && !tokenizer.IsExhausted())
                {
                    lastLogicalConnector = tokenizer.EatIfNextEnum<LogicalConnector>();
                    if (lastLogicalConnector == LogicalConnector.Or)
                    {
                        //When we encounter an OR, we null out the current group because whatever we find next will need to be in a new group.
                        currentConditionGroup = null;
                    }
                }
            }
        }

        private static ConditionEntry.ConditionValuesPair ParseRightAndLeft(ConditionCollection conditionCollection, Tokenizer tokenizer, int endOfWhereCaret)
        {
            int startLeftRightCaret = tokenizer.Caret;
            int startConditionSetCaret = tokenizer.Caret;
            string? leftExpressionString = null;
            string? rightExpressionString = null;

            LogicalQualifier logicalQualifier = LogicalQualifier.None;

            //Here we are just validating the condition tokens and finding the end of the condition pair values as well as the logical qualifier.
            while (tokenizer.Caret < endOfWhereCaret && !tokenizer.IsExhausted())
            {
                string token = tokenizer.GetNext();

                if (tokenizer.Caret >= endOfWhereCaret)
                {
                    //Found the end of the conditions, we're all good. We now have the right expression.
                    break;
                }
                else if (StaticConditionHelpers.IsLogicalQualifier(token))
                {
                    //This is a logical qualifier, we're all good. We now have the left expression and the qualifier.
                    leftExpressionString = tokenizer.Substring(startLeftRightCaret, tokenizer.Caret - startLeftRightCaret).Trim();
                    logicalQualifier = StaticConditionHelpers.ParseLogicalQualifier(tokenizer, token);
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
                    if (!tokenizer.TryIsNextNonIdentifier(['(']))
                    {
                        throw new KbParserException(tokenizer.GetCurrentLineNumber(), $"Function must be called with parentheses: [{token}].");
                    }
                    //This is a scaler function, we're all good.

                    tokenizer.EatNext();
                    tokenizer.EatGetMatchingScope('(', ')');
                }
                else if (token.IsQueryFieldIdentifier())
                {
                    if (tokenizer.TryIsNextNonIdentifier(['(']))
                    {
                        //The character after this identifier is an open parenthesis, so this
                        //  looks like a function call but the function is undefined.
                        throw new KbParserException(tokenizer.GetCurrentLineNumber(), $"Function is undefined: [{token}].");
                    }

                    //This is a document field, we're all good.
                    tokenizer.EatNext();
                }
                else
                {
                    throw new KbParserException(tokenizer.GetCurrentLineNumber(), $"Condition token is invalid: [{token}].");
                }
            }

            rightExpressionString = tokenizer.Substring(startLeftRightCaret, tokenizer.Caret - startLeftRightCaret).Trim();

            if (logicalQualifier == LogicalQualifier.None)
            {
                throw new KbParserException(tokenizer.GetCurrentLineNumber(), $"Missing logical qualifier.");
            }

            if (string.IsNullOrEmpty(leftExpressionString))
            {
                throw new KbParserException(tokenizer.GetCurrentLineNumber(), $"Missing left expression.");
            }

            if (string.IsNullOrEmpty(rightExpressionString))
            {
                throw new KbParserException(tokenizer.GetCurrentLineNumber(), $"Missing right expression.");
            }

            var left = StaticParserField.Parse(tokenizer, leftExpressionString, conditionCollection.FieldCollection);
            var right = StaticParserField.Parse(tokenizer, rightExpressionString, conditionCollection.FieldCollection);

            var conditionPair = new ConditionEntry.ConditionValuesPair(conditionCollection.NextExpressionVariable(), left, logicalQualifier, right);

            //Replace the condition with the name of the variable that must be evaluated to determine the value for this condition.
            string conditionSetText = tokenizer.Substring(startConditionSetCaret, tokenizer.Caret - startConditionSetCaret).Trim();
            conditionCollection.MathematicalExpression = conditionCollection.MathematicalExpression.ReplaceFirst(conditionSetText, conditionPair.ExpressionVariable);

            conditionCollection.FieldCollection.Add(new QueryField(string.Empty, conditionCollection.FieldCollection.Count, left));
            conditionCollection.FieldCollection.Add(new QueryField(string.Empty, conditionCollection.FieldCollection.Count, right));

            return conditionPair;
        }
    }
}
