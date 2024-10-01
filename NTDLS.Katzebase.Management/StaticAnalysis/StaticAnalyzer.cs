using ICSharpCode.AvalonEdit.Document;
using NTDLS.Helpers;
using NTDLS.Katzebase.Management.Classes.Editor.FoldingStrategy;
using NTDLS.Katzebase.Parsers.Query.SupportingTypes;
using System.Windows.Media;

namespace NTDLS.Katzebase.Management.StaticAnalysis
{
    internal class StaticAnalyzer
    {
        public static void ClientSideAnalysis(TextDocument textDocument,
            TextMarkerService textMarkerService, List<CachedSchema> schemaCache, QueryBatch batch, PreparedQuery query)
        {
            if (schemaCache.Count == 0)
            {
                return;
            }

            foreach (var querySchema in query.Schemas)
            {
                var serverMatchedSchema = schemaCache.FirstOrDefault(o => o.Path.Is(querySchema.Name));

                if (serverMatchedSchema == null)
                {
                    AddSyntaxError(textDocument, textMarkerService, querySchema.ScriptLine, $"Schema does not exist: [{querySchema.Name}]");
                }
            }

            /*
            //Validation (field list):
            foreach (var documentIdentifier in query.SelectFields.DocumentIdentifiers)
            {
                if (string.IsNullOrEmpty(documentIdentifier.Value.SchemaAlias) == false)
                {
                    if (query.Schemas.Any(o => o.Prefix.Is(documentIdentifier.Value.SchemaAlias)) == false)
                    {
                        exceptions.Add(new KbParserException(documentIdentifier.Value.ScriptLine ?? tokenizer.GetCurrentLineNumber(),
                            $"Schema [{documentIdentifier.Value.SchemaAlias}] referenced in field list for [{documentIdentifier.Value.FieldName}] does not exist in the query."));
                    }
                }
            }

            //Validation (conditions):
            foreach (var documentIdentifier in query.Conditions.FieldCollection.DocumentIdentifiers)
            {
                if (string.IsNullOrEmpty(documentIdentifier.Value.SchemaAlias) == false)
                {
                    if (query.Schemas.Any(o => o.Prefix.Is(documentIdentifier.Value.SchemaAlias)) == false)
                    {
                        exceptions.Add(new KbParserException(documentIdentifier.Value.ScriptLine ?? tokenizer.GetCurrentLineNumber(),
                            $"Schema [{documentIdentifier.Value.SchemaAlias}] referenced in condition for [{documentIdentifier.Value.FieldName}] does not exist in the query."));
                    }
                }
            }

            //Validation (join conditions):
            foreach (var schema in query.Schemas.Skip(1))
            {
                if (schema.Conditions != null)
                {
                    foreach (var documentIdentifier in schema.Conditions.FieldCollection.DocumentIdentifiers)
                    {
                        if (string.IsNullOrEmpty(documentIdentifier.Value.SchemaAlias) == false)
                        {
                            if (query.Schemas.Any(o => o.Prefix.Is(documentIdentifier.Value.SchemaAlias)) == false)
                            {
                                exceptions.Add(new KbParserException(documentIdentifier.Value.ScriptLine ?? tokenizer.GetCurrentLineNumber(),
                                    $"Schema [{documentIdentifier.Value.SchemaAlias}] referenced in join condition for [{documentIdentifier.Value.FieldName}] does not exist in the query."));
                            }
                        }
                    }
                }
            }

            //Validation (root conditions):
            foreach (var documentIdentifier in query.Conditions.FieldCollection.DocumentIdentifiers)
            {
                if (string.IsNullOrEmpty(documentIdentifier.Value.SchemaAlias) == false)
                {
                    if (query.Schemas.Any(o => o.Prefix.Is(documentIdentifier.Value.SchemaAlias)) == false)
                    {
                        exceptions.Add(new KbParserException(documentIdentifier.Value.ScriptLine ?? tokenizer.GetCurrentLineNumber(),
                            $"Schema [{documentIdentifier.Value.SchemaAlias}] referenced in condition for [{documentIdentifier.Value.FieldName}] does not exist in the query."));
                    }
                }
            }
            */
        }

        static void AddSyntaxError(TextDocument textDocument, TextMarkerService textMarkerService, int? lineNumber, string message)
        {
            if (lineNumber == null)
            {
                return;
            }

            var line = textDocument.GetLineByNumber(lineNumber.EnsureNotNull());
            int startOffset = line.Offset;
            int length = line.Length;
            textMarkerService.Create(startOffset, length, message, Colors.Red);
        }
    }
}
