﻿using NTDLS.Katzebase.Engine.QueryProcessing.Sorting;

namespace NTDLS.Katzebase.Engine.QueryProcessing.Searchers.Intersection
{
    internal class MaterializedRowCollection
    {
        public List<MaterializedRow> Rows { get; private set; } = new();

        public void RemoveDuplicateRows()
        {
            Rows = Rows.Distinct(new MaterializedRowEqualityComparer()).ToList();
        }

        /// <summary>
        /// This is only used when we just want to return a list of document pointers and no fields.
        /// </summary>
        public List<SchemaIntersectionRowDocumentIdentifier> DocumentIdentifiers { get; private set; } = new();
    }
}
