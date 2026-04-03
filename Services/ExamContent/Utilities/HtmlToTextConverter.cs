using System.Net;
using HtmlAgilityPack;

namespace GridAcademy.Services.ExamContent.Utilities;

public static class HtmlToTextConverter
{
    public static string Convert(string html)
    {
        if (string.IsNullOrWhiteSpace(html))
            return string.Empty;

        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        var text = WebUtility.HtmlDecode(doc.DocumentNode.InnerText);
        var normalized = string.Join(' ', text
            .Split(['\r', '\n', '\t', ' '], StringSplitOptions.RemoveEmptyEntries));

        return normalized.Trim();
    }
}
