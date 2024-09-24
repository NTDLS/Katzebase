using fs;
using NTDLS.Helpers;
using NTDLS.Katzebase.Client.Exceptions;
using NTDLS.Katzebase.Client.Types;
using NTDLS.Katzebase.Engine.Atomicity;
using NTDLS.Katzebase.Engine.Functions.Aggregate;
using NTDLS.Katzebase.Engine.Functions.Scaler;
using NTDLS.Katzebase.Engine.Parsers.Query;
using NTDLS.Katzebase.Engine.Parsers.Query.Exposed;
using NTDLS.Katzebase.Engine.Parsers.Query.Fields;
using NTDLS.Katzebase.Engine.Parsers.Query.Fields.Expressions;
using NTDLS.Katzebase.Engine.Parsers.Query.Functions;
using NTDLS.Katzebase.Engine.Parsers.Query.SupportingTypes;
using NTDLS.Katzebase.Engine.Parsers.Tokens;
using NTDLS.Katzebase.Engine.QueryProcessing.Searchers.Intersection;
using System.Text;
using static NTDLS.Katzebase.Engine.Parsers.Query.Fields.Expressions.ExpressionConstants;

namespace NTDLS.Katzebase.Engine.QueryProcessing
{
    internal static class StaticScalerExpressionProcessor
    {
        /// <summary>
        /// Resolves all of the query expressions (string concatenation, math and all recursive
        ///     function calls) on a row level and fills in the values in the resultingRows.
        /// </summary>
        public static void CollapseScalerRowExpressions(this SchemaIntersectionRowCollection resultingRows, Transaction transaction,
            PreparedQuery query, QueryFieldCollection fieldCollection)
        {
            //Resolve all expressions and fill in the row fields.
            foreach (var expressionField in fieldCollection.ExpressionFields.Where(o => o.CollapseType == CollapseType.Scaler))
            {
                foreach (var row in resultingRows)
                {
                    var collapsedResult = CollapseScalerExpression(transaction, query, fieldCollection, row.AuxiliaryFields, expressionField);
                    row.InsertValue(expressionField.FieldAlias, expressionField.Ordinal, collapsedResult);
                }
            }
        }

        /// <summary>
        /// Collapses a QueryField expression into a single value. This includes doing string concatenation, math and all recursive function calls.
        /// </summary>
        public static fstring? CollapseScalerQueryField(this IQueryField queryField, Transaction transaction,
            PreparedQuery query, QueryFieldCollection fieldCollection, KbInsensitiveDictionary<fstring, fstring?> auxiliaryFields)
        {
            if (queryField is QueryFieldExpressionNumeric expressionNumeric)
            {
                return CollapseScalerFunctionNumericParameter(transaction, query, fieldCollection, auxiliaryFields, expressionNumeric.FunctionDependencies, expressionNumeric.Value.s);
            }
            else if (queryField is QueryFieldExpressionString expressionString)
            {
                return CollapseScalerFunctionStringParameter(transaction, query, fieldCollection, auxiliaryFields, expressionString.FunctionDependencies, expressionString.Value.s);
            }
            else if (queryField is QueryFieldDocumentIdentifier documentIdentifier)
            {
                if (auxiliaryFields.TryGetValue(documentIdentifier.Value, out var exactAuxiliaryValue))
                {
                    return exactAuxiliaryValue ?? fstring.SEmpty; //TODO: Should auxiliaryFields really allow NULL values?
                }
                if (auxiliaryFields.TryGetValue( fstring.NewS(documentIdentifier.FieldName), out var auxiliaryValue))
                {
                    return auxiliaryValue ?? fstring.SEmpty; //TODO: Should auxiliaryFields really allow NULL values?
                }
                throw new KbEngineException($"Auxiliary fields not found: [{documentIdentifier.Value}].");
            }
            else if (queryField is QueryFieldConstantNumeric constantNumeric)
            {
                return query.Batch.GetLiteralValue(constantNumeric.Value.s);
            }
            else if (queryField is QueryFieldConstantString constantString)
            {
                return query.Batch.GetLiteralValue(constantString.Value.s);
            }
            else if (queryField is QueryFieldCollapsedValue collapsedValue)
            {
                return collapsedValue.Value;
            }
            else
            {
                throw new KbEngineException($"Field expression type is not implemented: [{queryField.GetType().Name}].");
            }
        }

