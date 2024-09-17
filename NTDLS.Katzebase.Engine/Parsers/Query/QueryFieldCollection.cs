using NTDLS.Katzebase.Client.Types;
using NTDLS.Katzebase.Engine.Parsers.Query.Exposed;
using NTDLS.Katzebase.Engine.Parsers.Query.Fields;
using NTDLS.Katzebase.Engine.Parsers.Query.Fields.Expressions;
using NTDLS.Katzebase.Engine.Parsers.Query.SupportingTypes;
using static NTDLS.Katzebase.Client.KbConstants;
using static NTDLS.Katzebase.Engine.Parsers.Query.Fields.Expressions.ExpressionConstants;

namespace NTDLS.Katzebase.Engine.Parsers.Query
{
    /// <summary>
    /// Collection of query fields, which contains their names and values.
    /// </summary>
    internal class QueryFieldCollection : List<QueryField>
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

        #region Exposed collection: FieldsWithScalerFunctionCalls.

        private List<ExposedFunction>? _exposedScalerFunctions = null;
        private readonly object _exposedScalerFunctionsLock = new();

        public void InvalidateFieldsWithScalerFunctionCallsCache()
        {
            lock (_exposedScalerFunctionsLock)
            {
                _exposedScalerFunctions = null;
            }
        }

        /// <summary>
        /// Returns a list of fields that have function call dependencies.
        /// </summary>
        public List<ExposedFunction> FieldsWithScalerFunctionCalls
        {
            get
            {
                if (_exposedScalerFunctions == null)
                {
                    lock (_exposedScalerFunctionsLock)
                    {
                        if (_exposedScalerFunctions != null)
                        {
                            //We check again here because other threads may have started waiting on the lock
                            //  with the intention of hydrating _exposedScalerFunctions themselves, we do this because
                            //  we don't want to lock on reads once this _exposedScalerFunctions is hydrated.
                            return _exposedScalerFunctions;
                        }

                        var results = new List<ExposedFunction>();

                        foreach (var queryField in this)
                        {
                            if (queryField.Expression is IQueryFieldExpression fieldExpression)
                            {
                                if (fieldExpression.FunctionDependencies.OfType<QueryFieldExpressionFunctionScaler>().Any())
                                {
                                    results.Add(new ExposedFunction(queryField.Ordinal, queryField.Alias, fieldExpression));
                                }
                            }
                        }

                        _exposedScalerFunctions = results;
                    }
                }

                return _exposedScalerFunctions;
            }
        }

        #endregion

        #region Collection: FieldsWithAggregateFunctionCalls.

        private List<QueryField>? _exposedAggregateFunctions = null;
        private readonly object _exposedAggregateFunctionsLock = new();

        public void InvalidateFieldsWithAggregateFunctionCallsCache()
        {
            lock (_exposedAggregateFunctionsLock)
            {
                _exposedAggregateFunctions = null;
            }
        }

        /// <summary>
        /// Returns a list of fields that have function call dependencies.
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

        #region Exposed collection: ConstantFields.

        private List<ExposedConstant>? _exposedConstants = null;
        private readonly object _exposedConstantsLock = new();

        public void InvalidateConstantFieldsCache()
        {
            lock (_exposedConstantsLock)
            {
                _exposedConstants = null;
            }
        }

        /// <summary>
        /// Returns a list of fields that have function call dependencies.
        /// </summary>
        public List<ExposedConstant> ConstantFields
        {
            get
            {
                if (_exposedConstants == null)
                {
                    lock (_exposedConstantsLock)
                    {
                        if (_exposedConstants != null)
                        {
                            //We check again here because other threads may have started waiting on the lock
                            //  with the intention of hydrating _exposedConstants themselves, we do this because
                            //  we don't want to lock on reads once this _exposedConstants is hydrated.
                            return _exposedConstants;
                        }

                        var results = new List<ExposedConstant>();

                        foreach (var queryField in this)
                        {
                            if (queryField.Expression is QueryFieldConstantNumeric constantNumeric)
                            {
                                results.Add(new ExposedConstant(queryField.Ordinal, KbBasicDataType.Numeric, queryField.Alias, constantNumeric.Value));
                            }
                            else if (queryField.Expression is QueryFieldConstantString constantString)
                            {
                                results.Add(new ExposedConstant(queryField.Ordinal, KbBasicDataType.String, queryField.Alias, constantString.Value));
                            }
                        }

                        _exposedConstants = results;
                    }
                }

                return _exposedConstants;
            }
        }

