namespace NTDLS.Katzebase.Parsers.Query.Functions
{
    public interface IExpressionFunctionParameter
    {
        string Expression { get; set; }

        public IExpressionFunctionParameter Clone();
    }
}
