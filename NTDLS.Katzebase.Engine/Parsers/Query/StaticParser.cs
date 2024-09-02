using NTDLS.Katzebase.Client.Exceptions;
using NTDLS.Katzebase.Engine.Functions.Aggregate;
using NTDLS.Katzebase.Engine.Functions.Scaler;
using NTDLS.Katzebase.Engine.Parsers.Query.Fields;
using NTDLS.Katzebase.Engine.Parsers.Query.Fields.Expressions;
using NTDLS.Katzebase.Engine.Parsers.Query.Functions;
using System.Text;
using static NTDLS.Katzebase.Engine.Library.EngineConstants;

namespace NTDLS.Katzebase.Engine.Parsers.Query
{
    public static class StaticParser
    {
        /// <summary>
        /// Parses the field expressions for a "select" or "select into" query.
        /// </summary>
        public static QueryFieldCollection ParseSelectFields(Tokenizer queryTokenizer)
        {
            var queryFields = new QueryFieldCollection();

            //Get the position which represents the end of the select list.
            int stopAt = queryTokenizer.InertGetNextIndexOf([" from ", " into "]);

            //Get the text for all of the select fields.
            var fieldsSegment = queryTokenizer.SubString(stopAt);

            //Split the select fields on the comma, respecting any commas in function scopes.
            var fields = fieldsSegment.ScopeSensitiveSplit();

            HashSet<QueryFieldDocumentIdentifier> documentIdentifiers = new();

            foreach (var field in fields)
            {
                string fieldAlias = string.Empty;

                //Parse the field alias.
                int aliasIndex = field.IndexOf(" as ", StringComparison.InvariantCultureIgnoreCase);
                if (aliasIndex > 0)
                {
                    fieldAlias = field.Substring(aliasIndex + 4).Trim();
                }

                var aliasRemovedFieldText = (aliasIndex > 0 ? field.Substring(0, aliasIndex) : field).Trim();

                var queryField = ParseField(aliasRemovedFieldText, queryTokenizer, ref documentIdentifiers);

                //If the query didn't provide an alias, figure one out.
                if (string.IsNullOrWhiteSpace(fieldAlias))
                {
                    if (queryField is QueryFieldDocumentIdentifier queryFieldDocumentIdentifier)
                    {
                        fieldAlias = queryFieldDocumentIdentifier.Name;
                    }
                    else
                    {
                        fieldAlias = queryFields.GetNextFieldAlias();
                    }
                }

                queryFields.Add(new QueryField(fieldAlias, queryField));
            }

            queryFields.DocumentIdentifiers = documentIdentifiers;

            return queryFields;
        }

        private static IQueryField ParseField(string givenFieldText, Tokenizer queryTokenizer, ref HashSet<QueryFieldDocumentIdentifier> documentIdentifiers)
        {
            #region This is a single value (document field, number or string), the simple case.

            Tokenizer tokenizer = new(givenFieldText);

            string token = tokenizer.GetNext();

            if (tokenizer.IsEnd())
            {
                if (token.IsIdentifier())
                {
                    if (ScalerFunctionCollection.TryGetFunction(token, out var _))
                    {
                        //This is a function call, but it is the only token - that's not a valid function call.
                        throw new KbParserException($"Function scaler expects parentheses: [{token}]");
                    }
                    if (AggregateFunctionCollection.TryGetFunction(token, out var _))
                    {
                        //This is a function call, but it is the only token - that's not a valid function call.
                        throw new KbParserException($"Function aggregate expects parentheses: [{token}]");
                    }

                    //This is not a function (those require evaluation) so its a single identifier, likely a document field name.
                    var queryFieldDocumentIdentifier = new QueryFieldDocumentIdentifier(token);
                    documentIdentifiers.Add(queryFieldDocumentIdentifier);
                    return queryFieldDocumentIdentifier;
                }
                else if (IsNumericExpression(token))
                {
                    return new QueryFieldConstantNumeric(token);
                }
                else
                {
                    return new QueryFieldConstantString(token);
                }
            }

            #endregion

            #region Fields that require expression evaluation.

            var validateNumberOfParameters = givenFieldText.ScopeSensitiveSplit();
            if (validateNumberOfParameters.Count > 1)
            {
                //We are testing to make sure that there are no commas that fall outside of function scopes.
                //This is because each call to ParseField should collapse to a single value.
                //E.g. "10 + Length() * 10" is allowed, but "10 + Length() * 10, Length()" is not allowed.
                throw new KbParserException($"Single field should not contain multiple values: [{givenFieldText}]");
            }

            //This field is going to require evaluation, so figure out if its a number or a string.

            if (IsNumericExpression(givenFieldText))
            {
                IQueryFieldExpression expression = new QueryFieldExpressionNumeric(givenFieldText);
                expression.Expression = ParseEvaluationRecursive(ref expression, givenFieldText, ref documentIdentifiers);
                return expression;
            }
            else
            {
                IQueryFieldExpression expression = new QueryFieldExpressionString();
                expression.Expression = ParseEvaluationRecursive(ref expression, givenFieldText, ref documentIdentifiers);
                return expression;
            }

            #endregion
        }

