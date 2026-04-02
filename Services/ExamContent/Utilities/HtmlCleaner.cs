using System.Net;
using System.Text.RegularExpressions;

namespace GridAcademy.Services.ExamContent.Utilities;

public static partial class HtmlCleaner
{
    public static string Clean(string htmlContent)
    {
        if (string.IsNullOrWhiteSpace(htmlContent)) return string.Empty;

        var withoutScripts = ScriptsRegex().Replace(htmlContent, " ");
        var withoutStyles = StylesRegex().Replace(withoutScripts, " ");
        var noTags = HtmlTagsRegex().Replace(withoutStyles, " ");
        var decoded = WebUtility.HtmlDecode(noTags);
        return MultiWhitespaceRegex().Replace(decoded, " ").Trim();
    }

    [GeneratedRegex("<script[\\s\\S]*?</script>", RegexOptions.IgnoreCase)]
    private static partial Regex ScriptsRegex();

    [GeneratedRegex("<style[\\s\\S]*?</style>", RegexOptions.IgnoreCase)]
    private static partial Regex StylesRegex();

    [GeneratedRegex("<[^>]+>", RegexOptions.IgnoreCase)]
    private static partial Regex HtmlTagsRegex();

    [GeneratedRegex("\\s+")]
    private static partial Regex MultiWhitespaceRegex();
}
