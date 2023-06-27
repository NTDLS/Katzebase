using Katzebase.Engine.Query;
using System.Collections.Generic;

namespace Katzebase.Engine.Method.ParsedMethodParameter
{
    internal class GenericParsedMethodParameters : List<GenericParsedMethodParameter>
    {
        public new void Add(GenericParsedMethodParameter param)
        {
            param.Ordinal = this.Count;
            base.Add(param);
        }

        public void RefillStringLiterals(Dictionary<string, string> literals)
        {
            RefillStringLiterals(this, literals);
        }

        private void RefillStringLiterals(List<GenericParsedMethodParameter> list, Dictionary<string, string> literals)
        {
            foreach (var param in list)
            {
                if (param is ParsedConstantParameter)
                {
                    foreach (var literal in literals)
                    {
                        ((ParsedConstantParameter)param).Value = ((ParsedConstantParameter)param).Value.Replace(literal.Key, literal.Value.Substring(1, literal.Value.Length - 2));
                    }
                }
                else if (param is ParsedExpression)
                {
                    foreach (var literal in literals)
                    {
                        ((ParsedExpression)param).Value = ((ParsedExpression)param).Value.Replace(literal.Key, "\"" + literal.Value.Substring(1, literal.Value.Length - 2) + "\"");
                    }
                }

                if (param is ParsedExpression)
                {
                    RefillStringLiterals(((ParsedExpression)param).Parameters, literals);
                }
                if (param is ParsedMethodAndParams)
                {
                    RefillStringLiterals(((ParsedMethodAndParams)param).Parameters, literals);
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

                var children = new List<GenericParsedMethodParameter>();

                children.AddRange(this.OfType<ParsedExpression>());
                children.AddRange(this.OfType<ParsedMethodAndParams>());

                foreach (var param in children)
                {
                    if (param is ParsedExpression)
                    {
                        GetAllFieldsRecursive(ref _allFields, ((ParsedExpression)param).Parameters);
                    }
                    if (param is ParsedMethodAndParams)
                    {
                        GetAllFieldsRecursive(ref _allFields, ((ParsedMethodAndParams)param).Parameters);
                    }
                }
            }

            return _allFields;
        }

        private void GetAllFieldsRecursive(ref List<PrefixedField> result, List<GenericParsedMethodParameter> list)
        {
            foreach (var param in list)
            {
                if (param is ParsedFieldParameter)
                {
                    var key = ((ParsedFieldParameter)param).Value.Key;

                    if (result.Any(o => o.Key == key) == false)
                    {
                        result.Add(((ParsedFieldParameter)param).Value);
                    }
                }

                if (param is ParsedExpression)
                {
                    GetAllFieldsRecursive(ref result, ((ParsedExpression)param).Parameters);
                }
                if (param is ParsedMethodAndParams)
                {
                    GetAllFieldsRecursive(ref result, ((ParsedMethodAndParams)param).Parameters);
                }
            }
        }
    }
}