namespace Katzebase.Engine.Query.QueryField
{
    internal class QueryFieldParameterBaseCollection : List<QueryFieldParameterBase>
    {
        public new void Add(QueryFieldParameterBase param)
        {
            param.Ordinal = Count;
            base.Add(param);
        }

        public void RefillStringLiterals(Dictionary<string, string> literals)
        {
            RefillStringLiterals(this, literals);
        }

        private void RefillStringLiterals(List<QueryFieldParameterBase> list, Dictionary<string, string> literals)
        {
            foreach (var param in list)
            {
                if (param is QueryFieldConstantParameter)
                {
                    foreach (var literal in literals)
                    {
                        ((QueryFieldConstantParameter)param).Value = ((QueryFieldConstantParameter)param).Value.Replace(literal.Key, literal.Value.Substring(1, literal.Value.Length - 2));
                    }
                }
                else if (param is QueryFieldExpression)
                {
                    foreach (var literal in literals)
                    {
                        ((QueryFieldExpression)param).Value = ((QueryFieldExpression)param).Value.Replace(literal.Key, "\"" + literal.Value.Substring(1, literal.Value.Length - 2) + "\"");
                    }
                }

                if (param is QueryFieldExpression)
                {
                    RefillStringLiterals(((QueryFieldExpression)param).Parameters, literals);
                }
                if (param is QueryFieldMethodAndParams)
                {
                    RefillStringLiterals(((QueryFieldMethodAndParams)param).Parameters, literals);
                }
            }
        }


        private List<PrefixedField>? _allFields = null;

        /// <summary>
        /// Returns a list of document fields that are referenced by the field list.
        /// </summary>
        /// <returns></returns>
        public List<PrefixedField> AllDocumentFields()
        {
            if (_allFields == null)
            {
                _allFields = new();

                var children = new List<QueryFieldParameterBase>();

                children.AddRange(this.OfType<QueryFieldExpression>());
                children.AddRange(this.OfType<QueryFieldMethodAndParams>());

                foreach (var param in children)
                {
                    if (param is QueryFieldExpression)
                    {
                        GetAllFieldsRecursive(ref _allFields, ((QueryFieldExpression)param).Parameters);
                    }
                    if (param is QueryFieldMethodAndParams)
                    {
                        GetAllFieldsRecursive(ref _allFields, ((QueryFieldMethodAndParams)param).Parameters);
                    }
                }
            }

            return _allFields;
        }

        private void GetAllFieldsRecursive(ref List<PrefixedField> result, List<QueryFieldParameterBase> list)
        {
            foreach (var param in list)
            {
                if (param is QueryFieldDocumentFieldParameter)
                {
                    var key = ((QueryFieldDocumentFieldParameter)param).Value.Key;

                    if (result.Any(o => o.Key == key) == false)
                    {
                        result.Add(((QueryFieldDocumentFieldParameter)param).Value);
                    }
                }

                if (param is QueryFieldExpression)
                {
                    GetAllFieldsRecursive(ref result, ((QueryFieldExpression)param).Parameters);
                }
                if (param is QueryFieldMethodAndParams)
                {
                    GetAllFieldsRecursive(ref result, ((QueryFieldMethodAndParams)param).Parameters);
                }
            }
        }
    }
}