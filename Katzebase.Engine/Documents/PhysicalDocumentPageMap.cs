namespace Katzebase.Engine.Documents
{
    /// <summary>
    /// Contains the list of document IDs that exist in a page.
    /// </summary>
    [Serializable]
    public class PhysicalDocumentPageMap
    {
        public HashSet<uint> DocumentIDs { get; private set; } = new();

        public int TotalDocumentCount()
        {
            return DocumentIDs.Count;
        }
    }
}
