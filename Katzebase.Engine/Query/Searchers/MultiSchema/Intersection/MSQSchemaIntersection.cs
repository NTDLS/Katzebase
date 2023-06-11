using System.Runtime.CompilerServices;

namespace Katzebase.Engine.Query.Searchers.MultiSchema.Intersection
{
    public class MSQSchemaIntersection
    {
        public Dictionary<Guid, MSQSchemaIntersectionDocumentCollection> Documents { get; set; } = new();
    }
}
