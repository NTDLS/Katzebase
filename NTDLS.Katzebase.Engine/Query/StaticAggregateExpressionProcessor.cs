﻿using NTDLS.Katzebase.Client.Exceptions;
using NTDLS.Katzebase.Client.Types;
using NTDLS.Katzebase.Engine.Atomicity;
using NTDLS.Katzebase.Engine.Functions.Aggregate;
using NTDLS.Katzebase.Engine.Functions.Aggregate.Parameters;
using NTDLS.Katzebase.Engine.Functions.Scaler;
using NTDLS.Katzebase.Engine.Parsers.Query.Exposed;
using NTDLS.Katzebase.Engine.Parsers.Query.Fields.Expressions;
using NTDLS.Katzebase.Engine.Parsers.Query.Functions;
using NTDLS.Katzebase.Engine.Parsers.Query.SupportingTypes;
using NTDLS.Katzebase.Engine.Parsers.Tokens;
using System.Text;
using static NTDLS.Katzebase.Engine.Parsers.Query.Fields.Expressions.ExpressionConstants;

namespace NTDLS.Katzebase.Engine.Query
{
    internal class StaticAggregateExpressionProcessor
    {
        /// <summary>
        /// Resolves all of the query expressions (string concatenation, math and all recursive
        ///     function calls) on a row level and fills in the values in the resultingRows.
        /// </summary>
        public static Dictionary<int, List<string>> CollapseAggregateResultExpressions(Transaction transaction,
            PreparedQuery query, KbInsensitiveDictionary<KbInsensitiveDictionary<List<string>>> groupedRows)
        {
            var results = new Dictionary<int, List<string>>();

            //Resolve all expressions and fill in the row fields.
            foreach (var expressionField in query.SelectFields.ExpressionFields.Where(o => o.CollapseType == CollapseType.Aggregate))
            {
                var resultingRow = new List<string>();

                foreach (var row in groupedRows)
                {
                    var collapsedResult = CollapseAggregateExpression(transaction, query, row.Value, expressionField);
                    resultingRow.Add(collapsedResult);

                    //row.InsertValue(expressionField.FieldAlias, expressionField.Ordinal, collapsedResult);
                }

                results.Add(expressionField.Ordinal, resultingRow);
            }

            return results;
        }

