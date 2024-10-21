namespace NTDLS.Katzebase.Parsers.Functions
{
    public interface IExpressionFunctionParameter
    {
        string Expression { get; set; }

        public IExpressionFunctionParameter Clone();
    }
}
