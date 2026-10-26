namespace AskTheDocs.Exceptions;

public class OllamaUnavailableException : Exception
{
    public OllamaUnavailableException(
        string message,
        Exception innerException)
        : base(message, innerException)
    {
    }
}