        /// <summary>
        /// Collapses a string or numeric expression into a single value. This includes doing string concatenation, math and all recursive function calls.
        /// </summary>
        public static string CollapseAggregateExpression(Transaction transaction,
            PreparedQuery query, KbInsensitiveDictionary<List<string>> groupedValues, ExposedExpression expression)
        {
            if (expression.FieldExpression is QueryFieldExpressionNumeric expressionNumeric)
            {
                return CollapseAggregateFunctionNumericParameter(transaction, query,
                    groupedValues, expressionNumeric.FunctionDependencies, expressionNumeric.Value).First().ToString();
            }
            else if (expression.FieldExpression is QueryFieldExpressionString expressionString)
            {
                return CollapseAggregateFunctionStringParameter(transaction, query, groupedValues,
                    expressionString.FunctionDependencies, expressionString.Value);
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
        static double[] CollapseAggregateFunctionNumericParameter(Transaction transaction,
            PreparedQuery query, KbInsensitiveDictionary<List<string>> groupedValues,
            List<IQueryFieldExpressionFunction> functions, string expressionString)
        {
            //Build a cachable numeric expression, interpolate the values and execute the expression.

            var tokenizer = new TokenizerSlim(expressionString, ['~', '!', '%', '^', '&', '*', '(', ')', '-', '/', '+']);

            int variableNumber = 0;

            var expressionVariables = new List<string>();

            int groupRowCount = groupedValues.First().Value.Count;

            var groupRows = new List<double>[groupRowCount];
            for (int i = 0; i < groupRowCount; i++)
            {
                groupRows[i] = new List<double>();
            }

            while (!tokenizer.Exausted())
            {
                var token = tokenizer.EatGetNext(out char delimiter);

                if (token.StartsWith("$f_") && token.EndsWith('$'))
                {
                    //Resolve the token to a field identifier.
                    if (query.SelectFields.DocumentIdentifiers.TryGetValue(token, out var fieldIdentifier))
                    {
                        //Resolve the field identifier to a value.
                        if (groupedValues.TryGetValue(fieldIdentifier.Value, out var textValues))
                        {
                            string mathVariable = $"v{variableNumber++}";
                            expressionString = expressionString.Replace(token, mathVariable);
                            expressionVariables.Add(mathVariable);

                            if (textValues.Count != groupRowCount)
                            {
                                throw new KbEngineException($"Incorrect number of aggregate values returned from expression: [{fieldIdentifier.Value}].");
                            }

                            for (int i = 0; i < groupRowCount; i++)
                            {
                                groupRows[i].Add(double.Parse(query.Batch.GetLiteralValue(textValues[i])));
                            }
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

                    if (ScalerFunctionCollection.TryGetFunction(subFunction.FunctionName, out var scalerFunction))
                    {
                    }
                    if (AggregateFunctionCollection.TryGetFunction(subFunction.FunctionName, out var aggregateFunction))
                    {
                    }

                    var functionResult = CollapseAggregateFunction(transaction, query, groupedValues, functions, subFunction);

                    for (int i = 0; i < groupRowCount; i++)
                    {
                        groupRows[i].Add(double.Parse(functionResult));
                    }

                    string mathVariable = $"v{variableNumber++}";
                    expressionString = expressionString.Replace(token, mathVariable);
                    expressionVariables.Add(mathVariable);
                }
                else if (token.StartsWith("$s_") && token.EndsWith('$'))
                {
                    //This is a string placeholder, get the literal value and complain about it.

                    throw new KbEngineException($"Could not perform mathematical operation on [{query.Batch.GetLiteralValue(token)}]");
                }
                else if (token.StartsWith("$n_") && token.EndsWith('$'))
                {
                    //This is a numeric placeholder, get the literal value and append it.

                    var literalValue = double.Parse(query.Batch.GetLiteralValue(token));

                    for (int i = 0; i < groupRowCount; i++)
                    {
                        groupRows[i].Add(literalValue);
                    }

                    string mathVariable = $"v{variableNumber++}";
                    expressionString = expressionString.Replace(token, mathVariable);
                    expressionVariables.Add(mathVariable);
                }
                else if (token.StartsWith('$') && token.EndsWith('$'))
                {
                    throw new KbEngineException($"Function parameter string sub-type is not implemented: [{token}].");
                }

                if (delimiter != '\0')
                {
                    for (int i = 0; i < groupRowCount; i++)
                    {
                        //stringResults[i] += delimiter;
                    }
                }
            }

            //var expressionHash = Library.Helpers.GetSHA1Hash(expressionString);

            var finalResults = new double[groupRowCount];

            var expression = new NCalc.Expression(expressionString);

            int rowIndex = 0;

            foreach (var groupRow in groupRows) 
            {
                int variableIndex = 0;
                foreach (var ffff in groupRow)
                {
                    expression.Parameters[expressionVariables[variableIndex++]] = ffff;
                }

                finalResults[rowIndex] = (double)expression.Evaluate();
                rowIndex++;
            }

            return finalResults;
        }

        /// <summary>
        /// Takes a string expression string and concatenates all of the values, including those from all
        ///     recursive function calls. Concatenation which is really the only operation we support for strings.
        /// </summary>
        static string CollapseAggregateFunctionStringParameter(Transaction transaction,
            PreparedQuery query, KbInsensitiveDictionary<List<string>> groupedValues,
            List<IQueryFieldExpressionFunction> functions, string expressionString)
        {
            var tokenizer = new TokenizerSlim(expressionString, ['+', '(', ')']);
            string token;

            var sb = new StringBuilder();

            while (!tokenizer.Exausted())
            {
                token = tokenizer.EatGetNext();

                if (token.StartsWith("$f_") && token.EndsWith('$'))
                {
                    //Resolve the token to a field identifier.
                    if (query.SelectFields.DocumentIdentifiers.TryGetValue(token, out var fieldIdentifier))
                    {
                        //Resolve the field identifier to a value.
                        if (groupedValues.TryGetValue(fieldIdentifier.Value, out var textValue))
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
                    var functionResult = CollapseAggregateFunction(transaction, query, groupedValues, functions, subFunction);
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

            return sb.ToString();
        }

        /// <summary>
        /// Takes a function and recursively collapses all of the parameters, then recursively
        ///     executes all dependency functions to collapse the function to a single value.
        /// </summary>
        static string CollapseAggregateFunction(Transaction transaction, PreparedQuery query,
            KbInsensitiveDictionary<List<string>> groupedValues, List<IQueryFieldExpressionFunction> functions, IQueryFieldExpressionFunction function)
        {
            var collapsedParameters = new List<double[]>();

            foreach (var parameter in function.Parameters)
            {
                if (parameter is ExpressionFunctionParameterString parameterString)
                {
                    //Trying math here instead of text...
                    var collapsedParameter = CollapseAggregateFunctionNumericParameter(transaction, query, groupedValues, functions, parameterString.Expression);
                    collapsedParameters.Add(collapsedParameter);
                }
                else if (parameter is ExpressionFunctionParameterNumeric parameterNumeric)
                {
                    var collapsedParameter = CollapseAggregateFunctionNumericParameter(transaction, query, groupedValues, functions, parameterNumeric.Expression);
                    collapsedParameters.Add(collapsedParameter);
                }
                else
                {
                    throw new KbEngineException($"Function parameter type is not implemented [{parameter.GetType().Name}].");
                }
            }

            //Execute function with the parameters from above ↑
            var methodResult = AggregateFunctionImplementation.ExecuteFunction(function.FunctionName, collapsedParameters.First(), groupedValues);

            return methodResult.ToString();
        }
    }
}
