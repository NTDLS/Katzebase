using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NTDLS.Katzebase.Api.Exceptions;

namespace NTDLS.Katzebase.Api.Models
{
    /// <summary>
    /// A document: a JSON object whose top-level properties are the document's fields.
    /// </summary>
    public class KbDocument
    {
        public uint Id { get; set; }
        public DateTime Created { get; set; }
        public DateTime Modified { get; set; }

        /// <summary>
        /// The document as a JSON object.
        /// </summary>
        public string Content { get; set; } = string.Empty;

        public KbDocument()
        {
        }

        /// <summary>
        /// Creates a document from an object (serialized to JSON), or from a string that contains a JSON object.
        /// </summary>
        public KbDocument(object contentObject)
        {
            if (contentObject is string json)
            {
                try
                {
                    if (JToken.Parse(json) is not JObject)
                    {
                        throw new KbInvalidArgumentException("Document content must be a JSON object.");
                    }
                }
                catch (JsonException ex)
                {
                    throw new KbInvalidArgumentException($"Document content is not valid JSON: {ex.Message}");
                }
                Content = json;
            }
            else
            {
                Content = JsonConvert.SerializeObject(contentObject);
            }
        }

        /// <summary>
        /// Deserializes the document's content to an object.
        /// </summary>
        public T? Deserialize<T>()
            => JsonConvert.DeserializeObject<T>(Content);
    }
}
