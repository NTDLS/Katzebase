using Katzebase.Engine.Documents;
using Katzebase.Engine.Query.Constraints;
using Katzebase.Engine.Query.Searchers.MultiSchema.Mapping;
using Katzebase.Engine.Schemas;
using Katzebase.Engine.Trace;
using Katzebase.Engine.Transactions;
using Katzebase.PrivateLibrary;
using Katzebase.PublicLibrary;
using Katzebase.PublicLibrary.Exceptions;
using Newtonsoft.Json.Linq;
using System.Text;
using static Katzebase.Engine.KbLib.EngineConstants;
using static Katzebase.Engine.Trace.PerformanceTrace;

namespace Katzebase.Engine.Query.Searchers.MultiSchema.Intersection
{
    internal static class MSQStaticSchemaJoiner
    {
        /// <summary>
        /// Build a generic key/value dataset which is the combined fieldset from each inner joined document.
        /// </summary>
        public static MSQSchemaIntersection IntersetSchemas(Core core, PerformanceTrace? pt, Transaction transaction,
            MSQQuerySchemaMap schemaMap, PreparedQuery query, ConditionLookupOptimization lookupOptimization)
        {
            //Here we should evaluate the join conditions (and probably the supplied actual conditions)
            //  to see if we can do some early document elimination. We should also evaluate the indexes
            //  for use on the join clause.

            var topLevel = schemaMap.First();
            var topLevelMap = topLevel.Value;

            Utility.EnsureNotNull(topLevelMap.SchemaMeta);
            Utility.EnsureNotNull(topLevelMap.SchemaMeta.DiskPath);

            //TODO: We should put some intelligence behind the thread count and queue size.
            int threadCount = Environment.ProcessorCount * 4 > 32 ? 32 : Environment.ProcessorCount * 4;

            var ptThreadCreation = pt?.BeginTrace(PerformanceTraceType.ThreadCreation);
            var param = new LookupThreadParam(core, pt, transaction, schemaMap, query, lookupOptimization);
            var threadPool = ThreadPoolQueue<PersistDocumentCatalogItem, LookupThreadParam>.CreateAndStart(LookupThreadProc, param, threadCount);
            ptThreadCreation?.EndTrace();

            foreach (var toplevelDocument in topLevelMap.DocuemntCatalog.Collection)
            {
                if (threadPool.HasException || threadPool.ContinueToProcessQueue == false)
                {
                    break;
                }

                threadPool.EnqueueWorkItem(toplevelDocument);
            }

            var ptThreadCompletion = pt?.BeginTrace(PerformanceTraceType.ThreadCompletion);
            threadPool.WaitForCompletion();
            ptThreadCompletion?.EndTrace();

            return param.Results;
        }

        private class LookupThreadParam
        {
            public MSQSchemaIntersection Results = new();
            public MSQQuerySchemaMap SchemaMap { get; private set; }
            public Core Core { get; private set; }
            public PerformanceTrace? PT { get; private set; }
            public Transaction Transaction { get; private set; }
            public PreparedQuery Query { get; private set; }
            public ConditionLookupOptimization? LookupOptimization { get; private set; }

            public LookupThreadParam(Core core, PerformanceTrace? pt, Transaction transaction,
                MSQQuerySchemaMap schemaMap, PreparedQuery query, ConditionLookupOptimization? lookupOptimization)
            {
                this.Core = core;
                this.PT = pt;
                this.Transaction = transaction;
                this.SchemaMap = schemaMap;
                this.Query = query;
                this.LookupOptimization = lookupOptimization;
            }
        }

        private static void LookupThreadProc(ThreadPoolQueue<PersistDocumentCatalogItem, LookupThreadParam> pool, LookupThreadParam? param)
        {
            Utility.EnsureNotNull(param);

            var topLevel = param.SchemaMap.First();
            var topLevelMap = topLevel.Value;

            Utility.EnsureNotNull(topLevelMap.SchemaMeta);
            Utility.EnsureNotNull(topLevelMap.SchemaMeta.DiskPath);
            Utility.EnsureNotNull(param.LookupOptimization);

            /*
            NCalc.Expression? expression = null;

            if (param.LookupOptimization != null)
            {
                expression = new NCalc.Expression(param.LookupOptimization.Conditions.HighLevelExpressionTree);
            }
            */

            while (pool.ContinueToProcessQueue)
            {
                var toplevelDocument = pool.DequeueWorkItem();
                if (toplevelDocument == null)
                {
                    continue;
                }

                var persistDocumentDiskPathTopLevel = Path.Combine(topLevelMap.SchemaMeta.DiskPath, toplevelDocument.FileName);

                var persistDocumentTopLevel = param.Core.IO.GetJson<PersistDocument>(param.PT, param.Transaction, persistDocumentDiskPathTopLevel, LockOperation.Read);
                Utility.EnsureNotNull(persistDocumentTopLevel);
                Utility.EnsureNotNull(persistDocumentTopLevel.Content);

                var jContentTopLevel = JObject.Parse(persistDocumentTopLevel.Content);

                var jContentByAlias = new Dictionary<string, JObject>
                {
                    { topLevel.Key, jContentTopLevel } //Start with the docuemnt from the top level.
                };

                foreach (var nextLevel in param.SchemaMap.Skip(1))
                {
                    var nextLevelMap = nextLevel.Value;

                    Utility.EnsureNotNull(nextLevelMap.SchemaMeta);
                    Utility.EnsureNotNull(nextLevelMap.SchemaMeta.DiskPath);

                    foreach (var nextLevelDocument in nextLevelMap.DocuemntCatalog.Collection)
                    {
                        var persistDocumentDiskPathNextLevel = Path.Combine(nextLevelMap.SchemaMeta.DiskPath, nextLevelDocument.FileName);

                        var persistDocumentNextLevel = param.Core.IO.GetJson<PersistDocument>(param.PT, param.Transaction, persistDocumentDiskPathNextLevel, LockOperation.Read);
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

                            bool subExpressionResult = SatisifySubExpressionByJsonContentAlias(param.PT, param.LookupOptimization, jContentByAlias, subExpression);
                            expression.Parameters[subsetKey] = subExpressionResult;
                        }

                        var ptEvaluate = param.PT?.BeginTrace(PerformanceTraceType.Evaluate);
                        bool evaluation = (bool)expression.Evaluate();
                        ptEvaluate?.EndTrace();

                        if (evaluation)
                        {
                            lock (param.Results)
                            {
                                if (param.Results.SchemaRIDs.ContainsKey(topLevel.Key) == false)
                                {
                                    param.Results.SchemaRIDs.Add(topLevel.Key, new HashSet<Guid>());
                                }
                                param.Results.SchemaRIDs[topLevel.Key].Add(toplevelDocument.Id);

                                if (param.Results.SchemaRIDs.ContainsKey(nextLevel.Key) == false)
                                {
                                    param.Results.SchemaRIDs.Add(nextLevel.Key, new HashSet<Guid>());
                                }
                                param.Results.SchemaRIDs[nextLevel.Key].Add(nextLevelDocument.Id);

                                param.Results.Add(new MSQSchemaIntersectionItem()); //TODO: add values.
                            }
                        }

                        jContentByAlias.Remove(nextLevel.Key);//We are no longer working with the document at this level.
                    }
                }
            }
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
