using NTDLS.Katzebase.Api.Models;

namespace NTDLS.Katzebase.Management.StaticAnalysis
{
    /// <summary>
    /// What is known about a server schema, cached for the server explorer and static analysis.
    ///
    /// Instances are immutable: the background cache replaces an item rather than modifying it, so readers on other
    /// threads (the UI and static analysis) can never observe a partially updated item.
    /// </summary>
    public class CachedSchema
    {
        public KbSchema Schema { get; }
        public IReadOnlyList<string> Fields { get; }
        public IReadOnlyList<KbIndex> Indexes { get; }

        /// <summary>
        /// When the schema's child schemas were last listed from the server, null if they have not been listed yet.
        /// Until they have, the absence of a child in the cache does not mean it does not exist on the server.
        /// </summary>
        public DateTime? ChildrenLoadedUtc { get; }

        /// <summary>
        /// When the schema's fields and indexes were last loaded from the server, null if they have not been loaded yet.
        /// </summary>
        public DateTime? DetailsLoadedUtc { get; }

        public CachedSchema(KbSchema schema, IReadOnlyList<string>? fields = null, IReadOnlyList<KbIndex>? indexes = null,
            DateTime? childrenLoadedUtc = null, DateTime? detailsLoadedUtc = null)
        {
            Schema = schema;
            Fields = fields ?? [];
            Indexes = indexes ?? [];
            ChildrenLoadedUtc = childrenLoadedUtc;
            DetailsLoadedUtc = detailsLoadedUtc;
        }

        public CachedSchema WithSchema(KbSchema schema)
            => new(schema, Fields, Indexes, ChildrenLoadedUtc, DetailsLoadedUtc);

        public CachedSchema WithChildrenLoaded(DateTime loadedUtc)
            => new(Schema, Fields, Indexes, loadedUtc, DetailsLoadedUtc);

        public CachedSchema WithDetails(IReadOnlyList<string> fields, IReadOnlyList<KbIndex> indexes, DateTime loadedUtc)
            => new(Schema, fields, indexes, ChildrenLoadedUtc, loadedUtc);

        /// <summary>
        /// Returns true if the given fields and indexes differ from this item's.
        /// </summary>
        public bool AreDetailsDifferent(IReadOnlyList<string> fields, IReadOnlyList<KbIndex> indexes)
        {
            static IEnumerable<string> FieldSignature(IEnumerable<string> values)
                => values.Select(o => o.ToLowerInvariant()).OrderBy(o => o, StringComparer.Ordinal);

            static IEnumerable<string> IndexSignature(IEnumerable<KbIndex> values)
                => values.Select(o => $"{o.Id}|{o.Name}|{o.IsUnique}|{string.Join(",", o.Attributes.Select(a => a.Field))}")
                    .OrderBy(o => o, StringComparer.Ordinal);

            return !FieldSignature(Fields).SequenceEqual(FieldSignature(fields))
                || !IndexSignature(Indexes).SequenceEqual(IndexSignature(indexes));
        }
    }
}
