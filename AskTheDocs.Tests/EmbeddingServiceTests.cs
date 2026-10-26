
using System.Net;
using System.Text;
using AskTheDocs.Configuration;
using AskTheDocs.Exceptions;
using AskTheDocs.Services;
using Microsoft.Extensions.Options;

namespace AskTheDocs.Tests;

public class EmbeddingServiceTests
{
    [Fact]
    public async Task GenerateEmbeddingAsync_WhenResponseIsValid_ReturnsEmbedding()
    {
        // Arrange
        string json = """
            {
                "embeddings": [[0.1, 0.2, 0.3]]
            }
            """;

        var handler = new FakeHttpMessageHandler(
            _ => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            });

        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("http://localhost")
        };

        var options = Options.Create(new OllamaOptions
        {
            EmbeddingModel = "test-model"
        });

        var service = new EmbeddingService(httpClient, options);

        // Act
        float[] result = await service.GenerateEmbeddingAsync("hello");

        // Assert
        Assert.Equal([0.1f, 0.2f, 0.3f], result);
    }

    [Fact]
    public async Task GenerateEmbeddingAsync_WhenResponseContainsNoEmbeddings_ThrowsInvalidOperationException()
    {
        // Arrange
        var handler = new FakeHttpMessageHandler(
            _ => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    """{"embeddings": []}""",
                    Encoding.UTF8,
                    "application/json")
            });

        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("http://localhost")
        };

        var options = Options.Create(new OllamaOptions
        {
            EmbeddingModel = "test-model"
        });

        var service = new EmbeddingService(httpClient, options);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.GenerateEmbeddingAsync("hello"));
    }

    [Fact]
    public async Task GenerateEmbeddingAsync_WhenServerReturnsError_ThrowsHttpRequestException()
    {
        // Arrange
        var handler = new FakeHttpMessageHandler(
            _ => new HttpResponseMessage(HttpStatusCode.InternalServerError));

        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("http://localhost")
        };

        var options = Options.Create(new OllamaOptions
        {
            EmbeddingModel = "test-model"
        });

        var service = new EmbeddingService(httpClient, options);

        // Act & Assert
        await Assert.ThrowsAsync<HttpRequestException>(
            () => service.GenerateEmbeddingAsync("hello"));
    }

    [Fact]
    public async Task GenerateEmbeddingAsync_WhenConnectionFails_ThrowsOllamaUnavailableException()
    {
        // Arrange
        var handler = new FakeHttpMessageHandler(
            _ => throw new HttpRequestException("Connection refused"));

        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("http://localhost")
        };

        var options = Options.Create(new OllamaOptions
        {
            EmbeddingModel = "test-model"
        });

        var service = new EmbeddingService(httpClient, options);

        // Act & Assert
        await Assert.ThrowsAsync<OllamaUnavailableException>(
            () => service.GenerateEmbeddingAsync("hello"));
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
