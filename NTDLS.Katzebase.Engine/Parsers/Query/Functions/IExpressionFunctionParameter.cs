namespace NTDLS.Katzebase.Engine.Parsers.Query.Functions
{
    internal interface IExpressionFunctionParameter
    {
        string Expression { get; set; }

        public IExpressionFunctionParameter Clone();
    }
}
