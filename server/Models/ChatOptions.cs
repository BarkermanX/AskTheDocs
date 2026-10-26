using System.Text.Json.Serialization;

namespace AskTheDocs.Api.Models
{
    public record ChatOptions(
        [property: JsonPropertyName("num_predict")] int NumPredict,
        [property: JsonPropertyName("format")] string? Format = null
        );
}
