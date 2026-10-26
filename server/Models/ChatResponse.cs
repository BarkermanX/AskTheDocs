using System.Text.Json.Serialization;

namespace AskTheDocs.Api.Models
{
    public record ChatResponse(
        [property: JsonPropertyName("message")]
        ChatMessage? Message);
}
