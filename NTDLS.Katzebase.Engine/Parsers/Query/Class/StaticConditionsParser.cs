using NTDLS.Katzebase.Engine.Parsers.Query.SupportingTypes;
using NTDLS.Katzebase.Engine.Parsers.Query.WhereAndJoinConditions;
using NTDLS.Katzebase.Engine.Parsers.Tokens;
using static NTDLS.Katzebase.Engine.Library.EngineConstants;

namespace NTDLS.Katzebase.Engine.Parsers.Query.Class
{
    internal static class StaticConditionsParser
    {
        /// <summary>
        /// Contains the collection of ConditionGroup, each group is comprised of AND expressions,
        /// and there is a seperate ConditionGroup for each OR expression.
        /// </summary>
        public class ConditionCollection : List<ConditionGroup>
        {
            /// <summary>
            /// For conditions on joins, this is the alias of the schema that these conditions are for.
            /// </summary>
            public string? SchemaAlias { get; set; }

            public ConditionCollection(string? schemaAlias = null)
            {
                this.SchemaAlias = schemaAlias;
            }
        }

        public class ConditionGroup : List<Condition>
        {
        }

        public class Condition
        {
            public string? LeftValue { get; set; }
            public LogicalQualifier Qualifier { get; set; }
            public string? RightValue { get; set; }

            public Condition(string? leftValue, LogicalQualifier qualifier, string? rightValue)
            {
                LeftValue = leftValue;
                Qualifier = qualifier;
                RightValue = rightValue;
            }
        }

        /// <summary>
        /// Parse the conditions, we are going to build a full expression with all of the condition values replaced with tokens so we
        /// can build a mathematical expression, but we are also going to build sets of groups for which there will be one per OR expression,
        /// and all conditions in a ConditionGroup will be comprised solely of AND conditions. This way we can use the groups to match indexes
        /// before evaluating the whole expression on the limited set of documents we derived from the indexing operations.
        /// </summary>
        public static void Parse(QueryBatch queryBatch, Tokenizer parentTokenizer, string conditionsText, string leftHandAliasOfJoin = "")
        {
            var tokenizer = new Tokenizer(conditionsText);

            var conditionCollection = new ConditionCollection(leftHandAliasOfJoin);

            if (conditionsText.Contains("LanguageId"))
            {
            }

            ParseRecursive(queryBatch, parentTokenizer, conditionCollection, conditionsText);

            if (conditionsText.Contains("LanguageId"))
            {
            }
        }

        private static void ParseRecursive(QueryBatch queryBatch, Tokenizer parentTokenizer,
            ConditionCollection conditionCollection, string conditionsText)
        {
            var tokenizer = new Tokenizer(conditionsText);

            var conditionGroup = new ConditionGroup();
            conditionCollection.Add(conditionGroup);

            while (!tokenizer.IsExhausted())
            {
                if (tokenizer.TryIsNextCharacter('('))
                {
                    string subConditionsText = tokenizer.EatMatchingScope();
                    ParseRecursive(queryBatch, parentTokenizer, conditionCollection, subConditionsText);
                }

                string leftToken = tokenizer.EatGetNext();
                var logicalQualifier = StaticConditionHelpers.ParseLogicalQualifier(tokenizer.EatGetNext());
                string rightToken = tokenizer.EatGetNext();

                conditionGroup.Add(new Condition(leftToken, logicalQualifier, rightToken));

                if (!tokenizer.IsExhausted())
                {
                    var logicalConnector = tokenizer.EatIsNextEnumToken<LogicalConnector>();

                    if (logicalConnector == LogicalConnector.Or)
                    {
                        conditionCollection.Add(conditionGroup);
                        conditionGroup = new ConditionGroup();
                    }
                }
            }
        }
    }
}
