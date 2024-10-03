using NTDLS.Helpers;
using NTDLS.Katzebase.Client.Exceptions;
using NTDLS.Katzebase.Parsers.Interfaces;
using System.Diagnostics.CodeAnalysis;

namespace NTDLS.Katzebase.Parsers.Functions.Aggregate
{
    public static class AggregateFunctionCollection<TData> where TData : IStringable
    {
    	internal static string[] PrototypeStrings = {
                //Prototype Format: "returnDataType functionName (parameterDataType parameterName, parameterDataType parameterName = defaultValue)"
                //Parameters that have a default specified are considered optional, they should come after non-optional parameters.
                "Numeric Avg (AggregationArray values)|'Returns the average value for the set.'",
                "Numeric Count (AggregationArray values, boolean countDistinct = false)|'Returns the count of values in the set.'",
                "Numeric CountDistinct (AggregationArray values, boolean caseSensitive = false)|'Returns the count of distinct values in the set.'",
                "Numeric GeometricMean (AggregationArray values)|'Returns the average that is used to calculate the central tendency of a the set.'",
                "Numeric Max (AggregationArray values)|'Returns the maximum value for the set.'",
                "Numeric Mean (AggregationArray values)|'Returns the mean value for the set.'",
                "Numeric Median (AggregationArray values)|'Returns the median value for the set.'",
                "Numeric Min (AggregationArray values)|'Returns the minimum value for the set.'",
                "Numeric Mode (AggregationArray values)|'Returns the value that appears most frequently for the set.'",
                "Numeric Sum (AggregationArray values)|'Returns the total summative value for the set.'",
                "Numeric Variance (AggregationArray values)|'Returns a the measure of dispersion of a the set from their mean value.'",
                "String MinString (AggregationArray values)|'Returns the minimum string value for the set.'",
                "String MaxString (AggregationArray values)|'Returns the maximum string value for the set.'",
                "String Sha1Agg (AggregationArray values)|'Returns the SHA1 hash for the for the set.'",
                "String Sha256Agg (AggregationArray values)|'Returns the SHA256 hash for the for the set.'",
                "String Sha512Agg (AggregationArray values)|'Returns the SHA512 hash for the for the set.'",
            };
        private static List<AggregateFunction<TData>>? _protypes = null;

        public static List<AggregateFunction<TData>> Prototypes
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

        public static void Initialize(Func<string, TData> strParse2TData)
        {
            if (_protypes == null)
            {
                _protypes = new List<AggregateFunction<TData>>();

                foreach (var prototype in PrototypeStrings)
                {
                    _protypes.Add(AggregateFunction<TData>.Parse(prototype, strParse2TData));
                }
            }
        }

        public static bool TryGetFunction(string name, [NotNullWhen(true)] out AggregateFunction<TData>? function)
        {
            function = Prototypes.FirstOrDefault(o => o.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase));
            return function != null;
        }

        public static AggregateFunctionParameterValueCollection<TData> ApplyFunctionPrototype(string functionName, List<TData?> parameters)
        {
            if (_protypes == null)
            {
                throw new KbFatalException("Function prototypes were not initialized.");
            }

            var function = _protypes.FirstOrDefault(o => o.Name.Is(functionName));

            if (function == null)
            {
                throw new KbFunctionException($"Undefined function: [{functionName}].");
            }

            return function.ApplyParameters(parameters);
        }
    }
}
