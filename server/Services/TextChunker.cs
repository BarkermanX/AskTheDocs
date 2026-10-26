
using System.Text;
using System.Text.RegularExpressions;

using AskTheDocs.Api.Models;

namespace AskTheDocs.Services;

public class TextChunker
{
    public List<TextChunk> ChunkPages(
        List<ExtractedPage> lstPages,
        int intChunkSize = 1000)
    {
        if (intChunkSize < 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(intChunkSize),
                "Chunk size must be positive.");
        }

        var lstChunks = new List<TextChunk>();

        foreach (var objPage in lstPages)
        {
            var arrParagraphs = Regex.Split(
                objPage.Text.Trim(),
                @"(?:\r?\n\s*){2,}");

            var strBuilderCurrentChunk = new StringBuilder();

            foreach (var strParagraph in arrParagraphs)
            {
                // Join lines that were wrapped by PDF extraction.
                string srtCleanedParagraph = strParagraph.Trim();


                if (srtCleanedParagraph.Length == 0)
                    continue;

                string[] arrSentences = Regex.Split(
                    srtCleanedParagraph,
                    @"(?<=[.!?])\s+");

                foreach (string strSentence in arrSentences)
                {
                    string strText = strSentence.Trim();

                    if (strText.Length == 0)
                        continue;

                    // A single sentence is too long for one chunk expand chunk size.  Do not split mid sentence.

                    string strSeparator = strBuilderCurrentChunk.Length == 0
                        ? ""
                        : arrSentences[0] == strSentence
                            ? Environment.NewLine + Environment.NewLine
                            : " ";

                    // Start a new chunk if adding this complete sentence exceeds the target size.
                    if (strBuilderCurrentChunk.Length > 0 &&
                        strBuilderCurrentChunk.Length + strSeparator.Length +
                        strText.Length > intChunkSize)
                    {
                        FlushChunk(
                            lstChunks,
                            strBuilderCurrentChunk,
                            objPage.PageNumber);

                        strSeparator = "";
                    }

                    if (strBuilderCurrentChunk.Length > 0)
                        strBuilderCurrentChunk.Append(strSeparator);

                    strBuilderCurrentChunk.Append(strText);
                }
            }

            FlushChunk(lstChunks, strBuilderCurrentChunk, objPage.PageNumber);
        }

        return lstChunks;
    }

    private static void FlushChunk(
        List<TextChunk> lstChunks,
        StringBuilder strBuilderCurrentChunk,
        int intPageNumber)
    {
        if (strBuilderCurrentChunk.Length == 0)
            return;

        lstChunks.Add(new TextChunk(
            intPageNumber,
            strBuilderCurrentChunk.ToString().Trim()));

        strBuilderCurrentChunk.Clear();
    }
}

