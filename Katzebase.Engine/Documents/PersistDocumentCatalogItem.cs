using Katzebase.Engine.KbLib;
using Katzebase.PublicLibrary.Payloads;

namespace Katzebase.Engine.Documents
{
    /// <summary>
    /// Represents a document in the document catalog. This will just contain basic info like document ID and the document file name.
    /// </summary>
    [Serializable]
    public class PersistDocumentCatalogItem
    {
        public Guid Id { get; set; }

        public KbDocumentCatalogItem ToPayload()
        {
            return new KbDocumentCatalogItem()
            {
                Id = Id
            };
        }

        public string FileName
        {
            get
            {
                return Helpers.GetDocumentModFilePath(Id);
            }
        }

        public PersistDocumentCatalogItem Clone()
        {
            return new PersistDocumentCatalogItem
            {
                Id = Id
            };
        }
    }
}
