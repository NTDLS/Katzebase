using static ParserV2.StandIn.Types;

namespace ParserV2.Expression
{
    internal class ReferencedFunction
    {
        public string Key { get; set; }
        public string Name { get; set; }
        public KbScalerFunctionParameterType ReturnType { get; set; }

        public ReferencedFunction(string key, string name, KbScalerFunctionParameterType returnType)
        {
            Key = key;
            Name = name;
            ReturnType = returnType;
        }
    }
}
