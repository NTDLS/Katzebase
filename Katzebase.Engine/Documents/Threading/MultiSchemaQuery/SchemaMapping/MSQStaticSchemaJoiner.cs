using Katzebase.Engine.Query;
using Katzebase.Engine.Query.Condition;
using Katzebase.Engine.Trace;
using Katzebase.Engine.Transactions;
using Katzebase.PublicLibrary;
using Katzebase.PublicLibrary.Exceptions;
using Newtonsoft.Json.Linq;
using System.Text;
using static Katzebase.Engine.KbLib.EngineConstants;
using static Katzebase.Engine.Trace.PerformanceTrace;

namespace Katzebase.Engine.Documents.Threading.MultiSchemaQuery.SchemaMapping
{
    public static class MSQStaticSchemaMapper
    {
        /// <summary>
        /// Build a generic key/value dataset which is the combined fieldset from each inner joined document.
        /// </summary>
        public static MSQSchemaIntersection IntersetSchemas(Core core, PerformanceTrace? pt, Transaction transaction,
            QuerySchemaMap schemaMap, PreparedQuery query, ConditionLookupOptimization lookupOptimization)
        {
            var results = new MSQSchemaIntersection();
            //Here we should evaluate the join conditions (and probably the supplied actual conditions)
            //  to see if we can do some early document elimination. We should also evaluate the indexes
            //  for use on the join clause.

            var topLevel = schemaMap.First();
            var topLevelMap = topLevel.Value;

            Utility.EnsureNotNull(topLevelMap.SchemaMeta);
            Utility.EnsureNotNull(topLevelMap.SchemaMeta.DiskPath);

            foreach (var toplevelDocument in topLevelMap.DocuemntCatalog.Collection)
            {
                var persistDocumentDiskPathTopLevel = Path.Combine(topLevelMap.SchemaMeta.DiskPath, toplevelDocument.FileName);

                var persistDocumentTopLevel = core.IO.GetJson<PersistDocument>(pt, transaction, persistDocumentDiskPathTopLevel, LockOperation.Read);
                Utility.EnsureNotNull(persistDocumentTopLevel);
                Utility.EnsureNotNull(persistDocumentTopLevel.Content);

                var jContentTopLevel = JObject.Parse(persistDocumentTopLevel.Content);

                var jContentByAlias = new Dictionary<string, JObject>
                {
                    { topLevel.Key, jContentTopLevel } //Start with the docuemnt from the top level.
                };

                foreach (var nextLevel in schemaMap.Skip(1))
                {
                    var nextLevelMap = nextLevel.Value;

                    Utility.EnsureNotNull(nextLevelMap.SchemaMeta);
                    Utility.EnsureNotNull(nextLevelMap.SchemaMeta.DiskPath);

                    foreach (var nextLevelDocument in nextLevelMap.DocuemntCatalog.Collection)
                    {
                        var persistDocumentDiskPathNextLevel = Path.Combine(nextLevelMap.SchemaMeta.DiskPath, nextLevelDocument.FileName);

                        var persistDocumentNextLevel = core.IO.GetJson<PersistDocument>(pt, transaction, persistDocumentDiskPathNextLevel, LockOperation.Read);
                        Utility.EnsureNotNull(persistDocumentNextLevel);
                        Utility.EnsureNotNull(persistDocumentNextLevel.Content);

                        var jContentNextLevel = JObject.Parse(persistDocumentNextLevel.Content);

                        jContentByAlias.Add(nextLevel.Key, jContentNextLevel);

                        Utility.EnsureNotNull(nextLevelMap.Conditions);

                        var expression = new NCalc.Expression(nextLevelMap.Conditions.Root.Expression);

                        //If we have subsets, then we need to satisify those in order to complete the equation.
                        foreach (var subsetKey in nextLevelMap.Conditions.Root.SubsetKeys)
                        {
                            var subExpression = nextLevelMap.Conditions.SubsetByKey(subsetKey);

                            bool subExpressionResult = SatisifySubExpressionByJsonContentAlias(pt, lookupOptimization, jContentByAlias, subExpression);
                            expression.Parameters[subsetKey] = subExpressionResult;
                        }

                        var ptEvaluate = pt?.BeginTrace(PerformanceTraceType.Evaluate);
                        bool evaluation = (bool)expression.Evaluate();
                        ptEvaluate?.EndTrace();

                        if (evaluation)
                        {
                            if (results.SchemaRIDs.ContainsKey(topLevel.Key) == false)
                            {
                                results.SchemaRIDs.Add(topLevel.Key, new HashSet<Guid>());
                            }
                            results.SchemaRIDs[topLevel.Key].Add(toplevelDocument.Id);

                            if (results.SchemaRIDs.ContainsKey(nextLevel.Key) == false)
                            {
                                results.SchemaRIDs.Add(nextLevel.Key, new HashSet<Guid>());
                            }
                            results.SchemaRIDs[nextLevel.Key].Add(nextLevelDocument.Id);

                            results.Add(new MSQSchemaMapResult()); //TODO: add values.
                        }

                        jContentByAlias.Remove(nextLevel.Key);//We are no longer working with the document at this level.
                    }
                }
            }

            return results;
        }

