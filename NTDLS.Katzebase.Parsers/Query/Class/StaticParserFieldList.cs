using NTDLS.Helpers;
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
        public static T Parse<T>(QueryBatch queryBatch, Tokenizer tokenizer, string[] stopAtTokens, bool allowEntireConsumption, Func<QueryBatch, T> factory)
            where T : QueryFieldCollection
        {
            var queryFields = factory(queryBatch);

            int endOfScopeCaret = tokenizer.FindEndOfQuerySegment(stopAtTokens, allowEntireConsumption);
            string testText = tokenizer.SubStringAbsolute(endOfScopeCaret).Trim();
            if (string.IsNullOrWhiteSpace(testText))
            {
                throw new KbParserException(tokenizer.GetCurrentLineNumber(), $"Expected field list expressions, found: [{testText}].");
            }

            try
            {
                tokenizer.PushSyntheticLimit(endOfScopeCaret);

                var exceptions = new List<Exception>();

                foreach (var field in tokenizer.EatScopeSensitiveSplit(endOfScopeCaret.EnsureNotNull()))
                {
                    try
                    {
                        string fieldAlias = string.Empty;
                        string suffixRemovedFieldText = string.Empty;

                        if (queryFields is OrderByFieldCollection sortFieldCollection)
                        {
                            if (field.EndsWith(" asc", StringComparison.InvariantCultureIgnoreCase))
                            {
                                sortFieldCollection.SortDirection = Client.KbConstants.KbSortDirection.Ascending;
                                suffixRemovedFieldText = field[..^4].Trim();
                            }
                            else if (field.EndsWith(" desc", StringComparison.InvariantCultureIgnoreCase))
                            {
                                sortFieldCollection.SortDirection = Client.KbConstants.KbSortDirection.Descending;
                                suffixRemovedFieldText = field[..^5].Trim();
                            }
                            else
                            {
                                suffixRemovedFieldText = field;
                            }
                        }
                        else
                        {
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

                            suffixRemovedFieldText = (aliasIndex > 0 ? field[..aliasIndex] : field).Trim();
                        }

                        //Breaks here when "as" is missing. Error line number is correct.
                        var queryField = StaticParserField.Parse(tokenizer, suffixRemovedFieldText, queryFields);

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

            }
            catch
            {
                throw;
            }
            finally
            {
                tokenizer.PopSyntheticLimit();
                tokenizer.EatWhiteSpace();
            }

            return queryFields;
        }
    }
}
