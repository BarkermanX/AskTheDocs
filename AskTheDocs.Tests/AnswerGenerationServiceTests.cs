
using System.Net;
using System.Text;
using AskTheDocs.Configuration;
using AskTheDocs.Exceptions;
using AskTheDocs.Services;
using Microsoft.Extensions.Options;

namespace AskTheDocs.Tests;

public class AnswerGenerationServiceTests
{
    [Fact]
    public async Task GenerateAnswerAsync_WhenResponseIsValid_ReturnsAnswer()
    {
        // Arrange
        string json = """
            {
                "message": {
                    "role": "assistant",
                    "content": "The policy provides 28 days of leave. [Page 3]"
                },
                "done": true
            }
            """;

        using var httpClient = CreateHttpClient(
            _ => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    json, Encoding.UTF8, "application/json")
            });

        var service = CreateService(httpClient);

        // Act
        string result = await service.GenerateAnswerAsync(
            "How much leave is provided?",
            "The policy provides 28 days of leave. [Page 3]");

        // Assert
        Assert.Equal(
            "The policy provides 28 days of leave. [Page 3]",
            result);
    }

    [Fact]
    public async Task GenerateAnswerAsync_WhenAnswerIsMissing_ThrowsInvalidOperationException()
    {
        // Arrange
        string json = """{"done": true}""";

        using var httpClient = CreateHttpClient(
            _ => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    json, Encoding.UTF8, "application/json")
            });

        var service = CreateService(httpClient);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.GenerateAnswerAsync("question", "context"));
    }

    [Fact]
    public async Task GenerateAnswerAsync_WhenServerReturnsError_ThrowsHttpRequestException()
    {
        // Arrange
        using var httpClient = CreateHttpClient(
            _ => new HttpResponseMessage(HttpStatusCode.InternalServerError));

        var service = CreateService(httpClient);

        // Act & Assert
        await Assert.ThrowsAsync<HttpRequestException>(
            () => service.GenerateAnswerAsync("question", "context"));
    }

    [Fact]
    public async Task GenerateAnswerAsync_WhenConnectionFails_ThrowsOllamaUnavailableException()
    {
        // Arrange
        using var httpClient = CreateHttpClient(
            _ => throw new HttpRequestException("Connection refused"));

        var service = CreateService(httpClient);

        // Act & Assert
        await Assert.ThrowsAsync<OllamaUnavailableException>(
            () => service.GenerateAnswerAsync("question", "context"));
    }

    private static HttpClient CreateHttpClient(
        Func<HttpRequestMessage, HttpResponseMessage> responder)
    {
        var handler = new FakeHttpMessageHandler(responder);

        return new HttpClient(handler)
        {
            BaseAddress = new Uri("http://localhost")
        };
    }

    private static AnswerGenerationService CreateService(HttpClient httpClient)
    {
        var options = Options.Create(new OllamaOptions
        {
            GenerationModel = "test-model"
        });

        return new AnswerGenerationService(httpClient, options);
    }

    private sealed class FakeHttpMessageHandler(
        Func<HttpRequestMessage, HttpResponseMessage> responder)
        : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(responder(request));
        }
    }
}
