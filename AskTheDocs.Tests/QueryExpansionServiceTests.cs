
using System.Net.Http.Json;
using Microsoft.Extensions.Options;
using AskTheDocs.Configuration;
using AskTheDocs.Api.Models;
using AskTheDocs.Exceptions;

namespace AskTheDocs.Services;

public class QueryExpansionService(
    HttpClient httpClient,
    IOptions<OllamaOptions> ollamaOptions)
{
    public async Task<List<string>> ExpandQueryAsync(
        string question,
        string documentContext)
    {
        var objRequest = new ChatRequest(
            ollamaOptions.Value.GenerationModel,
            [
                new ChatMessage(
                    "system",
                    """
                    Generate three alternative search queries that may help find passages
                    in the supplied document to answer the user's question.

                    Use the document context to understand the subject and terminology.
                    Preserve the meaning and domain of the original question.
                    Do not reinterpret abbreviations using meanings unrelated to the document.
                    Do not introduce organisations, products, or claim types unsupported
                    by the document context.
                    Do not answer the question.

                    Return only a JSON array containing exactly three strings.
                    """
                ),
                new ChatMessage(
                    "user",
                    $"""
                    DOCUMENT CONTEXT:
                    {documentContext}

                    QUESTION:
                    {question}
                    """
                )
            ],
            false,
            false,
            new ChatOptions(NumPredict: 150));

        try
        {
            using var objResponse = await httpClient.PostAsJsonAsync(
                "api/chat",
                objRequest);

            objResponse.EnsureSuccessStatusCode();

            var objResult = await objResponse.Content
                .ReadFromJsonAsync<ChatResponse>();

            string strContent = objResult?.Message?.Content
                ?? throw new InvalidOperationException(
                    "Ollama returned no query expansions.");

            return System.Text.Json.JsonSerializer
                .Deserialize<List<string>>(strContent)
                ?? [];
        }
        catch (HttpRequestException ex) when (ex.StatusCode is null)
        {
            throw new OllamaUnavailableException(
                "Unable to connect to Ollama.",
                ex);
        }
        catch (TaskCanceledException ex)
        {
            throw new TimeoutException(
                "The Ollama request timed out.",
                ex);
        }
    }
}
