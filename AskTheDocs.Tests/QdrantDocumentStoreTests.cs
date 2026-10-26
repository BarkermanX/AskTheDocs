
using AskTheDocs.Models;
using AskTheDocs.Services;
using Microsoft.Extensions.Configuration;
using Qdrant.Client;

namespace AskTheDocs.Tests;

public class QdrantDocumentStoreTests
{
    [Fact]
    public async Task ReplaceDocumentAsync_WhenChunkIsStored_CanBeFoundBySearch()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Qdrant:CollectionName"] = "ask-the-docs"
            })
            .Build();

        using var client = new QdrantClient("localhost", 6334);

        var store = new QdrantDocumentStore(client, configuration);

        const string documentId = "qdrant-integration-test.pdf";

        var chunk = new IndexedChunk(
            documentId,
            1,
            "Qdrant integration test content",
            CreateVector(1f, 0f));

        try
        {
            await store.ReplaceDocumentAsync(documentId, [chunk]);

            var results = await store.SearchAsync(
                CreateVector(1f, 0f),
                maxResults: 5);

            Assert.Contains(results, result =>
                result.Chunk.DocumentId == documentId &&
                result.Chunk.PageNumber == 1 &&
                result.Chunk.Text == chunk.Text);
        }
        finally
        {
            await store.ReplaceDocumentAsync(documentId, []);
        }
    }

    private static float[] CreateVector(float first, float second)
    {
        // Qdrant's collection is configured for 768 dimensions.
        var vector = new float[768];
        vector[0] = first;
        vector[1] = second;
        return vector;
    }
}
