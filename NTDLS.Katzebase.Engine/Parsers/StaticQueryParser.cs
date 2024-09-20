using NTDLS.Katzebase.Client.Exceptions;
using NTDLS.Katzebase.Client.Types;
using NTDLS.Katzebase.Engine.Parsers.Query.Class;
using NTDLS.Katzebase.Engine.Parsers.Query.Class.Helpers;
using NTDLS.Katzebase.Engine.Parsers.Query.SupportingTypes;
using NTDLS.Katzebase.Engine.Parsers.Tokens;
using static NTDLS.Katzebase.Client.KbConstants;
using static NTDLS.Katzebase.Engine.Library.EngineConstants;

namespace NTDLS.Katzebase.Engine.Parsers
{
    internal class StaticQueryParser
    {
        /// <summary>
        /// Parse the query batch (a single query text containing multiple queries).
        /// </summary>
        /// <param name="queryText"></param>
        /// <param name="userParameters"></param>
        /// <returns></returns>
        static public QueryBatch ParseBatch(EngineCore core, string queryText, KbInsensitiveDictionary<KbConstant>? userParameters = null)
        {
            var tokenizerConstants = core.Query.KbGlobalConstants.Clone();

            userParameters ??= new();
            //If we have user parameters, add them to a clone of the global tokenizer constants.
            foreach (var param in userParameters)
            {
                tokenizerConstants.Add(param.Key, param.Value);
            }

            queryText = PreParseQueryVariableDeclarations(queryText, ref tokenizerConstants);

            var tokenizer = new Tokenizer(queryText, true, tokenizerConstants);
            var queryBatch = new QueryBatch(tokenizer.Literals);

            while (!tokenizer.IsExhausted())
            {
                int preParseTokenPosition = tokenizer.Caret;
                var preparedQuery = ParseQuery(queryBatch, tokenizer);

                var singleQueryText = tokenizer.Substring(preParseTokenPosition, tokenizer.Caret - preParseTokenPosition);
                preparedQuery.Hash = Library.Helpers.ComputeSHA256(singleQueryText);

                queryBatch.Add(preparedQuery);
            }

            return queryBatch;
        }

