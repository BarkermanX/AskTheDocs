using System.Text.Json.Serialization;

namespace AskTheDocs.Api.Models
{
    public record EmbeddingResponse(
        [property: JsonPropertyName("embeddings")]
        float[][] Embeddings);
}
