using NTDLS.Helpers;
using NTDLS.Katzebase.Api.Exceptions;
using System.Diagnostics.CodeAnalysis;

namespace NTDLS.Katzebase.Parsers.Functions.Aggregate
{
    public static class AggregateFunctionCollection
    {
        internal static string[] PrototypeStrings = {
                //Prototype Format: "returnDataType functionName (parameterDataType parameterName, parameterDataType parameterName = defaultValue)"
                //Parameters that have a default specified are considered optional, they should come after non-optional parameters.
                "Numeric Avg (AggregationArray valuesArray)|'Returns the average value for the set.'",
                "Numeric Count (AggregationArray valuesArray)|'Returns the count of values in the set.'",
                "Numeric CountDistinct (AggregationArray valuesArray, boolean caseSensitive = false)|'Returns the count of distinct values in the set.'",
                "Numeric GeometricMean (AggregationArray valuesArray)|'Returns the average that is used to calculate the central tendency of a the set.'",
                "Numeric Max (AggregationArray valuesArray)|'Returns the maximum value for the set.'",
                "Numeric Mean (AggregationArray valuesArray)|'Returns the mean value for the set.'",
                "Numeric Median (AggregationArray valuesArray)|'Returns the median value for the set.'",
                "Numeric Min (AggregationArray valuesArray)|'Returns the minimum value for the set.'",
                "Numeric Mode (AggregationArray valuesArray)|'Returns the value that appears most frequently for the set.'",
                "Numeric Sum (AggregationArray valuesArray)|'Returns the total summative value for the set.'",
                "Numeric Variance (AggregationArray valuesArray)|'Returns a the measure of dispersion of a the set from their mean value.'",
                "String MinString (AggregationArray valuesArray)|'Returns the minimum string value for the set.'",
                "String MaxString (AggregationArray valuesArray)|'Returns the maximum string value for the set.'",
                "String Sha1Agg (AggregationArray valuesArray)|'Returns the SHA1 hash for the for the set.'",
                "String Sha256Agg (AggregationArray valuesArray)|'Returns the SHA256 hash for the for the set.'",
                "String Sha512Agg (AggregationArray valuesArray)|'Returns the SHA512 hash for the for the set.'",
            };


        private static List<AggregateFunction>? _protypes = null;

        public static List<AggregateFunction> Prototypes
        {
            get
            {
                if (_protypes == null)
                {
                    throw new KbFatalException("Function prototypes were not initialized.");
                }
                return _protypes;
            }
        }

        public static void Initialize()
        {
            if (_protypes == null)
            {
                _protypes = new List<AggregateFunction>();

                foreach (var prototype in PrototypeStrings)
                {
                    _protypes.Add(AggregateFunction.Parse(prototype));
                }
            }
        }

        public static bool TryGetFunction(string name, [NotNullWhen(true)] out AggregateFunction? function)
        {
            function = Prototypes.FirstOrDefault(o => o.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase));
            return function != null;
        }

        public static AggregateFunctionParameterValueCollection ApplyFunctionPrototype(string functionName, List<string?> parameters)
        {
            if (_protypes == null)
            {
                throw new KbFatalException("Function prototypes were not initialized.");
            }

            var function = _protypes.FirstOrDefault(o => o.Name.Is(functionName));

            return function == null
                ? throw new KbFunctionException($"Undefined function: [{functionName}].")
                : function.ApplyParameters(parameters);
        }
    }
}