        /// <summary>
        /// Collapses a QueryField expression into a single value. This includes doing string concatenation, math and all recursive function calls.
        /// </summary>
        public static fstring? CollapseScalerExpressionFunctionParameter(this IExpressionFunctionParameter parameter, Transaction transaction,
            PreparedQuery query, QueryFieldCollection fieldCollection, KbInsensitiveDictionary<fstring, fstring?> auxiliaryFields, List<IQueryFieldExpressionFunction> functionDependencies)
        {
            if (parameter is ExpressionFunctionParameterString parameterString)
            {
                return CollapseScalerFunctionStringParameter(transaction, query, fieldCollection, auxiliaryFields, functionDependencies, parameterString.Expression);
            }
            else if (parameter is ExpressionFunctionParameterNumeric parameterNumeric)
            {
                return CollapseScalerFunctionNumericParameter(transaction, query, fieldCollection, auxiliaryFields, functionDependencies, parameterNumeric.Expression);
            }
            else if (parameter is ExpressionFunctionParameterFunction expressionFunctionParameterFunction)
            {
                return CollapseScalerFunctionNumericParameter(transaction, query, fieldCollection, auxiliaryFields, functionDependencies, expressionFunctionParameterFunction.Expression);
            }
            else
            {
                throw new KbEngineException($"Function parameter type is not implemented [{parameter.GetType().Name}].");
            }
        }

        /// <summary>
        /// Collapses a string or numeric expression into a single value. This includes doing string concatenation, math and all recursive function calls.
        /// </summary>
        private static fstring? CollapseScalerExpression(Transaction transaction,
            PreparedQuery query, QueryFieldCollection fieldCollection, KbInsensitiveDictionary<fstring, fstring?> auxiliaryFields, ExposedExpression expression)
        {
            if (expression.FieldExpression is QueryFieldExpressionNumeric expressionNumeric)
            {
                return CollapseScalerFunctionNumericParameter(transaction, query, fieldCollection, auxiliaryFields, expressionNumeric.FunctionDependencies, expressionNumeric.Value.s);
            }
            else if (expression.FieldExpression is QueryFieldExpressionString expressionString)
            {
                return CollapseScalerFunctionStringParameter(transaction, query, fieldCollection, auxiliaryFields, expressionString.FunctionDependencies, expressionString.Value.s);
            }
            else
            {
                throw new KbEngineException($"Field expression type is not implemented: [{expression.FieldExpression.GetType().Name}].");
            }
        }

        /// <summary>
        /// Takes a string expression string and performs math on all of the values, including those from all
        ///     recursive function calls.
        /// </summary>
        private static fstring? CollapseScalerFunctionNumericParameter(Transaction transaction,
            PreparedQuery query, QueryFieldCollection fieldCollection, KbInsensitiveDictionary<fstring, fstring?> auxiliaryFields,
            List<IQueryFieldExpressionFunction> functions, string expressionString)
        {
            //Build a cachable numeric expression, interpolate the values and execute the expression.

            var tokenizer = new TokenizerSlim(expressionString, ['~', '!', '%', '^', '&', '*', '(', ')', '-', '/', '+']);

            int variableNumber = 0;

            var expressionVariables = new Dictionary<string, fstring?>();

            while (!tokenizer.IsExausted())
            {
                var token = tokenizer.EatGetNext();

                if (token.StartsWith("$f_") && token.EndsWith('$'))
                {
                    //Resolve the token to a field identifier.
                    if (fieldCollection.DocumentIdentifiers.TryGetValue(token, out var fieldIdentifier))
                    {
                        //Resolve the field identifier to a value.
                        if (auxiliaryFields.TryGetValue(fieldIdentifier.Value, out var textValue))
                        {
                            textValue.EnsureNotNull();
                            string mathVariable = $"v{variableNumber++}";
                            expressionString = expressionString.Replace(token, mathVariable);
                            expressionVariables.Add(mathVariable, query.Batch.GetLiteralValue(textValue.s));
                        }
                        else
                        {
                            throw new KbEngineException($"Function parameter auxiliary field is not defined: [{token}].");
                        }
                    }
                    else
                    {
                        throw new KbEngineException($"Function parameter field is not defined: [{token}].");
                    }
                }
                else if (token.StartsWith("$x_") && token.EndsWith('$'))
                {
                    //Search the dependency functions for the one with the expression key, this is the one we need to recursively resolve to fill in this token.
                    var subFunction = functions.Single(o => o.ExpressionKey == token);
                    var functionResult = CollapseScalerFunction(transaction, query, fieldCollection, auxiliaryFields, functions, subFunction);

                    string mathVariable = $"v{variableNumber++}";
                    expressionVariables.Add(mathVariable, functionResult);
                    expressionString = expressionString.Replace(token, mathVariable);
                }
                else if (token.StartsWith("$s_") && token.EndsWith('$'))
                {
                    //This is a string placeholder, get the literal value and complain about it.

                    throw new KbEngineException($"Could not perform mathematical operation on [{query.Batch.GetLiteralValue(token)}]");
                }
                else if (token.StartsWith("$n_") && token.EndsWith('$'))
                {
                    //This is a numeric placeholder, get the literal value and append it.

                    string mathVariable = $"v{variableNumber++}";
                    expressionString = expressionString.Replace(token, mathVariable);
                    expressionVariables.Add(mathVariable, query.Batch.GetLiteralValue(token));
                }
                else if (token.StartsWith('$') && token.EndsWith('$'))
                {
                    throw new KbEngineException($"Function parameter string sub-type is not implemented: [{token}].");
                }
            }

            if (expressionVariables.Count == 1)
            {
                //If this is the only token we have then we aren't even going to do math.
                //This is because this is more efficient and also because this might be a
                //string value from a document field that we assumed was numeric because the
                //expression contains no "string operations" such as literal text.

                //We do "best effort" math.
                return expressionVariables.First().Value;
            }

            //Perhaps we can pass in a cache object?
            var expression = new NCalc.Expression(expressionString);

            foreach (var expressionVariable in expressionVariables)
            {
                expression.Parameters[expressionVariable.Key] = expressionVariable.Value == null ? null : double.Parse(expressionVariable.Value.s);
            }
            var val = expression.Evaluate();
            return val != null ? fstring.NewS(val.ToString()) : fstring.SEmpty;
            
        }

