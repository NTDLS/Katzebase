using NTDLS.Helpers;
using NTDLS.Katzebase.Api.Exceptions;
using NTDLS.Katzebase.Engine.QueryProcessing.Expressions;
using NTDLS.Katzebase.Parsers.Fields.Expressions;
using NTDLS.Katzebase.Parsers.Fields;
using NTDLS.Katzebase.Parsers.Functions.Aggregate;
using NTDLS.Katzebase.Parsers.Tokens;

namespace NTDLS.Katzebase.Parsers.Validation
{
    public static class BasicQueryValidation
    {
        public static void Assert(Tokenizer tokenizer, PreparedQuery query)
        {
            var exceptions = new List<Exception>();

            if (query.TryGetAttribute<string>(PreparedQuery.Attribute.TargetSchemaAlias, out var schemaAliasAttribute))
            {
                if (query.Schemas.Any(o => o.Alias.Is(schemaAliasAttribute)) == false)
                {
                    exceptions.Add(new KbParserException(query.ScriptLine ?? tokenizer.GetCurrentLineNumber(),
                        $"Schema [{schemaAliasAttribute}] referenced in field list for [*] does not exist in the query."));
                }
            }

            //Validation (Dynamic Schema Filter):
            if (query.DynamicSchemaFieldFilter != null)
            {
                foreach (var schemaAlias in query.DynamicSchemaFieldFilter)
                {
                    if (query.Schemas.Any(o => o.Alias.Is(schemaAlias)) == false)
                    {
                        exceptions.Add(new KbParserException(query.ScriptLine ?? tokenizer.GetCurrentLineNumber(),
                            $"Schema [{schemaAlias}] referenced in field list for [*] does not exist in the query."));
                    }
                }
            }

            //Validation (Dynamic Schema Filter):
            if (query.DynamicSchemaFieldFilter != null)
            {
                foreach (var schemaAlias in query.DynamicSchemaFieldFilter)
                {
                    if (query.Schemas.Any(o => o.Alias.Is(schemaAlias)) == false)
                    {
                        exceptions.Add(new KbParserException(query.ScriptLine ?? tokenizer.GetCurrentLineNumber(),
                            $"Schema [{schemaAlias}] referenced in field list for [*] does not exist in the query."));
                    }
                }
            }

            //Validation (field list):
            foreach (var documentIdentifier in query.SelectFields.DocumentIdentifiers)
            {
                if (query.Schemas.Any(o => o.Alias.Is(documentIdentifier.Value.SchemaAlias)) == false)
                {
                    exceptions.Add(new KbParserException(documentIdentifier.Value.ScriptLine ?? tokenizer.GetCurrentLineNumber(),
                        $"Schema [{documentIdentifier.Value.SchemaAlias}] referenced in field list for [{documentIdentifier.Value.FieldName}] does not exist in the query."));
                }
            }

            //Validation (field list):
            foreach (var documentIdentifier in query.UpdateFieldValues.DocumentIdentifiers)
            {
                if (query.Schemas.Any(o => o.Alias.Is(documentIdentifier.Value.SchemaAlias)) == false)
                {
                    exceptions.Add(new KbParserException(documentIdentifier.Value.ScriptLine ?? tokenizer.GetCurrentLineNumber(),
                        $"Schema [{documentIdentifier.Value.SchemaAlias}] referenced in field list for [{documentIdentifier.Value.FieldName}] does not exist in the query."));
                }
            }

            //Validation (conditions):
            foreach (var documentIdentifier in query.Conditions.FieldCollection.DocumentIdentifiers)
            {
                if (query.Schemas.Any(o => o.Alias.Is(documentIdentifier.Value.SchemaAlias)) == false)
                {
                    exceptions.Add(new KbParserException(documentIdentifier.Value.ScriptLine ?? tokenizer.GetCurrentLineNumber(),
                        $"Schema [{documentIdentifier.Value.SchemaAlias}] referenced in condition for [{documentIdentifier.Value.FieldName}] does not exist in the query."));
                }
            }

            //Validation (join conditions):
            foreach (var schema in query.Schemas.Skip(1))
            {
                if (schema.Conditions != null)
                {
                    foreach (var documentIdentifier in schema.Conditions.FieldCollection.DocumentIdentifiers)
                    {
                        if (query.Schemas.Any(o => o.Alias.Is(documentIdentifier.Value.SchemaAlias)) == false)
                        {
                            exceptions.Add(new KbParserException(documentIdentifier.Value.ScriptLine ?? tokenizer.GetCurrentLineNumber(),
                                $"Schema [{documentIdentifier.Value.SchemaAlias}] referenced in join condition for [{documentIdentifier.Value.FieldName}] does not exist in the query."));
                        }
                    }
                }
            }

            //Validation (root conditions):
            foreach (var documentIdentifier in query.Conditions.FieldCollection.DocumentIdentifiers)
            {
                if (query.Schemas.Any(o => o.Alias.Is(documentIdentifier.Value.SchemaAlias)) == false)
                {
                    exceptions.Add(new KbParserException(documentIdentifier.Value.ScriptLine ?? tokenizer.GetCurrentLineNumber(),
                        $"Schema [{documentIdentifier.Value.SchemaAlias}] referenced in condition for [{documentIdentifier.Value.FieldName}] does not exist in the query."));
                }
            }

            //Validate group by:
            if (query.GroupBy.Count > 0)
            {
                var fieldsToValidate = new List<QueryField>();

                foreach (var field in query.GroupBy)
                {
                    var testText = field.Expression.SimplifyScalarQueryField(query, query.GroupBy);
                }

                foreach (var field in query.SelectFields)
                {

                    var testText = field.Expression.SimplifyScalarQueryField(query, query.SelectFields);

                    if (field.Expression is IQueryFieldExpression expressionField)
                    {



                        var isAggregate = expressionField.FunctionDependencies.Any(o => AggregateFunctionCollection.TryGetFunction(o.FunctionName, out _));
                        if (isAggregate == false)
                        {
                            fieldsToValidate.Add(field);
                        }

                    }
                    else if (field.Expression is QueryFieldDocumentIdentifier identifierField)
                    {
                        fieldsToValidate.Add(field);
                    }
                }


            }

            if (exceptions.Count > 0)
            {
                throw new AggregateException(exceptions);
            }
        }
    }
}
