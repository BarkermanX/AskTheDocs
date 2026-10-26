using AskTheDocs.Services;

namespace AskTheDocs.Tests;

public class AddressExtractorTests
{
    [Fact]
    public void ExtractAddresses_WhenTextContainsUkAddress_ReturnsAddress()
    {
        // Arrange
        var extractor = new AddressExtractor();
        string text = "Example Company\n10 High Street\nDerby\nDE1 1AA";

        // Act
        List<string> addresses = extractor.ExtractAddresses(text);

        // Assert
        Assert.Single(addresses);
        Assert.Contains("10 High Street", addresses[0]);
        Assert.Contains("Derby", addresses[0]);
        Assert.Contains("DE1 1AA", addresses[0]);
    }

    [Fact]
    public void ExtractAddresses_WhenTextIsNullEmptyOrWhitespace_ReturnsNoAddresses()
    {
        // Arrange
        var extractor = new AddressExtractor();

        // Act & Assert
        Assert.Empty(extractor.ExtractAddresses(null!));
        Assert.Empty(extractor.ExtractAddresses(string.Empty));
        Assert.Empty(extractor.ExtractAddresses("   \r\n  "));
    }

    [Fact]
    public void ExtractAddresses_WhenTextHasNoUkPostcode_ReturnsNoAddresses()
    {
        // Arrange
        var extractor = new AddressExtractor();
        string text = "Example Company\n10 High Street\nDerby";

        // Act
        List<string> addresses = extractor.ExtractAddresses(text);

        // Assert
        Assert.Empty(addresses);
    }

    [Fact]
    public void ExtractAddresses_WhenAddressUsesWindowsLineEndings_ReturnsAddress()
    {
        // Arrange
        var extractor = new AddressExtractor();
        string text = "Example Company\r\n10 High Street\r\nDerby\r\nDE1 1AA";

        // Act
        List<string> addresses = extractor.ExtractAddresses(text);

        // Assert
        Assert.Single(addresses);
        Assert.Contains("10 High Street", addresses[0]);
        Assert.Contains("DE1 1AA", addresses[0]);
    }

    [Fact]
    public void ExtractAddresses_WhenTextContainsTwoAddresses_ReturnsBothAddresses()
    {
        // Arrange
        var extractor = new AddressExtractor();
        string text = "First Company\n1 High Street\nDerby\nDE1 1AA\n\n" +
                      "Second Company\n2 Market Street\nNottingham\nNG1 1AA";

        // Act
        List<string> addresses = extractor.ExtractAddresses(text);

        // Assert
        Assert.Equal(2, addresses.Count);
        Assert.Contains(addresses, address => address.Contains("DE1 1AA"));
        Assert.Contains(addresses, address => address.Contains("NG1 1AA"));
    }

    [Fact]
    public void ExtractAddresses_WhenAddressIsRepeated_ReturnsItOnlyOnce()
    {
        // Arrange
        var extractor = new AddressExtractor();
        string address = "Example Company\n10 High Street\nDerby\nDE1 1AA";
        string text = address + "\n\n" + address;

        // Act
        List<string> addresses = extractor.ExtractAddresses(text);

        // Assert
        Assert.Single(addresses);
    }

    [Fact]
    public void ExtractAddresses_WhenBlankLinePrecedesAddress_DoesNotIncludeEarlierText()
    {
        // Arrange
        var extractor = new AddressExtractor();
        string text = "Unrelated document text\n\nExample Company\n10 High Street\nDerby\nDE1 1AA";

        // Act
        List<string> addresses = extractor.ExtractAddresses(text);

        // Assert
        Assert.Single(addresses);
        Assert.DoesNotContain("Unrelated document text", addresses[0]);
    }

    [Fact]
    public void ExtractAddresses_WhenPageMarkerPrecedesAddress_DoesNotIncludePageMarker()
    {
        // Arrange
        var extractor = new AddressExtractor();
        string text = "Page 2 of 4\nExample Company\n10 High Street\nDerby\nDE1 1AA";

        // Act
        List<string> addresses = extractor.ExtractAddresses(text);

        // Assert
        Assert.Single(addresses);
        Assert.DoesNotContain("Page 2 of 4", addresses[0]);
        Assert.Contains("DE1 1AA", addresses[0]);
    }

    [Fact]
    public void ExtractAddresses_WhenSentencePrecedesAddress_DoesNotIncludeSentence()
    {
        // Arrange
        var extractor = new AddressExtractor();
        string text = "Please send all correspondence to the following address.\n" +
                      "Example Company\n10 High Street\nDerby\nDE1 1AA";

        // Act
        List<string> addresses = extractor.ExtractAddresses(text);

        // Assert
        Assert.Single(addresses);
        Assert.DoesNotContain("Please send all correspondence", addresses[0]);
        Assert.Contains("DE1 1AA", addresses[0]);
    }
}
