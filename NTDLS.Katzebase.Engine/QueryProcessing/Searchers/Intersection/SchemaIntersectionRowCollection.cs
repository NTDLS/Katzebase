using NTDLS.Katzebase.Parsers.Interfaces;
namespace NTDLS.Katzebase.Engine.QueryProcessing.Searchers.Intersection
{
    internal class SchemaIntersectionRowCollection<TData> : List<SchemaIntersectionRow<TData>> where TData : IStringable
    {
    }
}
