
using System.Text.RegularExpressions;

namespace AskTheDocs.Services;

public class AddressExtractor
{
    private static readonly Regex regexUkPostcode = new(
        @"\b[A-Z]{1,2}\d[A-Z\d]?\s*\d[A-Z]{2}\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public List<string> ExtractAddresses(string text)
    {
        var strAddresses = new List<string>();

        if (string.IsNullOrWhiteSpace(text))
            return strAddresses;

        string[] arrLines = text
            .Replace("\r\n", "\n")
            .Replace('\r', '\n')
            .Split('\n');

        for (int intIndex = 0; intIndex < arrLines.Length; intIndex++)
        {
            string strLine = arrLines[intIndex].Trim();

            if (!regexUkPostcode.IsMatch(strLine))
                continue;

            var arrAddressLines = new List<string>();

            // Work backwards from the postcode to collect the address.
            for (int intPrevious = intIndex; intPrevious >= 0 &&
                 arrAddressLines.Count < 6; intPrevious--)
            {
                string strPreviousLine = arrLines[intPrevious].Trim();

                if (strPreviousLine.Length == 0 ||
                    Regex.IsMatch(strPreviousLine, @"^Page\s+\d+\s+of\s+\d+",
                        RegexOptions.IgnoreCase))
                {
                    break;
                }

                // A sentence is more likely to be surrounding document text.
                if (strPreviousLine.Contains('.') &&
                    !regexUkPostcode.IsMatch(strPreviousLine))
                {
                    break;
                }

                arrAddressLines.Insert(0, strPreviousLine);
            }

            string strAddress = string.Join(
                Environment.NewLine, arrAddressLines);

            if (strAddress.Length > 0 &&
                !strAddresses.Contains(strAddress))
            {
                strAddresses.Add(strAddress);
            }
        }

        return strAddresses;
    }
}
