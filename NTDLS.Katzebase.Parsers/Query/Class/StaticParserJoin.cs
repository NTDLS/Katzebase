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
                    throw new KbParserException(tokenizer.GetCurrentLineNumber(), $"Expected [join], found: [{tokenizer.EatGetNextEvaluated()}]");
                }

                string subSchemaSchema = tokenizer.EatGetNext();
                string subSchemaAlias = string.Empty;
                if (!TokenizerHelpers.IsValidIdentifier(subSchemaSchema, ':'))
                {
                    throw new KbParserException(tokenizer.GetCurrentLineNumber(), $"Expected schema name, found: [{tokenizer.ResolveLiteral(subSchemaSchema)}].");
                }

                if (tokenizer.TryEatIfNext("as"))
                {
                    subSchemaAlias = tokenizer.EatGetNext();
                }
                else
                {
                    throw new KbParserException(tokenizer.GetCurrentLineNumber(), $"Expected [as] (schema alias), found: [{tokenizer.EatGetNextEvaluated()}].");
                }


                if (tokenizer.TryEatIfNext("on", out token) == false)
                {
                    throw new KbParserException(tokenizer.GetCurrentLineNumber(), $"Expected [on], found: [{tokenizer.ResolveLiteral(token)}].");
                }

                int joinConditionsStartPosition = tokenizer.Caret;

                //Simple validation of join expression and finding the end caret of the join conditions.
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
                        throw new KbParserException(tokenizer.GetCurrentLineNumber(), $"Expected left side of join expression, found: [{tokenizer.ResolveLiteral(joinLeftCondition)}].");
                    }

                    token = tokenizer.EatGetNext();
                    if (StaticConditionHelpers.ParseLogicalQualifier(tokenizer, token) == LogicalQualifier.None)
                    {
                        throw new KbParserException(tokenizer.GetCurrentLineNumber(), $"Expected logical qualifier, found: [{tokenizer.ResolveLiteral(token)}].");
                    }

                    var joinRightCondition = tokenizer.EatGetNext();
                    if (!TokenizerHelpers.IsValidIdentifier(joinRightCondition, '.'))
                    {
                        throw new KbParserException(tokenizer.GetCurrentLineNumber(), $"Expected right side of join expression, found: [{tokenizer.ResolveLiteral(joinRightCondition)}].");
                    }
                }

                int joinConditionsEndPosition = tokenizer.Caret;

                int joinConditionsLength = tokenizer.Caret - joinConditionsStartPosition;
                var joinConditionsText = tokenizer.Text.Substring(joinConditionsStartPosition, joinConditionsLength).Trim();

                tokenizer.Caret = joinConditionsStartPosition;

                var joinConditions = StaticConditionsParser.Parse(queryBatch, tokenizer, joinConditionsText, joinConditionsEndPosition, subSchemaAlias);

                result.Add(new QuerySchema(subSchemaSchema.ToLowerInvariant(), subSchemaAlias.ToLowerInvariant(), joinConditions));
            }

            return result;
        }
    }
}
