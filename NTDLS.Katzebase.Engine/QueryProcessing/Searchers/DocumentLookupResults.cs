﻿using NTDLS.Katzebase.Engine.QueryProcessing.Searchers.Intersection;

namespace NTDLS.Katzebase.Engine.QueryProcessing.Searchers
{
    internal class DocumentLookupResults
    {
        public List<List<string?>> RowValues { get; private set; } = new();

        /// <summary>
        /// This is only used when we just want to return a list of document pointers and no fields.
        /// </summary>
        public List<SchemaIntersectionRowDocumentIdentifier> RowDocumentIdentifiers { get; private set; } = new();

        public void AddRange(OLD_SchemaIntersectionRowCollection rowCollection)
        {
            RowValues = rowCollection.Select(o => o.ToList()).ToList();
        }
    }
}
