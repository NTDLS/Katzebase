﻿using NTDLS.Katzebase.Client.Exceptions;
using NTDLS.Katzebase.Engine.Parsers.Query.SupportingTypes;
using NTDLS.Katzebase.Engine.Parsers.Query.WhereAndJoinConditions;
using NTDLS.Katzebase.Engine.Parsers.Tokens;
using static NTDLS.Katzebase.Engine.Library.EngineConstants;

namespace NTDLS.Katzebase.Engine.Parsers.Query.Class
{
    internal static class StaticParserJoin
    {
        public static List<QuerySchema> Parse(QueryBatch queryBatch, Tokenizer tokenizer)
        {
            var result = new List<QuerySchema>();
            string token;

            while (tokenizer.TryEatIsNextToken("inner"))
            {
                if (tokenizer.TryEatIsNextToken("join") == false)
                {
                    throw new KbParserException("Invalid query. Found '" + tokenizer.EatGetNext() + "', expected: 'join'.");
                }

                string subSchemaSchema = tokenizer.EatGetNext();
                string subSchemaAlias = string.Empty;
                if (!TokenizerHelpers.IsValidIdentifier(subSchemaSchema, ':'))
                {
                    throw new KbParserException("Invalid query. Found '" + subSchemaSchema + "', expected: schema name.");
                }

                if (tokenizer.TryEatIsNextToken("as"))
                {
                    subSchemaAlias = tokenizer.EatGetNext();
                }
                else
                {
                    throw new KbParserException("Invalid query. Found '" + tokenizer.GetNext() + "', expected: 'as' (schema alias).");
                }


                if (tokenizer.TryEatIsNextToken("on", out token) == false)
                {
                    throw new KbParserException("Invalid query. Found '" + token + "', expected 'on'.");
                }

                int joinConditionsStartPosition = tokenizer.Caret;

                while (true)
                {
                    if (tokenizer.TryIsNextToken(["where", "order", "inner", ""]))
                    {
                        //Found start of next part of query.
                        break;
                    }

                    if (tokenizer.TryCompareNextToken((o) => StaticParserUtility.IsStartOfQuery(o)))
                    {
                        //Found start of next query.
                        break;
                    }

                    if (tokenizer.TryIsNextToken(["and", "or"]))
                    {
                        tokenizer.EatNextToken();
                    }

                    var joinLeftCondition = tokenizer.EatGetNext();
                    if (!TokenizerHelpers.IsValidIdentifier(joinLeftCondition, '.'))
                    {
                        throw new KbParserException("Invalid query. Found '" + joinLeftCondition + "', expected: left side of join expression.");
                    }

                    token = tokenizer.EatGetNext();
                    if (StaticConditionHelpers.ParseLogicalQualifier(token) == LogicalQualifier.None)
                    {
                        throw new KbParserException("Invalid query. Found '" + token + "], expected logical qualifier.");
                    }

                    var joinRightCondition = tokenizer.EatGetNext();
                    if (!TokenizerHelpers.IsValidIdentifier(joinRightCondition, '.'))
                    {
                        throw new KbParserException("Invalid query. Found '" + joinRightCondition + "', expected: right side of join expression.");
                    }
                }

                var joinConditionsText = tokenizer.Text.Substring(joinConditionsStartPosition, tokenizer.Caret - joinConditionsStartPosition).Trim();
                var joinConditions = Conditions.Create(queryBatch, joinConditionsText, tokenizer, subSchemaAlias);

                result.Add(new QuerySchema(subSchemaSchema.ToLowerInvariant(), subSchemaAlias.ToLowerInvariant(), joinConditions));
            }

            return result;
        }
    }
}