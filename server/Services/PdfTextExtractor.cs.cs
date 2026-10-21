
using System.Text;
using UglyToad.PdfPig;
using AskTheDocs.Api.Models;

namespace AskTheDocs.Services;

public class PdfTextExtractor
{
    public string ExtractText(string strFilePath)
    {
        using var objDocument = PdfDocument.Open(strFilePath);
        var strBuilderText = new StringBuilder();

        foreach (var objPage in objDocument.GetPages())
        {
            strBuilderText.AppendLine(objPage.Text);
        }

        return strBuilderText.ToString();
    }

    public List<ExtractedPage> ExtractPages(string filePath)
    {
        using var document = PdfDocument.Open(filePath);

        var pages = new List<ExtractedPage>();

        foreach (var page in document.GetPages())
        {
            pages.Add(new ExtractedPage(
                page.Number,
                page.Text));
        }

        return pages;
    }

}
