using System;
using Katzebase.Library.Payloads;

namespace Katzebase.Engine.Documents
{
    [Serializable]
    public class PersistDocumentCatalogItem
    {
        public Guid Id { get; set; }

        public DocumentCatalogItem ToPayload()
        {
            return new DocumentCatalogItem()
            {
                Id = this.Id
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
                Id = this.Id
            };
        }
    }
}