        private static string ParseEvaluationRecursive(ref IQueryFieldExpression rootQueryFieldExpression, string givenExpressionText, ref HashSet<QueryFieldDocumentIdentifier> documentIdentifiers)
        {
            Tokenizer tokenizer = new(givenExpressionText);

            StringBuilder buffer = new();

            while (!tokenizer.IsEnd())
            {
                int positionBeforeToken = tokenizer.CaretPosition;

                string token = tokenizer.GetNext();

                if (token.StartsWith("$s_") && token.EndsWith('$')) //A string placeholder.
                {
                    buffer.Append(token);
                }
                else if (token.StartsWith("$n_") && token.EndsWith('$')) //A numeric placeholder.
                {
                    buffer.Append(token);
                }
                else if (ScalerFunctionCollection.TryGetFunction(token, out var scalerFunction))
                {
                    //The expression key is used to match the function calls to the token in the parent expression.
                    var expressionKey = rootQueryFieldExpression.GetKeyExpressionKey();
                    var basicDataType = scalerFunction.ReturnType == KbScalerFunctionParameterType.Numeric ? BasicDataType.Numeric : BasicDataType.String;
                    var queryFieldExpressionFunction = new QueryFieldExpressionFunctionScaler(scalerFunction.Name, expressionKey, basicDataType);

                    ParseFunctionCallRecursive(ref rootQueryFieldExpression, givenExpressionText, queryFieldExpressionFunction, ref documentIdentifiers, tokenizer, positionBeforeToken, expressionKey);
                }
                else if (AggregateFunctionCollection.TryGetFunction(token, out var aggregateFunction))
                {
                    //The expression key is used to match the function calls to the token in the parent expression.
                    var expressionKey = rootQueryFieldExpression.GetKeyExpressionKey();
                    var queryFieldExpressionFunction = new QueryFieldExpressionFunctionScaler(aggregateFunction.Name, expressionKey, BasicDataType.Numeric);

                    ParseFunctionCallRecursive(ref rootQueryFieldExpression, givenExpressionText, queryFieldExpressionFunction, ref documentIdentifiers, tokenizer, positionBeforeToken, expressionKey);
                }
                else if (token.IsIdentifier())
                {
                    documentIdentifiers.Add(new QueryFieldDocumentIdentifier(token));

                    if (tokenizer.InertIsNextNonIdentifier(['(']))
                    {
                        //The character after this identifier is an open parenthesis, so this
                        //  looks like a function call but the function is undefined.
                        throw new KbParserException($"Function [{token}] is undefined.");
                    }
                }
                else
                {
                    buffer.Append(token);
                }

            }

            return givenExpressionText;
        }

        /// <summary>
        /// Parses a function call and its parameters.
        /// Its unfortunate that this function needs so many parameters, the alternative is duplicating the code for scaler and aggregate functions.
        /// </summary>
        private static void ParseFunctionCallRecursive(ref IQueryFieldExpression rootQueryFieldExpression, string givenExpressionText,
            IQueryFieldExpressionFunction queryFieldExpressionFunction, ref HashSet<QueryFieldDocumentIdentifier> documentIdentifiers,
            Tokenizer tokenizer, int positionBeforeToken, string expressionKey)
        {
            //This contains the text between the open and close parenthesis of a function call, but not the parenthesis themselves or the function name.
            string functionCallParametersSegmentText = tokenizer.GetMatchingBraces('(', ')');

            //This contains the entire call signature. e.g. functionName(all, function, parameters).
            string wholeFunctionCallText = tokenizer.InertSubString(positionBeforeToken, tokenizer.CaretPosition - positionBeforeToken);

            //Replace the part of the expression for which we are going to handle with a function call, with the
            //  expressionKey so we can fill in the value later when we evaluate the expression and execute the function.
            givenExpressionText = givenExpressionText.Replace(wholeFunctionCallText, expressionKey);

            var functionCallParametersText = functionCallParametersSegmentText.ScopeSensitiveSplit();
            foreach (var functionCallParameterText in functionCallParametersText)
            {
                //Recursively process the function parameters.
                var resultingExpressionString = ParseEvaluationRecursive(ref rootQueryFieldExpression, functionCallParameterText, ref documentIdentifiers);

                IExpressionFunctionParameter? parameter = null;

                //TODO: ensure that this is a single token.
                if (resultingExpressionString.StartsWith("$x_") && resultingExpressionString.EndsWith('$') && IsSingleToken(resultingExpressionString))
                {
                    //This is a function call result placeholder.
                    parameter = new ExpressionFunctionParameterFunction(resultingExpressionString);
                }
                else if (IsNumericExpression(resultingExpressionString, rootQueryFieldExpression))
                {
                    //This expression contains only numeric placeholders.
                    parameter = new ExpressionFunctionParameterNumeric(resultingExpressionString);
                }
                else
                {
                    //This expression contains non-numeric placeholders.
                    parameter = new ExpressionFunctionParameterString(resultingExpressionString);
                }

                queryFieldExpressionFunction.Parameters.Add(parameter);
            }

            rootQueryFieldExpression.FunctionDependencies.Add(queryFieldExpressionFunction);
        }


