﻿using NTDLS.Katzebase.Client.Exceptions;
using NTDLS.Katzebase.Parsers.Query.SupportingTypes;
using NTDLS.Katzebase.Parsers.Tokens;
using static NTDLS.Katzebase.Parsers.Constants;

namespace NTDLS.Katzebase.Parsers.Query.Class
{
    public static class StaticParserExec
    {
        internal static PreparedQuery Parse(QueryBatch queryBatch, Tokenizer tokenizer)
        {
            var query = new PreparedQuery(queryBatch, QueryType.Exec)
            {
                SubQueryType = SubQueryType.Procedure
            };

            if (tokenizer.TryEatValidateNext((o) => TokenizerExtensions.IsIdentifier(o), out var procedureName) == false)
            {
                throw new KbParserException(tokenizer.GetCurrentLineNumber(), $"Expected procedure name, found: [{procedureName}].");
            }

            var parts = procedureName.Split(':');
            if (parts.Length == 1)
            {
                query.AddAttribute(PreparedQuery.QueryAttribute.Schema, ":");
                query.AddAttribute(PreparedQuery.QueryAttribute.ObjectName, procedureName);
            }
            else
            {
                var schemaName = string.Join(':', parts.Take(parts.Length - 1));
                query.AddAttribute(PreparedQuery.QueryAttribute.Schema, schemaName);
                query.AddAttribute(PreparedQuery.QueryAttribute.ObjectName, parts.Last());
            }

            if (tokenizer.TryCompareNext(o => o.StartsWith('$')))
            {
                //Were just testing for a literal placeholder, as its a common mistake to omit the parentheses when calling a function.
                throw new KbParserException(tokenizer.GetCurrentLineNumber(), $"Function must be called with parentheses: [{tokenizer.EatGetNextEvaluated()}].");
            }

            if (tokenizer.TryEatIfNextCharacter('('))
            {
                query.ProcedureParameters = new QueryFieldCollection(queryBatch);

                //var parametersScope = tokenizer.EatGetMatchingScope();
                //var paramTokenizer = new TokenizerSlim(parametersScope);

                while (!tokenizer.IsExhausted())
                {
                    string token;
                    int startCaret = tokenizer.Caret;
                    int endCaret = 0;

                    while (!tokenizer.IsExhausted())
                    {
                        token = tokenizer.GetNext();
                        if (token == "(")
                        {
                            tokenizer.EatMatchingScope();
                        }
                        else if (token == ")")
                        {
                            endCaret = tokenizer.Caret;
                            break; //exit loop to parse, found: end of parameter list.
                        }
                        else if (token.Length == 1 && token[0] == ',')
                        {
                            endCaret = tokenizer.Caret;
                            tokenizer.EatNext();
                            break; //exit loop to parse next field.
                        }
                        else if (token.Length == 1 && (token[0].IsTokenConnectorCharacter() || token[0].IsMathematicalOperator()))
                        {
                            tokenizer.EatNext();
                        }
                        else
                        {
                            tokenizer.EatNext();
                        }
                    }

                    var fieldValue = tokenizer.Substring(startCaret, endCaret - startCaret).Trim();
                    var queryField = StaticParserField.Parse(tokenizer, fieldValue, query.ProcedureParameters);

                    query.ProcedureParameters.Add(new QueryField($"p{query.ProcedureParameters.Count}", query.ProcedureParameters.Count, queryField));

                    if (tokenizer.TryEatIfNextCharacter(')'))
                    {
                        break; //exit loop to parse, found: end of parameter list.
                    }
                }
            }

            //query.ProcedureCall = StaticFunctionParsers.ParseProcedureParameters(tokenizer);

            return query;
        }
    }
}
