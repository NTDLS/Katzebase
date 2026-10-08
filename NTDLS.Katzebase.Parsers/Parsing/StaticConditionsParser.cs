using NTDLS.Helpers;
using NTDLS.Katzebase.Api.Exceptions;
using NTDLS.Katzebase.Parsers.Conditions;
using NTDLS.Katzebase.Parsers.Fields;
using NTDLS.Katzebase.Parsers.Functions.Scalar;
using NTDLS.Katzebase.Parsers.Tokens;
using System.Security.Cryptography;
using System.Text;
using static NTDLS.Katzebase.Parsers.Constants;

namespace NTDLS.Katzebase.Parsers.Parsing
{
    public static class StaticConditionsParser
    {
        /// <summary>
        /// Parse the conditions, we are going to build a full expression with all of the condition values replaced with tokens so we
        /// can build a mathematical expression, but we are also going to build sets of groups for which there will be one per OR expression,
        /// and all conditions in a ConditionGroup will be comprised solely of AND conditions. This way we can use the groups to match indexes
        /// before evaluating the whole expression on the limited set of documents we derived from the indexing operations.
        /// </summary>
        public static ConditionCollection Parse(PreparedQueryBatch queryBatch, Tokenizer tokenizer, string conditionsText, int endOfWhereCaret, string leftHandAliasOfJoin = "")
        {
            var conditionCollection = new ConditionCollection(queryBatch, conditionsText, leftHandAliasOfJoin);

            conditionCollection.MathematicalExpression = RewriteLogicalConnectors(conditionCollection.MathematicalExpression);

            using var incrementalSha256 = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);

            conditionCollection.IncrementalSha256 = incrementalSha256;

            ParseRecursive(queryBatch, tokenizer, conditionCollection, conditionCollection, endOfWhereCaret);
            conditionCollection.MathematicalExpression = conditionCollection.MathematicalExpression.Replace("  ", " ").Trim();

            conditionCollection.IncrementalSha256.AppendData(Encoding.UTF8.GetBytes(conditionCollection.MathematicalExpression));
            conditionCollection.IncrementalSha256.AppendData(Encoding.UTF8.GetBytes(leftHandAliasOfJoin));

            conditionCollection.Hash = Convert.ToHexString(conditionCollection.IncrementalSha256.GetCurrentHash());

            return conditionCollection;
        }

        /// <summary>
        /// Rewrites the AND and OR connectors as the operators used by the MathematicalExpression.
        /// </summary>
        private static string RewriteLogicalConnectors(string text)
            => text.Replace(" OR ", " || ", StringComparison.InvariantCultureIgnoreCase)
                .Replace(" AND ", " && ", StringComparison.InvariantCultureIgnoreCase);

        private static void ParseRecursive(PreparedQueryBatch queryBatch, Tokenizer tokenizer,
            ConditionCollection conditionCollection, ConditionGroup parentConditionGroup,
            int endOfWhereCaret, ConditionGroup? givenCurrentConditionGroup = null)
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

                    tokenizer.MatchingScope(out var endOfSubExpressionCaret);

                    tokenizer.EatIfNext('(');

                    ParseRecursive(queryBatch, tokenizer, conditionCollection,
                        currentConditionGroup, endOfSubExpressionCaret, currentConditionGroup);

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
            string? rightExpressionString;

