
using System.Text;
using UglyToad.PdfPig;

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
}
