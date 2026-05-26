namespace GridAcademy.Modules.AiGeneration.Infrastructure.Llm;

// ── Request / Response DTOs shared between LLM providers ──────────────────────

/// <summary>Fully-parsed AI generation result for a single question.</summary>
public sealed record GeneratedQuestion(
    string  QuestionText,
    List<GeneratedOption> Options,
    int     CorrectIndex,
    string? Explanation,
    List<CalculationStep>? CalculationSteps,
    string? DifficultyEstimate,
    int?    EstimatedSeconds
);

public sealed record GeneratedOption(string Label, string Text, bool IsCorrect);

public sealed record CalculationStep(string Op, List<decimal> Operands, decimal Result);

/// <summary>Output of a self-verification call.</summary>
public sealed record VerificationResult(
    bool   Matches,           // does the verifier agree with CorrectIndex?
    int?   VerifierAnswer,    // the index the verifier chose
    string? Reasoning
);

/// <summary>Raw completion from the LLM — provider-agnostic.</summary>
public sealed record LlmCompletion(
    string Text,
    int    PromptTokens,
    int    CompletionTokens,
    string Model
);