        private static bool IsSingleToken(string token)
        {
            if (token.StartsWith('$') == token.EndsWith('$') && token.Length >= 5) //Example: $n_0$, $x_0$, $p_0$
            {
                if (char.IsLetter(token[1]) && token[2] == '_')
                {
                    return token.Substring(3, token.Length - 4).All(char.IsDigit); //Validate the number in the middle of the markers.
                }
            }

            return false;

        }

        /// <summary>
        /// /// Returns true if all variables, placeholders and functions return numeric values.
        /// </summary>
        /// <param name="expressionText">The text to be evaluated for numeric or string operations.</param>
        /// <param name="rootExpressionEvaluation">Passed if available to determine the return type of any function references.</param>
        /// <returns></returns>
        private static bool IsNumericExpression(string expressionText, IQueryFieldExpression? rootExpressionEvaluation = null)
        {
            Tokenizer tokenizer = new(expressionText);

            while (!tokenizer.IsEnd())
            {
                if (tokenizer.InertIsNextCharacter(c => c.IsMathematicalOperator()))
                {
                    tokenizer.SkipNextCharacter();
                    continue;
                }

                string token = tokenizer.GetNext();
                if (string.IsNullOrEmpty(token))
                {
                    break;
                }

                if (token.StartsWith("$x_") && token.EndsWith('$'))
                {
                    //This is a function result placeholder.

                    if (rootExpressionEvaluation == null)
                    {
                        throw new KbParserException($"Function reference found without root expression: [{token}].");
                    }

                    //Find the function call so we can check the function return type.
                    var referencedFunction = rootExpressionEvaluation.FunctionDependencies.Single(f => f.ExpressionKey == token);

                    if (referencedFunction.ReturnType != BasicDataType.Numeric)
                    {
                        //This function returns something other then numeric, so we are evaluating strings.
                        return false;
                    }
                    continue;
                }
                else if (token.StartsWith("$s_") && token.EndsWith('$'))
                {
                    //This is a string, so this is not a numeric operation.
                    return false;
                }
                else if (token.StartsWith("$n_") && token.EndsWith('$'))
                {
                    //This is a number placeholder, so we still have a valid numeric operation.
                    continue;
                }
                else if (ScalerFunctionCollection.TryGetFunction(token, out var scalerFunction))
                {
                    if (scalerFunction.ReturnType == KbScalerFunctionParameterType.Numeric)
                    {
                        //This function returns a number, so we still have a valid numeric operation.

                        //Skip the function call.
                        string functionBody = tokenizer.GetMatchingBraces('(', ')');
                        continue;
                    }
                    else
                    {
                        //This function returns a non-number, so this is not a numeric operation.
                        return false;
                    }
                }
                else if (AggregateFunctionCollection.TryGetFunction(token, out var aggregateFunction))
                {
                    //This is an aggregate function that can only return a number, so we still have a valid numeric operation.

                    //Skip the function call.
                    string functionBody = tokenizer.GetMatchingBraces('(', ')');
                    continue;
                }
                else
                {
                    //throw new KbParserException($"Invalid query. Found [{token}], expected: scaler function.");

                    //This is likely a document field.
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Returns true if the next token in the sequence is a valid token as would be expected as the start of a new query.
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static bool IsNextStartOfQuery(string token, out QueryType type)
        {
            return Enum.TryParse(token.ToLowerInvariant(), true, out type) //Enum parse.
                && Enum.IsDefined(typeof(QueryType), type) //Is enum value über lenient.
                && int.TryParse(token, out _) == false; //Is not number, because enum parsing is "too" flexible.
        }
    }
}
