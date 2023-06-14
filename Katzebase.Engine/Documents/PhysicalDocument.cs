using Katzebase.PublicLibrary.Payloads;

namespace Katzebase.Engine.Documents
{
    [Serializable]
    public class PhysicalDocument
    {
        public string? Content { get; set; }
        public Guid? Id { get; set; }
        public DateTime? Created { get; set; }
        public DateTime? Modfied { get; set; }
        public PhysicalDocument Clone()
        {
            return new PhysicalDocument
            {
                Id = Id,
                Content = Content,
                Created = Created,
                Modfied = Modfied
            };
        }

        static public PhysicalDocument FromPayload(KbDocument document)
        {
            return new PhysicalDocument()
            {
                Id = document.Id,
                Created = document.Created,
                Modfied = document.Modfied,
                Content = document.Content
            };
        }
    }
}
