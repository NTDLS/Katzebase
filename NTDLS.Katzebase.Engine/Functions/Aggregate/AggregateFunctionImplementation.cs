using NTDLS.Katzebase.Client.Exceptions;
using NTDLS.Katzebase.Engine.QueryProcessing.Searchers.Intersection;
using System.Security.Cryptography;
using System.Text;

namespace NTDLS.Katzebase.Engine.Functions.Aggregate
{
    /// <summary>
    /// Contains all function protype definitions, function implementations and expression collapse functionality.
    /// </summary>
    internal class AggregateFunctionImplementation
    {
        internal static string[] PrototypeStrings = {
                //Prototype Format: "returnDataType functionName (parameterDataType parameterName, parameterDataType parameterName = defaultValue)"
                //Parameters that have a default specified are considered optional, they should come after non-optional parameters.
                "Numeric Count (AggregationArray values, boolean countDistinct = false)",
                "Numeric Sum (AggregationArray values)",
                "Numeric Min (AggregationArray values)",
                "Numeric Max (AggregationArray values)",
                "Numeric Avg (AggregationArray values)",
                "String Sha1Agg (AggregationArray values)"
            };

        public static string ExecuteFunction(string functionName, GroupAggregateFunctionParameter parameters)
        {
            var proc = AggregateFunctionCollection.ApplyFunctionPrototype(functionName, parameters.SupplementalParameters);

            switch (functionName.ToLowerInvariant())
            {
                case "sum":
                    {
                        return parameters.AggregationValues.Sum(o => double.Parse(o)).ToString();
                    }
                case "min":
                    {
                        return parameters.AggregationValues.Min(o => double.Parse(o)).ToString();
                    }
                case "max":
                    {
                        return parameters.AggregationValues.Max(o => double.Parse(o)).ToString();
                    }
                case "avg":
                    {
                        return parameters.AggregationValues.Average(o => double.Parse(o)).ToString();
                    }
                case "count":
                    {
                        if (proc.Get<bool>("countDistinct"))
                        {
                            return parameters.AggregationValues.Distinct().Count().ToString();
                        }
                        return parameters.AggregationValues.Count().ToString();
                    }
                case "sha1agg":
                    {
                        using (var sha1 = SHA1.Create())
                        {
                            foreach (var str in parameters.AggregationValues.OrderBy(o => o))
                            {
                                var inputBytes = Encoding.UTF8.GetBytes(str);
                                sha1.TransformBlock(inputBytes, 0, inputBytes.Length, null, 0);
                            }

                            sha1.TransformFinalBlock(Array.Empty<byte>(), 0, 0); // Finalize the hash

                            var sb = new StringBuilder();
                            var bytes = sha1.Hash ?? Array.Empty<byte>();
                            foreach (var b in bytes)
                            {
                                sb.Append(b.ToString("x2"));
                            }

                            return sb.ToString();
                        }
                    }
            }

            throw new KbFunctionException($"Undefined function: {functionName}.");
        }
    }
}
