using AskTheDocs.Services;
using AskTheDocs.Models;

namespace AskTheDocs.Tests;

public class DocumentIndexServiceTests
{
    [Fact]
    public void Search_WhenIndexIsEmpty_ReturnsNoResults()
    {
        // Arrange
        var cosineSimilarityService = new CosineSimilarityService();
        var indexService = new DocumentIndexService(cosineSimilarityService);
        var queryEmbedding = new float[] { 1.0f, 0.0f, 0.0f };

        // Act
        var results = indexService.Search(queryEmbedding);

        // Assert
        Assert.Empty(results);
    }

    [Fact]
    public void Search_WhenChunksHaveDifferentSimilarities_ReturnsMostSimilarFirst()
    {
        // Arrange
        var cosineSimilarityService = new CosineSimilarityService();
        var indexService = new DocumentIndexService(cosineSimilarityService);

        var queryEmbedding = new float[] { 1.0f, 0.0f };

        var lessRelevantChunk = new IndexedChunk(
            "document-1",
            1,
            "Less relevant content",
            new float[] { 0.0f, 1.0f });

        var mostRelevantChunk = new IndexedChunk(
            "document-1",
            2,
            "Most relevant content",
            new float[] { 0.9f, 0.1f });

        indexService.ReplaceChunks(
            [lessRelevantChunk, mostRelevantChunk]);

        // Act
        var results = indexService.Search(queryEmbedding);

        // Assert
        Assert.Equal(2, results.Count);
        Assert.Equal(mostRelevantChunk, results[0].Chunk);
        Assert.Equal(lessRelevantChunk, results[1].Chunk);
        Assert.True(results[0].Score > results[1].Score);
    }

    [Fact]
    public void Search_WhenMaxResultsIsTwo_ReturnsOnlyTwoResults()
    {
        // Arrange
        var cosineSimilarityService = new CosineSimilarityService();
        var indexService = new DocumentIndexService(cosineSimilarityService);

        var queryEmbedding = new float[] { 1.0f, 0.0f };

        var chunks = new[]
        {
        new IndexedChunk(
            "document-1", 1, "First chunk",
            new float[] { 1.0f, 0.0f }),

        new IndexedChunk(
            "document-1", 2, "Second chunk",
            new float[] { 0.9f, 0.1f }),

        new IndexedChunk(
            "document-1", 3, "Third chunk",
            new float[] { 0.8f, 0.2f })
    };

        indexService.ReplaceChunks(chunks);

        // Act
        var results = indexService.Search(queryEmbedding, maxResults: 2);

        // Assert
        Assert.Equal(2, results.Count);
    }

    [Fact]
    public void ReplaceChunks_WhenCalledAgain_ReplacesExistingChunks()
    {
        // Arrange
        var cosineSimilarityService = new CosineSimilarityService();
        var indexService = new DocumentIndexService(cosineSimilarityService);

        var oldChunk = new IndexedChunk(
            "document-1",
            1,
            "Old document content",
            new float[] { 1.0f, 0.0f });

        var newChunk = new IndexedChunk(
            "document-2",
            1,
            "New document content",
            new float[] { 0.0f, 1.0f });

        indexService.ReplaceChunks([oldChunk]);

        // Act
        indexService.ReplaceChunks([newChunk]);

        // Assert
        Assert.Single(indexService.Chunks);
        Assert.Equal("New document content", indexService.Chunks[0].Text);
        Assert.DoesNotContain(
            indexService.Chunks,
            chunk => chunk.Text == "Old document content");
    }

    [Fact]
    public void SearchByKeywords_MatchesTextIgnoringCase()
    {
        // Arrange
        var cosineSimilarityService = new CosineSimilarityService();
        var indexService = new DocumentIndexService(cosineSimilarityService);

        var matchingChunk = new IndexedChunk(
            "document-1",
            1,
            "The application supports Occupational Health services.",
            new float[] { 1.0f, 0.0f });

        var nonMatchingChunk = new IndexedChunk(
            "document-1",
            2,
            "This section covers annual leave.",
            new float[] { 0.0f, 1.0f });

        indexService.ReplaceChunks([matchingChunk, nonMatchingChunk]);

        // Act
        var results = indexService.SearchByKeywords(
            ["occupational", "HEALTH"]);

        // Assert
        Assert.Single(results);
        Assert.Equal(matchingChunk, results[0].Chunk);
        Assert.Equal(2.0, results[0].Score);
    }

    [Fact]
    public void SearchByKeywords_WhenKeywordsAreEmptyOrBlank_ReturnsNoResults()
    {
        // Arrange
        var cosineSimilarityService = new CosineSimilarityService();
        var indexService = new DocumentIndexService(cosineSimilarityService);

        var chunk = new IndexedChunk(
            "document-1",
            1,
            "Information about occupational health.",
            new float[] { 1.0f, 0.0f });

        indexService.ReplaceChunks([chunk]);

        // Act and Assert
        Assert.Empty(indexService.SearchByKeywords(Array.Empty<string>()));
        Assert.Empty(indexService.SearchByKeywords([""]));
        Assert.Empty(indexService.SearchByKeywords([" ", "   "]));
    }

