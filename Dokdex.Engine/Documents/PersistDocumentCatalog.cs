using System;
using System.Collections.Generic;
using System.Linq;

namespace Dokdex.Engine.Documents
{
    [Serializable]
    public class PersistDocumentCatalog
    {
        public List<PersistDocumentCatalogItem> Collection = new List<PersistDocumentCatalogItem>();

        public PersistDocumentCatalogItem Add(PersistDocument document)
        {
            var catalogItem = new PersistDocumentCatalogItem()
            {
                Id = document.Id
            };

            this.Collection.Add(catalogItem);

            return catalogItem;
        }

        public void Remove(PersistDocumentCatalogItem item)
        {
            Collection.Remove(item);
        }

        public void Add(PersistDocumentCatalogItem item)
        {
            this.Collection.Add(item);
        }

        public PersistDocumentCatalogItem GetById(Guid id)
        {
            return (from o in Collection where o.Id == id select o).FirstOrDefault();
        }

        public PersistDocumentCatalog Clone()
        {
            var catalog = new PersistDocumentCatalog();

            lock (this)
            {
                foreach (var obj in Collection)
                {
                    catalog.Collection.Add(obj.Clone());
                }
            }

            return catalog;
        }
    }
}
