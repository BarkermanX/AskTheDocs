
using AskTheDocs.Services;

namespace AskTheDocs.Tests;

public class CosineSimilarityServiceTests
{
    private readonly CosineSimilarityService _service = new();

    [Fact]
    public void Calculate_WhenVectorsAreIdentical_ReturnsOne()
    {
        // Arrange
        float[] vectorA = [1, 2, 3];
        float[] vectorB = [1, 2, 3];

        // Act
        double result = _service.Calculate(vectorA, vectorB);

        // Assert
        Assert.Equal(1.0, result, 10);
    }

    [Fact]
    public void Calculate_WhenVectorsAreOrthogonal_ReturnsZero()
    {
        // Arrange
        float[] vectorA = [1, 0];
        float[] vectorB = [0, 1];

        // Act
        double result = _service.Calculate(vectorA, vectorB);

        // Assert
        Assert.Equal(0.0, result, 10);
    }

    [Fact]
    public void Calculate_WhenVectorsPointInOppositeDirections_ReturnsNegativeOne()
    {
        // Arrange
        float[] vectorA = [1, 2];
        float[] vectorB = [-1, -2];

        // Act
        double result = _service.Calculate(vectorA, vectorB);

        // Assert
        Assert.Equal(-1.0, result, 10);
    }

    [Fact]
    public void Calculate_WhenVectorsHaveDifferentLengths_ThrowsArgumentException()
    {
        // Arrange
        float[] vectorA = [1, 2];
        float[] vectorB = [1, 2, 3];

        // Act & Assert
        Assert.Throws<ArgumentException>(
            () => _service.Calculate(vectorA, vectorB));
    }

    [Fact]
    public void Calculate_WhenFirstVectorIsZero_ThrowsArgumentException()
    {
        // Arrange
        float[] vectorA = [0, 0];
        float[] vectorB = [1, 2];

        // Act & Assert
        Assert.Throws<ArgumentException>(
            () => _service.Calculate(vectorA, vectorB));
    }

    [Fact]
    public void Calculate_WhenSecondVectorIsZero_ThrowsArgumentException()
    {
        // Arrange
        float[] vectorA = [1, 2];
        float[] vectorB = [0, 0];

        // Act & Assert
        Assert.Throws<ArgumentException>(
            () => _service.Calculate(vectorA, vectorB));
    }

    [Fact]
    public void Calculate_WhenVectorsHaveSameDirectionButDifferentMagnitudes_ReturnsOne()
    {
        // Arrange
        float[] vectorA = [1, 2, 3];
        float[] vectorB = [2, 4, 6];

        // Act
        double result = _service.Calculate(vectorA, vectorB);

        // Assert
        Assert.Equal(1.0, result, 10);
    }
}
