using NTDLS.Katzebase.Client.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NTDLS.Katzebase.Engine.Query.Searchers.Intersection
{
    internal class GroupRow
    {
        /// <summary>
        /// Contains the list of scaler values for each group, the key to the dictionary is the ordinal of the scaler expression field.
        /// </summary>
        public Dictionary<int, string> GroupingFieldValues { get; set; } = new();

        /// <summary>
        /// Contains the list of values that we will need to collapse aggregation functions.
        /// The key is the ExpressionKey of the aggregation function these values are for.
        /// </summary>
        public KbInsensitiveDictionary<List<string>> AggregationValues { get; set; } = new();
    }
}
