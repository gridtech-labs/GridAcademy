using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace GridAcademy.Services.ExamContent;

public partial class ContentProcessingService : IContentProcessingService
{
    public string CleanHtml(string htmlContent)
    {
        if (string.IsNullOrWhiteSpace(htmlContent)) return string.Empty;
        var withoutScripts = ScriptsRegex().Replace(htmlContent, string.Empty);
        var withoutStyle = StyleRegex().Replace(withoutScripts, string.Empty);
        return withoutStyle.Trim();
    }

    public string ExtractSummary(string htmlContent, int wordLimit = 150)
    {
        var normalized = NormalizeForHash(htmlContent);
        if (string.IsNullOrWhiteSpace(normalized)) return string.Empty;
        var words = normalized.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (words.Length <= wordLimit) return normalized;
        return string.Join(' ', words.Take(wordLimit)) + "...";
    }

    public string GenerateSlug(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return string.Empty;
        var value = WebUtility.HtmlDecode(input).ToLowerInvariant();
        value = NonAlphaNumericRegex().Replace(value, "-");
        value = MultiDashRegex().Replace(value, "-").Trim('-');
        return value;
    }

    public string GenerateContentHash(string htmlContent)
    {
        var normalized = NormalizeForHash(htmlContent);
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(normalized));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    public string NormalizeForHash(string htmlContent)
    {
        if (string.IsNullOrWhiteSpace(htmlContent)) return string.Empty;
        var noHtml = HtmlTagRegex().Replace(htmlContent, " ");
        var decoded = WebUtility.HtmlDecode(noHtml);
        var normalizedWhitespace = MultiWhitespaceRegex().Replace(decoded, " ").Trim();
        return normalizedWhitespace.ToLowerInvariant();
    }

    [GeneratedRegex("<script[\\s\\S]*?</script>", RegexOptions.IgnoreCase)]
    private static partial Regex ScriptsRegex();

    [GeneratedRegex("<style[\\s\\S]*?</style>", RegexOptions.IgnoreCase)]
    private static partial Regex StyleRegex();

    [GeneratedRegex("<[^>]+>", RegexOptions.IgnoreCase)]
    private static partial Regex HtmlTagRegex();

    [GeneratedRegex("[^a-z0-9]+", RegexOptions.IgnoreCase)]
    private static partial Regex NonAlphaNumericRegex();

    [GeneratedRegex("-+")]
    private static partial Regex MultiDashRegex();

    [GeneratedRegex("\\s+")]
    private static partial Regex MultiWhitespaceRegex();
}
