using NTDLS.Katzebase.Parsers.Fields;
using NTDLS.Katzebase.Parsers.Fields.Expressions;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using static NTDLS.Katzebase.Parsers.Constants;

namespace NTDLS.Katzebase.Parsers.Conditions
{
    /// <summary>
    /// Contains the collection of ConditionSets, each group contains AND expressions (NO OR expressions) as there
    ///     is a seperate ConditionGroup for each OR expression and for each expression contained in parentheses.
    /// </summary>
    public class ConditionCollection
        : ConditionGroup
    {
        /// <summary>
        /// The SHA256 hash of the mathematical expression, used for caching purposes.
        /// The hash also includes the query hash, so it can not be calculated until the query itself is fully parsed and hashed.
        /// </summary>
        internal IncrementalHash? IncrementalSha256 { get; set; }

        /// <summary>
        /// For conditions on joins, this is the alias of the schema that these conditions are for.
        /// </summary>
        public string? SchemaAlias { get; set; }

        /// <summary>
        /// After parsing, this will contain a mathematical expression, containing
        ///     variables, that can be evaluated to determine a document match.
        /// </summary>
        public string MathematicalExpression { get; set; } = string.Empty;

        /// <summary>
        /// Hash of the MathematicalExpression.
        /// </summary>
        public string? Hash { get; set; }

        /// <summary>
        /// Indicates whether this ConditionCollection is a clone of another collection.
        /// </summary>
        public bool IsClone { get; private set; }

        public QueryFieldCollection FieldCollection { get; set; }

        private int _nextExpressionVariable = 0;
        public string NextExpressionVariable()
            => $"v{_nextExpressionVariable++}";

        public ConditionCollection(PreparedQueryBatch queryBatch, string mathematicalExpression, string? schemaAlias = null)
            : base(LogicalConnector.None)
        {
            FieldCollection = new(queryBatch);
            MathematicalExpression = mathematicalExpression;
            SchemaAlias = schemaAlias;
        }

        public ConditionCollection(PreparedQueryBatch queryBatch)
            : base(LogicalConnector.None)
        {
            FieldCollection = new(queryBatch);
        }

        public new ConditionCollection Clone()
        {
            var clone = new ConditionCollection(FieldCollection.QueryBatch)
            {
                LogicalConnector = LogicalConnector,
                SchemaAlias = SchemaAlias,
                FieldCollection = FieldCollection,
                MathematicalExpression = MathematicalExpression,
                Hash = Hash,
                IsClone = true
            };

            foreach (var entry in Collection)
            {
                clone.Collection.Add(entry.Clone());
            }

            foreach (var usableIndex in UsableIndexes)
            {
                clone.UsableIndexes.Add(usableIndex.Clone());
            }

            return clone;
        }

        #region Explain Operations.

        private static string Pad(int indentation) => "".PadLeft(indentation * 2, ' ');

        public string ExplainOperations()
        {
            var result = new StringBuilder();

            result.AppendLine("<BEGIN>••••••••••••••••••••••••••••••••••••••••••••••••••••••••••••••••••••••••••••••••••••••••••••••••••••••••••");
            if (!string.IsNullOrEmpty(SchemaAlias)) result.AppendLine($"• " + $"Schema: {SchemaAlias}");
            result.AppendLine($"• " + $"Expression: {MathematicalExpression}");
            result.AppendLine($"• " + $"Hash: {Hash ?? string.Empty}");
            result.AppendLine("•••••••••••••••••••••••••••••••••••••••••••••••••••••••••••••••••••••••••••••••••••••••••••••••••••••••••••••••••");

            foreach (var item in Collection)
            {
                if (item is ConditionGroup group)
                {
                    result.AppendLine("• " + Pad(0) + $"{(group.LogicalConnector != LogicalConnector.None ? $"{group.LogicalConnector} " : string.Empty)}(");

                    ExplainOperationsRecursive(group, result);

                    result.AppendLine("• " + Pad(0) + ")");
                }
                else if (item is ConditionEntry entry)
                {
                    throw new NotImplementedException("Condition entries are not supported at the root level.");
                }
                else
                {
                    throw new NotImplementedException();
                }
            }

            result.AppendLine("<END>••••••••••••••••••••••••••••••••••••••••••••••••••••••••••••••••••••••••••••••••••••••••••••••••••••••••••••");

            return result.ToString();
        }

        private void ExplainOperationsRecursive(ConditionGroup givenGroup, StringBuilder result, int depth = 0)
        {
            foreach (var item in givenGroup.Collection)
            {
                if (item is ConditionGroup group)
                {
                    result.AppendLine("• " + Pad(1 + depth) + $"{(group.LogicalConnector != LogicalConnector.None ? $"{group.LogicalConnector} " : string.Empty)}(");

                    ExplainOperationsRecursive(group, result, depth + 1);

                    result.AppendLine("• " + Pad(1 + depth) + ")");
                }
                else if (item is ConditionEntry entry)
                {
                    string left;
                    if (entry.Left is QueryFieldExpressionNumeric)
                    {
                        left = "(Numeric Expression)";
                    }
                    else if (entry.Left is QueryFieldExpressionString)
                    {
                        left = "(String Expression)";
                    }
                    else
                    {
                        left = $"{FieldCollection.QueryBatch.Variables.Resolve(entry.Left.Value)}";
                    }

                    string right;
                    if (entry.Right is QueryFieldExpressionNumeric)
                    {
                        right = "(Numeric Expression)";
                    }
                    else if (entry.Right is QueryFieldExpressionString)
                    {
                        right = "(String Expression)";
                    }
                    else
                    {
                        right = $"{FieldCollection.QueryBatch.Variables.Resolve(entry.Right.Value)}";
                    }

                    result.AppendLine("• " + Pad(1 + depth) + $"[{left}] {entry.Qualifier} [{right}].");
                }
                else
                {
                    throw new NotImplementedException();
                }
            }

        }

        #endregion
    }
}
