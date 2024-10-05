using NTDLS.Katzebase.Client.Types;
using NTDLS.Katzebase.Parsers.Query.Exposed;
using NTDLS.Katzebase.Parsers.Query.Fields;
using NTDLS.Katzebase.Parsers.Query.Fields.Expressions;
using NTDLS.Katzebase.Parsers.Query.SupportingTypes;

namespace NTDLS.Katzebase.Parsers.Query
{
    /// <summary>
    /// Collection of query fields, which contains their names and values.
    /// </summary>
    public class QueryFieldCollection : List<QueryField>
    {
        public QueryBatch QueryBatch { get; private set; }

        /// <summary>
        /// A list of all distinct document identifiers from all fields, even nested expressions.
        /// We go out of our way to create this list because it helps optimize the query execution.
        /// </summary>
        public KbInsensitiveDictionary<QueryFieldDocumentIdentifier> DocumentIdentifiers { get; set; } = new();

        /// <summary>
        /// Gets a field alias for a field for which the query did not supply an alias.
        /// </summary>
        /// <returns></returns>
        public string GetNextFieldAlias()
            => $"Expression{nextFieldAlias++}";
        private int nextFieldAlias = 0;

        public string GetNextExpressionKey()
            => $"$x_{_nextExpressionKey++}$";
        private int _nextExpressionKey = 0;

        /// <summary>
        /// Get a document field placeholder.
        /// </summary>
        /// <returns></returns>
        public string GetNextDocumentFieldKey()
            => $"$f_{_nextDocumentFieldKey++}$";
        private int _nextDocumentFieldKey = 0;

        public QueryFieldCollection(QueryBatch queryBatch)
        {
            QueryBatch = queryBatch;
        }

        #region Collection: FieldsWithAggregateFunctionCalls.

        private List<QueryField>? _exposedAggregateFunctions = null;
        private readonly object _exposedAggregateFunctionsLock = new();

        /// <summary>
        /// Returns a list of fields that have aggregate function call dependencies.
        /// </summary>
        public List<QueryField> FieldsWithAggregateFunctionCalls
        {
            get
            {
                if (_exposedAggregateFunctions == null)
                {
                    lock (_exposedAggregateFunctionsLock)
                    {
                        if (_exposedAggregateFunctions != null)
                        {
                            //We check again here because other threads may have started waiting on the lock
                            //  with the intention of hydrating _exposedAggregateFunctions themselves, we do this because
                            //  we don't want to lock on reads once this _exposedAggregateFunctions is hydrated.
                            return _exposedAggregateFunctions;
                        }

                        var results = new List<QueryField>();

                        foreach (var queryField in this)
                        {
                            if (queryField.Expression is IQueryFieldExpression fieldExpression)
                            {
                                if (fieldExpression.FunctionDependencies.OfType<QueryFieldExpressionFunctionAggregate>().Any())
                                {
                                    results.Add(queryField);
                                }
                            }
                        }

                        _exposedAggregateFunctions = results;
                    }
                }

                return _exposedAggregateFunctions;
            }
        }

        #endregion

        #region Collection: AggregationFunctions.

        private List<ExposedAggregateFunction>? _aggregationFunctions = null;
        private readonly object _aggregationFunctionsLock = new();

        /// <summary>
        /// Returns a list of fields that have aggregate function call dependencies.
        /// </summary>
        public List<ExposedAggregateFunction> AggregationFunctions
        {
            get
            {
                if (_aggregationFunctions == null)
                {
                    lock (_aggregationFunctionsLock)
                    {
                        if (_aggregationFunctions != null)
                        {
                            //We check again here because other threads may have started waiting on the lock
                            //  with the intention of hydrating _aggregationFunctions themselves, we do this because
                            //  we don't want to lock on reads once this _aggregationFunctions is hydrated.
                            return _aggregationFunctions;
                        }

                        var results = new List<ExposedAggregateFunction>();

                        foreach (var queryField in this)
                        {
                            if (queryField.Expression is IQueryFieldExpression fieldExpression)
                            {
                                foreach (var function in fieldExpression.FunctionDependencies.OfType<QueryFieldExpressionFunctionAggregate>())
                                {
                                    results.Add(new ExposedAggregateFunction(function, fieldExpression.FunctionDependencies));
                                }
                            }
                        }

                        _aggregationFunctions = results;
                    }
                }

                return _aggregationFunctions;
            }
        }

        #endregion
    }
}
