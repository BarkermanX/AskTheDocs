
using AskTheDocs.Services;

namespace AskTheDocs.Tests;

public class PdfTextExtractorTests
{
    [Fact]
    public void ExtractPages_WhenPdfHasTwoPages_ReturnsBothPagesWithCorrectText()
    {
        // Arrange
        var extractor = new PdfTextExtractor();

        string filePath = Path.Combine(
            AppContext.BaseDirectory,
            "Documents",
            "TestDocument.pdf");

        // Act
        var pages = extractor.ExtractPages(filePath);

        // Assert
        Assert.Equal(2, pages.Count);

        Assert.Equal(1, pages[0].PageNumber);
        Assert.Contains("Right to Cancel", pages[0].Text);

        Assert.Equal(2, pages[1].PageNumber);
        Assert.Contains("Customer Address", pages[1].Text);
    }


    [Fact]
    public void ExtractText_WhenPdfHasTwoPages_ReturnsTextFromBothPages()
    {
        // Arrange
        var extractor = new PdfTextExtractor();

        string filePath = Path.Combine(
            AppContext.BaseDirectory,
            "Documents",
            "TestDocument.pdf");

        // Act
        string text = extractor.ExtractText(filePath);

        // Assert
        Assert.Contains("Right to Cancel", text);
        Assert.Contains("Customer Address", text);
    }

    [Fact]
    public void ExtractPages_WhenFileDoesNotExist_ThrowsException()
    {
        // Arrange
        var extractor = new PdfTextExtractor();

        string filePath = Path.Combine(
            AppContext.BaseDirectory,
            "Documents",
            "DoesNotExist.pdf");

        // Act & Assert
        Assert.ThrowsAny<Exception>(
            () => extractor.ExtractPages(filePath));
    }
}