        /// <summary>
        /// Takes a string expression string and concatenates all of the values, including those from all
        ///     recursive function calls. Concatenation which is really the only operation we support for strings.
        /// </summary>
        private static fstring CollapseScalerFunctionStringParameter(Transaction transaction,
            PreparedQuery query, QueryFieldCollection fieldCollection, KbInsensitiveDictionary<fstring, fstring?> auxiliaryFields,

            List<IQueryFieldExpressionFunction> functions, string expressionString)
        {
            var tokenizer = new TokenizerSlim(expressionString, ['+', '(', ')']);
            string token;

            var sb = new StringBuilder();

            while (!tokenizer.IsExausted())
            {
                token = tokenizer.EatGetNext();

                if (token.StartsWith("$f_") && token.EndsWith('$'))
                {
                    //Resolve the token to a field identifier.
                    if (fieldCollection.DocumentIdentifiers.TryGetValue(token, out var fieldIdentifier))
                    {
                        //Resolve the field identifier to a value.
                        if (auxiliaryFields.TryGetValue(fieldIdentifier.Value, out var textValue))
                        {
                            sb.Append(textValue);
                        }
                        else
                        {
                            throw new KbEngineException($"Function parameter auxiliary field is not defined: [{token}].");
                        }
                    }
                    else
                    {
                        throw new KbEngineException($"Function parameter field is not defined: [{token}].");
                    }
                }
                else if (token.StartsWith("$x_") && token.EndsWith('$'))
                {
                    //Search the dependency functions for the one with the expression key, this is the one we need to recursively resolve to fill in this token.
                    var subFunction = functions.Single(o => o.ExpressionKey == token);
                    var functionResult = CollapseScalerFunction(transaction, query, fieldCollection, auxiliaryFields, functions, subFunction);
                    sb.Append(functionResult);
                }
                else if (token.StartsWith("$s_") && token.EndsWith('$'))
                {
                    //This is a string placeholder, get the literal value and append it.
                    sb.Append(query.Batch.GetLiteralValue(token));
                }
                else if (token.StartsWith("$n_") && token.EndsWith('$'))
                {
                    //This is a numeric placeholder, get the literal value and append it.
                    sb.Append(query.Batch.GetLiteralValue(token));
                }
                else if (token.StartsWith('$') && token.EndsWith('$'))
                {
                    throw new KbEngineException($"Function parameter string sub-type is not implemented: [{token}].");
                }
                else
                {
                    sb.Append(query.Batch.GetLiteralValue(token));
                }
            }

            return fstring.NewS(sb.ToString());
        }

        /// <summary>
        /// Takes a function and recursively collapses all of the parameters, then recursively
        ///     executes all dependency functions to collapse the function to a single value.
        /// </summary>
        static fstring CollapseScalerFunction(Transaction transaction, PreparedQuery query, QueryFieldCollection fieldCollection,
            KbInsensitiveDictionary<fstring, fstring?> auxiliaryFields, List<IQueryFieldExpressionFunction> functions, IQueryFieldExpressionFunction function)
        {
            var collapsedParameters = new List<fstring?>();

            foreach (var parameter in function.Parameters)
            {
                collapsedParameters.Add(parameter.CollapseScalerExpressionFunctionParameter(transaction, query, fieldCollection, auxiliaryFields, functions));
            }

            if (AggregateFunctionCollection.TryGetFunction(function.FunctionName, out _))
            {
                throw new KbEngineException($"Cannot perform scaler operation on aggregate result of: [{function.FunctionName}].");
            }


            var kvs = new KbInsensitiveDictionary<fstring?>();

            foreach(var kv in auxiliaryFields)
            {
                kvs.Add(kv.Key.s, kv.Value);
            }
            //Execute function with the parameters from above ↑
            var methodResult = ScalerFunctionImplementation.ExecuteFunction(transaction, function.FunctionName, collapsedParameters, kvs);

            //TODO: think through the nullability here.
            return methodResult ?? fstring.SEmpty;
        }
    }
}
