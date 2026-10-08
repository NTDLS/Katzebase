using NTDLS.Helpers;
using NTDLS.Katzebase.Parsers.Conditions;
using NTDLS.Katzebase.Parsers.Fields;
using NTDLS.Katzebase.Parsers.Parsing;
using NTDLS.Katzebase.PersistentTypes.Index;
using System.Text;
using static NTDLS.Katzebase.Parsers.Constants;

namespace NTDLS.Katzebase.Engine.Indexes
{
    /// <summary>
    /// Suggests indexes that would let a query's conditions be answered by an index lookup instead of reading every document.
    /// Uses the same rules as IndexingConditionOptimization for which conditions can use an index, so that it never suggests
    ///  an index that the optimizer could not use.
    /// </summary>
    internal static class IndexAdvisor
    {
        private class Suggestion
        {
            public List<string> Fields { get; } = new();
            public PhysicalIndex? OutdatedIndex { get; set; }
            public List<string> Reasons { get; } = new();
        }

        /// <summary>
        /// Returns the suggestions as text, or null when there are none.
        /// </summary>
        /// <param name="schemaName">The schema name as written in the query, used in the suggested statements.</param>
        /// <param name="workingSchemaPrefix">The alias of the schema in the query.</param>
        /// <param name="indexCatalog">All of the schema's indexes, including those in an outdated storage format.</param>
        public static string? Suggest(string schemaName, string workingSchemaPrefix, ConditionCollection conditions, List<PhysicalIndex> indexCatalog)
        {
            var currentIndexes = indexCatalog.Where(o => o.IsCurrentStorageVersion()).ToList();
            var outdatedIndexes = indexCatalog.Where(o => !o.IsCurrentStorageVersion()).ToList();

            var suggestions = new List<Suggestion>();

            foreach (var group in conditions.FlattenToGroups())
            {
                var entries = group.Collection.OfType<ConditionEntry>()
                    .Where(o => o.Left.SchemaAlias.Is(workingSchemaPrefix)).ToList();

                if (entries.Count == 0)
                {
                    if (group.LogicalConnector == LogicalConnector.Or)
                    {
                        //An OR group that does not filter this schema means every document of the schema
                        //  can be part of the result, so no index on this schema can avoid reading them all.
                        return null;
                    }
                    continue;
                }

                //The fields that an index could seek on: equalities first, then at most one range, which can only be used last.
                var equalityFields = new List<string>();
                string? rangeField = null;

                //As in the optimizer: the left side must be a field of this schema and, for a schema that is not joined, the right side a constant.
                bool IsIndexable(ConditionEntry o)
                    => o.Left is QueryFieldDocumentIdentifier
                    && (!string.IsNullOrEmpty(workingSchemaPrefix) || IsConstant(o));

                foreach (var entry in entries.Where(IsIndexable))
                {
                    var fieldName = ((QueryFieldDocumentIdentifier)entry.Left).FieldName;

                    if (entry.Qualifier == LogicalQualifier.Equals)
                    {
                        if (!equalityFields.Any(o => o.Is(fieldName)))
                        {
                            equalityFields.Add(fieldName);
                        }
                    }
                    else if (rangeField == null && IsRangeQualifier(entry.Qualifier))
                    {
                        rangeField = fieldName;
                    }
                }

                if (rangeField != null && equalityFields.Any(o => o.Is(rangeField)))
                {
                    rangeField = null; //Already pinned by an equality.
                }

                if (equalityFields.Count == 0 && rangeField == null)
                {
                    continue; //Nothing in this group can use an index (e.g. only LIKE, != or expressions).
                }

                var proposedFields = equalityFields.Concat(rangeField == null ? [] : new[] { rangeField }).ToList();

                if (IsCovered(currentIndexes, equalityFields, rangeField))
                {
                    continue; //The existing indexes already serve this group as well as the suggested index would.
                }

                //An index in the old storage format that would serve this group just needs to be rebuilt.
                var outdated = outdatedIndexes
                    .Where(o => IsCovered([o], equalityFields, rangeField))
                    .OrderBy(o => o.Attributes.Count).FirstOrDefault();

                var reason = currentIndexes.Any(o => IsUsable(o, equalityFields, rangeField))
                    ? $"the existing indexes only partly cover [{string.Join("], [", proposedFields)}]"
                    : $"every document is read to evaluate [{string.Join("], [", proposedFields)}]";

                var existing = suggestions.FirstOrDefault(o => outdated != null
                    ? o.OutdatedIndex == outdated
                    : o.OutdatedIndex == null && o.Fields.SequenceEqual(proposedFields, StringComparer.InvariantCultureIgnoreCase));

                if (existing != null)
                {
                    if (!existing.Reasons.Contains(reason)) existing.Reasons.Add(reason);
                    continue;
                }

                var suggestion = new Suggestion { OutdatedIndex = outdated };
                suggestion.Fields.AddRange(proposedFields);
                suggestion.Reasons.Add(reason);
                suggestions.Add(suggestion);
            }

            if (suggestions.Count == 0)
            {
                return null;
            }

            var usedNames = new HashSet<string>(indexCatalog.Select(o => o.Name.EnsureNotNull()), StringComparer.InvariantCultureIgnoreCase);
            var leafName = schemaName.Split(':').Last();

            var result = new StringBuilder();
            result.AppendLine($"Suggested indexes for [{schemaName}]:");
            foreach (var suggestion in suggestions)
            {
                result.AppendLine($"  -- {string.Join("; ", suggestion.Reasons)}.");
                if (suggestion.OutdatedIndex != null)
                {
                    result.AppendLine($"  REBUILD INDEX {suggestion.OutdatedIndex.Name} ON {schemaName}");
                }
                else
                {
                    var name = UniqueName($"IX_{leafName}_{string.Join("_", suggestion.Fields)}", usedNames);
                    result.AppendLine($"  CREATE INDEX {name} ({string.Join(", ", suggestion.Fields)}) ON {schemaName}");
                }
            }

            return result.ToString();
        }

