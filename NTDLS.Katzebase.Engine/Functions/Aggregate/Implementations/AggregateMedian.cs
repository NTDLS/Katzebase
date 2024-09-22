using NTDLS.Katzebase.Engine.QueryProcessing.Searchers.Intersection;

namespace NTDLS.Katzebase.Engine.Functions.Aggregate.Implementations
{
    internal static class AggregateMedian
    {
        public static string Execute(GroupAggregateFunctionParameter parameters)
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
    }
}
