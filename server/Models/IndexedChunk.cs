namespace AskTheDocs.Models;

public record IndexedChunk(
    string DocumentId,
    int PageNumber,
    string Text,
    float[] Embedding);