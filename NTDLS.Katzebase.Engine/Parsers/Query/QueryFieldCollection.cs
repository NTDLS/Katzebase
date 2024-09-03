using NTDLS.Katzebase.Engine.Parsers.Query.Exposed;
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

                        _exposedScalerFunctions = new List<ExposedFunction>();

                        foreach (var queryField in this)
                        {
                            if (queryField.Expression is IQueryFieldExpression fieldExpression)
                            {
                                if (fieldExpression.FunctionDependencies.OfType<QueryFieldExpressionFunctionScaler>().Count() > 0)
                                {
                                    _exposedScalerFunctions.Add(new ExposedFunction(queryField.Ordinal, fieldExpression));
                                }
                            }
                        }
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

                        _exposedAggregateFunctions = new List<ExposedFunction>();

                        foreach (var queryField in this)
                        {
                            if (queryField.Expression is IQueryFieldExpression fieldExpression)
                            {
                                if (fieldExpression.FunctionDependencies.OfType<QueryFieldExpressionFunctionAggregate>().Count() > 0)
                                {
                                    _exposedAggregateFunctions.Add(new ExposedFunction(queryField.Ordinal, fieldExpression));
                                }
                            }
                        }
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

                        _exposedConstants = new List<ExposedConstant>();

                        foreach (var queryField in this)
                        {
                            if (queryField.Expression is QueryFieldConstantNumeric constantNumeric)
                            {
                                _exposedConstants.Add(new ExposedConstant(queryField.Ordinal, BasicDataType.Numeric, queryField.Alias, constantNumeric.Value));
                            }
                            else if (queryField.Expression is QueryFieldConstantString constantString)
                            {
                                _exposedConstants.Add(new ExposedConstant(queryField.Ordinal, BasicDataType.String, queryField.Alias, constantString.Value));
                            }
                        }
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

                        _exposedDocumentIdentifiers = new List<ExposedDocumentIdentifier>();

                        foreach (var queryField in this)
                        {
                            if (queryField.Expression is QueryFieldDocumentIdentifier documentIdentifier)
                            {
                                _exposedDocumentIdentifiers.Add(
                                    new ExposedDocumentIdentifier(queryField.Ordinal, queryField.Alias, documentIdentifier.SchemaAlias, documentIdentifier.Value));
                            }
                        }
                    }
                }

                return _exposedDocumentIdentifiers;
            }
        }

        #endregion
    }
}
