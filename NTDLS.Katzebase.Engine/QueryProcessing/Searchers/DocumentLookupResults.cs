﻿using NTDLS.Katzebase.Engine.QueryProcessing.Searchers.Intersection;

namespace NTDLS.Katzebase.Engine.QueryProcessing.Searchers
{
    internal class DocumentLookupResults
    {
        public List<List<string?>> Values { get; private set; } = new();

        /// <summary>
        /// This is only used when we just want to return a list of document pointers and no fields.
        /// </summary>
        public List<SchemaIntersectionRowDocumentIdentifier> DocumentIdentifiers { get; private set; } = new();

        public DocumentLookupResults(List<List<string?>> values, List<SchemaIntersectionRowDocumentIdentifier> documentIdentifiers)
        {
            Values = values;
            DocumentIdentifiers = documentIdentifiers;
        }

        public DocumentLookupResults()
        {
        }
    }
}
