using AskTheDocs.Api.Models;
using AskTheDocs.Services;

namespace AskTheDocs.Tests;

public class TextChunkerTests
{
    [Fact]
    public void ChunkPages_WhenTextFitsInOneChunk_ReturnsSingleChunk()
    {
        // Arrange
        var chunker = new TextChunker();

        var pages = new List<ExtractedPage>
        {
            new(1, "This is the first sentence. This is the second sentence.")
        };

        // Act
        var results = chunker.ChunkPages(pages);

        // Assert
        var chunk = Assert.Single(results);

        Assert.Equal(1, chunk.PageNumber);
        Assert.Equal(
            "This is the first sentence. This is the second sentence.",
            chunk.Text);
    }

    [Fact]
    public void ChunkPages_WhenTextExceedsChunkSize_SplitsAtSentenceBoundaries()
    {
        // Arrange
        var chunker = new TextChunker();

        var pages = new List<ExtractedPage>
    {
        new(1, "First sentence. Second sentence. Third sentence.")
    };

        // Act
        var results = chunker.ChunkPages(pages, intChunkSize: 25);

        // Assert
        Assert.Equal(3, results.Count);

        Assert.Equal("First sentence.", results[0].Text);
        Assert.Equal("Second sentence.", results[1].Text);
        Assert.Equal("Third sentence.", results[2].Text);

        Assert.All(results, chunk => Assert.Equal(1, chunk.PageNumber));
    }

    [Fact]
    public void ChunkPages_WhenPageIsBlank_DoesNotCreateChunks()
    {
        // Arrange
        var chunker = new TextChunker();

        var pages = new List<ExtractedPage>
    {
        new(1, ""),
        new(2, "   \r\n   "),
        new(3, "Useful document content.")
    };

        // Act
        var results = chunker.ChunkPages(pages);

        // Assert
        var chunk = Assert.Single(results);

        Assert.Equal(3, chunk.PageNumber);
        Assert.Equal("Useful document content.", chunk.Text);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void ChunkPages_WhenChunkSizeIsNotPositive_ThrowsArgumentOutOfRangeException(
    int chunkSize)
    {
        // Arrange
        var chunker = new TextChunker();

        var pages = new List<ExtractedPage>
    {
        new(1, "Some document content.")
    };

        // Act
        var exception = Assert.Throws<ArgumentOutOfRangeException>(
            () => chunker.ChunkPages(pages, chunkSize));

        // Assert
        Assert.Equal("intChunkSize", exception.ParamName);
    }

    [Fact]
    public void ChunkPages_WhenMultiplePagesAreProvided_KeepsChunksOnTheirOriginalPages()
    {
        // Arrange
        var chunker = new TextChunker();

        var pages = new List<ExtractedPage>
    {
        new(1, "Content from page one."),
        new(2, "Content from page two.")
    };

        // Act
        var results = chunker.ChunkPages(pages);

        // Assert
        Assert.Equal(2, results.Count);

        Assert.Equal(1, results[0].PageNumber);
        Assert.Equal("Content from page one.", results[0].Text);

        Assert.Equal(2, results[1].PageNumber);
        Assert.Equal("Content from page two.", results[1].Text);
    }

    [Fact]
    public void ChunkPages_WhenNoPagesAreProvided_ReturnsEmptyList()
    {
        // Arrange
        var chunker = new TextChunker();
        var pages = new List<ExtractedPage>();

        // Act
        var results = chunker.ChunkPages(pages);

        // Assert
        Assert.Empty(results);
    }
}