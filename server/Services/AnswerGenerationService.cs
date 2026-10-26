
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using AskTheDocs.Configuration;
using AskTheDocs.Api.Models;
using AskTheDocs.Exceptions;

namespace AskTheDocs.Services;

public class AnswerGenerationService(
    HttpClient httpClient,
    Microsoft.Extensions.Options.IOptions<OllamaOptions> ollamaOptions)
{
    public async Task<string> GenerateAnswerAsync(
        string strQuestion,
        string strContext)
    {

        var objRequest = new ChatRequest(
            ollamaOptions.Value.GenerationModel,
            [
                new ChatMessage(
            "system",
            """
            You are Ask the Docs, an assistant that answers questions
            using supplied document passages.

            Instructions:
            - Answer the question directly and concisely.
            - Use only facts supported by the supplied passages.
            - Include page citations in the format [Page 3].
            - Do not repeat or summarise the passages unnecessarily.
            - Do not invent facts.
            - If the passages do not contain the answer, say:
              "The available document passages do not provide enough
              information to answer this question."
            - When a passage contains a postal address, copy the complete
              address verbatim, preserving every line and word.
            - Do not rewrite, shorten, combine, or omit any part of the address.
            - Include the organisation or department name if it appears
              as a separate line in the source.
            """),

        new ChatMessage(
            "user",
            $"""
            DOCUMENT PASSAGES:
            {strContext}

            QUESTION:
            {strQuestion}

            Answer the question using the document passages.

            Important instructions:
            1. Find the passage that directly answers the question.
            2. If the question asks for an address, reproduce the complete
               address exactly as written in the passages.
            3. If the user asks for figures give accurate figures if possible.
            4. Include the relevant page citation, such as [Page 50].
            5. Do not substitute general instructions for a specific answer
               when the passages contain that answer.
            6. Do not ask for more documents or information.
            7. When answering questions about an address, include every line
              of the address exactly as shown in the source passage.
            8. Never omit parts of an address, including freepost codes,
              organisation names, department names or postal codes.

            """)
            ],
            false,
            false,
            new ChatOptions(NumPredict: 256));        

        try
        {
            using var objResponse = await httpClient.PostAsJsonAsync(
            "api/chat",
            objRequest);

            objResponse.EnsureSuccessStatusCode();

            var objResult = await objResponse.Content
                .ReadFromJsonAsync<ChatResponse>();

            return objResult?.Message?.Content
                ?? throw new InvalidOperationException(
                    "Ollama returned no answer.");
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
