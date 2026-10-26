namespace AskTheDocs.Api.Models
{
    public record ChatRequest(
        string Model,
        ChatMessage[] Messages,
        bool Stream,
        bool Think,
        ChatOptions Options);
}

    
