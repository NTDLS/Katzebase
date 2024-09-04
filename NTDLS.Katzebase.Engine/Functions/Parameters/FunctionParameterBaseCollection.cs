using NTDLS.Katzebase.Engine.Query.SupportingTypes;

namespace NTDLS.Katzebase.Engine.Functions.Parameters
{
    internal class FunctionParameterBaseCollection : List<FunctionParameterBase>
    {
        public new void Add(FunctionParameterBase param)
        {
            param.Ordinal = Count;
            base.Add(param);
        }

        private List<PrefixedField>? _allFields = null;

        /// <summary>
        /// Returns a list of document fields that are referenced by the field list.
        /// </summary>
        /// <returns></returns>
        public List<PrefixedField> AllDocumentFields
        {
            get
            {
                lock (this)
                {
                    if (_allFields == null)
                    {
                        _allFields = new(this.OfType<FunctionDocumentFieldParameter>().Select(o => o.Value));

                        var children = new List<FunctionParameterBase>();

                        children.AddRange(this.OfType<FunctionExpression>());
                        children.AddRange(this.OfType<FunctionWithParams>());

                        foreach (var param in children)
                        {
                            if (param is FunctionExpression functionExpression)
                            {
                                GetAllFieldsRecursive(ref _allFields, functionExpression.Parameters);
                            }
                            if (param is FunctionWithParams FunctionWithParams)
                            {
                                GetAllFieldsRecursive(ref _allFields, FunctionWithParams.Parameters);
                            }
                        }
                    }

                    return _allFields;
                }
            }
        }

        private static void GetAllFieldsRecursive(ref List<PrefixedField> result, List<FunctionParameterBase> list)
        {
            foreach (var param in list)
            {
                if (param is FunctionDocumentFieldParameter functionDocumentFieldParameter)
                {
                    var key = functionDocumentFieldParameter.Value.Key;

                    if (result.Any(o => o.Key == key) == false)
                    {
                        result.Add(functionDocumentFieldParameter.Value);
                    }
                }

                if (param is FunctionExpression functionExpression)
                {
                    GetAllFieldsRecursive(ref result, functionExpression.Parameters);
                }
                if (param is FunctionWithParams functionWithParams)
                {
                    GetAllFieldsRecursive(ref result, functionWithParams.Parameters);
                }
            }
        }
    }
}