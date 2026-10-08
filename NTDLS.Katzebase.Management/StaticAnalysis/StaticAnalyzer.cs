using ICSharpCode.AvalonEdit.Document;
using NTDLS.Helpers;
using NTDLS.Katzebase.Management.Classes.Editor;
using NTDLS.Katzebase.Parsers;
using System.Windows.Media;

namespace NTDLS.Katzebase.Management.StaticAnalysis
{
    internal class StaticAnalyzer
    {
        public static List<Action> ClientSideAnalysis(TextDocument textDocument, TextMarkerService textMarkerService,
            SchemaCacheSnapshot? schemaCache, PreparedQueryBatch batch, PreparedQuery query)
        {
            var actions = new List<Action>();

            if (schemaCache == null)
            {
                return actions;
            }

            foreach (var querySchema in query.Schemas)
            {
                if (string.IsNullOrWhiteSpace(querySchema.Name) || querySchema.Name.StartsWith('#'))
                {
                    continue; //Temporary schemas only exist within the session that creates them.
                }

                //Null when the relevant part of the schema tree hasn't been loaded yet: in that case nothing is reported,
                //  rather than flagging schemas that simply haven't been discovered yet.
                var exists = schemaCache.Exists(querySchema.Name);

                //Statements that create the schema require that it does not exist yet.
                if ((query.QueryType == Parsers.Constants.QueryType.Create || query.QueryType == Parsers.Constants.QueryType.Attach)
                    && query.SubQueryType == Parsers.Constants.SubQueryType.Schema)
                {
                    if (exists == true)
                    {
                        actions.Add(AddSyntaxError(textDocument, textMarkerService, querySchema.ScriptLine, $"Schema already exists: [{querySchema.Name}]"));
                    }
                }
                else if (exists == false)
                {
                    actions.Add(AddSyntaxError(textDocument, textMarkerService, querySchema.ScriptLine, $"Schema does not exist: [{querySchema.Name}]"));
                }
            }

            //Validation (field list):
            foreach (var documentIdentifier in query.SelectFields.DocumentIdentifiers)
            {
                if (string.IsNullOrEmpty(documentIdentifier.Value.SchemaAlias) == false)
                {
                    if (query.Schemas.Any(o => o.Alias.Is(documentIdentifier.Value.SchemaAlias)) == false)
                    {
                        //exceptions.Add(new KbParserException(documentIdentifier.Value.ScriptLine ?? tokenizer.GetCurrentLineNumber(),
                        //    $"Schema [{documentIdentifier.Value.SchemaAlias}] referenced in field list for [{documentIdentifier.Value.FieldName}] does not exist in the query."));
                    }
                }
            }

            //Validation (conditions):
            foreach (var documentIdentifier in query.Conditions.FieldCollection.DocumentIdentifiers)
            {
                if (string.IsNullOrEmpty(documentIdentifier.Value.SchemaAlias) == false)
                {
                    if (query.Schemas.Any(o => o.Alias.Is(documentIdentifier.Value.SchemaAlias)) == false)
                    {
                        //exceptions.Add(new KbParserException(documentIdentifier.Value.ScriptLine ?? tokenizer.GetCurrentLineNumber(),
                        //    $"Schema [{documentIdentifier.Value.SchemaAlias}] referenced in condition for [{documentIdentifier.Value.FieldName}] does not exist in the query."));
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
                            if (query.Schemas.Any(o => o.Alias.Is(documentIdentifier.Value.SchemaAlias)) == false)
                            {
                                //exceptions.Add(new KbParserException(documentIdentifier.Value.ScriptLine ?? tokenizer.GetCurrentLineNumber(),
                                //    $"Schema [{documentIdentifier.Value.SchemaAlias}] referenced in join condition for [{documentIdentifier.Value.FieldName}] does not exist in the query."));
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
                    if (query.Schemas.Any(o => o.Alias.Is(documentIdentifier.Value.SchemaAlias)) == false)
                    {
                        //exceptions.Add(new KbParserException(documentIdentifier.Value.ScriptLine ?? tokenizer.GetCurrentLineNumber(),
                        //    $"Schema [{documentIdentifier.Value.SchemaAlias}] referenced in condition for [{documentIdentifier.Value.FieldName}] does not exist in the query."));
                    }
                }
            }

            return actions;
        }

        static Action AddSyntaxError(TextDocument textDocument, TextMarkerService textMarkerService, int? lineNumber, string message)
        {
            return () =>
            {
                if (lineNumber == null)
                {
                    return;
                }

                try
                {
                    var line = textDocument.GetLineByNumber(lineNumber.EnsureNotNull());
                    int startOffset = line.Offset;
                    int length = line.Length;
                    textMarkerService.Create(startOffset, length, message, Colors.Red);
                }
                catch
                {
                }
            };
        }
    }
}
