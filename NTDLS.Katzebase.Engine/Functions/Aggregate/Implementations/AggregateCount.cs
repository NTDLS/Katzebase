using NTDLS.Katzebase.Engine.QueryProcessing.Searchers.Intersection;
using NTDLS.Katzebase.Parsers.Functions.Aggregate;
using NTDLS.Katzebase.Parsers.Interfaces;
namespace NTDLS.Katzebase.Engine.Functions.Aggregate.Implementations
{
    public static class AggregateCount<TData> where TData : IStringable
    {
        //public static string Execute(AggregateFunctionParameterValueCollection<TData> function, GroupAggregateFunctionParameter<TData> parameters)
        public static string Execute(GroupAggregateFunctionParameter<TData> parameters)
        { 
        
            return parameters.AggregationValues.Count.ToString();
			/*
			
			if (function.Get<bool>("countDistinct"))
            {
                return parameters.AggregationValues.Distinct().Count().ToString();
            }
            return parameters.AggregationValues.Count().ToString();
			*/
        }
    }
}
