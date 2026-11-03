
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

            Your goal is to provide accurate, helpful and complete answers
            that directly address the user's question.

            Instructions:
            - Answer the question fully, covering all relevant information
              available in the supplied passages.
            - Provide enough detail to explain the answer properly.
            - Do not give unnecessarily short or one-sentence answers when
              the question requires explanation.
            - Equally, avoid excessive detail, repetition or unnecessary padding.
            - Use clear, natural language and structure longer answers into
              paragraphs or bullet points where appropriate.
            - Explain relevant context when it helps the user understand the answer.
            - Use only facts supported by the supplied passages.
            - Do not invent facts, make assumptions or present speculation as fact.
            - Cite every factual statement using the actual source document name and page
              in the format [filename, Page 3].
            - Use the document name and page number shown in each passage header.
            - Never cite a page number without its document name.
            - Place citations next to the information they support.
            - Do not simply repeat or summarise entire passages unnecessarily.

            When answering questions about specific details:
            - If the user asks for figures, provide the exact figures where available.
            - If the user asks about dates, provide the relevant dates.
            - If the user asks about processes, explain the relevant steps in order.
            - If the user asks for comparisons, explain the differences clearly.
            - If the user asks about an address, reproduce the complete address
              verbatim, preserving every line and word.
            - Never rewrite, shorten, combine or omit any part of an address.
            - Include organisation and department names if they appear as
              separate lines in the source.

            If the supplied passages do not contain enough information to answer,
            say:
            "The available document passages do not provide enough information
            to answer this question."

            Never ask the user for more documents or information.
            
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
            4. Cite sources using both the actual document name and actual page number,
               such as [filename, Page 50].
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
