using NTDLS.Katzebase.Parsers.Query.Fields.Expressions;
using NTDLS.Katzebase.Parsers.Query.SupportingTypes;
using System.Text;
using static NTDLS.Katzebase.Parsers.Constants;
using NTDLS.Katzebase.Parsers.Interfaces;
namespace NTDLS.Katzebase.Parsers.Query.WhereAndJoinConditions
{
    /// <summary>
    /// Contains the collection of ConditionSets, each group contains AND expressions (NO OR expressions) as there
    ///     is a seperate ConditionGroup for each OR expression and for each expression contained in parentheses.
    /// </summary>
    public class ConditionCollection<TData> : ConditionGroup<TData> where TData : IStringable
    {
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

        public QueryFieldCollection<TData> FieldCollection { get; set; }

        private int _nextExpressionVariable = 0;
        public string NextExpressionVariable()
            => $"v{_nextExpressionVariable++}";

        public ConditionCollection(QueryBatch<TData> queryBatch, string mathematicalExpression, string? schemaAlias = null)
            : base(LogicalConnector.None)
        {
            FieldCollection = new(queryBatch);
            MathematicalExpression = mathematicalExpression;
            SchemaAlias = schemaAlias;
        }

        public ConditionCollection(QueryBatch<TData> queryBatch)
            : base(LogicalConnector.None)
        {
            FieldCollection = new(queryBatch);
        }

        public new ConditionCollection<TData> Clone()
        {
            var clone = new ConditionCollection<TData>(FieldCollection.QueryBatch)
            {
                Connector = this.Connector,
                SchemaAlias = this.SchemaAlias,
                FieldCollection = this.FieldCollection,
                MathematicalExpression = this.MathematicalExpression,
                Hash = this.Hash,
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
            result.AppendLine($"• " + $"Hash: {Hash}");
            result.AppendLine("•••••••••••••••••••••••••••••••••••••••••••••••••••••••••••••••••••••••••••••••••••••••••••••••••••••••••••••••••");

            foreach (var item in Collection)
            {
                if (item is ConditionGroup<TData> group)
                {
                    result.AppendLine("• " + Pad(0) + $"{(group.Connector != LogicalConnector.None ? $"{group.Connector} " : string.Empty)}(");

                    ExplainOperationsRecursive(group, result);

                    result.AppendLine("• " + Pad(0) + ")");
                }
                else if (item is ConditionEntry<TData> entry)
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

        private void ExplainOperationsRecursive(ConditionGroup<TData> givenGroup, StringBuilder result, int depth = 0)
        {
            foreach (var item in givenGroup.Collection)
            {
                if (item is ConditionGroup<TData> group)
                {
                    result.AppendLine("• " + Pad(1 + depth) + $"{(group.Connector != LogicalConnector.None ? $"{group.Connector} " : string.Empty)}(");

                    ExplainOperationsRecursive(group, result, depth + 1);

                    result.AppendLine("• " + Pad(1 + depth) + ")");
                }
                else if (item is ConditionEntry<TData> entry)
                {
                    string left;
                    if (entry.Left is QueryFieldExpressionNumeric<TData>)
                    {
                        left = "(Numeric Expression)";
                    }
                    else if (entry.Left is QueryFieldExpressionString<TData>)
                    {
                        left = "(String Expression)";
                    }
                    else
                    {
                        left = $"{FieldCollection.QueryBatch.GetLiteralValue(entry.Left.Value.ToT<string>())}";
                    }

                    string right;
                    if (entry.Right is QueryFieldExpressionNumeric<TData>)
                    {
                        right = "(Numeric Expression)";
                    }
                    else if (entry.Right is QueryFieldExpressionString<TData>)
                    {
                        right = "(String Expression)";
                    }
                    else
                    {
                        right = $"{FieldCollection.QueryBatch.GetLiteralValue(entry.Right.Value.ToT<string>())}";
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
