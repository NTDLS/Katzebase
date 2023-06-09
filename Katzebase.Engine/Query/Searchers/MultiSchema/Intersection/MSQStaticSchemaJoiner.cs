using Katzebase.Engine.Documents;
using Katzebase.Engine.Indexes;
using Katzebase.Engine.Query.Constraints;
using Katzebase.Engine.Query.Searchers.MultiSchema.Mapping;
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
                Core = core;
                PT = pt;
                Transaction = transaction;
                SchemaMap = schemaMap;
                Query = query;
                LookupOptimization = lookupOptimization;
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

                    Utility.EnsureNotNull(nextLevelMap.Conditions);
                    Utility.EnsureNotNull(nextLevelMap.SchemaMeta);
                    Utility.EnsureNotNull(nextLevelMap.SchemaMeta.DiskPath);

                    var expression = new NCalc.Expression(nextLevelMap.Conditions.HighLevelExpressionTree);

                    #region New indexing stuff..

                    //Create a reference to the entire document catalog.
                    var limitedDocumentCatalogItems = nextLevelMap.DocuemntCatalog.Collection;

                    if (nextLevelMap.Optimization?.CanApplyIndexing() == true)
                    {
                        //We are going to create a limited document catalog from the indexes. So kill the reference and create an empty list.
                        limitedDocumentCatalogItems = new List<PersistDocumentCatalogItem>();

                        //All condition subsets have a selected index. Start building a list of possible document IDs.
                        foreach (var subset in nextLevelMap.Optimization.Conditions.NonRootSubsets)
                        {
                            Utility.EnsureNotNull(subset.IndexSelection);
                            Utility.EnsureNotNull(subset.IndexSelection.Index.DiskPath);

                            var indexPageCatalog = param.Core.IO.GetPBuf<PersistIndexPageCatalog>(param.PT, param.Transaction, subset.IndexSelection.Index.DiskPath, LockOperation.Read);
                            Utility.EnsureNotNull(indexPageCatalog);

                            var keyValuePairs = new Dictionary<string, AmbidextrousConditionValue>();

                            foreach (var condition in subset.Conditions)
                            {
                                JToken? conditionToken = null;

                                //Figure out whether the top level schema is being referenced by the left or right of the condition.
                                if (condition?.Left?.Prefix == topLevel.Key)
                                {
                                    if (!jContentTopLevel.TryGetValue((condition.Left?.Value ?? ""), StringComparison.CurrentCultureIgnoreCase, out conditionToken))
                                    {
                                        throw new KbParserException($"Join clause field not found in document [{topLevel.Key}].");
                                    }
                                    keyValuePairs.Add(condition.Left?.Value ?? "", new AmbidextrousConditionValue(AmbidextrousConditionValue.Side.Left, conditionToken?.ToString() ?? ""));
                                }
                                else if (condition?.Right?.Prefix == topLevel.Key)
                                {
                                    if (!jContentTopLevel.TryGetValue((condition.Right?.Value ?? ""), StringComparison.CurrentCultureIgnoreCase, out conditionToken))
                                    {
                                        throw new KbParserException($"Join clause field not found in document [{topLevel.Key}].");
                                    }
                                    keyValuePairs.Add(condition.Right?.Value ?? "", new AmbidextrousConditionValue(AmbidextrousConditionValue.Side.Right, conditionToken?.ToString() ?? ""));
                                }
                                else
                                {
                                    throw new KbParserException($"Schema prefix could not be found for join clause [{topLevel.Key}].");
                                }
                            }

                            //Match on values from the document.
                            var documentIds = param.Core.Indexes.MatchDocuments(param.PT, indexPageCatalog, subset.IndexSelection, subset, keyValuePairs);

                            limitedDocumentCatalogItems.AddRange(nextLevelMap.DocuemntCatalog.Collection.Where(o => documentIds.Contains(o.Id)).ToList());
                        }
                    }
                    else
                    {
                        #region Why no indexing? Find out here!
                        //   * One or more of the conditon subsets lacks an index.
                        //   *
                        //   *   Since indexing requires that we can ensure document elimination we will have
                        //   *      to ensure that we have a covering index on EACH-and-EVERY conditon group.
                        //   *
                        //   *   Then we can search the indexes for each condition group to obtain a list of all possible
                        //   *       document IDs, then use those document IDs to early eliminate documents from the main lookup loop.
                        //   *
                        //   *   If any one conditon group does not have an index, then no indexing will be used at all since all
                        //   *      documents will need to be scaned anyway. To prevent unindexed scans, reduce the number of
                        //   *      condition groups (nested in parentheses).
                        //   *
                        //   * ConditionLookupOptimization:BuildFullVirtualExpression() Will tell you why we cant use an index.
                        //   * var explanationOfIndexability = lookupOptimization.BuildFullVirtualExpression();
                        //*
                        #endregion
                    }

                    #endregion

                    foreach (var nextLevelDocument in limitedDocumentCatalogItems)
                    {
                        var persistDocumentDiskPathNextLevel = Path.Combine(nextLevelMap.SchemaMeta.DiskPath, nextLevelDocument.FileName);

                        var persistDocumentNextLevel = param.Core.IO.GetJson<PersistDocument>(param.PT, param.Transaction, persistDocumentDiskPathNextLevel, LockOperation.Read);
                        Utility.EnsureNotNull(persistDocumentNextLevel);
                        Utility.EnsureNotNull(persistDocumentNextLevel.Content);

                        var jContentNextLevel = JObject.Parse(persistDocumentNextLevel.Content);

                        jContentByAlias.Add(nextLevel.Key, jContentNextLevel);

                        SetExpressionParameters(ref expression, nextLevelMap.Conditions, jContentByAlias);

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
        /// Gets the json content values for the specified conditions.
        /// </summary>
        /// <param name="expression"></param>
        /// <param name="conditions"></param>
        /// <param name="jContent"></param>
        private static void SetExpressionParameters(ref NCalc.Expression expression, Conditions conditions, Dictionary<string, JObject> jContentByAlias)
        {
            //If we have subsets, then we need to satisify those in order to complete the equation.
            foreach (var subsetKey in conditions.Root.SubsetKeys)
            {
                var subExpression = conditions.SubsetByKey(subsetKey);
                SetExpressionParametersRecursive(ref expression, conditions, subExpression, jContentByAlias);
            }
        }

        private static void SetExpressionParametersRecursive(ref NCalc.Expression expression, Conditions conditions, ConditionSubset conditionSubset, Dictionary<string, JObject> jContentByAlias)
        {
            //If we have subsets, then we need to satisify those in order to complete the equation.
            foreach (var subsetKey in conditionSubset.SubsetKeys)
            {
                var subExpression = conditions.SubsetByKey(subsetKey);
                SetExpressionParametersRecursive(ref expression, conditions, subExpression, jContentByAlias);
            }

            foreach (var condition in conditionSubset.Conditions)
            {
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

                var singleConditionResult = Condition.IsMatch(jLeftToken.ToString().ToLower(), condition.LogicalQualifier, jRightToken.ToString());

                expression.Parameters[condition.ConditionKey] = singleConditionResult;
            }
        }
    }
}
