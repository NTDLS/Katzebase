using NTDLS.Katzebase.Engine.QueryProcessing.Searchers.Intersection;

namespace NTDLS.Katzebase.Engine.Functions.Aggregate.Implementations
{
    internal static class AggregateMode
    {
        public static string Execute(GroupAggregateFunctionParameter parameters)
        {
            var numbers = parameters.AggregationValues.Select(o => double.Parse(o.s)).ToList();
            var frequencyDict = numbers.GroupBy(n => n).ToDictionary(g => g.Key, g => g.Count());
            int maxFrequency = frequencyDict.Values.Max();

            // Return the first number with the max frequency
            return (frequencyDict.First(kvp => kvp.Value == maxFrequency).Key).ToString();
        }
    }
}