        /// <summary>
        /// Parse the single.
        /// </summary>
        static public PreparedQuery ParseQuery(QueryBatch queryBatch, Tokenizer tokenizer)
        {
            string token = tokenizer.GetNext();

            if (StaticParserUtility.IsStartOfQuery(token, out var queryType) == false)
            {
                string acceptableValues = string.Join("', '", Enum.GetValues<QueryType>().Where(o => o != QueryType.None));
                throw new KbParserException($"Invalid query. Found '{token}', expected: '{acceptableValues}'.");
            }

            tokenizer.EatNext();

            return queryType switch
            {
                QueryType.Select => StaticParserSelect.Parse(queryBatch, tokenizer),
                QueryType.Delete => StaticParserDelete.Parse(queryBatch, tokenizer),
                QueryType.Insert => StaticParserInsert.Parse(queryBatch, tokenizer),
                QueryType.Update => StaticParserUpdate.Parse(queryBatch, tokenizer),
                QueryType.Begin => StaticParserBegin.Parse(queryBatch, tokenizer),
                QueryType.Commit => StaticParserCommit.Parse(queryBatch, tokenizer),
                QueryType.Rollback => StaticParserRollback.Parse(queryBatch, tokenizer),
                QueryType.Create => StaticParserCreate.Parse(queryBatch, tokenizer),

                _ => throw new KbParserException($"The query type is not implemented: [{token}]."),
            };

            #region Reimplment.

            #region Exec -----------------------------------------------------------------------------------------------

            /*
            if (queryType == QueryType.Exec)
            {
                result.ProcedureCall = StaticFunctionParsers.ParseProcedureParameters(tokenizer);
            }
            */

            #endregion

            #region Alter ----------------------------------------------------------------------------------------------

            /*
            else if (queryType == QueryType.Alter)
            {
            if (tokenizer.TryIsNextToken(["schema", "configuration"]) == false)
            {
                throw new KbParserException("Invalid query. Found '" + tokenizer.PeekNext() + "', expected 'schema' or 'configuration'.");
            }

            token = tokenizer.GetNext();
            if (Enum.TryParse(token, true, out SubQueryType subQueryType) == false)
            {
                throw new KbParserException("Invalid query. Found '" + token + "', expected: 'schema' or 'configuration'.");
            }
            result.SubQueryType = subQueryType;

            if (result.SubQueryType == SubQueryType.Configuration)
            {
                if (tokenizer.TryIsNextToken("with"))
                {
                    var options = new ExpectedWithOptions
                        {
                            { "BaseAddress", typeof(string) },
                            { "DataRootPath", typeof(string) },
                            { "TransactionDataPath", typeof(string) },
                            { "LogDirectory", typeof(string) },
                            { "FlushLog", typeof(bool) },
                            { "DefaultDocumentPageSize", typeof(int) },
                            { "UseCompression", typeof(bool) },
                            { "HealthMonitoringEnabled", typeof(bool) },
                            { "HealthMonitoringCheckpointSeconds", typeof(int) },
                            { "HealthMonitoringInstanceLevelEnabled", typeof(bool) },
                            { "HealthMonitoringInstanceLevelTimeToLiveSeconds", typeof(int) },
                            { "MaxIdleConnectionSeconds", typeof(int) },
                            { "DefaultIndexPartitions", typeof(int) },
                            { "DeferredIOEnabled", typeof(bool) },
                            { "WriteTraceData", typeof(bool) },
                            { "CacheEnabled", typeof(bool) },
                            { "CacheMaxMemory", typeof(int) },
                            { "CacheScavengeInterval", typeof(int) },
                            { "CachePartitions", typeof(int) },
                            { "CacheSeconds", typeof(int) }
                        };
                    StaticWithOptionsParser.ParseWithOptions(ref tokenizer, options, ref result);
                }
            }
            else if (result.SubQueryType == SubQueryType.Schema)
            {
                result.AddAttribute(PreparedQuery.QueryAttribute.IsUnique, (subQueryType == SubQueryType.UniqueKey));

                token = tokenizer.GetNext();
                if (token == string.Empty)
                {
                    throw new KbParserException("Invalid query. Found '" + token + "', expected: object name.");
                }
                result.Schemas.Add(new QuerySchema(token));

                if (tokenizer.TryIsNextToken("with"))
                {
                    var options = new ExpectedWithOptions
                        {
                            {"pagesize", typeof(uint) }
                        };
                    StaticWithOptionsParser.ParseWithOptions(ref tokenizer, options, ref result);
                }
            }
            else
            {
                throw new KbNotImplementedException();
            }
            }
            */
            #endregion

            #region Drop -----------------------------------------------------------------------------------------------

            /*
            else if (queryType == QueryType.Drop)
            {
            if (tokenizer.TryIsNextToken(["schema", "index", "uniquekey"]) == false)
            {
                throw new KbParserException("Invalid query. Found '" + tokenizer.PeekNext() + "', expected: 'schema', 'index' or 'uniquekey'.");
            }

            token = tokenizer.GetNext();
            if (Enum.TryParse(token, true, out SubQueryType subQueryType) == false)
            {
                throw new KbParserException("Invalid query. Found '" + token + "', expected: 'index' or 'uniquekey'.");
            }
            result.SubQueryType = subQueryType;

            if (subQueryType == SubQueryType.Index || subQueryType == SubQueryType.UniqueKey)
            {
                token = tokenizer.GetNext();
                if (token == string.Empty)
                {
                    throw new KbParserException("Invalid query. Found '" + token + "', expected: object name.");
                }
                result.AddAttribute(PreparedQuery.QueryAttribute.IndexName, token);

                if (tokenizer.GetNext().Is("on") == false)
                {
                    throw new KbParserException("Invalid query. Found '" + token + "', expected: 'on'.");
                }
            }

            token = tokenizer.GetNext();
            if (token == string.Empty)
            {
                throw new KbParserException("Invalid query. Found '" + token + "', expected: schema name.");
            }

            result.Schemas.Add(new QuerySchema(token));
            }
            */

            #endregion

            #region Rebuild --------------------------------------------------------------------------------------------

            /*
            else if (queryType == QueryType.Rebuild)
            {
            if (tokenizer.TryIsNextToken(["index", "uniquekey"]) == false)
            {
                throw new KbParserException("Invalid query. Found '" + tokenizer.PeekNext() + "', expected: 'index' or 'uniquekey'.");
            }

            token = tokenizer.GetNext();
            if (Enum.TryParse<SubQueryType>(token, true, out SubQueryType subQueryType) == false)
            {
                throw new KbParserException("Invalid query. Found '" + token + "', expected: 'index' or 'uniquekey'.");
            }
            result.SubQueryType = subQueryType;

            token = tokenizer.GetNext();
            if (token == string.Empty)
            {
                throw new KbParserException("Invalid query. Found '" + token + "', expected: index name.");
            }
            result.AddAttribute(PreparedQuery.QueryAttribute.IndexName, token);

            if (tokenizer.GetNext().Is("on") == false)
            {
                throw new KbParserException("Invalid query. Found '" + token + "', expected: 'on'.");
            }

            token = tokenizer.GetNext();
            if (token == string.Empty)
            {
                throw new KbParserException("Invalid query. Found '" + token + "', expected: schema name.");
            }

            result.Schemas.Add(new QuerySchema(token));

            if (tokenizer.TryIsNextToken("with"))
            {
                var options = new ExpectedWithOptions
                    {
                        {"partitions", typeof(uint) }
                    };
                StaticWithOptionsParser.ParseWithOptions(ref tokenizer, options, ref result);
            }
            }
            */

            #endregion

            #region Analyze --------------------------------------------------------------------------------------------

            /*
            else if (queryType == QueryType.Analyze)
            {
            if (tokenizer.TryIsNextToken(["index", "schema"]) == false)
            {
                throw new KbParserException("Invalid query. Found '" + tokenizer.PeekNext() + "', expected: 'index' or 'schema'.");
            }

            token = tokenizer.GetNext();
            if (Enum.TryParse<SubQueryType>(token, true, out SubQueryType subQueryType) == false)
            {
                throw new KbParserException("Invalid query. Found '" + token + "', expected: 'index' or 'schema'.");
            }

            result.SubQueryType = subQueryType;

            if (result.SubQueryType == SubQueryType.Index)
            {

                token = tokenizer.GetNext();
                if (token == string.Empty)
                {
                    throw new KbParserException("Invalid query. Found '" + token + "', expected: object name.");
                }
                result.AddAttribute(PreparedQuery.QueryAttribute.IndexName, token);

                if (tokenizer.GetNext().Is("on") == false)
                {
                    throw new KbParserException("Invalid query. Found '" + tokenizer.Breadcrumbs.Last() + "', expected: 'on'.");
                }

                token = tokenizer.GetNext();
                if (token == string.Empty)
                {
                    throw new KbParserException("Invalid query. Found '" + token + "', expected: schema name.");
                }
                result.AddAttribute(PreparedQuery.QueryAttribute.Schema, token);
            }
            else if (result.SubQueryType == SubQueryType.Schema)
            {
                token = tokenizer.GetNext();
                if (token == string.Empty)
                {
                    throw new KbParserException("Invalid query. Found '" + token + "', expected: schema name.");
                }
                result.AddAttribute(PreparedQuery.QueryAttribute.Schema, token);
                result.Schemas.Add(new QuerySchema(token));

                if (tokenizer.TryIsNextToken("with"))
                {
                    var options = new ExpectedWithOptions
                        {
                            {"includephysicalpages", typeof(bool) }
                        };
                    StaticWithOptionsParser.ParseWithOptions(ref tokenizer, options, ref result);
                }
            }
            else
            {
                throw new KbParserException("Invalid query. Found '" + token + "', expected: 'index' or 'schema'.");
            }
            }
            */

            #endregion

            #region Sample ---------------------------------------------------------------------------------------------

            /*
            else if (queryType == QueryType.Sample)
            {
            result.SubQueryType = SubQueryType.Documents;

            token = tokenizer.GetNext();
            if (token == string.Empty)
            {
                throw new KbParserException("Invalid query. Found '" + tokenizer.PeekNext() + "', expected: schema name.");
            }

            result.Schemas.Add(new QuerySchema(token));

            token = tokenizer.GetNext();
            if (token != string.Empty)
            {
                if (int.TryParse(token, out int topCount) == false)
                {
                    throw new KbParserException("Invalid query. Found '" + token + "', expected: numeric top count.");
                }
                result.RowLimit = topCount;
            }
            else
            {
                result.RowLimit = 100;
            }
            }
            */

            #endregion

            #region List -----------------------------------------------------------------------------------------------

            /*
            else if (queryType == QueryType.List)
            {
            if (tokenizer.TryIsNextToken(["documents", "schemas"]) == false)
            {
                throw new KbParserException("Invalid query. Found '" + tokenizer.PeekNext() + "', expected: 'documents' or 'schemas'.");
            }

            token = tokenizer.GetNext();
            if (Enum.TryParse<SubQueryType>(token, true, out SubQueryType subQueryType) == false)
            {
                throw new KbParserException("Invalid query. Found '" + token + "', expected: 'documents' or 'schemas'.");
            }
            result.SubQueryType = subQueryType;

            token = tokenizer.GetNext();
            if (token == string.Empty)
            {
                throw new KbParserException("Invalid query. Found '" + token + "', expected: schema name.");
            }

            result.Schemas.Add(new QuerySchema(token));

            token = tokenizer.GetNext();
            if (token != string.Empty)
            {
                if (int.TryParse(token, out int topCount) == false)
                {
                    throw new KbParserException("Invalid query. Found '" + token + "', expected: numeric top count.");
                }
                result.RowLimit = topCount;
            }
            else
            {
                result.RowLimit = 100;
            }
            }
            */

            #endregion

            #region Delete ---------------------------------------------------------------------------------------------

            /*
            else if (queryType == QueryType.Delete)
            {
            token = tokenizer.GetNext().ToLowerInvariant();
            if (token.Is("from") == false)
            {
                result.Attributes.Add(PreparedQuery.QueryAttribute.SpecificSchemaPrefix, token);

                if (tokenizer.GetNext().Is("from") == false)
                {
                    throw new KbParserException("Invalid query. Found '" + tokenizer.Breadcrumbs.Last() + "', expected: 'from'.");
                }
            }

            string sourceSchema = tokenizer.GetNext();
            string schemaAlias = string.Empty;
            if (!TokenHelpers.IsValidIdentifier(sourceSchema, ':'))
            {
                throw new KbParserException("Invalid query. Found '" + token + "', expected: schema name.");
            }

            if (tokenizer.TryIsNextToken("as"))
            {
                schemaAlias = tokenizer.GetNext();
            }

            result.Schemas.Add(new QuerySchema(sourceSchema.ToLowerInvariant(), schemaAlias.ToLowerInvariant()));

            while (tokenizer.TryIsNextToken("inner"))
            {
                if (tokenizer.TryIsNextToken("join") == false)
                {
                    throw new KbParserException("Invalid query. Found '" + tokenizer.GetNext() + "', expected: join.");
                }
                tokenizer.SkipNext();

                string subSchemaSchema = tokenizer.GetNext();
                string subSchemaAlias = string.Empty;
                if (!TokenHelpers.IsValidIdentifier(subSchemaSchema, ':'))
                {
                    throw new KbParserException("Invalid query. Found '" + token + "', expected: schema name.");
                }

                if (tokenizer.TryIsNextToken("as"))
                {
                    subSchemaAlias = tokenizer.GetNext();
                }
                else
                {
                    throw new KbParserException("Invalid query. Found '" + tokenizer.GetNext() + "', expected: 'as' (schema alias).");
                }

                token = tokenizer.GetNext();
                if (!token.Is("on"))
                {
                    throw new KbParserException("Invalid query. Found '" + token + "', expected: 'on'.");
                }

                int joinConditionsStartPosition = tokenizer.Caret;

                while (true)
                {
                    if (tokenizer.TryIsNextToken(["where", "inner", ""]))
                    {
                        break;
                    }

                    if (tokenizer.TryIsNextToken(["and", "or"]))
                    {
                        tokenizer.SkipNext();
                    }

                    var joinLeftCondition = tokenizer.GetNext();
                    if (TokenHelpers.IsValidIdentifier(joinLeftCondition, '.'))
                    {
                        throw new KbParserException("Invalid query. Found '" + joinLeftCondition + "', expected: left side of join expression.");
                    }

                    int logicalQualifierPos = tokenizer.Caret;

                    token = ConditionTokenizer.GetNext(tokenizer.Text, ref logicalQualifierPos);
                    if (ConditionTokenizer.ParseLogicalQualifier(token) == LogicalQualifier.None)
                    {
                        throw new KbParserException("Invalid query. Found '" + token + "], logical qualifier.");
                    }

                    tokenizer.SetCaret(logicalQualifierPos);

                    var joinRightCondition = tokenizer.GetNext();
                    if (!TokenHelpers.IsValidIdentifier(joinRightCondition, '.'))
                    {
                        throw new KbParserException("Invalid query. Found '" + joinRightCondition + "', expected: right side of join expression.");
                    }
                }

                var joinConditionsText = tokenizer.Text.Substring(joinConditionsStartPosition, tokenizer.Caret - joinConditionsStartPosition).Trim();
                var joinConditions = Conditions.Create(joinConditionsText, tokenizer, subSchemaAlias);

                result.Schemas.Add(new QuerySchema(subSchemaSchema.ToLowerInvariant(), subSchemaAlias.ToLowerInvariant(), joinConditions));
            }

            token = tokenizer.GetNext();
            if (token != string.Empty && !token.Is("where"))
            {
                throw new KbParserException("Invalid query. Found '" + token + "', expected: 'where' or end of statement.");
            }

            if (token.Is("where"))
            {
                var conditionTokenizer = new ConditionTokenizer(tokenizer.Text, tokenizer.Caret);
                int parenthesisScope = 0;

                while (true)
                {
                    int previousTokenPosition = conditionTokenizer.Caret;
                    var conditionToken = conditionTokenizer.GetNext();

                    if (conditionToken == "(") parenthesisScope++;
                    if (conditionToken == ")") parenthesisScope--;

                    if (parenthesisScope < 0 || int.TryParse(conditionToken, out _) == false && Enum.TryParse(conditionToken, true, out QueryType testQueryType) && Enum.IsDefined(typeof(QueryType), testQueryType))
                    {
                        //We found the beginning of a new statement, break here.
                        conditionTokenizer.SetCaret(previousTokenPosition);
                        tokenizer.SetCaret(previousTokenPosition);
                        break;
                    }
                    else if ((new string[] { "order", "group", "" }).Contains(conditionToken) && conditionTokenizer.TryIsNextToken("by"))
                    {
                        throw new KbParserException("Invalid query. Found '" + conditionToken + "', expected: end of statement.");
                    }
                    else if (conditionToken == string.Empty)
                    {
                        //Set both the condition and query position to the beginning of the "ORDER BY" or "GROUP BY".
                        conditionTokenizer.SetCaret(previousTokenPosition);
                        tokenizer.SetCaret(previousTokenPosition);
                        break;
                    }
                }

                string conditionText = tokenizer.Text.Substring(conditionTokenizer.StartPosition, conditionTokenizer.Caret - conditionTokenizer.StartPosition).Trim();
                if (conditionText == string.Empty)
                {
                    throw new KbParserException("Invalid query. Found '" + token + "', expected: list of conditions.");
                }

                result.Conditions = Conditions.Create(conditionText, tokenizer);
            }
            }
            */

            #endregion

            #region Kill -----------------------------------------------------------------------------------------------

            /*
            else if (queryType == QueryType.Kill)
            {
            string referencedProcessId = tokenizer.GetNext();
            try
            {
                result.AddAttribute(PreparedQuery.QueryAttribute.ProcessId, ulong.Parse(referencedProcessId));
            }
            catch
            {
                throw new KbParserException("Invalid query. Found '" + referencedProcessId + "', expected: numeric process id.");
            }
            }
            */

            #endregion

            #region Set ------------------------------------------------------------------------------------------------

            /*
            else if (queryType == QueryType.Set)
            {
            //Variable 
            string variableName = tokenizer.GetNext();
            string variableValue = tokenizer.GetNext();
            result.VariableValues.Add(new(variableName, variableValue));
            }
            */

            #endregion

            #region Cleanup and Validation.

            /*
            if (result.UpsertValues != null) //Fill in upsert string literals.
            {
                foreach (var upsertRow in result.UpsertValues)
                {
                    foreach (var kvp in upsertRow)
                    {
                        if (query.LiteralStrings.ContainsKey(kvp.Value.ToString()))
                        {
                            kvp.Value.Value = query.LiteralStrings[kvp.Value.Value ?? ""];
                        }
                    }
                }
            }

            foreach (var field in result.GroupFields)
            {
                //if (result.Schemas.Any(o => o.Prefix == field.Prefix) == false)
                //{
                //    throw new KbParserException($"Group-by schema alias [{field.Prefix}] for [{field.Field}] was not found in the query.");
                //}
            }

            if (result.DynamicSchemaFieldFilter != null)
            {
                foreach (var filterSchema in result.DynamicSchemaFieldFilter)
                {
                    if (result.Schemas.Any(o => o.Prefix == filterSchema) == false)
                    {
                        throw new KbParserException($"Select schema alias [{filterSchema}] was not found in the query.");
                    }
                }
            }

            if (result.Schemas.Count > 0 && result.Conditions.AllFields.Count > 0)
            {
                //If we have a schema, then we will associate the conditions with the first schema
                //  because it is the one with the WHERE clause, the other conditions are for joins.
                result.Schemas.First().Conditions = result.Conditions;
            }

            foreach (var schema in result.Schemas)
            {
                if (tokenizer.StringLiterals.TryGetValue(schema.Name, out var name))
                {
                    schema.Name = name.Substring(1, name.Length - 2);
                }

                if (tokenizer.StringLiterals.TryGetValue(schema.Prefix, out var prefix))
                {
                    schema.Prefix = prefix.Substring(1, prefix.Length - 2);
                }
            }

            foreach (var field in result.SelectFields) //Top level fields.
            {
                if (tokenizer.StringLiterals.TryGetValue(field.Alias, out var alias))
                {
                    field.Alias = alias.Substring(1, alias.Length - 2);
                }
            }

            foreach (var field in result.SelectFields.AllDocumentFields) //Document related fields.
            {
                if (result.Schemas.Any(o => o.Prefix == field.Prefix) == false)
                {
                    throw new KbParserException($"Select schema alias [{field.Prefix}] for [{field.Field}] was not found in the query.");
                }
            }

            foreach (var field in result.SortFields)
            {
                if (tokenizer.StringLiterals.TryGetValue(field.Alias, out string? alias))
                {
                    field.Alias = alias.Substring(1, alias.Length - 2);
                    field.Field = field.Alias;
                }

                if (result.SelectFields.Any(o => o.Alias == field.Alias) == false)
                {
                    //throw new KbParserException($"Order-by field [{field.Field}] was not found in the query.");
                }

                //if (result.SelectFields.Any(o => o.Key == field.Key) == false && result.DynamicallyBuildSelectList == false)
                //{
                //    throw new KbParserException($Sort-by schema alias [{field.Prefix}] for [{field.Field}] was not found in the query.");
                //}
            }

            foreach (var field in result.Conditions.AllFields)
            {
                if (field.Field.StartsWith('@'))
                {
                    //This is a variable.
                    continue;
                }

                if (result.Schemas.Any(o => o.Prefix == field.Prefix) == false)
                {
                    throw new KbParserException($"Condition schema alias [{field.Prefix}] for [{field.Field}] was not found in the query.");
                }
            }

            if (result.QueryType == QueryType.Select)
            {
                //result.DynamicallyBuildSelectListFromSchemas ??= new();

                //var starTokens = starPeek.Split('.');
                //var starSchema = string.Join('.', starTokens.Take(starTokens.Length - 1));
                //result.DynamicallyBuildSelectListFromSchemas.Add(starSchema);

                if (result.DynamicSchemaFieldFilter == null && result.SelectFields.Count == 0)
                {
                    throw new KbParserException("No fields were selected.");
                }

                if (result.Schemas.Count == 0)
                {
                    throw new KbParserException("No schemas were selected.");
                }

                if (result.DynamicSchemaFieldFilter != null && result.SelectFields.Count > 0)
                {
                    throw new KbParserException("Queries with dynamic field-sets cannot also contain explicit fields.");
                }
            }
            */

            #endregion

            #endregion
        }

