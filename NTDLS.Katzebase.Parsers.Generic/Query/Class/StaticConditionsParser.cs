using NTDLS.Helpers;
using NTDLS.Katzebase.Client.Exceptions;
using NTDLS.Katzebase.Parsers.Functions.Scaler;
using NTDLS.Katzebase.Parsers.Query.Class.Helpers;
using NTDLS.Katzebase.Parsers.Query.SupportingTypes;
using NTDLS.Katzebase.Parsers.Query.WhereAndJoinConditions;
using NTDLS.Katzebase.Parsers.Query.WhereAndJoinConditions.Helpers;
using NTDLS.Katzebase.Parsers.Tokens;
using NTDLS.Katzebase.Parsers.Interfaces;
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
        public static ConditionCollection<TData> Parse<TData>(QueryBatch<TData> queryBatch, Tokenizer<TData> tokenizer, string conditionsText, Func<string, TData> parseStringToDoc, Func<string, TData> castStringToDoc, string leftHandAliasOfJoin = "")
            where TData : IStringable
        {
            var conditionCollection = new ConditionCollection<TData>(queryBatch, conditionsText, leftHandAliasOfJoin);

            conditionCollection.MathematicalExpression = conditionCollection.MathematicalExpression
                .Replace(" OR ", " || ", StringComparison.InvariantCultureIgnoreCase)
                .Replace(" AND ", " && ", StringComparison.InvariantCultureIgnoreCase);

            ParseRecursive(queryBatch, tokenizer, conditionCollection, conditionCollection, conditionsText, parseStringToDoc, castStringToDoc);

            conditionCollection.MathematicalExpression = conditionCollection.MathematicalExpression.Replace("  ", " ").Trim();
            conditionCollection.Hash = StaticParserUtility.GetSHA256Hash(conditionCollection.MathematicalExpression);

            return conditionCollection;
        }

        private static void ParseRecursive<TData>(QueryBatch<TData> queryBatch, Tokenizer<TData> tokenizer,
            ConditionCollection<TData> conditionCollection, ConditionGroup<TData> parentConditionGroup,
            string conditionsText, 
            Func<string, TData> parseStringToDoc, Func<string, TData> castStringToDoc,
            ConditionGroup<TData>? givenCurrentConditionGroup = null
            )
            where TData : IStringable
        {
            var conditionTokenizer = new Tokenizer<TData>(conditionsText, parseStringToDoc);

            var lastLogicalConnector = LogicalConnector.None;

            ConditionGroup<TData>? currentConditionGroup = givenCurrentConditionGroup;

            while (!conditionTokenizer.IsExhausted())
            {
                if (conditionTokenizer.TryIsNextCharacter('('))
                {
                    //When we encounter an "(", we create a new condition group.
                    currentConditionGroup = new ConditionGroup<TData>(lastLogicalConnector);
                    parentConditionGroup.Collection.Add(currentConditionGroup);

                    string subConditionsText = conditionTokenizer.EatMatchingScope();
                    ParseRecursive(queryBatch, tokenizer, conditionCollection,
                        currentConditionGroup, subConditionsText, parseStringToDoc, castStringToDoc, currentConditionGroup);

                    //After we finish recursively parsing the parentheses, we null out the current group because whatever we find next will need to be in a new group.
                    currentConditionGroup = null;
                }
                else
                {
                    if (currentConditionGroup == null)
                    {
                        currentConditionGroup = new ConditionGroup<TData>(lastLogicalConnector);
                        parentConditionGroup.Collection.Add(currentConditionGroup);
                    }

                    var leftAndRight = ParseRightAndLeft(conditionCollection, tokenizer, conditionTokenizer, parseStringToDoc, castStringToDoc);
                    currentConditionGroup.Collection.Add(new ConditionEntry<TData>(leftAndRight));
                }

                if (!conditionTokenizer.IsExhausted())
                {
                    lastLogicalConnector = conditionTokenizer.EatIfNextEnum<LogicalConnector>();
                    if (lastLogicalConnector == LogicalConnector.Or)
                    {
                        //When we encounter an OR, we null out the current group because whatever we find next will need to be in a new group.
                        currentConditionGroup = null;
                    }
                }
            }
        }

        private static ConditionEntry<TData>.ConditionValuesPair<TData> ParseRightAndLeft<TData>(ConditionCollection<TData> conditionCollection, Tokenizer<TData> parentTokenizer, Tokenizer<TData> tokenizer, Func<string, TData> parseStringToDoc, Func<string, TData> castStringToDoc)
            where TData : IStringable
        {
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
                else if (ScalerFunctionCollection<TData>.TryGetFunction(token, out var scalerFunction))
                {
                    if (!tokenizer.IsNextNonIdentifier(['(']))
                    {
                        throw new KbParserException(parentTokenizer.GetCurrentLineNumber(), $"Function [{token}] must be called with parentheses.");
                    }
                    //This is a scaler function, we're all good.

                    tokenizer.EatNext();
                    tokenizer.EatGetMatchingScope('(', ')');
                }
                else if (token.IsQueryFieldIdentifier())
                {
                    if (tokenizer.IsNextNonIdentifier(['(']))
                    {
                        //The character after this identifier is an open parenthesis, so this
                        //  looks like a function call but the function is undefined.
                        throw new KbParserException(parentTokenizer.GetCurrentLineNumber(), $"Function [{token}] is undefined.");
                    }

                    //This is a document field, we're all good.
                    tokenizer.EatNext();
                }
                else
                {
                    throw new KbParserException(parentTokenizer.GetCurrentLineNumber(), $"Condition token [{token}] is invalid.");
                }
            }

            rightExpressionString = tokenizer.Substring(startLeftRightCaret, tokenizer.Caret - startLeftRightCaret).Trim();

            var left = StaticParserField<TData>.Parse(parentTokenizer, leftExpressionString.EnsureNotNullOrEmpty(), conditionCollection.FieldCollection, parseStringToDoc, castStringToDoc);
            var right = StaticParserField<TData>.Parse(parentTokenizer, rightExpressionString.EnsureNotNullOrEmpty(), conditionCollection.FieldCollection, parseStringToDoc, castStringToDoc);

            var conditionPair = new ConditionEntry<TData>.ConditionValuesPair<TData>(conditionCollection.NextExpressionVariable(), left, logicalQualifier, right);

            //Replace the condition with the name of the variable that must be evaluated to determine the value for this condition.
            string conditionSetText = tokenizer.Substring(startConditionSetCaret, tokenizer.Caret - startConditionSetCaret).Trim();
            conditionCollection.MathematicalExpression = conditionCollection.MathematicalExpression.ReplaceFirst(conditionSetText, conditionPair.ExpressionVariable);

            conditionCollection.FieldCollection.Add(new QueryField<TData>(string.Empty, conditionCollection.FieldCollection.Count, left));
            conditionCollection.FieldCollection.Add(new QueryField<TData>(string.Empty, conditionCollection.FieldCollection.Count, right));

            return conditionPair;
        }
    }
}
