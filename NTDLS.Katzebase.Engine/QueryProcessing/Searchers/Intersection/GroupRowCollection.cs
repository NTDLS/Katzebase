using NTDLS.Katzebase.Client.Types;

namespace NTDLS.Katzebase.Engine.QueryProcessing.Searchers.Intersection
{
    /// <summary>
    /// Contains the template row for grouping operations (GroupRow)
    /// </summary>
    internal class GroupRowCollection<TData> where TData : IStringable
    {
        /// <summary>
        /// Contains the template row for the group.
        /// </summary>
        public SchemaIntersectionRow<TData> GroupRow { get; set; } = new();

        public KbInsensitiveDictionary<GroupAggregateFunctionParameter<TData>> GroupAggregateFunctionParameters { get; set; } = new();
    }
}
