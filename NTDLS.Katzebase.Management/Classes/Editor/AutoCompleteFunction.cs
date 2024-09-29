namespace NTDLS.Katzebase.Management.Classes.Editor
{
    internal class AutoCompleteFunction
    {
        public enum FunctionType
        {
            System,
            Aggregate,
            Scaler
        }

        public string Name { get; set; }
        public FunctionType Type { get; set; }
        public string ReturnType { get; set; }
        public string Description { get; set; }

        public List<AutoCompleteFunctionParameter> Parameters { get; set; }

        public AutoCompleteFunction(FunctionType type, string name, string returnType, string description, List<AutoCompleteFunctionParameter> parameters)
        {
            Type = type;
            Name = name;
            Description = description;
            Parameters = parameters;
            ReturnType = returnType;
        }
    }
}
