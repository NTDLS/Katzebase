using NTDLS.Katzebase.Client.Types;

namespace NTDLS.Katzebase.Engine.QueryProcessing.Searchers.Intersection
{
    /// <summary>
    /// Contains the template row for grouping operations (GroupRow)
    /// </summary>
    internal class GroupRowCollection
    {
        /// <summary>
        /// Contains the template row for the group.
        /// </summary>
        public SchemaIntersectionRow GroupRow { get; set; } = new();

        public KbInsensitiveDictionary<GroupAggregateFunctionParameter> GroupAggregateFunctionParameters { get; set; } = new();
    }
}
