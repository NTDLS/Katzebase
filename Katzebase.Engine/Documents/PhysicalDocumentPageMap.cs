namespace Katzebase.Engine.Documents
{
    /// <summary>
    /// This is the collection item that is physically written to disk via PhysicalDocumentPage. It contains the
    ///     page number as well as a list of document IDs that will be stored in the seperate file "PhysicalDocumentPage".
    /// </summary>
    [Serializable]
    public class PhysicalDocumentPageMap
    {
        public int PageNumber { get; set; }
        public HashSet<Guid> DocumentIDs { get; set; } = new();

        public PhysicalDocumentPageMap(int pageNumber)
        {
            PageNumber = pageNumber;
        }
    }
}
