using static ParserV2.StandIn.Types;

namespace ParserV2.Parsers.Query.Functions
{
    internal class FunctionReference
    {
        public string Key { get; set; }
        public string Name { get; set; }
        public KbScalerFunctionParameterType ReturnType { get; set; }

        public FunctionReference(string key, string name, KbScalerFunctionParameterType returnType)
        {
            Key = key;
            Name = name;
            ReturnType = returnType;
        }
    }
}
