using NTDLS.Katzebase.Parsers.Query.Fields;
using NTDLS.Katzebase.Parsers.Query.SupportingTypes;
using NTDLS.Katzebase.Parsers.Tokens;
using NTDLS.Katzebase.Parsers.Interfaces;

namespace NTDLS.Katzebase.Parsers.Query.Class
{
    /// <summary>
    /// Used to parse complex field lists that contain expressions and are aliases, such as SELECT and GROUP BY fields.
    /// Yes, group by fields do not need aliases, but I opted to not duplicate code.
    /// </summary>
    public static class StaticParserFieldList<TData> where TData : IStringable
    {
        /// <summary>
        /// Parses the field expressions for a "select" or "select into" query.
        /// </summary>
        /// <param name="stopAtTokens">Array of tokens for which the parsing will stop if encountered.</param>
        /// <param name="allowEntireConsumption">If true, in the event that stopAtTokens are not found, the entire remaining text will be consumed.</param>
        /// <returns></returns>
        public static QueryFieldCollection<TData> Parse(QueryBatch<TData> queryBatch, Tokenizer<TData> tokenizer, string[] stopAtTokens, bool allowEntireConsumption, Func<string, TData> parseStringToDoc, Func<string, TData> castStringToDoc)
        {
            var queryFields = new QueryFieldCollection<TData>(queryBatch);

            //Get the position which represents the end of the select list.
            if (tokenizer.TryGetFirstIndexOf(stopAtTokens, out int stopAt) == false)
            {
                if (allowEntireConsumption)
                {
                    stopAt = tokenizer.Length;
                }
                else
                {
                    throw new Exception($"Expected string not found [{string.Join("],[", stopAtTokens)}].");
                }
            }

            //Get the text for all of the select fields.
            var fieldsSegment = tokenizer.EatSubStringAbsolute(stopAt);

            //Split the select fields on the comma, respecting any commas in function scopes.
            var fields = fieldsSegment.ScopeSensitiveSplit(',');

            foreach (var field in fields)
            {
                string fieldAlias = string.Empty;

                //Parse the field alias.
                int aliasIndex = field.LastIndexOf(" as ", StringComparison.InvariantCultureIgnoreCase);
                if (aliasIndex > 0)
                {
                    //Get the next token after the "as".
                    var fieldAliasTokenizer = new TokenizerSlim(field.Substring(aliasIndex + 4).Trim());
                    fieldAlias = fieldAliasTokenizer.EatGetNext();

                    //Make sure that the single token was the entire alias, otherwise we have a syntax error.
                    if (!fieldAliasTokenizer.IsExhausted())
                    {
                        throw new Exception($"Expected end of alias, found [{fieldAliasTokenizer.Remainder()}].");
                    }
                }

                var aliasRemovedFieldText = (aliasIndex > 0 ? field.Substring(0, aliasIndex) : field).Trim();

                var queryField = StaticParserField<TData>.Parse(tokenizer, aliasRemovedFieldText, queryFields, parseStringToDoc, castStringToDoc);

                //If the query didn't provide an alias, figure one out.
                if (string.IsNullOrWhiteSpace(fieldAlias))
                {
                    if (queryField is QueryFieldDocumentIdentifier<TData> queryFieldDocumentIdentifier)
                    {
                        fieldAlias = queryFieldDocumentIdentifier.FieldName;
                    }
                    else
                    {
                        fieldAlias = queryFields.GetNextFieldAlias();
                    }
                }

                queryFields.Add(new QueryField<TData>(fieldAlias, queryFields.Count, queryField));
            }

            return queryFields;
        }
    }
}
