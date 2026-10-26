using AskTheDocs.Models;
namespace AskTheDocs.Services;

public class DocumentIndexService(
    CosineSimilarityService cosineSimilarityService)
{
    private readonly List<IndexedChunk> _chunks = [];

    public IReadOnlyList<IndexedChunk> Chunks => _chunks;

    public void ReplaceChunks(IEnumerable<IndexedChunk> chunks)
    {
        _chunks.Clear();
        _chunks.AddRange(chunks);
    }

    public IReadOnlyList<(IndexedChunk Chunk, double Score)> Search(
        float[] queryEmbedding,
        int maxResults = 3)
    {
        return _chunks
            .Select(chunk => (
                Chunk: chunk,
                Score: cosineSimilarityService.Calculate(
                        queryEmbedding,
                        chunk.Embedding)))
            .OrderByDescending(result => result.Score)
            .Take(maxResults)
            .ToList();
    }

    public IReadOnlyList<IndexedChunk> GetPage(
    string documentId,
    int pageNumber)
    {
        return _chunks
            .Where(chunk =>
                chunk.DocumentId == documentId &&
                chunk.PageNumber == pageNumber)
            .ToList();
    }

    public IReadOnlyList<IndexedChunk> GetLastPage(string documentId)
    {
        int intLastPage = _chunks
            .Where(chunk => chunk.DocumentId == documentId)
            .Select(chunk => chunk.PageNumber)
            .DefaultIfEmpty()
            .Max();

        if (intLastPage == 0)
            return [];

        return GetPage(documentId, intLastPage);
    }

    public IReadOnlyList<(IndexedChunk Chunk, double Score)> SearchByKeywords(
        IEnumerable<string> keywords,
        int intMaxResults = 5)
    {
        var arrKeywords = keywords
            .Where(keyword => !string.IsNullOrWhiteSpace(keyword))
            .Select(keyword => keyword.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (arrKeywords.Length == 0 || intMaxResults < 1)
            return [];

        return _chunks
            .Select(chunk =>
            {
                int intMatches = arrKeywords.Count(keyword =>
                    chunk.Text.Contains(
                        keyword,
                        StringComparison.OrdinalIgnoreCase));

                return (Chunk: chunk, Score: (double)intMatches);
            })
            .Where(result => result.Score > 0)
            .OrderByDescending(result => result.Score)
            .Take(intMaxResults)
            .ToList();
    }
}