        /// <summary>
        /// Mathematically collapses all subexpressions to return a boolean match.
        /// </summary>
        /// <param name="lookupOptimization"></param>
        /// <param name="jContent"></param>
        /// <param name="conditionSubset"></param>
        /// <returns></returns>
        /// <exception cref="KbParserException"></exception>
        private static bool SatisifySubExpressionByJsonContentAlias(PerformanceTrace? pt,
            ConditionLookupOptimization lookupOptimization, Dictionary<string, JObject> jContentByAlias, ConditionSubset conditionSubset)
        {
            var expression = new NCalc.Expression(conditionSubset.Expression);

            /*
            //TODO: What do we do here?
            //If we have subsets, then we need to satisify those in order to complete the equation.
            foreach (var subsetKey in conditionSubset.SubsetKeys)
            {
                var subExpression = lookupOptimization.Conditions.SubsetByKey(subsetKey);

                bool subExpressionResult = SatisifySubExpressionByJsonContentAlias(pt, lookupOptimization, jContentByAlias, subExpression);
                expression.Parameters[subsetKey] = subExpressionResult;
            }
            */

            var expressionString = new StringBuilder();

            foreach (var condition in conditionSubset.Conditions)
            {
                expressionString.Clear();

                Utility.EnsureNotNull(condition.Left.Value);
                Utility.EnsureNotNull(condition.Right.Value);

                var jContent = jContentByAlias[condition.Left.Prefix];

                //Get the value of the condition:
                if (!jContent.TryGetValue(condition.Left.Value, StringComparison.CurrentCultureIgnoreCase, out JToken? jLeftToken))
                {
                    throw new KbParserException($"Field not found in document [{condition.Left.Value}].");
                }

                jContent = jContentByAlias[condition.Right.Prefix];

                //Get the value of the condition:
                if (!jContent.TryGetValue(condition.Right.Value, StringComparison.CurrentCultureIgnoreCase, out JToken? jRightToken))
                {
                    throw new KbParserException($"Field not found in document [{condition.Right.Value}].");
                }

                /*
                if (expressionString.Length > 0)
                {
                    expressionString.Append(ConditionTokenizer.LogicalConnectorToOperator(condition.LogicalConnector));
                }
                var singleConditionResult =  Condition.IsMatch(jLeftToken.ToString().ToLower(), condition.LogicalQualifier, jRightToken.ToString());
                expressionString.Append(singleConditionResult ? "1==1" : "1==0");
                */

                var singleConditionResult = Condition.IsMatch(jLeftToken.ToString().ToLower(), condition.LogicalQualifier, jRightToken.ToString());

                expression.Parameters[condition.ConditionKey] = singleConditionResult;
            }

            var ptEvaluate = pt?.BeginTrace(PerformanceTraceType.Evaluate);
            var evaluation = (bool)expression.Evaluate();
            ptEvaluate?.EndTrace();

            return evaluation;
        }

    }
}