        /// <summary>
        /// The number of leading attributes of the index that are constrained by an equality.
        /// </summary>
        private static int EqualityDepth(PhysicalIndex index, List<string> equalityFields)
            => index.Attributes.TakeWhile(a => equalityFields.Any(f => f.Is(a.Field))).Count();

        /// <summary>
        /// Whether the optimizer can use the index at all: its first attribute must be constrained.
        /// </summary>
        private static bool IsUsable(PhysicalIndex index, List<string> equalityFields, string? rangeField)
            => index.Attributes.Count > 0
            && (EqualityDepth(index, equalityFields) > 0 || (rangeField != null && index.Attributes[0].Field.EnsureNotNull().Is(rangeField)));

        /// <summary>
        /// Whether the indexes serve the equality fields (and range field) as well as a suggested index would:
        ///   an index whose leading attributes are all of the equality fields, followed by the range field when there is one;
        ///   or a unique index that is entirely pinned by equalities (a single key read);
        ///   or, without a range, indexes whose leading equality fields together cover every equality field
        ///   (the optimizer seeks each of them and intersects the results).
        /// </summary>
        private static bool IsCovered(List<PhysicalIndex> indexes, List<string> equalityFields, string? rangeField)
        {
            var pinnedFields = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);

            foreach (var index in indexes)
            {
                var attributes = index.Attributes.Select(o => o.Field.EnsureNotNull()).ToList();
                int depth = EqualityDepth(index, equalityFields);

                if (depth == equalityFields.Count && depth < attributes.Count + (rangeField == null ? 1 : 0)
                    && (rangeField == null || attributes[depth].Is(rangeField)) && (depth > 0 || rangeField != null))
                {
                    return true;
                }

                if (depth > 0)
                {
                    if (index.IsUnique && depth == attributes.Count)
                    {
                        return true;
                    }
                    //The index is sought on its leading equality fields, and the lookups of several indexes are intersected.
                    pinnedFields.UnionWith(attributes.Take(depth));
                }
            }

            return rangeField == null && equalityFields.All(pinnedFields.Contains);
        }

        private static bool IsRangeQualifier(LogicalQualifier qualifier)
            => qualifier is LogicalQualifier.GreaterThan or LogicalQualifier.GreaterThanOrEqual
            or LogicalQualifier.LessThan or LogicalQualifier.LessThanOrEqual or LogicalQualifier.Between;

        private static bool IsConstant(ConditionEntry entry)
            => (entry.Right is QueryFieldCollapsedValue || StaticParserField.IsConstantExpression(entry.Right.Value))
            && (entry.RightHigh == null || entry.RightHigh is QueryFieldCollapsedValue || StaticParserField.IsConstantExpression(entry.RightHigh.Value));

        private static string UniqueName(string name, HashSet<string> usedNames)
        {
            var candidate = name;
            for (int i = 2; usedNames.Contains(candidate); i++)
            {
                candidate = $"{name}_{i}";
            }
            usedNames.Add(candidate);
            return candidate;
        }
    }
}
