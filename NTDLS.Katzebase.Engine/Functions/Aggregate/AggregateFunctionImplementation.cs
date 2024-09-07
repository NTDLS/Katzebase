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
                "Numeric Avg (AggregationArray values)",
                "Numeric Count (AggregationArray values, boolean countDistinct = false)",
                "Numeric GeometricMean (AggregationArray values)",
                "Numeric Max (AggregationArray values)",
                "Numeric Mean (AggregationArray values)",
                "Numeric Median (AggregationArray values)",
                "Numeric Min (AggregationArray values)",
                "Numeric Mode (AggregationArray values)",
                "Numeric Sum (AggregationArray values)",
                "Numeric Variance (AggregationArray values)",
                "String MinString (AggregationArray values)",
                "String MaxString (AggregationArray values)",
                "String Sha1Agg (AggregationArray values)",
                "String Sha256Agg (AggregationArray values)",
                "String Sha512Agg (AggregationArray values)",
            };

        public static string ExecuteFunction(string functionName, GroupAggregateFunctionParameter parameters)
        {
            var proc = AggregateFunctionCollection.ApplyFunctionPrototype(functionName, parameters.SupplementalParameters);

            switch (functionName.ToLowerInvariant())
            {
                case "sum":
                    return parameters.AggregationValues.Sum(o => double.Parse(o)).ToString();
                case "min":
                    return parameters.AggregationValues.Min(o => double.Parse(o)).ToString();
                case "max":
                    return parameters.AggregationValues.Max(o => double.Parse(o)).ToString();
                case "minstring":
                    return parameters.AggregationValues.OrderBy(o => o).First().ToString();
                case "maxstring":
                    return parameters.AggregationValues.OrderByDescending(o => o).First().ToString();
                case "avg":
                    return parameters.AggregationValues.Average(o => double.Parse(o)).ToString();
                case "count":
                    if (proc.Get<bool>("countDistinct"))
                    {
                        return parameters.AggregationValues.Distinct().Count().ToString();
                    }
                    return parameters.AggregationValues.Count().ToString();
                case "median":
                    {
                        var sortedNumbers = parameters.AggregationValues.Select(o => double.Parse(o)).OrderBy(n => n).ToList();
                        int count = sortedNumbers.Count;

                        if (count % 2 == 0)
                        {
                            // Even count: return average of the two middle elements
                            return ((sortedNumbers[count / 2 - 1] + sortedNumbers[count / 2]) / 2.0).ToString();
                        }
                        else
                        {
                            // Odd count: return the middle element
                            return (sortedNumbers[count / 2]).ToString();
                        }
                    }
                case "geometricmean":
                    {
                        var numbers = parameters.AggregationValues.Select(o => double.Parse(o)).ToList();
                        double product = numbers.Aggregate(1.0, (acc, n) => acc * n);
                        return (Math.Pow(product, 1.0 / numbers.Count)).ToString();
                    }
                case "variance":
                    {
                        var numbers = parameters.AggregationValues.Select(o => double.Parse(o)).ToList();
                        double mean = numbers.Average();
                        return (numbers.Sum(n => Math.Pow(n - mean, 2)) / numbers.Count).ToString();
                    }
                case "mode":
                    {
                        var numbers = parameters.AggregationValues.Select(o => double.Parse(o)).ToList();
                        var frequencyDict = numbers.GroupBy(n => n)
                                                   .ToDictionary(g => g.Key, g => g.Count());
                        int maxFrequency = frequencyDict.Values.Max();

                        // Return the first number with the max frequency
                        return (frequencyDict.First(kvp => kvp.Value == maxFrequency).Key).ToString();
                    }
                case "sha1agg":
                    {
                        using var sha1 = SHA1.Create();
                        foreach (var str in parameters.AggregationValues.OrderBy(o => o))
                        {
                            var inputBytes = Encoding.UTF8.GetBytes(str);
                            sha1.TransformBlock(inputBytes, 0, inputBytes.Length, null, 0);
                        }

                        sha1.TransformFinalBlock(Array.Empty<byte>(), 0, 0);

                        var sb = new StringBuilder();
                        var bytes = sha1.Hash ?? Array.Empty<byte>();
                        foreach (var b in bytes)
                        {
                            sb.Append(b.ToString("x2"));
                        }

                        return sb.ToString();
                    }
                case "sha256agg":
                    {
                        using var sha256 = SHA256.Create();
                        foreach (var str in parameters.AggregationValues.OrderBy(o => o))
                        {
                            var inputBytes = Encoding.UTF8.GetBytes(str);
                            sha256.TransformBlock(inputBytes, 0, inputBytes.Length, null, 0);
                        }

                        sha256.TransformFinalBlock(Array.Empty<byte>(), 0, 0);

                        var sb = new StringBuilder();
                        var hashBytes = sha256.Hash ?? Array.Empty<byte>();
                        foreach (var b in hashBytes)
                        {
                            sb.Append(b.ToString("x2"));
                        }

                        return sb.ToString();
                    }
                case "sha512agg":
                    {
                        using var sha512 = SHA512.Create();

                        foreach (var str in parameters.AggregationValues.OrderBy(o => o))
                        {
                            var inputBytes = Encoding.UTF8.GetBytes(str);
                            sha512.TransformBlock(inputBytes, 0, inputBytes.Length, null, 0);
                        }

                        sha512.TransformFinalBlock(Array.Empty<byte>(), 0, 0);

                        var sb = new StringBuilder();
                        var hashBytes = sha512.Hash ?? Array.Empty<byte>();
                        foreach (var b in hashBytes)
                        {
                            sb.Append(b.ToString("x2"));
                        }

                        return sb.ToString();
                    }
            }

            throw new KbFunctionException($"Undefined function: {functionName}.");
        }
    }
}
