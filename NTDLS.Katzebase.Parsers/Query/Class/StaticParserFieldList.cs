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
            if (tokenizer.TryGetFirstIndexOf(stopAtTokens, out int endOfScopeCaret) == false)
            {
                if (allowEntireConsumption)
                {
                    endOfScopeCaret = tokenizer.Length;
                }
                else
                {
                    throw new KbParserException(tokenizer.GetCurrentLineNumber(), $"Unexpected : [{string.Join("],[", stopAtTokens)}].");
                }
            }

            var exceptions = new List<Exception>();

            foreach (var field in tokenizer.EatScopeSensitiveSplit(endOfScopeCaret))
            {
                try
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
                            //Breaks here when "comma" is missing. Error line number is INCORRECT.

                            throw new KbParserException(tokenizer.GetCurrentLineNumber(), $"Expected end of alias, found: [{fieldAliasTokenizer.Remainder()}].");
                        }
                    }

                    var aliasRemovedFieldText = (aliasIndex > 0 ? field.Substring(0, aliasIndex) : field).Trim();

                    //Breaks here when "as" is missing. Error line number is correct.
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
                }
                catch (Exception e)
                {
                    exceptions.Add(e);
                }
            }

            if (exceptions.Count != 0)
            {
                throw new AggregateException(exceptions);
            }

            return queryFields;
        }
    }
}
