using NTDLS.Katzebase.Client.Exceptions;
using NTDLS.Katzebase.Client.Types;
using NTDLS.Katzebase.Engine.Functions;
using NTDLS.Katzebase.Engine.Functions.Procedures;
using NTDLS.Katzebase.Engine.Functions.Procedures.Persistent;
using NTDLS.Katzebase.Engine.Query.Constraints;
using NTDLS.Katzebase.Engine.Query.Tokenizers;
using NTDLS.Katzebase.Shared;
using static NTDLS.Katzebase.Client.KbConstants;
using static NTDLS.Katzebase.Engine.Library.EngineConstants;

namespace NTDLS.Katzebase.Engine.Query
{
    internal class StaticQueryParser
    {
        static public List<PreparedQuery> PrepareBatch(string queryText, KbInsensitiveDictionary<string?>? userParameters = null)
        {
            var tokenizer = new QueryTokenizer(queryText, userParameters);

            var queries = new List<PreparedQuery>();

            while (tokenizer.IsEnd() == false)
            {
                queries.Add(PrepareNextQuery(tokenizer));
            }

            return queries;
        }

        static public PreparedQuery PrepareNextQuery(QueryTokenizer tokenizer)
        {
            var result = new PreparedQuery();

            string token;

            if (tokenizer.IsNextStartOfQuery(out var queryType) == false)
            {
                string acceptableValues = string.Join("', '", Enum.GetValues<QueryType>().Where(o => o != QueryType.None));
                throw new KbParserException($"Invalid query. Found '{tokenizer.PeekNext()}', expected: '{acceptableValues}'.");
            }

            tokenizer.SkipNext();

            result.QueryType = queryType;

            //Parser insanity. Keep these region tags at 100 characters! :D

            #region Exec -----------------------------------------------------------------------------------------------
            if (queryType == QueryType.Exec)
            {
                result.ProcedureCall = StaticFunctionParsers.ParseProcedureParameters(tokenizer);
            }
            #endregion

            #region Begin ----------------------------------------------------------------------------------------------
            else if (queryType == QueryType.Begin)
            {
                if (tokenizer.PeekNext().Is("transaction") == false)
                    if (tokenizer.PeekNext().Is("transaction") == false)
                    {
                        throw new KbParserException("Invalid query. Found '" + tokenizer.PeekNext() + "', expected: 'transaction'.");
                    }

                token = tokenizer.GetNext();
                if (Enum.TryParse(token, true, out SubQueryType subQueryType) == false)
                {
                    throw new KbParserException("Invalid query. Found '" + token + "', expected: 'transaction'.");
                }
                result.SubQueryType = subQueryType;
            }
            #endregion

            #region Commit ---------------------------------------------------------------------------------------------
            else if (queryType == QueryType.Commit)
            {
                if (tokenizer.PeekNext().Is("transaction") == false)
                {
                    throw new KbParserException("Invalid query. Found '" + tokenizer.PeekNext() + "', expected: 'transaction'.");
                }

                token = tokenizer.GetNext();
                if (Enum.TryParse(token, true, out SubQueryType subQueryType) == false)
                {
                    throw new KbParserException("Invalid query. Found '" + token + "', expected: 'transaction.");
                }
                result.SubQueryType = subQueryType;
            }
            #endregion

            #region Rollback -------------------------------------------------------------------------------------------
            else if (queryType == QueryType.Rollback)
            {
                if (tokenizer.PeekNext().Is("transaction") == false)
                {
                    throw new KbParserException("Invalid query. Found '" + tokenizer.PeekNext() + "', expected: 'transaction'.");
                }

                token = tokenizer.GetNext();
                if (Enum.TryParse(token, true, out SubQueryType subQueryType) == false)
                {
                    throw new KbParserException("Invalid query. Found '" + token + "', expected: 'transaction'.");
                }
                result.SubQueryType = subQueryType;
            }
            #endregion

            #region Alter ----------------------------------------------------------------------------------------------
            else if (queryType == QueryType.Alter)
            {
                if (tokenizer.PeekNext().IsOneOf(["schema", "configuration"]) == false)
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
                    if (tokenizer.PeekNext().Is("with"))
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

                    if (tokenizer.PeekNext().Is("with"))
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
            #endregion

            #region Create ---------------------------------------------------------------------------------------------
            else if (queryType == QueryType.Create)
            {
                if (tokenizer.PeekNext().IsOneOf(["schema", "index", "uniquekey", "procedure"]) == false)
                {
                    throw new KbParserException("Invalid query. Found '" + tokenizer.PeekNext() + "', expected 'schema', 'index', 'uniquekey' or 'procedure'.");
                }

                token = tokenizer.GetNext();
                if (Enum.TryParse(token, true, out SubQueryType subQueryType) == false)
                {
                    throw new KbParserException("Invalid query. Found '" + token + "', expected: 'schema', 'index' or 'uniquekey'.");
                }
                result.SubQueryType = subQueryType;

                token = tokenizer.GetNext();
                if (token == string.Empty)
                {
                    throw new KbParserException("Invalid query. Found '" + token + "', expected: object name.");
                }

                if (subQueryType == SubQueryType.Procedure)
                {
                    result.AddAttribute(PreparedQuery.QueryAttribute.ObjectName, token);

                    var parameters = new List<PhysicalProcedureParameter>();

                    if (tokenizer.NextCharacter == '(') //Parse parameters
                    {
                        tokenizer.SkipNextCharacter();

                        while (true)
                        {
                            var paramName = tokenizer.GetNext();
                            if (tokenizer.GetNext().Is("as") == false)
                            {
                                throw new KbParserException("Invalid query. Found '" + tokenizer.Breadcrumbs.Last() + "', expected: 'AS'.");
                            }
                            token = tokenizer.GetNext();

                            if (Enum.TryParse(token, true, out KbProcedureParameterType paramType) == false || Enum.IsDefined(typeof(KbProcedureParameterType), paramType) == false)
                            {
                                string acceptableValues = string.Join("', '",
                                    Enum.GetValues<KbProcedureParameterType>().Where(o => o != KbProcedureParameterType.Undefined));

                                throw new KbParserException($"Invalid query. Found '{token}', expected: '{acceptableValues}'.");
                            }

                            parameters.Add(new PhysicalProcedureParameter(paramName, paramType));

                            if (tokenizer.NextCharacter != ',')
                            {
                                if (tokenizer.NextCharacter != ')')
                                {
                                    throw new KbParserException("Invalid query. Found '" + tokenizer.NextCharacter + "', expected: ')'.");
                                }
                                tokenizer.SkipNextCharacter();
                                break;
                            }
                            tokenizer.SkipNextCharacter();
                        }
                    }

                    result.AddAttribute(PreparedQuery.QueryAttribute.Parameters, parameters);

                    if (tokenizer.GetNext().Is("on") == false)
                    {
                        throw new KbParserException("Invalid query. Found '" + tokenizer.Breadcrumbs.Last() + "', expected: 'ON'.");
                    }

                    token = tokenizer.GetNext();
                    if (token == string.Empty)
                    {
                        throw new KbParserException("Invalid query. Found '" + token + "', expected: schema name.");
                    }

                    result.AddAttribute(PreparedQuery.QueryAttribute.Schema, token);

                    if (tokenizer.GetNext().Is("as") == false)
                    {
                        throw new KbParserException("Invalid query. Found '" + tokenizer.Breadcrumbs.Last() + "', expected: 'AS'.");
                    }

                    if (tokenizer.NextCharacter != '(')
                    {
                        throw new KbParserException("Invalid query. Found '" + tokenizer.NextCharacter + "', expected: '('.");
                    }

                    if (tokenizer.Remainder().Last() != ')')
                    {
                        throw new KbParserException("Invalid query. Found '" + tokenizer.NextCharacter + "', expected: ')'.");
                    }

                    tokenizer.SkipNextCharacter(); // Skip the '('.

                    var batches = new List<string>();

                    int previousPosition = tokenizer.Position;

                    while (tokenizer.IsEnd() == false)
                    {
                        if (tokenizer.NextCharacter == ')')
                        {
                            tokenizer.SkipNextCharacter();
                        }
                        else
                        {
                            _ = PrepareNextQuery(tokenizer);

                            string queryText = tokenizer.Text.Substring(previousPosition, tokenizer.Position - previousPosition).Trim();

                            foreach (var literalString in tokenizer.StringLiterals)
                            {
                                queryText = queryText.Replace(literalString.Key, literalString.Value);
                            }

                            batches.Add(queryText);

                            previousPosition = tokenizer.Position;
                            var nextToken = tokenizer.PeekNext();
                        }
                    }



                    result.AddAttribute(PreparedQuery.QueryAttribute.Batches, batches);
                }
                else if (subQueryType == SubQueryType.Schema)
                {
                    result.Schemas.Add(new QuerySchema(token));

                    if (tokenizer.PeekNext().Is("with"))
                    {
                        var options = new ExpectedWithOptions
                        {
                            {"pagesize", typeof(uint) }
                        };
                        StaticWithOptionsParser.ParseWithOptions(ref tokenizer, options, ref result);
                    }
                }
                else if (subQueryType == SubQueryType.Index || subQueryType == SubQueryType.UniqueKey)
                {
                    result.AddAttribute(PreparedQuery.QueryAttribute.IndexName, token);
                    result.AddAttribute(PreparedQuery.QueryAttribute.IsUnique, (subQueryType == SubQueryType.UniqueKey));


                    if (tokenizer.NextCharacter != '(')
                    {
                        throw new KbParserException("Invalid query. Found '" + tokenizer.NextCharacter + "', expected: ','.");
                    }
                    tokenizer.SkipDelimiters('(');

                    while (true) //Get fields
                    {
                        token = tokenizer.GetNext().ToLowerInvariant();
                        if (token == string.Empty)
                        {
                            throw new KbParserException("Invalid query. Found '" + tokenizer.PeekNext() + "', expected: ',' or ')'.");
                        }

                        result.CreateFields.Add(token);

                        if (tokenizer.NextCharacter == ',')
                        {
                            tokenizer.SkipDelimiters(',');
                        }
                        if (tokenizer.NextCharacter == ')')
                        {
                            tokenizer.SkipDelimiters(')');
                            break;
                        }
                    }

                    if (tokenizer.GetNext().Is("on") == false)
                    {
                        throw new KbParserException("Invalid query. Found '" + token + "', expected: 'on'.");
                    }

                    token = tokenizer.GetNext();
                    if (!TokenHelpers.IsValidIdentifier(token, ':'))
                    {
                        throw new KbParserException("Invalid query. Found '" + token + "', expected: schema name.");
                    }

                    result.Schemas.Add(new QuerySchema(token));

                    if (tokenizer.PeekNext().Is("with"))
                    {
                        var options = new ExpectedWithOptions
                        {
                            {"partitions", typeof(uint) }
                        };
                        StaticWithOptionsParser.ParseWithOptions(ref tokenizer, options, ref result);
                    }
                }
                else
                {
                    throw new KbNotImplementedException();
                }
            }
            #endregion

            #region Drop -----------------------------------------------------------------------------------------------
            else if (queryType == QueryType.Drop)
            {
                if (tokenizer.PeekNext().IsOneOf(["schema", "index", "uniquekey"]) == false)
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
            #endregion

            #region Rebuild --------------------------------------------------------------------------------------------
            else if (queryType == QueryType.Rebuild)
            {
                if (tokenizer.PeekNext().IsOneOf(["index", "uniquekey"]) == false)
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

                if (tokenizer.PeekNext().Is("with"))
                {
                    var options = new ExpectedWithOptions
                    {
                        {"partitions", typeof(uint) }
                    };
                    StaticWithOptionsParser.ParseWithOptions(ref tokenizer, options, ref result);
                }
            }
            #endregion

            #region Update ---------------------------------------------------------------------------------------------
            else if (queryType == QueryType.Update)
            {
                string sourceSchema = tokenizer.GetNext();
                string schemaAlias = string.Empty;
                if (!TokenHelpers.IsValidIdentifier(sourceSchema, ':'))
                {
                    throw new KbParserException("Invalid query. Found '" + sourceSchema + "', expected: schema name.");
                }

                result.Schemas.Add(new QuerySchema(sourceSchema.ToLowerInvariant()));

                if (tokenizer.GetNext().Is("set") == false)
                {
                    throw new KbParserException("Invalid query. Found '" + tokenizer.Breadcrumbs.Last() + "', expected: 'set'.");
                }

                result.UpdateValues = StaticFunctionParsers.ParseUpdateFields(tokenizer);
                result.UpdateValues.RepopulateStringNumbersAndParameters(tokenizer);

                token = tokenizer.GetNext();
                if (token != string.Empty && !token.Is("where"))
                {
                    throw new KbParserException("Invalid query. Found '" + token + "', expected: 'where' or end of statement.");
                }

                if (token.Is("where"))
                {
                    var conditionTokenizer = new ConditionTokenizer(tokenizer.Text, tokenizer.Position);
                    int parenthesisScope = 0;

                    while (true)
                    {
                        int previousTokenPosition = conditionTokenizer.Position;
                        var conditionToken = conditionTokenizer.PeekNext();

                        if (conditionToken == "(") parenthesisScope++;
                        if (conditionToken == ")") parenthesisScope--;

                        if (parenthesisScope < 0 || int.TryParse(conditionToken, out _) == false && Enum.TryParse(conditionToken, true, out QueryType testQueryType) && Enum.IsDefined(typeof(QueryType), testQueryType))
                        {
                            //We found the beginning of a new statement, break here.
                            conditionTokenizer.SetPosition(previousTokenPosition);
                            tokenizer.SetPosition(previousTokenPosition);
                            break;
                        }

                        conditionTokenizer.SkipNext();

                        if ((new string[] { "order", "group", "" }).Contains(conditionToken) && conditionTokenizer.PeekNext().Is("by"))
                        {
                            throw new KbParserException("Invalid query. Found '" + conditionToken + "', expected: end of statement.");
                        }
                        else if (conditionToken == string.Empty)
                        {
                            //Set both the condition and query position to the beginning of the "ORDER BY" or "GROUP BY".
                            conditionTokenizer.SetPosition(previousTokenPosition);
                            tokenizer.SetPosition(previousTokenPosition);
                            break;
                        }
                    }

                    string conditionText = tokenizer.Text.Substring(conditionTokenizer.StartPosition, conditionTokenizer.Position - conditionTokenizer.StartPosition).Trim();
                    if (conditionText == string.Empty)
                    {
                        throw new KbParserException("Invalid query. Found '" + token + "', expected: list of conditions.");
                    }

                    result.Conditions = Conditions.Create(conditionText, tokenizer);
                }
            }
            #endregion

            #region Analyze --------------------------------------------------------------------------------------------
            else if (queryType == QueryType.Analyze)
            {
                if (tokenizer.PeekNext().IsOneOf(["index", "schema"]) == false)
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

                    if (tokenizer.PeekNext().Is("with"))
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
            #endregion

            #region Sample ---------------------------------------------------------------------------------------------
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
            #endregion

            #region List -----------------------------------------------------------------------------------------------
            else if (queryType == QueryType.List)
            {
                if (tokenizer.PeekNext().IsOneOf(["documents", "schemas"]) == false)
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
            #endregion

            #region Select ---------------------------------------------------------------------------------------------
            else if (queryType == QueryType.Select)
            {
                if (tokenizer.PeekNext().Is("top"))
                {
                    tokenizer.SkipNext();
                    result.RowLimit = tokenizer.GetNextAsInt();
                }

                var starPeek = tokenizer.PeekNext();
                if (starPeek == "*")
                {
                    //Select all fields from all schemas.
                    tokenizer.SkipNext();

                    result.DynamicSchemaFieldFilter ??= new();
                }
                else if (starPeek.EndsWith(".*"))
                {
                    //Select all fields from given schema.
                    tokenizer.SkipNext();

                    result.DynamicSchemaFieldFilter ??= new();
                    var starSchemaAlias = starPeek.Substring(0, starPeek.Length - 2); //Trim off the trailing .*
                    result.DynamicSchemaFieldFilter.Add(starSchemaAlias.ToLowerInvariant());
                }
                else
                {
                    result.SelectFields = StaticFunctionParsers.ParseQueryFields(tokenizer);
                    result.SelectFields.RepopulateStringNumbersAndParameters(tokenizer);
                }

                if (tokenizer.PeekNext().Is("into"))
                {
                    tokenizer.SkipNext();
                    var selectIntoSchema = tokenizer.GetNext();
                    result.AddAttribute(PreparedQuery.QueryAttribute.TargetSchema, selectIntoSchema);

                    result.QueryType = QueryType.SelectInto;
                }

                if (tokenizer.PeekNext().Is("from"))
                {
                    tokenizer.SkipNext();
                }
                else
                {
                    throw new KbParserException("Invalid query. Found '" + tokenizer.PeekNext() + "', expected: 'from'.");
                }

                string sourceSchema = tokenizer.GetNext();
                string schemaAlias = string.Empty;
                if (!TokenHelpers.IsValidIdentifier(sourceSchema, ['#', ':']))
                {
                    throw new KbParserException("Invalid query. Found '" + sourceSchema + "', expected: schema name.");
                }

                if (tokenizer.PeekNext().Is("as"))
                {
                    tokenizer.SkipNext();
                    schemaAlias = tokenizer.GetNext();
                }

                result.Schemas.Add(new QuerySchema(sourceSchema.ToLowerInvariant(), schemaAlias.ToLowerInvariant()));

                while (tokenizer.PeekNext().Is("inner"))
                {
                    tokenizer.SkipNext();
                    if (tokenizer.PeekNext().Is("join") == false)
                    {
                        throw new KbParserException("Invalid query. Found '" + tokenizer.GetNext() + "', expected: 'join'.");
                    }
                    tokenizer.SkipNext();

                    string subSchemaSchema = tokenizer.GetNext();
                    string subSchemaAlias = string.Empty;
                    if (!TokenHelpers.IsValidIdentifier(subSchemaSchema, ':'))
                    {
                        throw new KbParserException("Invalid query. Found '" + subSchemaSchema + "', expected: schema name.");
                    }

                    if (tokenizer.PeekNext().Is("as"))
                    {
                        tokenizer.SkipNext();
                        subSchemaAlias = tokenizer.GetNext();
                    }
                    else
                    {
                        throw new KbParserException("Invalid query. Found '" + tokenizer.GetNext() + "', expected: 'as' (schema alias).");
                    }

                    token = tokenizer.GetNext();
                    if (!token.Is("on"))
                    {
                        throw new KbParserException("Invalid query. Found '" + token + "', expected 'on'.");
                    }

                    int joinConditionsStartPosition = tokenizer.Position;

                    while (true)
                    {
                        if (tokenizer.PeekNext().IsOneOf(["where", "order", "inner", ""]))
                        {
                            break;
                        }

                        if (tokenizer.IsNextStartOfQuery())
                        {
                            //Found start of next query.
                            break;
                        }

                        if (tokenizer.PeekNext().IsOneOf(["and", "or"]))
                        {
                            tokenizer.SkipNext();
                        }

                        var joinLeftCondition = tokenizer.GetNext();
                        if (!TokenHelpers.IsValidIdentifier(joinLeftCondition, '.'))
                        {
                            throw new KbParserException("Invalid query. Found '" + joinLeftCondition + "', expected: left side of join expression.");
                        }

                        int logicalQualifierPos = tokenizer.Position;

                        token = ConditionTokenizer.GetNext(tokenizer.Text, ref logicalQualifierPos);
                        if (ConditionTokenizer.ParseLogicalQualifier(token) == LogicalQualifier.None)
                        {
                            throw new KbParserException("Invalid query. Found '" + token + "], expected logical qualifier.");
                        }

                        tokenizer.SetPosition(logicalQualifierPos);

                        var joinRightCondition = tokenizer.GetNext();
                        if (!TokenHelpers.IsValidIdentifier(joinRightCondition, '.'))
                        {
                            throw new KbParserException("Invalid query. Found '" + joinRightCondition + "', expected: right side of join expression.");
                        }
                    }

                    var joinConditionsText = tokenizer.Text.Substring(joinConditionsStartPosition, tokenizer.Position - joinConditionsStartPosition).Trim();
                    var joinConditions = Conditions.Create(joinConditionsText, tokenizer, subSchemaAlias);

                    result.Schemas.Add(new QuerySchema(subSchemaSchema.ToLowerInvariant(), subSchemaAlias.ToLowerInvariant(), joinConditions));
                }

                if (tokenizer.PeekNext().Is("where"))
                {
                    tokenizer.SkipNext();

                    var conditionTokenizer = new ConditionTokenizer(tokenizer.Text, tokenizer.Position);
                    int parenthesisScope = 0;

                    while (true)
                    {
                        int previousTokenPosition = conditionTokenizer.Position;
                        var conditionToken = conditionTokenizer.PeekNext();

                        if (conditionToken == "(") parenthesisScope++;
                        if (conditionToken == ")") parenthesisScope--;

                        if (parenthesisScope < 0 || int.TryParse(conditionToken, out _) == false && Enum.TryParse(conditionToken, true, out QueryType testQueryType) && Enum.IsDefined(typeof(QueryType), testQueryType))
                        {
                            //We found the beginning of a new statement, break here.
                            conditionTokenizer.SetPosition(previousTokenPosition);
                            tokenizer.SetPosition(previousTokenPosition);
                            break;
                        }

                        conditionTokenizer.SkipNext();

                        if (((new string[] { "order", "group", "" }).Contains(conditionToken) && conditionTokenizer.PeekNext().Is("by"))
                            || conditionToken == string.Empty)
                        {
                            //Set both the condition and query position to the beginning of the "ORDER BY" or "GROUP BY".
                            conditionTokenizer.SetPosition(previousTokenPosition);
                            tokenizer.SetPosition(previousTokenPosition);
                            break;
                        }
                    }

                    string conditionText = tokenizer.Text.Substring(conditionTokenizer.StartPosition, conditionTokenizer.Position - conditionTokenizer.StartPosition).Trim();
                    if (conditionText == string.Empty)
                    {
                        throw new KbParserException("Invalid query. Found '" + conditionText + "', expected: list of conditions.");
                    }

                    result.Conditions = Conditions.Create(conditionText, tokenizer);
                }

                if (tokenizer.PeekNext().Is("group"))
                {
                    tokenizer.SkipNext();

                    if (tokenizer.PeekNext().Is("by") == false)
                    {
                        throw new KbParserException("Invalid query. Found '" + tokenizer.GetNext() + "', expected: 'by'.");
                    }
                    tokenizer.SkipNext();

                    result.GroupFields = StaticFunctionParsers.ParseGroupByFields(tokenizer);

                    /*

                    while (true)
                    {
                        int previousTokenPosition = query.Position;
                        var fieldToken = query.GetNextToken();

                        if (result.SortFields.Count > 0)
                        {
                            if (query.NextCharacter == ',')
                            {
                                query.SkipDelimiters();
                                fieldToken = query.GetNextToken();
                            }
                            else if (!(query.Position < query.Length || query.PeekNextToken().Is("order") == false)) //We should have consumed the entire GROUP BY at this point.
                            {
                                throw new KbParserException("Invalid query. Found '" + fieldToken + "', expected: ','.");
                            }
                        }

                        if (((new string[] { "order", "" }).Contains(fieldToken) && query.PeekNextToken().Is("by")) || fieldToken == string.Empty)
                        {
                            //Set query position to the beginning of the "ORDER BY"..
                            query.SetPosition(previousTokenPosition);
                            break;
                        }

                        result.GroupFields.Add(fieldToken);

                        if (query.NextCharacter == ',')
                        {
                            query.SkipDelimiters();
                        }
                    }
                    */
                }

                if (tokenizer.PeekNext().Is("order"))
                {
                    tokenizer.SkipNext();

                    if (tokenizer.PeekNext().Is("by") == false)
                    {
                        throw new KbParserException("Invalid query. Found '" + tokenizer.GetNext() + "', expected: 'by'.");
                    }
                    tokenizer.SkipNext();

                    var fields = new List<string>();

                    while (true)
                    {
                        int previousTokenPosition = tokenizer.Position;
                        var fieldToken = tokenizer.PeekNext();

                        if (int.TryParse(fieldToken, out _) == false && Enum.TryParse(fieldToken, true, out QueryType testQueryType) && Enum.IsDefined(typeof(QueryType), testQueryType))
                        {
                            //We found the beginning of a new statement, break here.
                            break;
                        }

                        tokenizer.SkipNext();

                        if (result.SortFields.Count > 0)
                        {
                            if (tokenizer.NextCharacter == ',')
                            {
                                tokenizer.SkipDelimiters();
                                fieldToken = tokenizer.GetNext();
                            }
                            else if (tokenizer.Position < tokenizer.Length) //We should have consumed the entire query at this point.
                            {
                                throw new KbParserException("Invalid query. Found '" + fieldToken + "', expected: ','.");
                            }
                        }

                        if (fieldToken == string.Empty)
                        {
                            if (tokenizer.Position < tokenizer.Length)
                            {
                                throw new KbParserException("Invalid query. Found '" + tokenizer.Remainder() + "', expected: end of statement.");
                            }

                            tokenizer.SetPosition(previousTokenPosition);
                            break;
                        }

                        var sortDirection = KbSortDirection.Ascending;
                        if (tokenizer.PeekNext().IsOneOf(["asc", "desc"]))
                        {
                            if (tokenizer.GetNext().Is("desc"))
                            {
                                sortDirection = KbSortDirection.Descending;
                            }
                        }

                        result.SortFields.Add(fieldToken, sortDirection);
                    }
                }
            }
            #endregion

            #region Delete ---------------------------------------------------------------------------------------------
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

                if (tokenizer.PeekNext().Is("as"))
                {
                    tokenizer.SkipNext();
                    schemaAlias = tokenizer.GetNext();
                }

                result.Schemas.Add(new QuerySchema(sourceSchema.ToLowerInvariant(), schemaAlias.ToLowerInvariant()));

                while (tokenizer.PeekNext().Is("inner"))
                {
                    tokenizer.SkipNext();
                    if (tokenizer.PeekNext().Is("join") == false)
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

                    if (tokenizer.PeekNext().Is("as"))
                    {
                        tokenizer.SkipNext();
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

                    int joinConditionsStartPosition = tokenizer.Position;

                    while (true)
                    {
                        if (tokenizer.PeekNext().IsOneOf(["where", "inner", ""]))
                        {
                            break;
                        }

                        if (tokenizer.PeekNext().IsOneOf(["and", "or"]))
                        {
                            tokenizer.SkipNext();
                        }

                        var joinLeftCondition = tokenizer.GetNext();
                        if (TokenHelpers.IsValidIdentifier(joinLeftCondition, '.'))
                        {
                            throw new KbParserException("Invalid query. Found '" + joinLeftCondition + "', expected: left side of join expression.");
                        }

                        int logicalQualifierPos = tokenizer.Position;

                        token = ConditionTokenizer.GetNext(tokenizer.Text, ref logicalQualifierPos);
                        if (ConditionTokenizer.ParseLogicalQualifier(token) == LogicalQualifier.None)
                        {
                            throw new KbParserException("Invalid query. Found '" + token + "], logical qualifier.");
                        }

                        tokenizer.SetPosition(logicalQualifierPos);

                        var joinRightCondition = tokenizer.GetNext();
                        if (!TokenHelpers.IsValidIdentifier(joinRightCondition, '.'))
                        {
                            throw new KbParserException("Invalid query. Found '" + joinRightCondition + "', expected: right side of join expression.");
                        }
                    }

                    var joinConditionsText = tokenizer.Text.Substring(joinConditionsStartPosition, tokenizer.Position - joinConditionsStartPosition).Trim();
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
                    var conditionTokenizer = new ConditionTokenizer(tokenizer.Text, tokenizer.Position);
                    int parenthesisScope = 0;

                    while (true)
                    {
                        int previousTokenPosition = conditionTokenizer.Position;
                        var conditionToken = conditionTokenizer.GetNext();

                        if (conditionToken == "(") parenthesisScope++;
                        if (conditionToken == ")") parenthesisScope--;

                        if (parenthesisScope < 0 || int.TryParse(conditionToken, out _) == false && Enum.TryParse(conditionToken, true, out QueryType testQueryType) && Enum.IsDefined(typeof(QueryType), testQueryType))
                        {
                            //We found the beginning of a new statement, break here.
                            conditionTokenizer.SetPosition(previousTokenPosition);
                            tokenizer.SetPosition(previousTokenPosition);
                            break;
                        }
                        else if ((new string[] { "order", "group", "" }).Contains(conditionToken) && conditionTokenizer.PeekNext().Is("by"))
                        {
                            throw new KbParserException("Invalid query. Found '" + conditionToken + "', expected: end of statement.");
                        }
                        else if (conditionToken == string.Empty)
                        {
                            //Set both the condition and query position to the beginning of the "ORDER BY" or "GROUP BY".
                            conditionTokenizer.SetPosition(previousTokenPosition);
                            tokenizer.SetPosition(previousTokenPosition);
                            break;
                        }
                    }

                    string conditionText = tokenizer.Text.Substring(conditionTokenizer.StartPosition, conditionTokenizer.Position - conditionTokenizer.StartPosition).Trim();
                    if (conditionText == string.Empty)
                    {
                        throw new KbParserException("Invalid query. Found '" + token + "', expected: list of conditions.");
                    }

                    result.Conditions = Conditions.Create(conditionText, tokenizer);
                }
            }
            #endregion

            #region Kill -----------------------------------------------------------------------------------------------
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
            #endregion

            #region Set ------------------------------------------------------------------------------------------------
            else if (queryType == QueryType.Set)
            {
                //Variable 
                string variableName = tokenizer.GetNext();
                string variableValue = tokenizer.GetNext();
                result.VariableValues.Add(new(variableName, variableValue));
            }
            #endregion

            #region Insert ---------------------------------------------------------------------------------------------
            else if (queryType == QueryType.Insert)
            {
                if (tokenizer.GetNext().Is("into") == false)
                {
                    throw new KbParserException("Invalid query. Found '" + tokenizer.Breadcrumbs.Last() + "', expected: 'into'.");
                }

                token = tokenizer.GetNext();
                if (token == string.Empty)
                {
                    throw new KbParserException("Invalid query. Found '" + tokenizer.Breadcrumbs.Last() + "', expected: schema name.");
                }
                result.Schemas.Add(new QuerySchema(token));

                if (tokenizer.NextCharacter != '(')
                {
                    throw new KbParserException("Invalid query. Found '" + tokenizer.NextCharacter + "', expected: '('.");
                }

                result.UpsertValues = StaticFunctionParsers.ParseInsertFields(tokenizer);
                foreach (var upsertValue in result.UpsertValues)
                {
                    upsertValue.RepopulateStringNumbersAndParameters(tokenizer);
                }
            }
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
            */

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

            #endregion

            return result;
        }

        /*
        private static UpsertKeyValues ParseUpsertKeyValues(string conditionsText, ref int position)
        {
            UpsertKeyValues keyValuePairs = new UpsertKeyValues();
            int beforeTokenPosition;

            while (true)
            {
                string token;
                beforeTokenPosition = position;
                if ((token = Utilities.GetNextToken(conditionsText, ref position)) == string.Empty)
                {
                    if (keyValuePairs.Collection.Count > 0)
                    {
                        break; //Completed successfully.
                    }
                    throw new KbParserException("Invalid query. Unexpected end of query found.");
                }

                if (token.Is("where"))
                {
                    position = beforeTokenPosition;
                    break; //Completed successfully.
                }

                var keyValue = new UpsertKeyValue();

                if (!Utilities.IsValidIdentifier(token))
                {
                    throw new KbParserException("Invalid query. Found '" + token + "', expected: identifier name.");
                }
                keyValue.Key = token;

                token = Utilities.GetNextToken(conditionsText, ref position);
                if (token != "=")
                {
                    throw new KbParserException("Invalid query. Found '" + token + "', expected: '='.");
                }

                if ((token = Utilities.GetNextToken(conditionsText, ref position)) == string.Empty)
                {
                    throw new KbParserException("Invalid query. Found '" + token + "', expected: condition value.");
                }
                keyValue.Value.Value = token;

                keyValuePairs.Collection.Add(keyValue);
            }

            return keyValuePairs;
        }
        */
    }
}
