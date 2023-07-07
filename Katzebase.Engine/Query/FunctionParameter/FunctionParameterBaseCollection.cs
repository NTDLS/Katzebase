namespace Katzebase.Engine.Query.FunctionParameter
{
    internal class FunctionParameterBaseCollection : List<FunctionParameterBase>
    {
        public new void Add(FunctionParameterBase param)
        {
            param.Ordinal = Count;
            base.Add(param);
        }

        public void RefillStringLiterals(Dictionary<string, string> literals)
        {
            RefillStringLiterals(this, literals);
        }

        private void RefillStringLiterals(List<FunctionParameterBase> list, Dictionary<string, string> literals)
        {
            foreach (var param in list)
            {
                if (param is FunctionConstantParameter)
                {
                    foreach (var literal in literals)
                    {
                        ((FunctionConstantParameter)param).Value = ((FunctionConstantParameter)param).Value.Replace(literal.Key, literal.Value.Substring(1, literal.Value.Length - 2));
                    }
                }
                else if (param is FunctionExpression)
                {
                    foreach (var literal in literals)
                    {
                        ((FunctionExpression)param).Value = ((FunctionExpression)param).Value.Replace(literal.Key, "\"" + literal.Value.Substring(1, literal.Value.Length - 2) + "\"");
                    }
                }

                if (param is FunctionExpression)
                {
                    RefillStringLiterals(((FunctionExpression)param).Parameters, literals);
                }
                if (param is FunctionWithParams)
                {
                    RefillStringLiterals(((FunctionWithParams)param).Parameters, literals);
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

                _allFields.AddRange(this.OfType<FunctionDocumentFieldParameter>().Select(o => o.Value).ToList());

                var children = new List<FunctionParameterBase>();

                children.AddRange(this.OfType<FunctionExpression>());
                children.AddRange(this.OfType<FunctionWithParams>());

                foreach (var param in children)
                {
                    if (param is FunctionExpression)
                    {
                        GetAllFieldsRecursive(ref _allFields, ((FunctionExpression)param).Parameters);
                    }
                    if (param is FunctionWithParams)
                    {
                        GetAllFieldsRecursive(ref _allFields, ((FunctionWithParams)param).Parameters);
                    }
                }
            }

            return _allFields;
        }

        private void GetAllFieldsRecursive(ref List<PrefixedField> result, List<FunctionParameterBase> list)
        {
            foreach (var param in list)
            {
                if (param is FunctionDocumentFieldParameter)
                {
                    var key = ((FunctionDocumentFieldParameter)param).Value.Key;

                    if (result.Any(o => o.Key == key) == false)
                    {
                        result.Add(((FunctionDocumentFieldParameter)param).Value);
                    }
                }

                if (param is FunctionExpression)
                {
                    GetAllFieldsRecursive(ref result, ((FunctionExpression)param).Parameters);
                }
                if (param is FunctionWithParams)
                {
                    GetAllFieldsRecursive(ref result, ((FunctionWithParams)param).Parameters);
                }
            }
        }
    }
}