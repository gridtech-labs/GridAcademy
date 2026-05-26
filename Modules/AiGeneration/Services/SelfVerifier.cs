using GridAcademy.Modules.AiGeneration.Infrastructure.Llm;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace GridAcademy.Modules.AiGeneration.Services;

/// <summary>
/// Sends each generated question to the LLM a second time and asks it to
/// independently pick the correct answer. Flags questions where the verifier
/// disagrees with the original correct_index.
/// </summary>
public sealed class SelfVerifier
{
    private readonly ILLMProvider _llm;
    private readonly LlmUsageTracker _usage;
    private readonly ILogger<SelfVerifier> _log;

    public SelfVerifier(ILLMProvider llm, LlmUsageTracker usage, ILogger<SelfVerifier> log)
    {
        _llm   = llm;
        _usage = usage;
        _log   = log;
    }

    /// <summary>
    /// Returns a VerificationResult. On any LLM/parse error returns a non-matching
    /// result so the draft gets flagged rather than silently passing.
    /// </summary>
    public async Task<VerificationResult> VerifyAsync(
        GeneratedQuestion q,
        CancellationToken ct = default)
    {
        var optionLines = string.Join("\n",
            q.Options.Select((o, i) => $"{o.Label}) {o.Text}"));

        var prompt =
            $$"""
            You are an expert verifier. Read the question and choose the SINGLE correct answer.

            QUESTION:
            {{q.QuestionText}}

            OPTIONS:
            {{optionLines}}

            Respond ONLY with JSON: {"correct_index": <0-based int>, "reasoning": "..."}
            """;

        LlmCompletion completion;
        try
        {
            completion = await _llm.CompleteAsync(prompt, ct);
            await _usage.RecordAsync(completion, ct);
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "SelfVerifier LLM call failed — flagging question.");
            return new VerificationResult(false, null, "LLM call failed");
        }

        try
        {
            // Strip potential markdown fences
            var raw  = completion.Text.Trim().TrimStart('`');
            if (raw.StartsWith("json", StringComparison.OrdinalIgnoreCase))
                raw = raw[4..].TrimStart();
            raw = raw.TrimEnd('`');

            var node = JsonNode.Parse(raw);
            var idx  = node?["correct_index"]?.GetValue<int>();
            var reason = node?["reasoning"]?.GetValue<string>();

            bool matches = idx.HasValue && idx.Value == q.CorrectIndex;
            return new VerificationResult(matches, idx, reason);
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "SelfVerifier JSON parse failed: {Raw}", completion.Text[..Math.Min(200, completion.Text.Length)]);
            return new VerificationResult(false, null, "Parse error");
        }
    }
}
