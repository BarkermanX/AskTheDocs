using AskTheDocs.Models;
using AskTheDocs.Services;
using Microsoft.AspNetCore.Mvc;

namespace AskTheDocs.Controllers;

[ApiController]
[Route("api/questions")]
public class QuestionsController : ControllerBase
{
    private readonly EmbeddingService _embeddingService;
    private readonly QdrantDocumentStore _documentStore;
    private readonly AnswerGenerationService _answerGenerationService;
    private readonly AddressExtractor _addressExtractor;
    private readonly QueryExpansionService _queryExpansionService;
    private readonly DocumentIndexService _indexService;

    public QuestionsController(
        EmbeddingService embeddingService,
        QdrantDocumentStore documentStore,
        AnswerGenerationService answerGenerationService,
        AddressExtractor addressExtractor,
        QueryExpansionService queryExpansionService,
        DocumentIndexService indexService)
    {
        _embeddingService = embeddingService;
        _documentStore = documentStore;
        _answerGenerationService = answerGenerationService;
        _addressExtractor = addressExtractor;
        _queryExpansionService = queryExpansionService;
        _indexService = indexService;
    }

    [HttpPost]
    public async Task<IActionResult> Ask([FromQuery] string question) 
    {
            if (string.IsNullOrWhiteSpace(question))
            {
                return BadRequest("Question cannot be empty.");
            }

            if (!await _documentStore.HasIndexedDocumentsAsync())
            {
                return Conflict(
                    "No document has been indexed. Index the sample PDF first.");
            }

            // 1. Find passages relevant to the original question.
            float[] initialEmbedding =
                await _embeddingService.GenerateEmbeddingAsync(question);

            var initialResults = await _documentStore.SearchAsync(initialEmbedding, 5);

            // 2. Give the most relevant passages to the query expansion service.
            string documentContext = string.Join(
                "\n\n",
                initialResults.Select(result => result.Chunk.Text));

            var expandedQueries = await _queryExpansionService.ExpandQueryAsync(
                question,
                documentContext);

            // 3. Search using the alternative queries as well.
            var relevantChunks = initialResults.ToList();

            foreach (string expandedQuery in expandedQueries
                .Where(query => !string.IsNullOrWhiteSpace(query))
                .Distinct(StringComparer.OrdinalIgnoreCase))
            {
                float[] queryEmbedding =
                    await _embeddingService.GenerateEmbeddingAsync(expandedQuery);

                var objResults = await _documentStore.SearchAsync(queryEmbedding, 5);

                relevantChunks.AddRange(objResults);
            }

            // 4. Remove duplicate passages and keep the strongest score for each.
            relevantChunks = relevantChunks
                .GroupBy(result => (
                    result.Chunk.DocumentId,
                    result.Chunk.PageNumber,
                    result.Chunk.Text))
                .Select(group => group
                    .OrderByDescending(result => result.Score)
                    .First())
                .OrderByDescending(result => result.Score)
                .Take(10)
                .ToList();

            Console.WriteLine($"Question: {question}");

            foreach (var result in relevantChunks)
            {
                Console.WriteLine(
                    $"Page {result.Chunk.PageNumber}, " +
                    $"Score: {result.Score:F4}, " +
                    $"Text: {result.Chunk.Text[..Math.Min(100, result.Chunk.Text.Length)]}");
            }

            // 5. Preserve the existing deterministic address extraction.
            if (question.Contains("address", StringComparison.OrdinalIgnoreCase))
            {
                var addressPageChunks = new List<IndexedChunk>();

                var documentIds = relevantChunks
                    .Where(result => result.Chunk.Text.Contains(
                        "last page",
                        StringComparison.OrdinalIgnoreCase))
                    .Select(result => result.Chunk.DocumentId)
                    .Distinct();

                foreach (var documentId in documentIds)
                {
                    var lastPageChunks =
                        await _documentStore.GetLastPageAsync(documentId);

                    addressPageChunks.AddRange(lastPageChunks);
                }

                addressPageChunks.AddRange(
                    relevantChunks.Select(result => result.Chunk));

                var distinctAddressChunks = addressPageChunks
                    .DistinctBy(chunk => (
                        chunk.DocumentId,
                        chunk.PageNumber,
                        chunk.Text))
                    .ToList();

                var extractedAddresses = distinctAddressChunks
                    .SelectMany(chunk =>
                        _addressExtractor.ExtractAddresses(chunk.Text)
                            .Select(address => new
                            {
                                chunk.DocumentId,
                                chunk.PageNumber,
                                Address = address
                            }))
                    .ToList();

                if (extractedAddresses.Count > 0)
                {
                    var address = extractedAddresses[0];

                    return Ok(new
                    {
                        question,
                        answer = address.Address +
                            $" [Page {address.PageNumber}]",
                        sources = new[]
                        {
                    new
                    {
                        documentId = address.DocumentId,
                        pageNumber = address.PageNumber,
                        text = address.Address,
                        score = 1.0
                    }
                }
                    });
                }
            }

            // 6. Generate an answer using the retrieved evidence.
            string context = string.Join(
                "\n\n",
                relevantChunks.Select((result, index) =>
                    $"Passage {index + 1} (page {result.Chunk.PageNumber}):\n" +
                    result.Chunk.Text));

            string answer = await _answerGenerationService.GenerateAnswerAsync(
                question,
                context);

            return Ok(new
            {
                question,
                answer,
                sources = relevantChunks.Select(result => new
                {
                    result.Chunk.DocumentId,
                    result.Chunk.PageNumber,
                    result.Chunk.Text,
                    score = Math.Round(result.Score, 4)
                })
            }); 
    }

    [HttpPost("expand")]
    public async Task<IActionResult> Expand([FromQuery] string question)
    {
        if (string.IsNullOrWhiteSpace(question))
            return BadRequest("Question cannot be empty.");

        var queryEmbedding = await _embeddingService.GenerateEmbeddingAsync(question);

        var relevantChunks = _indexService.Search(queryEmbedding, 5);

        var documentContext = string.Join(
            "\n\n",
            relevantChunks.Select(result => result.Chunk.Text));

        var queries = await _queryExpansionService.ExpandQueryAsync(question, documentContext);

        return Ok(new
        {
            question,
            queries
        });
    }
}