            //For BETWEEN and NOT BETWEEN ("x BETWEEN low AND high"), the low bound is found at the separating AND
            //  and becomes the right expression, the expression that follows the AND is the high bound.
            string? lowBoundExpressionString = null;
            string? rightHighExpressionString = null;
            bool isAwaitingBetweenSeparator = false;
            int parenthesesDepth = 0;

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
                else if (token.Is("not"))
                {
                    leftExpressionString = tokenizer.Substring(startLeftRightCaret, tokenizer.Caret - startLeftRightCaret).Trim();

                    tokenizer.EatNext();

                    //This is a logical qualifier, we're all good. We now have the left expression and the qualifier.
                    if (tokenizer.TryEatIfNext("between"))
                    {
                        logicalQualifier = LogicalQualifier.NotBetween;
                        isAwaitingBetweenSeparator = true;
                    }
                    else if (tokenizer.TryEatIfNext("like"))
                    {
                        logicalQualifier = LogicalQualifier.NotLike;
                    }
                    else
                    {
                        throw new KbParserException(tokenizer.GetCurrentLineNumber(), $"Expected [like] or [between], found: [{tokenizer.EatGetNextResolved()}].");
                    }
                    startLeftRightCaret = tokenizer.Caret;
                }
                else if (StaticConditionHelpers.IsLogicalQualifier(token))
                {
                    //This is a logical qualifier, we're all good. We now have the left expression and the qualifier.
                    leftExpressionString = tokenizer.Substring(startLeftRightCaret, tokenizer.Caret - startLeftRightCaret).Trim();
                    logicalQualifier = StaticConditionHelpers.ParseLogicalQualifier(tokenizer, token);
                    isAwaitingBetweenSeparator = logicalQualifier == LogicalQualifier.Between;
                    tokenizer.EatNext();
                    startLeftRightCaret = tokenizer.Caret;
                }
                else if (isAwaitingBetweenSeparator && parenthesesDepth == 0 && token.Is("and"))
                {
                    //This AND separates the low and high bounds of a BETWEEN, it does not connect two conditions.
                    lowBoundExpressionString = tokenizer.Substring(startLeftRightCaret, tokenizer.Caret - startLeftRightCaret).Trim();
                    isAwaitingBetweenSeparator = false;
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
                    if (token[0] == '(') parenthesesDepth++;
                    else if (token[0] == ')') parenthesesDepth--;
                    tokenizer.EatNext();
                }
                else if (token.StartsWith("$s_") && token.EndsWith('$')) //A string placeholder.
                {
                    //This is a string placeholder, we're all good.
                    tokenizer.EatNext();
                }
                else if (token.StartsWith("$v_") && token.EndsWith('$')) //A variable placeholder.
                {
                    //This is a variable placeholder, we're all good.
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
                else if (ScalarFunctionCollection.TryGetFunction(token, out _))
                {
                    if (!tokenizer.TryIsNextNonIdentifier(['(']))
                    {
                        throw new KbParserException(tokenizer.GetCurrentLineNumber(), $"Function must be called with parentheses: [{token}].");
                    }
                    //This is a scalar function, we're all good.

                    tokenizer.EatNext();
                    tokenizer.EatGetMatchingScope('(', ')');
                }
                else if (token.IsQueryFieldIdentifier())
                {
                    tokenizer.EatNext();
                    tokenizer.EatWhiteSpace();

                    //Only the character directly after the identifier (and any white space) is checked: TryIsNextNonIdentifier()
                    //  also skips over words, so it would treat "Name LIKE ('a')" or "Id BETWEEN (1 + 1) AND 4" as function calls.
                    if (tokenizer.TryIsNextCharacter('('))
                    {
                        //The character after this identifier is an open parenthesis, so this
                        //  looks like a function call but the function is undefined.
                        throw new KbParserException(tokenizer.GetCurrentLineNumber(), $"Function is undefined: [{token}].");
                    }

                    //This is a document field, we're all good.
                }
                else
                {
                    throw new KbParserException(tokenizer.GetCurrentLineNumber(), $"Condition token is invalid: [{token}].");
                }
            }

            rightExpressionString = tokenizer.Substring(startLeftRightCaret, tokenizer.Caret - startLeftRightCaret).Trim();

            if (logicalQualifier == LogicalQualifier.Between || logicalQualifier == LogicalQualifier.NotBetween)
            {
                if (lowBoundExpressionString == null)
                {
                    throw new KbParserException(tokenizer.GetCurrentLineNumber(), "Expected [and] between the low and high values of [between].");
                }

                rightHighExpressionString = rightExpressionString;
                rightExpressionString = lowBoundExpressionString;

                if (string.IsNullOrEmpty(rightHighExpressionString))
                {
                    throw new KbParserException(tokenizer.GetCurrentLineNumber(), $"Missing high value of [between].");
                }
            }

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

            conditionCollection.IncrementalSha256?.AppendData(Encoding.UTF8.GetBytes($"{leftExpressionString}:{rightExpressionString}:{rightHighExpressionString}"));

            var left = StaticParserField.Parse(tokenizer, leftExpressionString, conditionCollection.FieldCollection);
            var right = StaticParserField.Parse(tokenizer, rightExpressionString, conditionCollection.FieldCollection);
            var rightHigh = rightHighExpressionString == null ? null
                : StaticParserField.Parse(tokenizer, rightHighExpressionString, conditionCollection.FieldCollection);

            var conditionPair = new ConditionEntry.ConditionValuesPair(conditionCollection.NextExpressionVariable(), left, logicalQualifier, right, rightHigh);

            //Replace the condition with the name of the variable that must be evaluated to determine the value for this condition.
            //  Parse() rewrote the connectors in the MathematicalExpression (" AND " to " && "), so the condition text is rewritten the same
            //  way to find it. This only changes the text of a BETWEEN, whose AND separates its low and high values.
            string conditionSetText = RewriteLogicalConnectors(tokenizer.Substring(startConditionSetCaret, tokenizer.Caret - startConditionSetCaret).Trim());
            conditionCollection.MathematicalExpression = Text.ReplaceFirstOccurrence(conditionCollection.MathematicalExpression, conditionSetText, conditionPair.ExpressionVariable);

            conditionCollection.FieldCollection.Add(new QueryField(string.Empty, conditionCollection.FieldCollection.Count, left));
            conditionCollection.FieldCollection.Add(new QueryField(string.Empty, conditionCollection.FieldCollection.Count, right));
            if (rightHigh != null)
            {
                conditionCollection.FieldCollection.Add(new QueryField(string.Empty, conditionCollection.FieldCollection.Count, rightHigh));
            }

            return conditionPair;
        }
    }
}
