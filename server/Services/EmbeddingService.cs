
using AskTheDocs.Api.Models;
using AskTheDocs.Configuration;
using Microsoft.Extensions.Options;
using AskTheDocs.Exceptions;

namespace AskTheDocs.Services;

public class EmbeddingService(HttpClient httpClient, IOptions<OllamaOptions> ollamaOptions)
{
    public async Task<float[]> GenerateEmbeddingAsync(string strText)
    {
        var objRequest = new EmbeddingRequest(
            ollamaOptions.Value.EmbeddingModel,
            strText);

        try
        {
            using var objResponse = await httpClient.PostAsJsonAsync(
                "api/embed",
                objRequest);

            objResponse.EnsureSuccessStatusCode();

            var objResult = await objResponse.Content
                .ReadFromJsonAsync<EmbeddingResponse>();

            if (objResult?.Embeddings is not { Length: > 0 })
            {
                throw new InvalidOperationException(
                    "Ollama returned no embedding.");
            }

            return objResult.Embeddings[0];
        }
        catch (HttpRequestException ex) when (ex.StatusCode is null)
        {
            throw new OllamaUnavailableException(
                "Unable to connect to Ollama.", ex);
        }
        catch (TaskCanceledException ex)
        {
            throw new TimeoutException(
                "The Ollama request timed out.", ex);
        }
    }
}
