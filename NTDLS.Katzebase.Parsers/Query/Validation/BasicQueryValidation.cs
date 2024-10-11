using NTDLS.Helpers;
using NTDLS.Katzebase.Api.Exceptions;
using NTDLS.Katzebase.Parsers.Tokens;

namespace NTDLS.Katzebase.Parsers.Query.Validation
{
    public static class BasicQueryValidation
    {
        public static void Assert(Tokenizer tokenizer, SupportingTypes.Query query)
        {
            var exceptions = new List<Exception>();

            if (query.TryGetAttribute<string>(SupportingTypes.Query.Attribute.TargetSchemaAlias, out var schemaAliasAttribute))
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

            if (exceptions.Count > 0)
            {
                throw new AggregateException(exceptions);
            }
        }
    }
}
