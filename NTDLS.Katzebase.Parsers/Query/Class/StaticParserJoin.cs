using NTDLS.Katzebase.Client.Exceptions;
using NTDLS.Katzebase.Parsers.Query.Class.Helpers;
using NTDLS.Katzebase.Parsers.Query.SupportingTypes;
using NTDLS.Katzebase.Parsers.Query.WhereAndJoinConditions.Helpers;
using NTDLS.Katzebase.Parsers.Tokens;
using static NTDLS.Katzebase.Parsers.Constants;

namespace NTDLS.Katzebase.Parsers.Query.Class
{
    public static class StaticParserJoin
    {
        public static List<QuerySchema> Parse(QueryBatch queryBatch, Tokenizer tokenizer)
        {
            var result = new List<QuerySchema>();
            string token;

            while (tokenizer.TryEatIfNext("inner"))
            {
                if (tokenizer.TryEatIfNext("join") == false)
                {
                    throw new KbParserException(tokenizer.GetCurrentLineNumber(), $"Found [{tokenizer.EatGetNext()}], expected: 'join'.");
                }

                string subSchemaSchema = tokenizer.EatGetNext();
                string subSchemaAlias = string.Empty;
                if (!TokenizerHelpers.IsValidIdentifier(subSchemaSchema, ':'))
                {
                    throw new KbParserException(tokenizer.GetCurrentLineNumber(), $"Found [{subSchemaSchema}], expected: schema name.");
                }

                if (tokenizer.TryEatIfNext("as"))
                {
                    subSchemaAlias = tokenizer.EatGetNext();
                }
                else
                {
                    throw new KbParserException(tokenizer.GetCurrentLineNumber(), $"Found [{tokenizer.GetNext()}], expected: [as] (schema alias).");
                }


                if (tokenizer.TryEatIfNext("on", out token) == false)
                {
                    throw new KbParserException(tokenizer.GetCurrentLineNumber(), $"Found [{token}], expected [on].");
                }

                int joinConditionsStartPosition = tokenizer.Caret;

                while (true)
                {
                    if (tokenizer.TryIsNext(["where", "order", "inner", ""]))
                    {
                        //Found start of next part of query.
                        break;
                    }

                    if (tokenizer.TryCompareNext((o) => StaticParserUtility.IsStartOfQuery(o)))
                    {
                        //Found start of next query.
                        break;
                    }

                    if (tokenizer.TryIsNext(["and", "or"]))
                    {
                        tokenizer.EatNext();
                    }

                    var joinLeftCondition = tokenizer.EatGetNext();
                    if (!TokenizerHelpers.IsValidIdentifier(joinLeftCondition, '.'))
                    {
                        throw new KbParserException(tokenizer.GetCurrentLineNumber(), $"Found [{joinLeftCondition}], expected: left side of join expression.");
                    }

                    token = tokenizer.EatGetNext();
                    if (StaticConditionHelpers.ParseLogicalQualifier(tokenizer, token) == LogicalQualifier.None)
                    {
                        throw new KbParserException(tokenizer.GetCurrentLineNumber(), $"Found [{token}], expected logical qualifier.");
                    }

                    var joinRightCondition = tokenizer.EatGetNext();
                    if (!TokenizerHelpers.IsValidIdentifier(joinRightCondition, '.'))
                    {
                        throw new KbParserException(tokenizer.GetCurrentLineNumber(), $"Found [{joinRightCondition}], expected: right side of join expression.");
                    }
                }

                var joinConditionsText = tokenizer.Text.Substring(joinConditionsStartPosition, tokenizer.Caret - joinConditionsStartPosition).Trim();
                var joinConditions = StaticConditionsParser.Parse(queryBatch, tokenizer, joinConditionsText, subSchemaAlias);

                result.Add(new QuerySchema(subSchemaSchema.ToLowerInvariant(), subSchemaAlias.ToLowerInvariant(), joinConditions));
            }

            return result;
        }
    }
}
