using static ParserV2.StandIn.Types;

namespace ParserV2.StandIn
{
    internal class ScalerFunction
    {
        public string Name { get; private set; }
        public KbScalerFunctionParameterType ReturnType { get; private set; }
        //public List<ScalerFunctionParameterPrototype> Parameters { get; private set; } = new();

        public ScalerFunction(string name, KbScalerFunctionParameterType returnType)
        {
            Name = name;
            ReturnType = returnType;

        }
    }
}
