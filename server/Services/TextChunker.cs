
using AskTheDocs.Api.Models;

namespace AskTheDocs.Services;

public class TextChunker
{
    public List<TextChunk> ChunkPages(
        List<ExtractedPage> lstPages,
        int intChunkSize = 1000)
    {
        var lstChunks = new List<TextChunk>();

        foreach (var objPage in lstPages)
        {
            for (int intStart = 0; intStart < objPage.Text.Length; intStart += intChunkSize)
            {
                var intLength = Math.Min(
                    intChunkSize,
                    objPage.Text.Length - intStart);

                var strText = objPage.Text.Substring(intStart, intLength);

                lstChunks.Add(new TextChunk(
                    objPage.PageNumber,
                    strText));
            }
        }

        return lstChunks;
    }
}