        /// <summary>
        /// Parse the variable declaration in the query and remove them from the query text.
        /// </summary>
        static string PreParseQueryVariableDeclarations(string queryText, ref KbInsensitiveDictionary<KbConstant> tokenizerConstants)
        {
            var lines = queryText.Split("\n", StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim());
            lines = lines.Where(o => o.StartsWith("declare", StringComparison.InvariantCultureIgnoreCase));

            foreach (var line in lines)
            {
                var lineTokenizer = new TokenizerSlim(line);

                if (!lineTokenizer.TryEatIsNextToken("declare", out var token))
                {
                    throw new KbParserException($"Invalid query. Found '{token}', expected: 'declare'.");
                }

                if (lineTokenizer.NextCharacter != '@')
                {
                    throw new KbParserException($"Invalid query. Found '{lineTokenizer.NextCharacter}', expected: '@'.");
                }
                lineTokenizer.EatNextCharacter();

                if (lineTokenizer.TryEatValidateNextToken((o) => TokenizerExtensions.IsIdentifier(o), out var variableName) == false)
                {
                    throw new KbParserException($"Invalid query. Found '{token}', expected: 'declare'.");
                }

                if (lineTokenizer.NextCharacter != '=')
                {
                    throw new KbParserException($"Invalid query. Found '{lineTokenizer.NextCharacter}', expected: '='.");
                }
                lineTokenizer.EatNextCharacter();

                var variableValue = lineTokenizer.Remainder().Trim();

                KbBasicDataType variableType;
                if (variableValue.StartsWith('\'') && variableValue.EndsWith('\''))
                {
                    variableType = KbBasicDataType.String;
                    variableValue = variableValue.Substring(1, variableValue.Length - 2);
                }
                else
                {
                    variableType = KbBasicDataType.Numeric;
                    if (variableValue != null && double.TryParse(variableValue?.ToString(), out _) == false)
                    {
                        throw new Exception($"Non-string value of [{variableName}] cannot be converted to numeric.");
                    }
                }

                tokenizerConstants.Add($"@{variableName}", new KbConstant(variableValue, variableType));

                queryText = queryText.Replace(line, "");
            }

            return queryText.Trim();
        }

    }
}
