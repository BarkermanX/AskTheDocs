
using System.Security.Cryptography;
using System.Text;
using AskTheDocs.Models;
using Qdrant.Client;
using Qdrant.Client.Grpc;
using static Qdrant.Client.Grpc.Conditions;

namespace AskTheDocs.Services;

public class QdrantDocumentStore(
    QdrantClient qdrantClient,
    IConfiguration configuration)
{
    private readonly string _collectionName =
        configuration["Qdrant:CollectionName"]
        ?? throw new InvalidOperationException(
            "Qdrant:CollectionName is not configured.");

   public async Task ReplaceDocumentAsync(
    string documentId,
    IEnumerable<IndexedChunk> chunks)
    {
        var documentChunks = chunks.ToList();

        // An empty replacement means remove the document.
        if (documentChunks.Count == 0)
        {
            await qdrantClient.DeleteAsync(
                collectionName: _collectionName,
                filter: MatchKeyword("documentId", documentId));

            return;
        }

        // Give this indexing operation its own version.
        string versionId = Guid.NewGuid().ToString();

        var points = documentChunks.Select(chunk => new PointStruct
        {
            Id = Guid.NewGuid(),
            Vectors = chunk.Embedding,
            Payload =
        {
            ["documentId"] = chunk.DocumentId,
            ["versionId"] = versionId,
            ["pageNumber"] = chunk.PageNumber,
            ["text"] = chunk.Text
        }
        }).ToList();

        // Upload the complete replacement before deleting the old version.
        await qdrantClient.UpsertAsync(
            collectionName: _collectionName,
            points: points);

        // Delete the previous versions, keeping the newly uploaded version.
        await qdrantClient.DeleteAsync(
            collectionName: _collectionName,
            filter: MatchKeyword("documentId", documentId) &
                    MatchExcept("versionId", new[] { versionId }));
    }

    public async Task<IReadOnlyList<(IndexedChunk Chunk, double Score)>>
        SearchAsync(float[] queryEmbedding, int maxResults = 3)
    {
        if (maxResults < 1)
        {
            return [];
        }

        var points = await qdrantClient.QueryAsync(
            collectionName: _collectionName,
            query: queryEmbedding,
            limit: (ulong)maxResults,
            payloadSelector: true);

        return points.Select(point =>
        {
            var chunk = new IndexedChunk(
                DocumentId: point.Payload["documentId"].StringValue,
                PageNumber: (int)point.Payload["pageNumber"].IntegerValue,
                Text: point.Payload["text"].StringValue,
                Embedding: []);

            return (Chunk: chunk, Score: (double)point.Score);
        }).ToList();
    }

    private static Guid CreatePointId(IndexedChunk chunk)
    {
        string identity =
            $"{chunk.DocumentId}\n{chunk.PageNumber}\n{chunk.Text}";

        byte[] hash = MD5.HashData(Encoding.UTF8.GetBytes(identity));

        return new Guid(hash);
    }



    public async Task<bool> HasIndexedDocumentsAsync()
    {
        var response = await qdrantClient.ScrollAsync(
            collectionName: _collectionName,
            limit: 1,
            payloadSelector: false,
            vectorsSelector: false);

        return response.Result.Count > 0;
    }


    
    public async Task<IReadOnlyList<IndexedChunk>> GetDocumentChunksAsync(
        string documentId)
    {
        var points = await qdrantClient.ScrollAsync(
            collectionName: _collectionName,
            filter: MatchKeyword("documentId", documentId),
            limit: 1000,
            payloadSelector: true,
            vectorsSelector: false);

        return points.Result
            .Select(point => new IndexedChunk(
                DocumentId: point.Payload["documentId"].StringValue,
                PageNumber: (int)point.Payload["pageNumber"].IntegerValue,
                Text: point.Payload["text"].StringValue,
                Embedding: []))
            .ToList();
    }

    public async Task<IReadOnlyList<IndexedChunk>> GetLastPageAsync(
        string documentId)
    {
        var chunks = await GetDocumentChunksAsync(documentId);

        if (chunks.Count == 0)
        {
            return [];
        }

        int lastPage = chunks.Max(chunk => chunk.PageNumber);

        return chunks
            .Where(chunk => chunk.PageNumber == lastPage)
            .ToList();
    }

    public async Task<List<string>> GetIndexedDocumentIdsAsync()
    {
        var documentIds = new HashSet<string>();
        PointId? offset = null;

        while (true)
        {
            var response = await qdrantClient.ScrollAsync(
                collectionName: _collectionName,
                limit: 500,
                offset: offset,
                payloadSelector: true,
                vectorsSelector: false);

            foreach (var point in response.Result)
            {
                if (point.Payload.TryGetValue("documentId", out var value))
                {
                    documentIds.Add(value.StringValue);
                }
            }

            if (response.NextPageOffset is null)
            {
                break;
            }

            offset = response.NextPageOffset;
        }

        return documentIds.OrderBy(id => id).ToList();
    }

    public async Task DeleteDocumentAsync(string documentId)
    {
        await qdrantClient.DeleteAsync(
            collectionName: _collectionName,
            filter: MatchKeyword("documentId", documentId));
    }
}
