using AskTheDocs.Models;
using AskTheDocs.Services;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics.Metrics;
using System.IO;

namespace AskTheDocs.Controllers;

[ApiController]
[Route("api/documents")]
public class DocumentsController : ControllerBase
{
    private readonly PdfTextExtractor _extractor;
    private readonly TextChunker _chunker;
    private readonly EmbeddingService _embeddingService;
    private readonly QdrantDocumentStore _documentStore;
    private readonly IWebHostEnvironment _environment;

    public DocumentsController(
        PdfTextExtractor extractor,
        TextChunker chunker,
        EmbeddingService embeddingService,
        QdrantDocumentStore documentStore,
        IWebHostEnvironment environment)
    {
        _extractor = extractor;
        _chunker = chunker;
        _embeddingService = embeddingService;
        _documentStore = documentStore;
        _environment = environment;
    }

    [HttpPost("index")]
    public async Task<IActionResult> Index()
    {
        string[] astrFileNames = [
            "pip2-form-and-information-booklet.pdf",
            "PIP assessment guide part 1 - the assessment process - GOV.UK.pdf",
            "PIP assessment guide part 2 - the assessment criteria - GOV.UK.pdf",
            "PIP assessment guide part 3 - health professional performance - GOV.UK.pdf",
            ];

        int intTotalIndexedChunks = 0;

        foreach(string strFileName in astrFileNames) {
            var filePath = Path.Combine(
                _environment.ContentRootPath,
                "DocumentsToRead",
                strFileName);

            if (!System.IO.File.Exists(filePath))
            {
                return NotFound("Document not found.");
            }

            var pages = _extractor.ExtractPages(filePath);
            var chunks = _chunker.ChunkPages(pages);

            var indexedChunks = new List<IndexedChunk>();

            foreach (var chunk in chunks)
            {
                float[] embedding =
                    await _embeddingService.GenerateEmbeddingAsync(chunk.Text);

                indexedChunks.Add(new IndexedChunk(
                    DocumentId: strFileName,
                    chunk.PageNumber,
                    chunk.Text,
                    embedding));
            }

            await _documentStore.ReplaceDocumentAsync(
                strFileName,
                indexedChunks);
            
            // Increment total chunks indexed
            intTotalIndexedChunks += indexedChunks.Count;
        }

        return Ok(new
        {
            document = astrFileNames,
            indexedChunkCount = intTotalIndexedChunks
        });
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var documentIds =
            await _documentStore.GetIndexedDocumentIdsAsync();

        return Ok(documentIds.Select(id => new
        {
            id,
            fileName = id
        }));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        var documentIds =
            await _documentStore.GetIndexedDocumentIdsAsync();

        if (!documentIds.Contains(id, StringComparer.OrdinalIgnoreCase))
        {
            return NotFound("Document not found.");
        }

        await _documentStore.DeleteDocumentAsync(id);

        return NoContent();
    }
}