        #endregion

        #region Exposed collection: DocumentIdentifierFields.

        private List<ExposedDocumentIdentifier>? _exposedDocumentIdentifiers = null;
        private readonly object _exposedDocumentIdentifiersLock = new();

        public void InvalidateDocumentIdentifierFieldsCache()
        {
            lock (_exposedDocumentIdentifiersLock)
            {
                _exposedDocumentIdentifiers = null;
            }
        }

        /// <summary>
        /// Returns a list of fields that are of type QueryFieldDocumentIdentifier.
        /// </summary>
        public List<ExposedDocumentIdentifier> DocumentIdentifierFields
        {
            get
            {
                if (_exposedDocumentIdentifiers == null)
                {
                    lock (_exposedDocumentIdentifiersLock)
                    {
                        if (_exposedDocumentIdentifiers != null)
                        {
                            //We check again here because other threads may have started waiting on the lock
                            //  with the intention of hydrating _exposedDocumentIdentifiers themselves, we do this because
                            //  we don't want to lock on reads once this _exposedDocumentIdentifiers is hydrated.
                            return _exposedDocumentIdentifiers;
                        }

                        var results = new List<ExposedDocumentIdentifier>();

                        foreach (var queryField in this)
                        {
                            if (queryField.Expression is QueryFieldDocumentIdentifier documentIdentifier)
                            {
                                results.Add(new ExposedDocumentIdentifier(queryField.Ordinal, queryField.Alias, documentIdentifier.SchemaAlias, documentIdentifier.FieldName));
                            }
                        }

                        _exposedDocumentIdentifiers = results;
                    }
                }

                return _exposedDocumentIdentifiers;
            }
        }

        #endregion

        #region Exposed collection: ExpressionFields.

        private List<ExposedExpression>? _exposedExpressions = null;
        private readonly object _exposedExpressionsLock = new();

        public void InvalidateExpressionFieldsCache()
        {
            lock (_exposedExpressionsLock)
            {
                _exposedExpressions = null;
            }
        }

        /// <summary>
        /// Returns a list of fields that have function call dependencies.
        /// </summary>
        public List<ExposedExpression> ExpressionFields
        {
            get
            {
                if (_exposedExpressions == null)
                {
                    lock (_exposedExpressionsLock)
                    {
                        if (_exposedExpressions != null)
                        {
                            //We check again here because other threads may have started waiting on the lock
                            //  with the intention of hydrating _exposedExpressions themselves, we do this because
                            //  we don't want to lock on reads once this _exposedExpressions is hydrated.
                            return _exposedExpressions;
                        }

                        var results = new List<ExposedExpression>();

                        foreach (var queryField in this)
                        {
                            if (queryField.Expression is IQueryFieldExpression fieldExpression)
                            {
                                var collapseType = CollapseType.Scaler;

                                if (fieldExpression.FunctionDependencies.OfType<QueryFieldExpressionFunctionAggregate>().Any())
                                {
                                    collapseType = CollapseType.Aggregate;
                                }

                                results.Add(new ExposedExpression(queryField.Ordinal, queryField.Alias, fieldExpression, collapseType));
                            }
                        }

                        _exposedExpressions = results;
                    }
                }

                return _exposedExpressions;
            }
        }

        #endregion

        #region Collection: AggregationFunctions.

        private List<ExposedAggregateFunction>? _aggregationFunctions = null;
        private readonly object _aggregationFunctionsLock = new();

        public void InvalidateAggregationFunctionsCache()
        {
            lock (_aggregationFunctionsLock)
            {
                _aggregationFunctions = null;
            }
        }

        /// <summary>
        /// Returns a list of fields that have function call dependencies.
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