    [Fact]
    public void SearchByKeywords_WhenMaxResultsIsOne_ReturnsOnlyOneResult()
    {
        // Arrange
        var cosineSimilarityService = new CosineSimilarityService();
        var indexService = new DocumentIndexService(cosineSimilarityService);

        var chunks = new[]
        {
        new IndexedChunk(
            "document-1",
            1,
            "Information about occupational health",
            new float[] { 1.0f, 0.0f }),

        new IndexedChunk(
            "document-1",
            2,
            "Occupational health services and advice",
            new float[] { 0.9f, 0.1f })
    };

        indexService.ReplaceChunks(chunks);

        // Act
        var results = indexService.SearchByKeywords(
            ["occupational", "health"],
            intMaxResults: 1);

        // Assert
        Assert.Single(results);
        Assert.Equal(2.0, results[0].Score);
    }

    [Fact]
    public void GetPage_ReturnsOnlyChunksFromRequestedDocumentAndPage()
    {
        // Arrange
        var cosineSimilarityService = new CosineSimilarityService();
        var indexService = new DocumentIndexService(cosineSimilarityService);

        var expectedChunk = new IndexedChunk(
            "document-1", 2, "Expected page content",
            new float[] { 1.0f, 0.0f });

        var chunks = new[]
        {
        new IndexedChunk(
            "document-1", 1, "Page one content",
            new float[] { 1.0f, 0.0f }),

        expectedChunk,

        new IndexedChunk(
            "document-2", 2, "Different document",
            new float[] { 0.0f, 1.0f })
    };

        indexService.ReplaceChunks(chunks);

        // Act
        var results = indexService.GetPage("document-1", 2);

        // Assert
        var result = Assert.Single(results);
        Assert.Equal(expectedChunk, result);
    }

    [Fact]
    public void GetLastPage_ReturnsChunksFromHighestPageNumber()
    {
        // Arrange
        var indexService = new DocumentIndexService(
            new CosineSimilarityService());

        var chunks = new[]
        {
        new IndexedChunk(
            "document-1", 1, "First page",
            new float[] { 1.0f, 0.0f }),

        new IndexedChunk(
            "document-1", 3, "Last page chunk one",
            new float[] { 0.9f, 0.1f }),

        new IndexedChunk(
            "document-1", 2, "Middle page",
            new float[] { 0.8f, 0.2f }),

        new IndexedChunk(
            "document-1", 3, "Last page chunk two",
            new float[] { 0.7f, 0.3f }),

        new IndexedChunk(
            "document-2", 5, "Another document",
            new float[] { 0.0f, 1.0f })
    };

        indexService.ReplaceChunks(chunks);

        // Act
        var results = indexService.GetLastPage("document-1");

        // Assert
        Assert.Equal(2, results.Count);
        Assert.All(results, chunk => Assert.Equal(3, chunk.PageNumber));
        Assert.Contains(results, chunk => chunk.Text == "Last page chunk one");
        Assert.Contains(results, chunk => chunk.Text == "Last page chunk two");
    }

    [Fact]
    public void GetLastPage_WhenDocumentDoesNotExist_ReturnsNoResults()
    {
        // Arrange
        var indexService = new DocumentIndexService(
            new CosineSimilarityService());

        var chunk = new IndexedChunk(
            "document-1",
            1,
            "Some document content",
            new float[] { 1.0f, 0.0f });

        indexService.ReplaceChunks([chunk]);

        // Act
        var results = indexService.GetLastPage("document-999");

        // Assert
        Assert.Empty(results);
    }


    [Fact]
    public void Search_WhenMaxResultsIsZero_ReturnsNoResults()
    {
        // Arrange
        var indexService = new DocumentIndexService(
            new CosineSimilarityService());

        indexService.ReplaceChunks(
        [
            new IndexedChunk("doc1", 1, "Example text", [1f, 0f])
        ]);

        // Act
        var results = indexService.Search([1f, 0f], maxResults: 0);

        // Assert
        Assert.Empty(results);
    }


    [Fact]
    public void Search_WhenMaxResultsIsNegative_ReturnsNoResults()
    {
        // Arrange
        var indexService = new DocumentIndexService(
            new CosineSimilarityService());

        indexService.ReplaceChunks(
        [
            new IndexedChunk("doc1", 1, "Example text", [1f, 0f])
        ]);

        // Act
        var results = indexService.Search([1f, 0f], maxResults: -1);

        // Assert
        Assert.Empty(results);
    }


    [Fact]
    public void Search_WhenQueryEmbeddingIsEmpty_ThrowsArgumentException()
    {
        // Arrange
        var indexService = new DocumentIndexService(
            new CosineSimilarityService());

        indexService.ReplaceChunks(
        [
            new IndexedChunk("doc1", 1, "Example text", [1f, 0f])
        ]);

        // Act & Assert
        Assert.Throws<ArgumentException>(
            () => indexService.Search([]));
    }

}