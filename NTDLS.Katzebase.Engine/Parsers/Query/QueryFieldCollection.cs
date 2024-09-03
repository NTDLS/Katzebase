﻿using NTDLS.Katzebase.Engine.Parsers.Query.Exposed;
using NTDLS.Katzebase.Engine.Parsers.Query.Fields;
using NTDLS.Katzebase.Engine.Parsers.Query.Fields.Expressions;
using NTDLS.Katzebase.Engine.Query;
using static NTDLS.Katzebase.Engine.Library.EngineConstants;

namespace NTDLS.Katzebase.Engine.Parsers.Query
{
    /// <summary>
    /// Collection of query fields, which contains their names and values.
    /// </summary>
    internal class QueryFieldCollection : List<QueryField>
    {
        private int nextFieldAlias = 0;

        public QueryBatch QueryBatch { get; private set; }

        /// <summary>
        /// A list of all distinct document identifiers from all fields, even nested expressions.
        /// We go out of our way to create this list because it helps optimize the query execution.
        /// </summary>
        public HashSet<QueryFieldDocumentIdentifier> DocumentIdentifiers { get; set; } = new();

        /// <summary>
        /// Gets a field alias for a field for which the query did not supply an alias.
        /// </summary>
        /// <returns></returns>
        public string GetNextFieldAlias()
            => $"Expression{nextFieldAlias++}";

        public QueryFieldCollection(QueryBatch queryBatch)
        {
            QueryBatch = queryBatch;
        }

        #region Exposed collection: ScalerFunctions.

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
                                if (fieldExpression.FunctionDependencies.OfType<QueryFieldExpressionFunctionScaler>().Count() > 0)
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

        #region Exposed collection: AggregateFunctions.

        private List<ExposedFunction>? _exposedAggregateFunctions = null;
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
        public List<ExposedFunction> FieldsWithAggregateFunctionCalls
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

                        var results = new List<ExposedFunction>();

                        foreach (var queryField in this)
                        {
                            if (queryField.Expression is IQueryFieldExpression fieldExpression)
                            {
                                if (fieldExpression.FunctionDependencies.OfType<QueryFieldExpressionFunctionAggregate>().Count() > 0)
                                {
                                    results.Add(new ExposedFunction(queryField.Ordinal, queryField.Alias, fieldExpression));
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

        #region Exposed collection: Constants.

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
                                results.Add(new ExposedConstant(queryField.Ordinal, BasicDataType.Numeric, queryField.Alias, constantNumeric.Value));
                            }
                            else if (queryField.Expression is QueryFieldConstantString constantString)
                            {
                                results.Add(new ExposedConstant(queryField.Ordinal, BasicDataType.String, queryField.Alias, constantString.Value));
                            }
                        }

                        _exposedConstants = results;
                    }
                }

                return _exposedConstants;
            }
        }

        #endregion

        #region Exposed collection: DocumentIdentifiers.

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
        /// Returns a list of fields that have function call dependencies.
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
                                results.Add(new ExposedDocumentIdentifier(queryField.Ordinal, queryField.Alias, documentIdentifier.SchemaAlias, documentIdentifier.Value));
                            }
                        }

                        _exposedDocumentIdentifiers = results;
                    }
                }

                return _exposedDocumentIdentifiers;
            }
        }

        #endregion

        #region Exposed collection: Expressions.

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
                                if (fieldExpression.FunctionDependencies.OfType<QueryFieldExpressionFunctionScaler>().Count() > 0)
                                {
                                    results.Add(new ExposedExpression(queryField.Ordinal, queryField.Alias, fieldExpression));
                                }
                            }
                        }

                        _exposedExpressions = results;
                    }
                }

                return _exposedExpressions;
            }
        }

        #endregion
    }
}