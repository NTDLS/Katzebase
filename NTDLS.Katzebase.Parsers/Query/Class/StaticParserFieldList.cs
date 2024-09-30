using NTDLS.Katzebase.Client.Exceptions;
using NTDLS.Katzebase.Parsers.Query.Fields;
using NTDLS.Katzebase.Parsers.Query.SupportingTypes;
using NTDLS.Katzebase.Parsers.Tokens;

namespace NTDLS.Katzebase.Parsers.Query.Class
{
    /// <summary>
    /// Used to parse complex field lists that contain expressions and are aliases, such as SELECT and GROUP BY fields.
    /// Yes, group by fields do not need aliases, but I opted to not duplicate code.
    /// </summary>
    public static class StaticParserFieldList
    {
        /// <summary>
        /// Parses the field expressions for a "select" or "select into" query.
        /// </summary>
        /// <param name="stopAtTokens">Array of tokens for which the parsing will stop if encountered.</param>
        /// <param name="allowEntireConsumption">If true, in the event that stopAtTokens are not found, the entire remaining text will be consumed.</param>
        /// <returns></returns>
        public static QueryFieldCollection Parse(QueryBatch queryBatch, Tokenizer tokenizer, string[] stopAtTokens, bool allowEntireConsumption)
        {
            var queryFields = new QueryFieldCollection(queryBatch);

            //Get the position which represents the end of the select list.
            if (tokenizer.TryGetFirstIndexOf(stopAtTokens, out int stopAt) == false)
            {
                if (allowEntireConsumption)
                {
                    stopAt = tokenizer.Length;
                }
                else
                {
                    throw new KbParserException(tokenizer.GetCurrentLineNumber(), $"Expected string not found [{string.Join("],[", stopAtTokens)}].");
                }
            }

            var fieldsSegment = tokenizer.SubStringAbsolute(stopAt);
            //var fieldsSegment = tokenizer.EatSubStringAbsolute(stopAt);

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
                        throw new KbParserException(tokenizer.GetCurrentLineNumber(), $"Expected end of alias, found [{fieldAliasTokenizer.Remainder()}].");
                    }
                }

                var aliasRemovedFieldText = (aliasIndex > 0 ? field.Substring(0, aliasIndex) : field).Trim();

                var queryField = StaticParserField.Parse(tokenizer, aliasRemovedFieldText, queryFields);

                //If the query didn't provide an alias, figure one out.
                if (string.IsNullOrWhiteSpace(fieldAlias))
                {
                    if (queryField is QueryFieldDocumentIdentifier queryFieldDocumentIdentifier)
                    {
                        fieldAlias = queryFieldDocumentIdentifier.FieldName;
                    }
                    else
                    {
                        fieldAlias = queryFields.GetNextFieldAlias();
                    }
                }

                queryFields.Add(new QueryField(fieldAlias, queryFields.Count, queryField));

                //TODO: Find a better way to skip this, we are just trying to keep track of the
                //  caret for error reporting because it is correctly set after the fields are parsed.
                tokenizer.SetCaret(tokenizer.Caret + field.Length + 4); //Skip this field.
            }

            tokenizer.SetCaret(stopAt); //Skip the fields.
            tokenizer.EatWhiteSpace();

            return queryFields;
        }
    }
}
