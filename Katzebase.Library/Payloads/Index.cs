using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Katzebase.Library.Payloads
{
    public class Index
    {
        public List<IndexAttribute> Attributes { get; set; }
        public string Name { get; set; }
        public Guid Id { get; set; }
        public DateTime Created { get; set; }
        public DateTime Modfied { get; set; }
        public bool IsUnique { get; set; }

        public Index()
        {
            Attributes = new List<IndexAttribute>();
        }

        public void AddAttribute(string name)
        {
            AddAttribute(new IndexAttribute()
            {
                Name = name
            });
        }
        public void AddAttribute(IndexAttribute attribute)
        {
            Attributes.Add(attribute);
        }
    }
}
