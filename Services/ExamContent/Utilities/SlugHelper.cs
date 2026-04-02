using System.Net;
using System.Text.RegularExpressions;

namespace GridAcademy.Services.ExamContent.Utilities;

public static partial class SlugHelper
{
    public static string Generate(string title)
    {
        if (string.IsNullOrWhiteSpace(title)) return "untitled";

        var decoded = WebUtility.HtmlDecode(title).ToLowerInvariant();
        var slug = NonAlphaNumericRegex().Replace(decoded, "-");
        slug = MultiDashRegex().Replace(slug, "-").Trim('-');

        return string.IsNullOrWhiteSpace(slug) ? "untitled" : slug;
    }

    [GeneratedRegex("[^a-z0-9]+", RegexOptions.IgnoreCase)]
    private static partial Regex NonAlphaNumericRegex();

    [GeneratedRegex("-+")]
    private static partial Regex MultiDashRegex();
}
