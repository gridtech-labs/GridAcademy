using HtmlAgilityPack;
using GridAcademy.Services.ExamContent.Utilities;

namespace GridAcademy.Services.ExamContent.AI;

public class AIRewriteService(
    IAiApiClient aiApiClient,
    ILogger<AIRewriteService> logger) : IAIRewriteService
{
    private const string PromptTemplate =
        """
        You are an expert educational content writer.

        Rewrite the given exam notification into:

        * 100% unique content
        * SEO optimized
        * Easy for Indian students

        Rules:

        * Do NOT copy sentences
        * Add structured sections
        * Use simple language
        * Minimum 800 words
        * Add FAQs

        Output ONLY clean HTML.
        """;

    public async Task<AIRewriteResult> RewriteAsync(string rawHtml, string title, CancellationToken ct = default)
    {
        var plainText = HtmlToTextConverter.Convert(rawHtml);
        if (string.IsNullOrWhiteSpace(plainText))
            throw new InvalidOperationException($"Cannot rewrite empty content for '{title}'.");

        var fullPrompt =
            $"""
            {PromptTemplate}

            Title: {title}

            Source Text:
            {plainText}

            Required section order in HTML:
            1) Overview
            2) Important Dates
            3) Eligibility
            4) Application Fee
            5) Selection Process
            6) How to Apply
            7) FAQs (minimum 5 FAQs)

            Also include these tags in the output HTML:
            <meta-title>...</meta-title>
            <meta-description>...</meta-description>

            Constraints:
            - Meta title max 60 chars
            - Meta description max 160 chars
            - Content must be valid semantic HTML with headings and lists where relevant
            """;

        var (html, usage) = await aiApiClient.GenerateHtmlAsync(fullPrompt, ct);

        if (usage is not null)
        {
            logger.LogInformation(
                "AI token usage for {Title}: prompt={PromptTokens}, completion={CompletionTokens}, total={TotalTokens}",
                title,
                usage.PromptTokens,
                usage.CompletionTokens,
                usage.TotalTokens);
        }

        var result = ParseResult(html);
        logger.LogInformation("AI rewrite successful for {Title}", title);
        return result;
    }

    private static AIRewriteResult ParseResult(string html)
    {
        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        var metaTitleNode = doc.DocumentNode.SelectSingleNode("//meta-title");
        var metaDescriptionNode = doc.DocumentNode.SelectSingleNode("//meta-description");

        var metaTitle = metaTitleNode?.InnerText?.Trim() ?? string.Empty;
        var metaDescription = metaDescriptionNode?.InnerText?.Trim() ?? string.Empty;

        metaTitleNode?.ParentNode.RemoveChild(metaTitleNode);
        metaDescriptionNode?.ParentNode.RemoveChild(metaDescriptionNode);

        if (string.IsNullOrWhiteSpace(metaTitle) || string.IsNullOrWhiteSpace(metaDescription))
            throw new InvalidOperationException("AI output missing <meta-title> or <meta-description>.");

        metaTitle = metaTitle.Length > 60 ? metaTitle[..60].TrimEnd() : metaTitle;
        metaDescription = metaDescription.Length > 160 ? metaDescription[..160].TrimEnd() : metaDescription;

        var cleanedHtml = doc.DocumentNode.InnerHtml.Trim();
        if (string.IsNullOrWhiteSpace(cleanedHtml))
            throw new InvalidOperationException("AI output did not contain valid HTML content.");

        return new AIRewriteResult(cleanedHtml, metaTitle, metaDescription);
    }
}
