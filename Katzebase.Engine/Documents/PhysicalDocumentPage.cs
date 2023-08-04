namespace Katzebase.Engine.Documents
{
    /// <summary>
    /// This is the actual file that gets saved for each document page. It contains the
    ///     page number and a dictonary of the document IDs along with the actual document data.
    /// </summary>
    [Serializable]
    public class PhysicalDocumentPage
    {
        public Dictionary<uint, PhysicalDocument> Documents { get; set; } = new();
    }
}
