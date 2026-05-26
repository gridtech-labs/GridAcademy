using GridAcademy.Modules.AiGeneration.Infrastructure.Llm;
using Microsoft.Extensions.Logging;

namespace GridAcademy.Modules.AiGeneration.Services;

/// <summary>
/// Validates the calculation_steps JSON produced by the LLM.
/// Replays each step in C# and checks that the LLM's stated result is correct.
/// Flags the question if any step is numerically wrong.
/// </summary>
public sealed class MathRecomputer
{
    private readonly ILogger<MathRecomputer> _log;

    public MathRecomputer(ILogger<MathRecomputer> log) => _log = log;

    /// <summary>
    /// Returns true when all steps check out (or when there are no steps).
    /// Returns false when a step's stated result deviates from C#-computed result.
    /// </summary>
    public bool Validate(GeneratedQuestion q)
    {
        if (q.CalculationSteps is not { Count: > 0 })
            return true; // nothing to check

        foreach (var step in q.CalculationSteps)
        {
            decimal expected;
            try
            {
                expected = Compute(step.Op, step.Operands);
            }
            catch (Exception ex)
            {
                _log.LogDebug(ex, "MathRecomputer: unsupported op '{Op}' — skipping step.", step.Op);
                continue; // unknown op → skip but don't flag
            }

            if (Math.Abs(expected - step.Result) > 0.0001m)
            {
                _log.LogDebug(
                    "MathRecomputer: step {Op}({Operands}) → expected {Expected}, got {Stated}",
                    step.Op,
                    string.Join(", ", step.Operands),
                    expected,
                    step.Result);
                return false;
            }
        }

        return true;
    }

    // ── Arithmetic engine ─────────────────────────────────────────────────────

    private static decimal Compute(string op, List<decimal> operands)
    {
        if (operands.Count == 0)
            throw new ArgumentException("No operands");

        return op.ToLowerInvariant() switch
        {
            "add"      or "+"  => operands.Aggregate((a, b) => a + b),
            "subtract" or "-"  => operands.Aggregate((a, b) => a - b),
            "multiply" or "*"  or "×" => operands.Aggregate((a, b) => a * b),
            "divide"   or "/"  or "÷" => operands.Aggregate((a, b) => b == 0
                                                ? throw new DivideByZeroException()
                                                : a / b),
            "power"    or "^"  or "pow" => operands.Count == 2
                                ? (decimal)Math.Pow((double)operands[0], (double)operands[1])
                                : throw new ArgumentException("power requires exactly 2 operands"),
            "sqrt"     => operands.Count == 1
                                ? (decimal)Math.Sqrt((double)operands[0])
                                : throw new ArgumentException("sqrt requires exactly 1 operand"),
            "mod"      or "%"  => operands.Count == 2
                                ? operands[0] % operands[1]
                                : throw new ArgumentException("mod requires exactly 2 operands"),
            _ => throw new NotSupportedException($"Unsupported op: {op}")
        };
    }
}
